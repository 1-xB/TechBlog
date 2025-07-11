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

public class AuthService(DatabaseContext context, IOptions<JwtSettings> jwtSettings, BruteForceDefenseService bruteForceDefenseService) : IAuthService
{

    private readonly BruteForceDefenseService _bruteForceDefenseService = bruteForceDefenseService;
    
    public async Task<(bool Succeeded, string Message)> RegisterAsync(RegisterDto model, string role,
        string? firstName = null, string? lastName = null)
    {
        if (role == "Author" && firstName is null && lastName is null)
            return (false, "Author's first name and last name is null");
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // TODO : ADD EMAIL VALIDATION
            if (await context.Users.AnyAsync(u => u.Username == model.Username))
                return (false, "Username already exists.");
            if (await context.Users.AnyAsync(u => u.Email == model.Email))
                return (false, "User with this email already exist.");

            CreatePasswordHash(model.Password, out var passwordHash, out var passwordSalt);
            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Role = role
            };

            context.Users.Add(user);

            if (role == "Author")
                user.Author = new Author
                {
                    FirstName = firstName,
                    LastName = lastName
                };

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, $"{role} created successfully.");
        }

        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, ex.Message);
        }
    }


    public async Task<(bool Succeeded, AuthResponseDto? Response, string Message)> LoginAsync(LoginDto model,
        string? role = null)
    {
        await Task.Delay(await _bruteForceDefenseService.GetDelayForUserAsync(model.Username));
        if (await _bruteForceDefenseService.IsLockedOutAsync(model.Username))
        {
            return (false, null, "The ability to log in to this account has been temporarily blocked.");
        }
        
        var user = await context.Users.Include(u => u.Author).FirstOrDefaultAsync(u => u.Username == model.Username);
        if (user == null || !VerifyPasswordHash(model.Password, user.PasswordHash, user.PasswordSalt))
        {
            await this._bruteForceDefenseService.RecordAttemptAsync(model.Username, false);
            return (false, null, "Invalid username or password");
        }


        if (user is not null && role is not null && user.Role != role)
        {
            await this._bruteForceDefenseService.RecordAttemptAsync(model.Username, false);
            return (false, null, "Cannot log in : No permissions.");
        }
            
        await this._bruteForceDefenseService.RecordAttemptAsync(model.Username, true);
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
            ExpiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.Value.AccessTokenExpiryInMinutes)
        };

        return (true, response, "Login successful.");
    }

    public async Task<(bool Succeeded, AuthResponseDto? Response, string Message)> RefreshTokenAsync(
        string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken)) return (false, null, "Invalid refresh token.");

        var user = await context.Users.Include(u => u.Author).FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        if (user == null) return (false, null, "Invalid refresh token.");

        if (user.Role == "Author" && user.Author == null)
        {
            // Re-load the user with explicit Author loading
            user = await context.Users
                .Include(u => u.Author)
                .FirstOrDefaultAsync(u => u.UserId == user.UserId);

            if (user?.Author == null) return (false, null, "Author data could not be loaded.");
        }

        if (user.RefreshTokenExpiryDate <= DateTime.UtcNow) return (false, null, "Refresh token expired.");

        var accessToken = GenerateAccessToken(user);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpiryInDays);
        await context.SaveChangesAsync();

        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.Value.AccessTokenExpiryInMinutes)
        };

        return (true, response, "Token refresh successful");
    }

    public async Task<bool> RevokeTokenAsync(string username)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return false;

        user.RefreshToken = null;
        await context.SaveChangesAsync();
        return true;
    }

    // ---------------------------- Password ----------------------------
    private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using var hmac = new HMACSHA512(); // When creating HMACSHA512, a random key is automatically generated
        passwordSalt = hmac.Key; // The generated key, we assign to salt
        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password)); // Hash the password
    }

    private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
    {
        using var hmac = new HMACSHA512(storedSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        
        int result = 0;
        
        for (var i = 0; i < computedHash.Length; i++) {
            // XOR operation returns 0 for identical bytes
            // Operations are performed for ALL bytes, regardless of differences
            // Timing Attacks will no longer work.
            result |= computedHash[i] ^ storedHash[i];
        }

        return result == 0;
    }

    // ---------------------------- Tokens ----------------------------

    private string GenerateAccessToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Value.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(ClaimTypes.GivenName, user.Author?.FirstName ?? string.Empty),
            new(ClaimTypes.Surname, user.Author?.LastName ?? string.Empty),
            new("AuthorId", user.Author?.AuthorId.ToString() ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            jwtSettings.Value.Issuer,
            jwtSettings.Value.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(jwtSettings.Value.AccessTokenExpiryInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}