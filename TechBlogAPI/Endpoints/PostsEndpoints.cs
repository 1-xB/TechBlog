using Microsoft.EntityFrameworkCore;
using TechBlogAPI.Data;

namespace TechBlogAPI.Endpoints;

public static class PostsEndpoints
{
    public static RouteGroupBuilder MapPostRoutes(this WebApplication app)
    {
        var group = app.MapGroup("api/posts");

        group.MapGet("/", async (DatabaseContext dbContext) =>
        {
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
        });

        group.MapGet("/{id:int}", async (int id, DatabaseContext dbContext) =>
        {
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
        });

        return group;
    }
}