using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace TechBlogAPI.Client.Auth;

public class CustomAuthStateProvider(IJSRuntime jsRuntime, AuthService authService) : AuthenticationStateProvider
{
    private readonly AuthenticationState _anonymousState = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await authService.GetAccessTokenAsync();

            if (string.IsNullOrEmpty(token)) return this._anonymousState;

            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch
        {
            return this._anonymousState;
        }
    }

    public async Task NotifyUserAuthenticationChangedAsync()
    {
        var authState = await GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        return token.Claims;
    }
}