using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TechBlogAPI.Data;
using TechBlogAPI.Dtos;
using TechBlogAPI.Entity;
using TechBlogAPI.Settings;

namespace TechBlogAPI.Services;

public class AuthService(DatabaseContext context, IOptions<JwtSettings> jwtSettings) : IAuthService
{
    
    public async Task<(bool Succeeded, string Message)> RegisterAsync(RegisterDto model)
    {
        if (await context.Users.AnyAsync(u => u.Username == model.Username))
        {
            return (false, "Username already exists.");
        }
        if (await context.Users.AnyAsync(u => u.Email == model.Email))
        {
            return (false, "User with this email already exist.");
        }
        
        CreatePasswordHash(model.Password, out byte[] passwordHash, out byte[] passwordSalt);

        // Create user entity
        var user = new User
        {
            Username = model.Username,
            Email = model.Email,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            Role = "User" // Default role
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        return (true, "User created successfully.");
    }

    public async Task<(bool Succeeded, AuthResponseDto Response, string Message)> LoginAsync(LoginDto model)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
        if (user == null || !VerifyPasswordHash(model.Password, user.PasswordHash, user.PasswordSalt))
        {
            return (false, null, "Invalid username or password");
        }
        
        // Tokens
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpiryInDays);
        await context.SaveChangesAsync();
        
        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.Value.AccessTokenExpiryInMinutes),
            Username = user.Username,
            Role = user.Role
        };

        return (true, response, "Login successful.");
    }

    public async Task<(bool Succeeded, AuthResponseDto Response, string Message)> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return (false, null, "Invalid refresh token.");
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        if (user == null)
        {
            return (false, null, "Invalid refresh token.");
        }

        if (user.RefreshTokenExpiryDate <= DateTime.UtcNow)
        {
            return (false, null, "Refresh token expired.");
        }

        var accessToken = GenerateAccessToken(user);
        var newRefreshToken = GenerateRefreshToken();
        
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpiryInDays);
        await context.SaveChangesAsync();
        
        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.Value.AccessTokenExpiryInMinutes),
            Username = user.Username,
            Role = user.Role
        };

        return (true, response, "Token refresh successful");
    }

    public async Task<bool> RevokeTokenAsync(string username)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return false;
        }

        user.RefreshToken = null;
        await context.SaveChangesAsync();
        return true;
    }
    
    // ---------------------------- Password ----------------------------
    private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using var hmac = new HMACSHA512(); // When creating HMACSHA512, a random key is automatically generated
        passwordSalt = hmac.Key; // The generated key, we assign to salt
        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password)); // Hash the password
    }

    private bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
    {
        using var hmac = new HMACSHA512(storedSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        
        for (int i = 0; i < computedHash.Length; i++)
        {
            if (computedHash[i] != storedHash[i])
                return false;
        }
            
        return true;
    }
    
    // ---------------------------- Tokens ----------------------------

    private string GenerateAccessToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Value.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Value.Issuer,
            audience: jwtSettings.Value.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtSettings.Value.AccessTokenExpiryInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}