using System.Text.Json.Serialization;

namespace TechBlogAPI.Entity;

public class Post
{
    public int PostId { get; set; }
    public string Title { get; set; }
    public string PostImage { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int AuthorId { get; set; }
    [JsonIgnore] public Author Author { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}