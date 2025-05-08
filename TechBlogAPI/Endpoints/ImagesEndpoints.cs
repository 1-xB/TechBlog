using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TechBlogAPI.Endpoints;

public static class ImagesEndpoints
{
    public static RouteGroupBuilder MapImagesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("api/images");

        // TODO : FIX ANTIFORGERY
        group.MapPost("/upload", [Authorize(Policy = "AuthorOnly")] async (HttpContext httpContext, IFormFile file) =>
        {
            try
            {
                if (file == null)
                {
                    return Results.BadRequest("File is null");
                }

                if (file.Length == 0)
                {
                    return Results.BadRequest("File length is 0");
                }

                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "images");
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var request = httpContext.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}";
                var fileUrl = $"{baseUrl}/images/{fileName}";

                return Results.Ok(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
            }
        }).AllowAnonymous().DisableAntiforgery();
        
        return group;
    }
}