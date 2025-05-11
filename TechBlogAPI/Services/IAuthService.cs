using TechBlogAPI.Dtos;

namespace TechBlogAPI.Services;

public interface IAuthService
{
    Task<(bool Succeeded, string Message)> RegisterAsync(RegisterDto model, string role, string? firstName = null,
        string? lastName = null);

    Task<(bool Succeeded, AuthResponseDto? Response, string Message)> LoginAsync(LoginDto model, string? role = null);
    Task<(bool Succeeded, AuthResponseDto? Response, string Message)> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(string username);
}