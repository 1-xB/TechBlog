using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TechBlogAPI.Dtos;

public class EditPostDto
{
    [Required] [StringLength(100)] public string Title { get; set; }
    [Required] public string Content { get; set; }
    [Required] public List<int> CategoryIds { get; set; }
}