namespace TechBlogAPI.Dtos;

public class AuthResponseDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Username { get; set; }
    public string Role { get; set; }
}