using Bunit;
using Maliev.Intranet.Client.Layout;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Tests.Testing;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Maliev.Intranet.Tests.Client.Pages;

/// <summary>
/// Tests for Phase 2: Navigation &amp; Layout Overhaul.
/// Verifies the top navigation renders the flat employee modules and breadcrumbs work.
/// </summary>
public class Phase2NavLayoutTests : BunitContext, IAsyncLifetime
{
    private readonly Mock<AuthenticationStateProvider> _authMock = new();

    public Phase2NavLayoutTests()
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
        authServiceMock.Setup(x => x.AuthorizeAsync(
            It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Success());
        Services.AddSingleton(authServiceMock.Object);

        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);

        var layoutLoggerMock = new Mock<ILogger<LayoutService>>();
        var layoutService = new LayoutService(JSInterop.JSRuntime, layoutLoggerMock.Object, null!);
        Services.AddSingleton<LayoutService>(layoutService);
        Services.AddSingleton<ChatService>();
        Services.AddScoped<BreadcrumbService>();
        Services.AddLogging();
        Services.AddScoped<CurrencyService>();

        _authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-user")], "Test"))));

        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void TopBar_ShouldRender_WithoutException()
    {
        var cut = Render<TopBar>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void TopBar_ShouldContain_QuoteLink()
    {
        var cut = Render<TopBar>();
        Assert.Contains("sales/projects/new", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TopBar_ShouldContain_FlatModuleLinks()
    {
        var cut = Render<TopBar>();
        Assert.Contains("href=\"customers\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"accounting\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"purchasing\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"admin\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"iam\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sales/customers", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BreadcrumbService_SetPageLabel_UpdatesCurrentLabel()
    {
        var svc = new BreadcrumbService();
        svc.SetPageLabel("Acme Corp");
        Assert.Equal("Acme Corp", svc.CurrentLabel);
    }

    [Fact]
    public void BreadcrumbService_Clear_ResetsLabel()
    {
        var svc = new BreadcrumbService();
        svc.SetPageLabel("Test Label");
        svc.Clear();
        Assert.Null(svc.CurrentLabel);
    }

    [Fact]
    public void BreadcrumbService_OnChanged_FiresWhenLabelSet()
    {
        var svc = new BreadcrumbService();
        var fired = false;
        svc.OnChanged += () => fired = true;
        svc.SetPageLabel("Test");
        Assert.True(fired);
    }
}
