using TechBlogAPI.Client.Models;

namespace TechBlogAPI.Client.Services;

public interface IPostService
{
    public Task<List<PostModel>?> GetAllPostsAsync();
    public Task<(bool Success, string? ErrorMessage)> DeletePostAsync(int id);
}