using System.Text.Json.Serialization;

namespace TechBlogAPI.Entity;

public class Author
{
    public int AuthorId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int UserId { get; set; }
    [JsonIgnore] public User User { get; set; }
    public ICollection<Post> Posts { get; set; }
}