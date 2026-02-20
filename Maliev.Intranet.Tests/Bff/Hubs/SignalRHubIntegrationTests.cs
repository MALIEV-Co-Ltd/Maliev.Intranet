using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace Maliev.Intranet.Tests.Bff.Hubs;

public class SignalRHubIntegrationTests(BffTestWebApplicationFactory factory) : IClassFixture<BffTestWebApplicationFactory>
{
    private readonly BffTestWebApplicationFactory _factory = factory;

    [Theory]
    [InlineData("/hubs/notifications/negotiate")]
    [InlineData("/hubs/chat/negotiate")]
    public async Task HubNegotiate_ReturnsOk_WhenAuthenticated(string url)
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = _factory.CreateTestToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsync(url, null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/hubs/notifications/negotiate")]
    [InlineData("/hubs/chat/negotiate")]
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
}
