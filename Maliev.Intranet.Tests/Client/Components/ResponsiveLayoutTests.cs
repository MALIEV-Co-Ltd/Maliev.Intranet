using Bunit;
using Maliev.Intranet.Client.Layout;
using Maliev.Intranet.Client.Services;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using Microsoft.Extensions.Logging;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Maliev.Intranet.Tests.Client.Components;

public class ResponsiveLayoutTests : BunitContext, IAsyncLifetime
{
    private readonly Mock<ILogger<MainLayout>> _loggerMock = new();
    private readonly Mock<ILogger<LayoutService>> _layoutLoggerMock = new();
    private readonly Mock<AuthenticationStateProvider> _authMock = new();

    public ResponsiveLayoutTests()
    {
        Services.AddMudServices();
        Services.AddAuthorizationCore();
        Services.AddCascadingAuthenticationState();
        Services.AddSingleton<AuthenticationStateProvider>(_authMock.Object);

        var authServiceMock = new Mock<IAuthorizationService>();
        authServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());
        authServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Success());
        Services.AddSingleton(authServiceMock.Object);

        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(_loggerMock.Object);
        Services.AddSingleton(_layoutLoggerMock.Object);

        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);

        // Required for LayoutService
        var layoutService = new LayoutService(JSInterop.JSRuntime, _layoutLoggerMock.Object, null!);

        Services.AddSingleton<LayoutService>(layoutService);
        Services.AddSingleton<ChatService>();
        Services.AddScoped<BreadcrumbService>();

        _authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-user")], "Test"))));
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact(Skip = "MudBreakpointProvider requires JS interop setup not available in bUnit loose mode")]
    public async Task ShouldAdjustLayout_WhenBreakpointChanges()
    {
        var cut = Render<MainLayout>();

        // Find the MudBreakpointProvider
        var breakpointProvider = cut.FindComponent<MudBreakpointProvider>();

        // 1. Simulate Mobile (Xs)
        await breakpointProvider.InvokeAsync(() => breakpointProvider.Instance.OnBreakpointChanged.InvokeAsync(Breakpoint.Xs));

        // On mobile, bottom appbar should be visible (check for class in markup)
        Assert.Contains("class=\"mud-appbar mud-appbar-fixed-bottom", cut.Markup);

        // 2. Simulate Desktop (Lg)
        await breakpointProvider.InvokeAsync(() => breakpointProvider.Instance.OnBreakpointChanged.InvokeAsync(Breakpoint.Lg));

        // On desktop, regular appbar should be back
        Assert.Contains("mud-appbar-fixed-top", cut.Markup);
        Assert.DoesNotContain("class=\"mud-appbar mud-appbar-fixed-bottom", cut.Markup);
    }
}
