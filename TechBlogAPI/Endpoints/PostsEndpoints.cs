using System.Security.Claims;
using System.Text.RegularExpressions;
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
            try
            {
                var posts = await dbContext.Posts
                    .Select(post => new
                    {
                        post.PostId,
                        post.Title,
                        post.PostImage,
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
            catch (Exception ex)
            {
                return Results.Problem($"An error occurred: {ex.Message}");
            }
        });

        group.MapGet("/{id:int}", async (int id, DatabaseContext dbContext) =>
        {
            try
            {
                var post = await dbContext.Posts.Where(p => p.PostId == id).Select(post => new
                {
                    post.PostId,
                    post.Title,
                    post.PostImage,
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
            catch (Exception ex)
            {
                return Results.Problem($"An error occurred: {ex.Message}");
            }
        });

        group.MapGet("/author-posts/{authorId:int}", async (int authorId, DatabaseContext dbContext) =>
        {
            var author = await dbContext.Authors.Include(a => a.Posts).ThenInclude(post => post.Categories)
                .FirstOrDefaultAsync(a => a.AuthorId == authorId);
            if (author is null) return Results.NotFound($"Author with id {authorId} does not exist.");
            return Results.Ok(author.Posts.Select(post => new
            {
                post.PostId,
                post.Title,
                post.PostImage,
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
            }).ToList());
        });

        group.MapPost("/", [Authorize(Policy = "AuthorOnly")]
            async (DatabaseContext dbContext, AddPostDto newPost, ClaimsPrincipal user) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(newPost.Content) || string.IsNullOrWhiteSpace(newPost.Title))
                        return Results.BadRequest(new { message = "Title and content can't be empty" });

                    if (newPost.Title.Length > 100)
                        return Results.BadRequest(new { message = "Title cannot exceed 100 characters" });

                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

                    var author = await dbContext.Authors.FirstOrDefaultAsync(a => a.UserId.ToString() == userId);
                    if (author is null) return Results.NotFound($"Author with UserId : {userId} is not exist!");
                    var post = new Post
                    {
                        Content = newPost.Content,
                        Title = newPost.Title,
                        PostImage = newPost.PostImage,
                        AuthorId = author.AuthorId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    if (newPost.CategoryIds is not null && newPost.CategoryIds.Any())
                        foreach (var ID in newPost.CategoryIds)
                        {
                            var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.CategoryId == ID);
                            if (category is not null)
                                post.Categories.Add(category);
                            else
                                return Results.BadRequest("No valid categories were found.");
                        }
                    else
                        return Results.BadRequest("A post must have at least one category.");

                    await dbContext.Posts.AddAsync(post);
                    await dbContext.SaveChangesAsync();
                    return Results.Created($"/api/posts/{post.PostId}", new
                    {
                        post.PostId,
                        post.Title,
                        post.PostImage,
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
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"An error occurred: {ex.Message}");
                }
            });

        group.MapDelete("/{id:int}", [Authorize(Policy = "AuthorOnly")]
            async (int id, DatabaseContext dbContext, ClaimsPrincipal user,HttpContext httpContext) =>
            {
                try
                {
                    var post = await dbContext.Posts.FirstOrDefaultAsync(p => p.PostId == id);
                    if (post is null) return Results.NotFound($"Post with id {id} does not exist.");

                    if (!await CheckEditPermissions(user, post, dbContext)) return Results.Forbid();

                    var postImage = post.PostImage;
                    if (!string.IsNullOrEmpty(postImage))
                    {
                        postImage = Path.GetFileName(postImage);
                        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "images");
                        var filePath = Path.Combine(uploadPath, postImage);
                        if (File.Exists(filePath)) File.Delete(filePath);
                    }

                    var htmlContent = post.Content;
                    DeleteImagesFromHtml(htmlContent, httpContext);
                    
                    dbContext.Posts.Remove(post);
                    await dbContext.SaveChangesAsync();
                    return Results.Ok(new { message = $"Post with id {id} was successfully deleted" });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"An error occurred: {ex.Message}");
                }
            });

        group.MapPut("/{id:int}", [Authorize(Policy = "AuthorOnly")]
            async (int id, DatabaseContext dbContext, EditPostDto newPost, ClaimsPrincipal user) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(newPost.Content) || string.IsNullOrWhiteSpace(newPost.Title))
                        return Results.BadRequest(new { message = "Title and content can't be empty" });

                    if (newPost.Title.Length > 100)
                        return Results.BadRequest(new { message = "Title cannot exceed 100 characters" });

                    var post = await dbContext.Posts
                        .Include(p => p.Categories)
                        .FirstOrDefaultAsync(p => p.PostId == id);
                    if (post is null) return Results.NotFound($"Post with id {id} does not exist.");

                    if (!await CheckEditPermissions(user, post, dbContext)) return Results.Forbid();

                    if (string.IsNullOrWhiteSpace(newPost.Title) || string.IsNullOrWhiteSpace(newPost.Content))
                        return Results.BadRequest("Title and content cannot be empty");

                    post.Title = newPost.Title;
                    post.Content = newPost.Content;
                    post.UpdatedAt = DateTime.UtcNow;

                    if (newPost.CategoryIds is not null && newPost.CategoryIds.Any())
                    {
                        post.Categories.Clear();
                        foreach (var ID in newPost.CategoryIds)
                        {
                            var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.CategoryId == ID);
                            if (category is not null)
                                post.Categories.Add(category);
                            else
                                return Results.BadRequest("No valid categories were found.");
                        }
                    }
                    else
                    {
                        return Results.BadRequest("A post must have at least one category.");
                    }

                    await dbContext.SaveChangesAsync();
                    return Results.Ok(new { message = $"Post with id {id} was successfully updated" });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"An error occurred: {ex.Message}");
                }
            });

        return group;
    }

    private static async Task<bool> CheckEditPermissions(ClaimsPrincipal user, Post post, DatabaseContext dbContext)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = user.IsInRole("Admin");

        // Sprawdź czy zalogowany użytkownik jest autorem tego posta
        var author = await dbContext.Authors.FirstOrDefaultAsync(a => a.UserId.ToString() == userId);
        var isPostAuthor = author != null && post.AuthorId == author.AuthorId;

        return isAdmin || isPostAuthor;
    }


    private static void DeleteImagesFromHtml(string htmlContent, HttpContext httpContext)
    {
        string regexImgSrc = @"<img[^>]*?src\s*=\s*[""']?([^'"" >]+?)[ '""][^>]*?>";
        MatchCollection matchesImgSrc = Regex.Matches(htmlContent, regexImgSrc, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var request = httpContext.Request;
        var baseImageUrl = $"{request.Scheme}://{request.Host}/images";
        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "images");
        foreach (Match m in matchesImgSrc)
        {
            string imageUrl = m.Groups[1].Value;
            if (imageUrl.Contains(baseImageUrl))
            {
                var fileName = Path.GetFileName(imageUrl);
                var filePath = Path.Combine(uploadPath, fileName);
                if (File.Exists(filePath)) File.Delete(filePath);
            }
        }
        
    }
}