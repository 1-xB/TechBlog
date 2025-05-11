using System.ComponentModel.DataAnnotations;

namespace TechBlogAPI.Dtos;

public class RegisterDto
{
    [Required] [StringLength(50)] public string Username { get; set; }
    [Required] [EmailAddress] public string Email { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 0)]
    public string Password { get; set; }
}