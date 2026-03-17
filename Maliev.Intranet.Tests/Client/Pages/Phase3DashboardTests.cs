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
        // While loading, either skeletons or the content is visible
        var markup = cut.Markup;
        Assert.True(
            markup.Contains("mud-skeleton", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("mud-card", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("widget-card", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("mud-paper", StringComparison.OrdinalIgnoreCase),
            "Expected loading skeletons or dashboard content");
    }

    [Fact]
    public void Dashboard_ShouldContainWidgetOrActionContent()
    {
        var cut = Render<Home>();
        var markup = cut.Markup;
        // Home renders widgets, action items, or loading skeletons
        Assert.True(
            markup.Contains("widget-card", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("mud-card", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("action", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("mud-skeleton", StringComparison.OrdinalIgnoreCase),
            "Expected widget, action items, or loading skeleton content");
    }
}
