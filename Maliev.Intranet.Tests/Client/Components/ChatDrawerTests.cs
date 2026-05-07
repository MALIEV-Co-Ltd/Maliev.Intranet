using Bunit;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Client.Services;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using System.Net;
using Maliev.Intranet.Tests.Testing;
using System.Net.Http.Json;

namespace Maliev.Intranet.Tests.Client.Components;

public class ChatDrawerTests : BunitContext, IAsyncLifetime
{
    private readonly Mock<AuthenticationStateProvider> _authMock = new();

    public ChatDrawerTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(_authMock.Object);

        // Mock ChatService
        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(new ChatService(client, null!, new CookieProvider()));

        Services.AddSingleton(client);

        _authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task ShouldShowUnavailable_WhenHealthCheckFails()
    {
        // Arrange: health check returns 500
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };

        // Replace services for this specific test
        await using var testContext = new BunitContext();
        testContext.Services.AddMudServices();
        testContext.JSInterop.Mode = JSRuntimeMode.Loose;
        testContext.Services.AddSingleton(_authMock.Object);
        testContext.Services.AddSingleton(new ChatService(client, null!, new CookieProvider()));
        testContext.Services.AddSingleton(client);
        testContext.Render<MudPopoverProvider>();

        // Act
        var cut = testContext.Render<ChatDrawer>();

        // Assert
        Assert.Contains("AI Assistant is currently unavailable", cut.Markup);
    }

    [Fact]
    public async Task ShouldShowChat_WhenHealthCheckSucceeds()
    {
        // Arrange: health check returns success
        var healthResponse = new { canInitiateSession = true };
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(healthResponse) }));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };

        await using var testContext = new BunitContext();
        testContext.Services.AddMudServices();
        testContext.JSInterop.Mode = JSRuntimeMode.Loose;
        testContext.Services.AddSingleton(_authMock.Object);
        testContext.Services.AddSingleton(new ChatService(client, null!, new CookieProvider()));
        testContext.Services.AddSingleton(client);
        testContext.Render<MudPopoverProvider>();

        // Act
        var cut = testContext.Render<ChatDrawer>();

        // Assert: Sidekick-style empty state and input area are visible when AI is available
        Assert.Contains("How can I help?", cut.Markup);
        Assert.Contains("What's new?", cut.Markup);
        Assert.Contains("Ask anything...", cut.Markup);
        Assert.Contains("sidekick-composer", cut.Markup);
    }
}
