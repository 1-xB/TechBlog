using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TechBlogAPI.Client.Models
{
    public class LoginModel
    {
        [Required, StringLength(50)] public string Username { get; set; }
        [Required, StringLength(255), DataType(DataType.Password), MinLength(1)]public string Password { get; set; }
    }
}