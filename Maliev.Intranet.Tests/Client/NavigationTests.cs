using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace Maliev.Intranet.Tests.Client;

public class NavigationTests(BffTestWebApplicationFactory factory) : IClassFixture<BffTestWebApplicationFactory>
{
    private readonly BffTestWebApplicationFactory _factory = factory;

    [Theory]
    [InlineData("/")]
    [InlineData("/sales/customers")]
    [InlineData("/sales/companies")]
    [InlineData("/sales/orders")]
    [InlineData("/sales/quotations")]
    [InlineData("/sales/pricing-ai")]
    [InlineData("/sales/projects")]
    [InlineData("/sales/projects/new")]
    [InlineData("/finance/invoices")]
    [InlineData("/finance/payments")]
    [InlineData("/finance/accounting")]
    [InlineData("/finance/delivery-notes")]
    [InlineData("/hr/directory")]
    [InlineData("/hr/leave")]
    [InlineData("/hr/training")]
    [InlineData("/hr/compliance")]
    [InlineData("/hr/recruitment")]
    [InlineData("/hr/profile")]
    [InlineData("/admin/system-health")]
    [InlineData("/admin/iam")]
    [InlineData("/admin/audit")]
    [InlineData("/admin/settings")]
    [InlineData("/mfg/models")]
    [InlineData("/mfg/materials")]
    [InlineData("/mfg/procurement")]
    [InlineData("/mfg/production-queue")]
    [InlineData("/mfg/equipment")]
    [InlineData("/mfg/equipment/new")]
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
