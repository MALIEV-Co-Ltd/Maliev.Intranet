using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace Maliev.Intranet.Tests.Bff;

public class ControllerRouteTests(BffTestWebApplicationFactory factory) : IClassFixture<BffTestWebApplicationFactory>
{
    private readonly BffTestWebApplicationFactory _factory = factory;

    [Theory]
    [InlineData("/api/auth/user")]
    [InlineData("/api/companies")]
    [InlineData("/api/customers")]
    [InlineData("/api/orders")]
    [InlineData("/api/quotations")]
    [InlineData("/api/invoices")]
    [InlineData("/api/payments")]
    [InlineData("/api/suppliers")]
    [InlineData("/api/materials")]
    [InlineData("/api/employees")]
    [InlineData("/api/timeoff/balances")]
    [InlineData("/api/receipts")]
    [InlineData("/api/procurement")]
    [InlineData("/api/deliverynotes")]
    [InlineData("/api/accounting/journal-entries")]
    [InlineData("/api/performance/reviews")]
    [InlineData("/api/compliance/stats")]
    [InlineData("/api/compensation/summary")]
    [InlineData("/api/referencedata/countries")]
    [InlineData("/api/notifications/templates")]
    [InlineData("/api/dashboard")]
    [InlineData("/api/preferences/UI")]
    [InlineData("/api/diagnostics/me")]
    [InlineData("/api/system-health")]
    [InlineData("/api/models")]
    [InlineData("/api/recruitment/jobs")]
    [InlineData("/api/billing-notes")]
    [InlineData("/api/credit-terms")]
    [InlineData("/api/permissions/available")]
    [InlineData("/api/onboarding")]
    [InlineData("/api/equipments")]
    public async Task Get_Endpoints_ReturnSuccessOrUnauthorized(string url)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.NoContent ||
                    response.StatusCode == HttpStatusCode.Unauthorized ||
                    response.StatusCode == HttpStatusCode.Forbidden,
                    $"URL {url} returned {response.StatusCode}");
    }

    [Fact]
    public async Task Post_PricingCalculate_ReturnsSuccessOrUnauthorized()
    {
        var client = _factory.CreateClient();
        var request = new PricingRequestDto
        {
            FileId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            MaterialId = Guid.NewGuid(),
            MaterialCode = "TEST",
            ManufacturingProcessId = Guid.NewGuid(),
            ManufacturingProcessName = "TEST",
            Geometry = new GeometryMetricsDto
            {
                VolumeCm3 = 1,
                SurfaceAreaCm2 = 1,
                BoundingBoxX = 1,
                BoundingBoxY = 1,
                BoundingBoxZ = 1,
                IsManifold = true,
                TriangleCount = 100,
                SupportVolumeCm3 = 0
            }
        };

        var response = await client.PostAsJsonAsync("/api/pricing/calculate", request);

        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.Unauthorized ||
                    response.StatusCode == HttpStatusCode.Forbidden ||
                    response.StatusCode == HttpStatusCode.BadRequest,
                    $"URL /api/pricing/calculate returned {response.StatusCode}");
    }

    [Fact]
    public async Task Authenticated_DiagnosticsMe_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = _factory.CreateTestToken("test-user", MalievPermissions.System.DiagnosticsRead);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/diagnostics/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
