using Bunit;
using Maliev.Intranet.Client.Layout;
using Maliev.Intranet.Client.Services;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>Tests for the MainLayout component.</summary>
public class MainLayoutTests : BunitContext, IAsyncLifetime
{
    private readonly Mock<ILogger<MainLayout>> _loggerMock = new();
    private readonly Mock<ILogger<LayoutService>> _layoutLoggerMock = new();
    private readonly Mock<AuthenticationStateProvider> _authMock = new();

    /// <summary>Initializes a new instance of the <see cref="MainLayoutTests"/> class.</summary>
    public MainLayoutTests()
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

        _authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-user")], "Test"))));
    }

    /// <summary>Initializes the test asynchronously.</summary>
    public Task InitializeAsync() => Task.CompletedTask;
    /// <summary>Disposes resources used by the test asynchronously.</summary>
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    /// <summary>Verifies that the navigation layout is rendered.</summary>
    public void ShouldRenderNavigation()
    {
        var cut = Render<MainLayout>();

        // Check for basic layout elements
        Assert.Contains("mud-layout", cut.Markup);
    }
}
