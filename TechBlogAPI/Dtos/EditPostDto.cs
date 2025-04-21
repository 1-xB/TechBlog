using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TechBlogAPI.Dtos
{
    public class EditPostDto
    {
        [Required] public int PostId { get; set; }
        [Required, StringLength(100)] public string Title { get; set; }
        [Required] public string Content { get; set; }
    }
}