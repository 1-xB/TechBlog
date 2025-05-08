using System.Net.Http.Json;
using TechBlogAPI.Client.Models;

namespace TechBlogAPI.Client.Services;

public class CategoryService(HttpClient httpClient) : ICategoryService
{
    public async Task<List<Category>?> GetAllCategoriesAsync()
    {
        var response = await httpClient.GetAsync("api/category");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Category>>();
        }
        return null;
    }
}