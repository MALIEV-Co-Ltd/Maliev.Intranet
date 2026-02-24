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

/// <summary>Tests for the ChatDrawer component.</summary>
public class ChatDrawerTests : BunitContext, IAsyncLifetime
{
    private readonly Mock<AuthenticationStateProvider> _authMock = new();

    /// <summary>Initializes a new instance of the <see cref="ChatDrawerTests"/> class.</summary>
    public ChatDrawerTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(_authMock.Object);

        // Mock ChatService
        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(new ChatService(client, null!)); // Mocking SignalR as null

        Services.AddSingleton(client);

        _authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }

    /// <summary>Initializes the test asynchronously.</summary>
    public Task InitializeAsync() => Task.CompletedTask;
    /// <summary>Disposes resources used by the test asynchronously.</summary>
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    /// <summary>Verifies that an unavailable message is shown when the health check fails.</summary>
    public void ShouldShowUnavailable_WhenHealthCheckFails()
    {
        // Arrange: health check returns 500
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };

        // Replace services for this specific test
        using var testContext = new BunitContext();
        testContext.Services.AddMudServices();
        testContext.JSInterop.Mode = JSRuntimeMode.Loose;
        testContext.Services.AddSingleton(_authMock.Object);
        testContext.Services.AddSingleton(new ChatService(client, null!));
        testContext.Services.AddSingleton(client);

        // Act
        var cut = testContext.Render<ChatDrawer>();

        // Assert
        Assert.Contains("AI Assistant is currently unavailable", cut.Markup);
    }

    [Fact]
    /// <summary>Verifies that the chat interface is shown when the health check succeeds.</summary>
    public void ShouldShowChat_WhenHealthCheckSucceeds()
    {
        // Arrange: health check returns success
        var healthResponse = new { canInitiateSession = true };
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(healthResponse) }));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };

        using var testContext = new BunitContext();
        testContext.Services.AddMudServices();
        testContext.JSInterop.Mode = JSRuntimeMode.Loose;
        testContext.Services.AddSingleton(_authMock.Object);
        testContext.Services.AddSingleton(new ChatService(client, null!));
        testContext.Services.AddSingleton(client);

        // Act
        var cut = testContext.Render<ChatDrawer>();

        // Assert: input area is visible when AI is available
        Assert.Contains("AI can make mistakes", cut.Markup);
        Assert.Contains("Ask anything", cut.Markup); // Placeholder in text field
    }
}
