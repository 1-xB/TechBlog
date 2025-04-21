using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TechBlogAPI.Data;
using TechBlogAPI.Dtos;
using TechBlogAPI.Entity;

namespace TechBlogAPI.Endpoints;

public static class PostsEndpoints
{
    public static RouteGroupBuilder MapPostRoutes(this WebApplication app)
    {
        var group = app.MapGroup("api/posts");

        group.MapGet("/", async (DatabaseContext dbContext) =>
        {
            try {
                var posts = await dbContext.Posts
                .Select(post => new 
                {
                    post.PostId,
                    post.Title,
                    post.Content,
                    post.CreatedAt,
                    post.UpdatedAt,
                    Author = new 
                    {
                        post.Author.AuthorId,
                        post.Author.FirstName,
                        post.Author.LastName
                    }
                })
                .ToListAsync();

                return Results.Ok(posts);
            }
            catch (Exception ex) {
                return Results.Problem($"An error occurred: {ex.Message}");
            }
            
        });

        group.MapGet("/{id:int}", async (int id, DatabaseContext dbContext) =>
        {
            try {
                var post = await dbContext.Posts.Where(p => p.PostId == id).Select(post => new
                {
                    post.PostId,
                    post.Title,
                    post.Content,
                    post.CreatedAt,
                    post.UpdatedAt,
                    Author = new
                    {
                        post.Author.AuthorId,
                        post.Author.FirstName,
                        post.Author.LastName
                    }
                }).FirstOrDefaultAsync();
                return post is null ? Results.NotFound() : Results.Ok(post);
            }
            catch (Exception ex) {
                return Results.Problem($"An error occurred: {ex.Message}");
            }
            
        });
        
        group.MapPost("/", [Authorize(Policy = "AuthorOnly")] async (DatabaseContext dbContext, AddPostDto newPost) =>
        {
            try {
                var author = await dbContext.Authors.FindAsync(newPost.AuthorId);
                if (author is null)
                {
                    return Results.NotFound($"Author id : {newPost.AuthorId} is not exist!");
                }
                var post = new Post
                {
                    Content = newPost.Content,
                    Title = newPost.Title,
                    AuthorId = newPost.AuthorId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                await dbContext.Posts.AddAsync(post);
                await dbContext.SaveChangesAsync();
                return Results.Ok();
            }
            catch (Exception ex) {
                return Results.Problem($"An error occurred: {ex.Message}");
            }
        });
        
        group.MapDelete("/{id:int}", [Authorize(Policy = "AuthorOnly")] async (int id, DatabaseContext dbContext, ClaimsPrincipal user) => {
            
            try {

            }
            catch (Exception ex) {
                return Results.Problem($"An error occurred: {ex.Message}");
            }
            var post = await dbContext.Posts.FirstOrDefaultAsync(p => p.PostId == id);
            if (post is null) {
                return Results.NotFound($"Post with id {id} does not exist.");
            }

            if (!CanEdit(user, post)) {
                return Results.Forbid();
            }

            dbContext.Posts.Remove(post);
            await dbContext.SaveChangesAsync();
            return Results.Ok(new { message = $"Post with id {id} was successfully deleted" });
        });

        group.MapPut("/{id:int}", [Authorize(Policy = "AuthorOnly")] async (int id, DatabaseContext dbContext,EditPostDto newPost, ClaimsPrincipal user) => {
            try {
                var post = await dbContext.Posts.FirstOrDefaultAsync(p => p.PostId == id);
                if (post is null) {
                    return Results.NotFound($"Post with id {id} does not exist.");
                }

                if (!CanEdit(user, post)) {
                    return Results.Forbid();
                }

                if (string.IsNullOrWhiteSpace(newPost.Title) || string.IsNullOrWhiteSpace(newPost.Content)) {
                    return Results.BadRequest("Title and content cannot be empty");
                }

                post.Title = newPost.Title;
                post.Content = newPost.Content;
                post.UpdatedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();
                return Results.Ok(new { message = $"Post with id {id} was successfully updated" });
            }
            catch (Exception ex) {
                return Results.Problem($"An error occurred: {ex.Message}");
            }
        });

        return group;
    }

    private static bool CanEdit(ClaimsPrincipal user, Post post) {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isAdmin = user.IsInRole("Admin");
        bool isAuthor = user.IsInRole("Author");

        if (!(isAdmin || (isAuthor && userId == post.AuthorId.ToString()))) {
            return false;
        }
        return true;
    }
}