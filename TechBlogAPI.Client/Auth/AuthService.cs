using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using TechBlogAPI.Client.Models;

namespace TechBlogAPI.Client.Auth
{
    public class AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                LoginModel loginModel = new()
                {
                    Username = username,
                    Password = password
                };

                var response = await httpClient.PostAsJsonAsync("/api/auth/login-admin", loginModel);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    if (result is null) return false;
                    await SaveTokensAsync(result);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> LoginAuthorAsync(string username, string password)
        {
            try
            {
                LoginModel loginModel = new()
                {
                    Username = username,
                    Password = password
                };

                var response = await httpClient.PostAsJsonAsync("/api/auth/login-author", loginModel);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    if (result is null) return false;
                    await SaveTokensAsync(result);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                var refreshToken = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "refreshToken");
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return false;
                }

                var refreshTokenRequest = new RefreshTokenRequest { RefreshToken = refreshToken };
                var response = await httpClient.PostAsJsonAsync("api/auth/refresh-token", refreshTokenRequest);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    if (result is null) return false;
                    await SaveTokensAsync(result);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                var accessToken = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "accessToken");

                if (!string.IsNullOrEmpty(accessToken))
                {
                    using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/auth/revoke-token");
                    requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                    await httpClient.SendAsync(requestMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during token revocation: {ex.Message}");
            }
            finally
            {
                await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "accessToken");
                await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
                await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "tokenExpiration");
            }
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var token = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "accessToken");
            var expirationStr = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "tokenExpiration");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(expirationStr))
            {
                return null;
            }

            if (DateTime.TryParse(expirationStr, out var expiration))
            {
                if (expiration <= DateTime.UtcNow.AddMinutes(1))
                {
                    var refreshed = await RefreshTokenAsync();
                    if (refreshed)
                    {
                        return await jsRuntime.InvokeAsync<string>("localStorage.getItem", "accessToken");
                    }

                    await LogoutAsync();
                    return null;
                }
            }

            return token;
        }

        private async Task SaveTokensAsync(LoginResponse login)
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", "accessToken", login.AccessToken);
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", login.RefreshToken);
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", "tokenExpiration", login.ExpiresAt.ToString("o"));
        }
    }
}
