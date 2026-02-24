using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Authentication state provider that integrates with the BFF.
/// </summary>
public class BffAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="BffAuthenticationStateProvider"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    public BffAuthenticationStateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
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
                    new Claim(ClaimTypes.NameIdentifier, userContext.UserId),
                    new Claim("email", userContext.Email)
                };

                claims.AddRange(userContext.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
                // Also add "roles" for compatibility with pages looking for the literal string
                claims.AddRange(userContext.Roles.Select(r => new Claim("roles", r)));

                // Standardize on both "permissions" and "permission" to ensure compatibility with all Authorize attributes
                claims.AddRange(userContext.Permissions.Select(p => new Claim("permissions", p)));
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
