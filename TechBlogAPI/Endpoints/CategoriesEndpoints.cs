using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TechBlogAPI.Data;
using TechBlogAPI.Entity;

namespace TechBlogAPI.Endpoints
{
    public static class CategoriesEndpoints
    {
        public static RouteGroupBuilder MapCategoriesRoutes(this WebApplication app) {
            var group = app.MapGroup("api/category");

            group.MapGet("/", async (DatabaseContext dbContext) => {
                var categories = dbContext.Categories.Select(c => new {
                    c.CategoryId,
                    c.Name
                }
                );
                return Results.Ok(categories.Select(c => new {
                    c.CategoryId,
                    c.Name
                }));
            });

            group.MapPost("/", [Authorize(Policy = "AdminOnly")] async (DatabaseContext dbContext, string name) => {
                if (string.IsNullOrWhiteSpace(name)) {
                    return Results.BadRequest("Category name cannot be null.");
                }
                
                var category = new Category() {
                    Name = name
                };

                dbContext.Categories.Add(category);
                await dbContext.SaveChangesAsync();
                return Results.Ok();
            });

            group.MapPut("/{id:int}", [Authorize(Policy = "AdminOnly")] async (int id, string name, DatabaseContext dbContext) => {
                 if (string.IsNullOrWhiteSpace(name)) {
                    return Results.BadRequest("Category name cannot be null.");
                }

                var category = dbContext.Categories.FirstOrDefault(c => c.CategoryId == id);
                if (category is null)
                {
                    return Results.NotFound($"Category with id {id} does not exist.");
                }
                category.Name = name;
                await dbContext.SaveChangesAsync();
                return Results.Ok(new { message = $"Category with id {id} was successfully updated" });
            });

            group.MapDelete("/{id:int}", [Authorize(Policy="AdminOnly")] async (int id, DatabaseContext dbContext) => {
                var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.CategoryId == id);
                if (category is null) {
                    return Results.NotFound($"Category with id {id} does not exist.");
                }
                dbContext.Categories.Remove(category);
                await dbContext.SaveChangesAsync();
                return Results.Ok(new { message = $"Category with id {id} was successfully deleted" });
            });

            return group;
        }
    }
}