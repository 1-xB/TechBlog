using System.Security.Claims;
using TechBlogAPI.Dtos;
using TechBlogAPI.Services;

namespace TechBlogAPI.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthRoutes(this WebApplication app)
    {
        var group = app.MapGroup("api/auth");

        group.MapPost("/register", async (IAuthService auth, RegisterDto model) =>
        {
            var result = await auth.RegisterAsync(model);
            if (!result.Succeeded)
            {
                return Results.BadRequest(result.Message);
            }

            return Results.Ok(result.Message);
        });

        group.MapPost("/login", async (IAuthService auth, LoginDto model) =>
        {
            var result = await auth.LoginAsync(model);
            if (!result.Succeeded)
            {
                return Results.BadRequest(result.Message);
            }

            return Results.Ok(result.Response);
            
        });

        group.MapPost("/refresh-token", async (IAuthService auth, RefreshTokenDto model) =>
        {
            var result = await auth.RefreshTokenAsync(model.RefreshToken);
            if (!result.Succeeded)
            {
                return Results.Unauthorized();
            }
            
            return Results.Ok(result.Response);
        });
        
        group.MapPost("/revoke-token", async (IAuthService auth, ClaimsPrincipal user) =>
            {
                var username = user.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return Results.BadRequest(new { message = "Username not found." });
                }

                var result = await auth.RevokeTokenAsync(username);
                if (!result)
                {
                    return Results.BadRequest(new { message = "Token revocation failed." });
                }

                return Results.Ok(new { message = "Token revoked successfully." });
            })
            .RequireAuthorization(); 
            
        
        return group;
    }
}