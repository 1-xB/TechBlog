using TechBlogAPI.Client.Models;

namespace TechBlogAPI.Client.Services;

public interface ICategoryService
{
    public Task<List<Category>?> GetAllCategoriesAsync();
}