using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechBlogAPI.Client.Auth;
using TechBlogAPI.Client.Models;

namespace TechBlogAPI.Client.Services;

public class PostService(HttpClient http, AuthService authService) : IPostService
{
    public async Task<(bool Success, string? Message)> PublishPostAsync(PublishPostRequest newPost)
    {
        var accessToken = await authService.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            await authService.LogoutAsync();
            return (false, "Access token is null");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/posts/");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(newPost);

            var response = await http.SendAsync(request);
            switch (response.StatusCode)
            {
                case HttpStatusCode.Created:
                    return (true, null);
                case HttpStatusCode.Forbidden:
                    await authService.LogoutAsync();
                    return (false, "You have no permissions to create posts.");
                case HttpStatusCode.NotFound:
                    await authService.LogoutAsync();
                    return (false, "Something went wrong with your account. Try log in again.");
                case HttpStatusCode.BadRequest:
                    return (false, $"Error : {response.Content}");
            }

            return (false, "Something went wrong. Try again later");
        }
        catch (Exception ex)
        {
            return (false, $"Exception : {ex.Message}");
        }
    }

    public async Task<List<PostModel>?> GetAllPostsAsync()
    {
        var response = await http.GetAsync("api/posts");
        if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<List<PostModel>>();
        return null;
    }

    public async Task<List<PostModel>?> GetAuthorPostsAsync(int authorId)
    {
        var response = await http.GetAsync($"api/posts/author-posts/{authorId}");
        if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<List<PostModel>>();
        return null;
    }

    public async Task<(bool Success, string? ErrorMessage)> DeletePostAsync(int id)
    {
        var accessToken = await authService.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            await authService.LogoutAsync();
            return (false, "Access token is null");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/posts/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await http.SendAsync(request);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return (true, null);
                case HttpStatusCode.NotFound:
                    return (false, $"Post with ID {id} does not exist.");
                case HttpStatusCode.Forbidden:
                    await authService.LogoutAsync();
                    return (false, "You have no permissions.");
            }

            return (false, "Something went wrong. Try again later");
        }
        catch (Exception ex)
        {
            return (false, $"Exception : {ex.Message}");
        }
    }
    
    public async Task<PostModel?> GetPostByIdAsync(int id)
    {
        var response = await http.GetAsync($"api/posts/{id}");
        if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<PostModel>();
        return null;
    }
}