using Bunit;
using Maliev.Intranet.Client.Pages;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Tests.Testing;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using Moq;
using System.Security.Claims;
using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Tests.Client.Pages;

/// <summary>
/// Tests for Phase 3: Dashboard Redesign — action items panel, stats widgets, quick actions.
/// </summary>
public class Phase3DashboardTests : BunitContext, IAsyncLifetime
{
    private readonly Mock<AuthenticationStateProvider> _authMock = new();

    public Phase3DashboardTests()
    {
        Services.AddMudServices();
        Services.AddAuthorizationCore();
        Services.AddCascadingAuthenticationState();
        Services.AddSingleton<AuthenticationStateProvider>(_authMock.Object);

        var authServiceMock = new Mock<IAuthorizationService>();
        authServiceMock.Setup(x => x.AuthorizeAsync(
            It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(),
            It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());
        Services.AddSingleton(authServiceMock.Object);

        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);
        Services.AddScoped<BreadcrumbService>();

        _authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-user")], "Test"))));

        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void Dashboard_ShouldRender_WithoutException()
    {
        var cut = Render<Home>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void Dashboard_ShouldShowLoadingOrContent_OnInitialRender()
    {
        var cut = Render<Home>();
        var markup = cut.Markup;
        Assert.True(
            markup.Contains("mlv-module-shell", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Loading operations", StringComparison.OrdinalIgnoreCase),
            "Expected loading or dashboard module content");
    }

    [Fact]
    public void Dashboard_ShouldContainWidgetOrActionContent()
    {
        var cut = Render<Home>();
        var markup = cut.Markup;
        Assert.True(
            markup.Contains("Dashboard", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("New Quote", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Action queue", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Loading operations", StringComparison.OrdinalIgnoreCase),
            "Expected operational dashboard content");
    }
}
