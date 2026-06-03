using System.Net.Http.Json;
using System.Security.Claims;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Components.Authorization;

namespace Maliev.Intranet.Client;

/// <summary>
/// Authentication state provider for WebAssembly that resolves the current employee from the BFF cookie.
/// </summary>
public class PersistentAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> _unauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistentAuthenticationStateProvider"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for BFF communication.</param>
    public PersistentAuthenticationStateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var userContext = await _httpClient.GetFromJsonAsync<UserContextDto>("api/v1/auth/user");

            if (userContext is not null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userContext.DisplayName),
                    new Claim(ClaimTypes.NameIdentifier, userContext.UserId),
                    new Claim("email", userContext.Email),
                };

                if (!string.IsNullOrWhiteSpace(userContext.ProfileImageUrl))
                {
                    claims.Add(new Claim("picture", userContext.ProfileImageUrl));
                }

                claims.AddRange(userContext.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
                claims.AddRange(userContext.Roles.Select(r => new Claim("roles", r)));
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

        return await _unauthenticatedTask;
    }
}
