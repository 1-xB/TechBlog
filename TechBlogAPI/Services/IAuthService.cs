using TechBlogAPI.Dtos;

namespace TechBlogAPI.Services;

public interface IAuthService
{
    Task<(bool Succeeded, string Message)> RegisterAsync(RegisterDto model);
    Task<(bool Succeeded, AuthResponseDto Response, string Message)> LoginAsync(LoginDto model);
    Task<(bool Succeeded, AuthResponseDto Response, string Message)> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(string username);
}