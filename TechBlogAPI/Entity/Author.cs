namespace TechBlogAPI.Entity;

public class Author
{
    public int AuthorId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public ICollection<Post> Posts { get; set; }
}