using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Client.Services;

public class BffAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;

    public BffAuthenticationStateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var userContext = await _httpClient.GetFromJsonAsync<UserContextDto>("api/auth/user");

            if (userContext != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userContext.DisplayName),
                    new Claim(ClaimTypes.NameIdentifier, userContext.UserId.ToString())
                };

                claims.AddRange(userContext.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
                claims.AddRange(userContext.Permissions.Select(p => new Claim("permission", p)));

                var identity = new ClaimsIdentity(claims, "BFF");
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
        }
        catch
        {
            // Not authenticated
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }
}
