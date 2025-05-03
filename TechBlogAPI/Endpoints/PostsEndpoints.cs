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
                //Todo : Add CATEGORIES!
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
                    },
                    Categories = post.Categories.Select(c => new 
                    {
                        c.CategoryId,
                        c.Name
                    }).ToList()
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
                    },
                    Categories = post.Categories.Select(c => new 
                    {
                        c.CategoryId,
                        c.Name
                    }).ToList()
                }).FirstOrDefaultAsync();
                return post is null ? Results.NotFound() : Results.Ok(post);
            }
            catch (Exception ex) {
                return Results.Problem($"An error occurred: {ex.Message}");
            }
            
        });
        
        group.MapPost("/", [Authorize(Policy = "AuthorOnly")] async (DatabaseContext dbContext, AddPostDto newPost, ClaimsPrincipal user) =>
        {
            try {
                if (string.IsNullOrWhiteSpace(newPost.Content) || string.IsNullOrWhiteSpace(newPost.Title)) {
                    return Results.BadRequest(new {message = "Title and content can't be empty"});
                }

                if (newPost.Title.Length > 100) {
                    return Results.BadRequest(new {message = "Title cannot exceed 100 characters"});
                }

                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

                var author = await dbContext.Authors.FirstOrDefaultAsync(a => a.UserId.ToString() == userId);
                if (author is null)
                {
                    return Results.NotFound($"Author with UserId : {userId} is not exist!");
                }
                var post = new Post
                {
                    Content = newPost.Content,
                    Title = newPost.Title,
                    AuthorId = author.AuthorId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                if (newPost.CategoryIds is not null && newPost.CategoryIds.Any()) {
                    foreach (int ID in newPost.CategoryIds)
                    {
                        var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.CategoryId == ID);
                        if (category is not null) {
                            post.Categories.Add(category);
                        }
                        else {
                            return Results.BadRequest("No valid categories were found.");
                        }
                    }
                }
                else {
                    return Results.BadRequest("A post must have at least one category.");
                }
                await dbContext.Posts.AddAsync(post);
                await dbContext.SaveChangesAsync();
                return Results.Created($"/api/posts/{post.PostId}", new {
                    post.PostId,
                    post.Title,
                    post.Content,
                    post.CreatedAt,
                    post.UpdatedAt,
                    Author = new {
                        post.Author.AuthorId,
                        post.Author.FirstName,
                        post.Author.LastName
                    }
                });
            }
            catch (Exception ex) {
                return Results.Problem($"An error occurred: {ex.Message}");
            }
        });
        
        group.MapDelete("/{id:int}", [Authorize(Policy = "AuthorOnly")] async (int id, DatabaseContext dbContext, ClaimsPrincipal user) => {
            
            try {
                var post = await dbContext.Posts.FirstOrDefaultAsync(p => p.PostId == id);
                if (post is null) {
                    return Results.NotFound($"Post with id {id} does not exist.");
                }

                if (!await CheckEditPermissions(user, post, dbContext)) {
                    return Results.Forbid();
                }

                dbContext.Posts.Remove(post);
                await dbContext.SaveChangesAsync();
                return Results.Ok(new { message = $"Post with id {id} was successfully deleted" });
            }
            catch (Exception ex) {
                return Results.Problem($"An error occurred: {ex.Message}");
            }
            
        });

        group.MapPut("/{id:int}", [Authorize(Policy = "AuthorOnly")] async (int id, DatabaseContext dbContext,EditPostDto newPost, ClaimsPrincipal user) => {
            try {
                
                if (string.IsNullOrWhiteSpace(newPost.Content) || string.IsNullOrWhiteSpace(newPost.Title)) {
                    return Results.BadRequest(new {message = "Title and content can't be empty"});
                }

                if (newPost.Title.Length > 100) {
                    return Results.BadRequest(new {message = "Title cannot exceed 100 characters"});
                }

                var post = await dbContext.Posts
                    .Include(p => p.Categories)
                    .FirstOrDefaultAsync(p => p.PostId == id);
                if (post is null) {
                    return Results.NotFound($"Post with id {id} does not exist.");
                }

                if (! await CheckEditPermissions(user, post, dbContext)) {
                    return Results.Forbid();
                }

                if (string.IsNullOrWhiteSpace(newPost.Title) || string.IsNullOrWhiteSpace(newPost.Content)) {
                    return Results.BadRequest("Title and content cannot be empty");
                }

                post.Title = newPost.Title;
                post.Content = newPost.Content;
                post.UpdatedAt = DateTime.UtcNow;

                if (newPost.CategoryIds is not null && newPost.CategoryIds.Any()) {
                    post.Categories.Clear();
                    foreach (int ID in newPost.CategoryIds)
                    {
                        var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.CategoryId == ID);
                        if (category is not null) {
                            post.Categories.Add(category);
                        }
                        else {
                            return Results.BadRequest("No valid categories were found.");
                        }
                    }
                }
                else {
                    return Results.BadRequest("A post must have at least one category.");
                }

                await dbContext.SaveChangesAsync();
                return Results.Ok(new { message = $"Post with id {id} was successfully updated" });
            }
            catch (Exception ex) {
                return Results.Problem($"An error occurred: {ex.Message}");
            }
        });

        return group;
    }

    private static async Task<bool> CheckEditPermissions(ClaimsPrincipal user, Post post, DatabaseContext dbContext) {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isAdmin = user.IsInRole("Admin");
        
        // Sprawdź czy zalogowany użytkownik jest autorem tego posta
        var author = await dbContext.Authors.FirstOrDefaultAsync(a => a.UserId.ToString() == userId);
        bool isPostAuthor = author != null && post.AuthorId == author.AuthorId;
        
        return isAdmin || isPostAuthor;
    }
}