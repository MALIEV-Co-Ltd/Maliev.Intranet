using System.Net;
using System.Net.Http.Headers;
using Maliev.Intranet.Shared;
using Xunit;

namespace Maliev.Intranet.Tests.Bff.Hubs;

/// <summary>
/// Integration tests for SignalR hub negotiation endpoints.
/// Uses SignalRTestFactory to strip service discovery DelegatingHandlers
/// that would otherwise cause DNS resolution hangs during WebApplicationFactory startup.
/// </summary>
public class SignalRHubIntegrationTests(SignalRTestFactory factory) : IClassFixture<SignalRTestFactory>
{
    private readonly SignalRTestFactory _factory = factory;

    [Theory]
    [InlineData("/hubs/notifications/negotiate")]
    [InlineData("/hubs/chat/negotiate")]
    [InlineData("/hubs/production/negotiate")]
    public async Task HubNegotiate_ReturnsOk_WhenAuthenticated(string url)
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = url.StartsWith("/hubs/chat", StringComparison.Ordinal)
            ? _factory.CreateTestToken("chat-reader", MalievPermissions.Chat.SessionsRead)
            : url.StartsWith("/hubs/production", StringComparison.Ordinal)
                ? _factory.CreateTestToken("job-reader", MalievPermissions.Job.Read)
                : _factory.CreateTestToken("project-reader", MalievPermissions.Project.Read);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsync(url, null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/hubs/notifications/negotiate")]
    [InlineData("/hubs/chat/negotiate")]
    [InlineData("/hubs/production/negotiate")]
    public async Task HubNegotiate_ReturnsUnauthorized_WhenNoToken(string url)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync(url, null);

        // Assert
        // SignalR hubs in this project might have [Authorize]
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChatHubNegotiate_ReturnsForbidden_WhenAuthenticatedCallerLacksConversationReadPermission()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateTestToken("chat-no-read"));

        var response = await client.PostAsync("/hubs/chat/negotiate", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NotificationHubNegotiate_ReturnsForbidden_WhenAuthenticatedCallerLacksProjectReadPermission()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateTestToken("project-no-read"));

        var response = await client.PostAsync("/hubs/notifications/negotiate", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProductionHubNegotiate_ReturnsForbidden_WhenAuthenticatedCallerLacksJobReadPermission()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateTestToken("job-no-read"));

        var response = await client.PostAsync("/hubs/production/negotiate", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
