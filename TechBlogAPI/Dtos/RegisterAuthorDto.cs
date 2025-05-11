using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace TechBlogAPI.Dtos;

public class RegisterAuthorDto
{
    [Required] [StringLength(50)] public string FirstName { get; set; }
    [Required] [StringLength(50)] public string LastName { get; set; }
    [Required] [StringLength(50)] public string Username { get; set; }
    [Required] [EmailAddress] public string Email { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 0)]
    public string Password { get; set; }
}