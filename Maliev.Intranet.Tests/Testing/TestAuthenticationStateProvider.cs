using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Maliev.Intranet.Tests.Testing;

public class TestAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ClaimsPrincipal _user;

    public TestAuthenticationStateProvider(ClaimsPrincipal? user = null)
    {
        _user = user ?? CreateDefaultUser();
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_user));
    }

    public void SetUser(ClaimsPrincipal user)
    {
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    private static ClaimsPrincipal CreateDefaultUser()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new(ClaimTypes.Email, "testuser@maliev.com"),
            new("permissions", "project.projects.create"),
            new("permissions", "project.projects.read"),
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}
