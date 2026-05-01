using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace Maliev.Intranet.Tests.Client;

public class NavigationTests(BffTestWebApplicationFactory factory) : IClassFixture<BffTestWebApplicationFactory>
{
    private readonly BffTestWebApplicationFactory _factory = factory;

    [Theory(Skip = "Requires Docker/RabbitMQ — host startup hangs due to service discovery and IAM token provider")]
    [InlineData("/")]
    [InlineData("/sales/customers")]
    [InlineData("/sales/projects")]
    [InlineData("/sales/projects/new")]
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

        // The shell should contain blazor boot script
        Assert.Contains("_framework/blazor.web.js", content);
    }
}
