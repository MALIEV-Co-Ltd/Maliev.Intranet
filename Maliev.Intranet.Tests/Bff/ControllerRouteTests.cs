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

    [Theory(Skip = "Requires Docker/RabbitMQ for MassTransit bus initialization")]
    [InlineData("/api/v1/auth/user")]
    [InlineData("/api/v1/companies")]
    [InlineData("/api/v1/customers")]
    [InlineData("/api/v1/orders")]
    [InlineData("/api/v1/quotations")]
    [InlineData("/api/v1/invoices")]
    [InlineData("/api/v1/payments")]
    [InlineData("/api/v1/suppliers")]
    [InlineData("/api/v1/materials")]
    [InlineData("/api/v1/employees")]
    [InlineData("/api/v1/timeoff/balances")]
    [InlineData("/api/v1/receipts")]
    [InlineData("/api/v1/procurement")]
    [InlineData("/api/v1/deliverynotes")]
    [InlineData("/api/v1/accounting/journal-entries")]
    [InlineData("/api/v1/performance/reviews")]
    [InlineData("/api/v1/compliance/stats")]
    [InlineData("/api/v1/compensation/summary")]
    [InlineData("/api/v1/referencedata/countries")]
    [InlineData("/api/v1/notifications/templates")]
    [InlineData("/api/v1/dashboard")]
    [InlineData("/api/v1/preferences/UI")]
    [InlineData("/api/v1/diagnostics/me")]
    [InlineData("/api/v1/system-health")]
    [InlineData("/api/v1/models")]
    [InlineData("/api/v1/recruitment/jobs")]
    [InlineData("/api/v1/billing-notes")]
    [InlineData("/api/v1/credit-terms")]
    [InlineData("/api/v1/permissions/available")]
    [InlineData("/api/v1/onboarding")]
    [InlineData("/api/v1/equipments")]
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

    [Fact(Skip = "Requires Docker/RabbitMQ for MassTransit bus initialization")]
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

        var response = await client.PostAsJsonAsync("/api/v1/pricing/calculate", request);

        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.Unauthorized ||
                    response.StatusCode == HttpStatusCode.Forbidden ||
                    response.StatusCode == HttpStatusCode.BadRequest,
                    $"URL /api/v1/pricing/calculate returned {response.StatusCode}");
    }

    [Fact(Skip = "Requires Docker/RabbitMQ for MassTransit bus initialization")]
    public async Task Authenticated_DiagnosticsMe_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = _factory.CreateTestToken("test-user", MalievPermissions.System.DiagnosticsRead);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/v1/diagnostics/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
