using System.ComponentModel.DataAnnotations;

namespace TechBlogAPI.Client.Models;

public class PublishPostRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, ErrorMessage = "The title must not exceed 100 characters")]
    public string Title { get; set; }
    public string PostImage { get; set; }
    public string Content { get; set; }

    [Required(ErrorMessage = "Select at least one category")]
    public List<int> CategoryIds { get; set; }
}