namespace TechBlogAPI.Settings;

public class JwtSettings
{
    public string Key { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int AccessTokenExpiryInMinutes { get; set; }
    public int RefreshTokenExpiryInDays { get; set; }
}