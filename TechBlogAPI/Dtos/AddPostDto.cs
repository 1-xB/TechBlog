namespace TechBlogAPI.Dtos;

public class AddPostDto
{
    public string Title { get; set; }
    public string Content { get; set; }
    public int AuthorId { get; set; }
}