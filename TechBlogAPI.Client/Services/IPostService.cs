using TechBlogAPI.Client.Models;

namespace TechBlogAPI.Client.Services;

public interface IPostService
{
    public Task<List<PostModel>?> GetAllPostsAsync();
    public Task<List<PostModel>?> GetAuthorPostsAsync(int authorId);
    public Task<(bool Success, string? ErrorMessage)> DeletePostAsync(int id);
}