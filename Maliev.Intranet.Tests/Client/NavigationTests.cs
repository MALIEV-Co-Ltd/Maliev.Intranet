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
    [InlineData("/finance/invoices")]
    [InlineData("/finance/payments")]
    [InlineData("/finance/receipts")]
    [InlineData("/finance/delivery-notes")]
    [InlineData("/finance/ledger")]
    [InlineData("/finance/reports")]
    [InlineData("/hr/employees")]
    [InlineData("/hr/directory")]
    [InlineData("/hr/recruitment")]
    [InlineData("/hr/onboarding")]
    [InlineData("/hr/time-off")]
    [InlineData("/hr/performance")]
    [InlineData("/hr/compensation")]
    [InlineData("/hr/compliance")]
    [InlineData("/hr/analytics")]
    [InlineData("/admin/system-health")]
    [InlineData("/admin/iam")]
    [InlineData("/admin/notifications")]
    [InlineData("/admin/reference-data")]
    [InlineData("/admin/audit")]
    [InlineData("/admin/content")]
    [InlineData("/admin/workflows")]
    [InlineData("/admin/portal-config")]
    [InlineData("/mfg/models")]
    [InlineData("/mfg/materials")]
    [InlineData("/mfg/procurement")]
    [InlineData("/mfg/suppliers")]
    [InlineData("/mfg/inventory")]
    [InlineData("/mfg/production-queue")]
    [InlineData("/mfg/equipment")]
    [InlineData("/mfg/equipment/new")]
    [InlineData("/finance/budget")]
    [InlineData("/finance/report-builder")]
    [InlineData("/hr/training")]
    [InlineData("/hr/profile")]
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
