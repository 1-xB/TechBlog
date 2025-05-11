using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
using Radzen;
using TechBlogAPI.Client.Auth;
using TechBlogAPI.Client.Services;

namespace TechBlogAPI.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Logging.SetMinimumLevel(LogLevel.Debug);

        // HttpClient Configuration
        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5208") });

        builder.Services.AddScoped<IPostService, PostService>();
        builder.Services.AddScoped<ICategoryService, CategoryService>();

        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
        builder.Services.AddAuthorizationCore();

        builder.Services.AddRadzenComponents();

        await builder.Build().RunAsync();
    }
}