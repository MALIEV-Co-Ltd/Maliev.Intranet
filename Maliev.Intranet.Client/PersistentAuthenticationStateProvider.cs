using System.Net.Http.Json;
using System.Security.Claims;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Maliev.Intranet.Client;

/// <summary>
/// Authentication state provider for WebAssembly that retrieves persisted authentication state
/// and falls back to BFF API if persisted state is not available.
/// </summary>
public class PersistentAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> _unauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    private readonly HttpClient _httpClient;
    private Task<AuthenticationState>? _authenticationStateTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistentAuthenticationStateProvider"/> class.
    /// </summary>
    /// <param name="state">The persistent component state.</param>
    /// <param name="httpClient">HTTP client for BFF communication.</param>
    public PersistentAuthenticationStateProvider(PersistentComponentState state, HttpClient httpClient)
    {
        _httpClient = httpClient;

        // Try to get persisted state first (fast path - from prerendering)
        if (state.TryTakeFromJson<UserInfo>(nameof(UserInfo), out var userInfo) && userInfo is not null)
        {
            var claims = userInfo.Claims.Select(c => new Claim(c.Type, c.Value)).ToList();

            _authenticationStateTask = Task.FromResult(
                new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
                    claims,
                    authenticationType: nameof(PersistentAuthenticationStateProvider),
                    nameType: userInfo.NameClaimType,
                    roleType: userInfo.RoleClaimType))));
        }
    }

    /// <inheritdoc />
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // If we have persisted state, use it
        if (_authenticationStateTask is not null)
        {
            return await _authenticationStateTask;
        }

        // Otherwise, fall back to fetching from BFF API
        try
        {
            var userContext = await _httpClient.GetFromJsonAsync<UserContextDto>("api/auth/user");

            if (userContext is not null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userContext.DisplayName),
                    new Claim(ClaimTypes.NameIdentifier, userContext.UserId),
                    new Claim("email", userContext.Email)
                };

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

    private sealed class UserInfo
    {
        public string NameClaimType { get; set; } = string.Empty;
        public string RoleClaimType { get; set; } = string.Empty;
        public List<ClaimInfo> Claims { get; set; } = [];
    }

    private sealed class ClaimInfo
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
