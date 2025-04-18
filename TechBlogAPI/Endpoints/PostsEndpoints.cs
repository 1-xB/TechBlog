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
        
        // TODO: [Authorize(Roles = "Author, Admin")]
        group.MapPost("/",  async (DatabaseContext dbContext, AddPostDto newPost) =>
        {
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
        });

        return group;
    }
}