using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
using TechBlogAPI.Client.Auth;

namespace TechBlogAPI.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        // Włączenie szczegółowych logów
        builder.Logging.SetMinimumLevel(LogLevel.Debug);

        // HttpClient Configuration
        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5208") });

        // Rejestracja serwisów autentykacji - fix the dependency order
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
        builder.Services.AddAuthorizationCore();

        await builder.Build().RunAsync();
    }
}