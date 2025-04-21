using System.Security.Claims;
using TechBlogAPI.Dtos;
using TechBlogAPI.Services;
using Microsoft.AspNetCore.Authorization;

namespace TechBlogAPI.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthRoutes(this WebApplication app)
    {
        var group = app.MapGroup("api/auth");

        group.MapPost("/register", async (IAuthService auth, RegisterDto model) =>
        {
            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password)) {
                return Results.BadRequest(new { message = "All fields are required." });
            }
            var result = await auth.RegisterAsync(model);
            if (!result.Succeeded)
            {
                return Results.BadRequest(new {message = result.Message});
            }

            return Results.Ok(new {message = result.Message});
        });

        group.MapPost("register-author", [Authorize(Policy = "AdminOnly")] async (IAuthService auth, RegisterAuthorDto model) => {
            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Email) 
            || string.IsNullOrWhiteSpace(model.Password) || string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
            {
            return Results.BadRequest(new { message = "All fields are required." });
            }

            var result = await auth.RegisterAuthorAsync(model);
            if (!result.Succeeded)
            {
            return Results.BadRequest(new {message = result.Message});
            }

            return Results.Ok(new {message = result.Message});
        });

        group.MapPost("/login", async (IAuthService auth, LoginDto model) =>
        {
            var result = await auth.LoginAsync(model);
            if (!result.Succeeded)
            {
                return Results.BadRequest(new {message = result.Message});
            }

            return Results.Ok(new {message = result.Response});
            
        });

        group.MapPost("/refresh-token", async (IAuthService auth, RefreshTokenDto model) =>
        {
            var result = await auth.RefreshTokenAsync(model.RefreshToken);
            if (!result.Succeeded)
            {
                return Results.Unauthorized();
            }
            
            return Results.Ok(new {message = result.Response});
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