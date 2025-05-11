using System.ComponentModel.DataAnnotations;

namespace TechBlogAPI.Dtos;

public class AddPostDto
{
    [Required] [StringLength(100)] public string Title { get; set; }
    [Required] public string Content { get; set; }
    [Required] public List<int> CategoryIds { get; set; }
}