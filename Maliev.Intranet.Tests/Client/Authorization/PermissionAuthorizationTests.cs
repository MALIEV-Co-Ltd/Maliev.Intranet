using System.Security.Claims;
using Maliev.Intranet.Client.Authorization;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.Intranet.Tests.Client.Authorization;

public sealed class PermissionAuthorizationTests
{
    [Fact]
    public async Task AuthorizeAsync_WhenUserHasWildcardPermission_AllowsCommerceProductRead()
    {
        await using var provider = BuildProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var user = CreateUser(new Claim("permissions", "*"));

        var result = await authorizationService.AuthorizeAsync(
            user,
            resource: null,
            $"Permission:{MalievPermissions.Commerce.ProductsRead}");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserHasExactPermission_AllowsCommerceProductRead()
    {
        await using var provider = BuildProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var user = CreateUser(new Claim("permission", MalievPermissions.Commerce.ProductsRead));

        var result = await authorizationService.AuthorizeAsync(
            user,
            resource: null,
            $"Permission:{MalievPermissions.Commerce.ProductsRead}");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserHasPermissionWildcardSuffix_AllowsCommerceProductRead()
    {
        await using var provider = BuildProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var user = CreateUser(new Claim("permissions", "commerce.products.*"));

        var result = await authorizationService.AuthorizeAsync(
            user,
            resource: null,
            $"Permission:{MalievPermissions.Commerce.ProductsRead}");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserLacksRequiredPermission_DeniesCommerceProductRead()
    {
        await using var provider = BuildProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var user = CreateUser(new Claim("permissions", MalievPermissions.Customer.Read));

        var result = await authorizationService.AuthorizeAsync(
            user,
            resource: null,
            $"Permission:{MalievPermissions.Commerce.ProductsRead}");

        Assert.False(result.Succeeded);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationCore();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static ClaimsPrincipal CreateUser(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }
}
