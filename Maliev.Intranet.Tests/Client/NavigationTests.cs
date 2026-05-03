using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace Maliev.Intranet.Tests.Client;

public class NavigationTests(BffTestWebApplicationFactory factory) : IClassFixture<BffTestWebApplicationFactory>
{
    private readonly BffTestWebApplicationFactory _factory = factory;

    [Theory(Skip = "Requires Docker/RabbitMQ — host startup hangs due to service discovery and IAM token provider")]
    [InlineData("/")]
    [InlineData("/sales/projects/new")]
    [InlineData("/customers")]
    [InlineData("/accounting")]
    [InlineData("/purchasing")]
    [InlineData("/admin")]
    [InlineData("/iam")]
    public async Task FrontendRoutes_ReturnBlazorShell(string url)
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true
        });

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();

        // The server-owned shell should contain the WASM boot path and static loading screen.
        Assert.Contains("_framework/blazor.web.js", content);
        Assert.Contains("wasm-loading", content);
    }
}
