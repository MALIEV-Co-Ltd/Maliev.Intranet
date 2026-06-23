using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class ComplianceControllerTests
{
    private readonly Mock<IComplianceServiceClient> _clientMock = new();

    [Fact]
    public async Task GetStats_WhenClientReturnsNull_ReturnsEmptyStats()
    {
        _clientMock.Setup(client => client.GetComplianceStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComplianceStatsDto?)null);
        var controller = new ComplianceController(_clientMock.Object);

        var result = await controller.GetStats();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ComplianceStatsDto>(ok.Value);
        Assert.Equal(0, dto.Expiring30Days);
        Assert.Equal(0, dto.Expiring60Days);
        Assert.Equal(0, dto.TotalActive);
    }

    [Fact]
    public async Task GetById_WhenRecordMissing_ReturnsNotFound()
    {
        _clientMock.Setup(client => client.GetComplianceRecordByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComplianceRecordDto?)null);
        var controller = new ComplianceController(_clientMock.Object);

        var result = await controller.GetById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_WhenClientCreatesRecord_ReturnsCreatedAtGetById()
    {
        var recordId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        _clientMock.Setup(client => client.CreateComplianceRecordAsync(It.IsAny<CreateComplianceRecordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComplianceRecordDto { Id = recordId });
        var controller = new ComplianceController(_clientMock.Object);

        var result = await controller.Create(new CreateComplianceRecordRequest { EmployeeId = Guid.NewGuid(), Type = "SafetyTraining", Date = DateTime.UtcNow }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(ComplianceController.GetById), created.ActionName);
        Assert.Equal(recordId, created.RouteValues?["id"]);
    }

    [Fact]
    public async Task Delete_WhenClientReportsFailure_ReturnsNotFound()
    {
        _clientMock.Setup(client => client.DeleteComplianceRecordAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = new ComplianceController(_clientMock.Object);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Theory]
    [InlineData(nameof(ComplianceController.GetStats), MalievPermissions.Compliance.Read)]
    [InlineData(nameof(ComplianceController.Get), MalievPermissions.Compliance.Read)]
    [InlineData(nameof(ComplianceController.GetById), MalievPermissions.Compliance.Read)]
    [InlineData(nameof(ComplianceController.Create), MalievPermissions.Compliance.Write)]
    [InlineData(nameof(ComplianceController.Delete), MalievPermissions.Compliance.Write)]
    public void ComplianceEndpoints_RequireCompliancePermissions(string actionName, string expectedPermission)
    {
        var method = typeof(ComplianceController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == actionName);

        var attribute = Assert.Single(method.GetCustomAttributes<RequirePermissionAttribute>());

        Assert.Contains(expectedPermission, attribute.Policy, StringComparison.Ordinal);
        Assert.Equal("Bearer,Cookies", attribute.AuthenticationSchemes);
    }

    [Fact]
    public async Task ComplianceServiceClient_MapsUnresolvedAlertsToPagedRecords()
    {
        var alertId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var employeeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var requestedPaths = new List<string>();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            requestedPaths.Add(request.RequestUri?.PathAndQuery ?? string.Empty);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[]
                {
                    new
                    {
                        id = alertId,
                        employeeId,
                        alertType = "CertificationExpired",
                        severity = "High",
                        isResolved = false,
                        createdDate = DateTime.Parse("2026-06-23T08:00:00Z").ToUniversalTime()
                    }
                })
            });
        });
        var client = new ComplianceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://compliance") });

        var result = await client.GetComplianceRecordsAsync(page: 1, pageSize: 20);

        Assert.NotNull(result);
        Assert.Equal("/compliance/v1/compliance-alerts?isResolved=false", Assert.Single(requestedPaths));
        var record = Assert.Single(result.Data);
        Assert.Equal(alertId, record.Id);
        Assert.Equal(employeeId, record.EmployeeId);
        Assert.Equal("CertificationExpired", record.Type);
        Assert.Equal("High", record.Status);
        Assert.Equal(1, result.Meta.TotalCount);
    }

    [Fact]
    public async Task ComplianceServiceClient_MapsComplianceReportToStats()
    {
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/compliance/v1/compliance-reports/compliance", request.RequestUri?.PathAndQuery);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    requiresAuthorization = 42,
                    expiringSoon = 6,
                    expired = 3
                })
            });
        });
        var client = new ComplianceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://compliance") });

        var result = await client.GetComplianceStatsAsync();

        Assert.NotNull(result);
        Assert.Equal(6, result.Expiring30Days);
        Assert.Equal(9, result.Expiring60Days);
        Assert.Equal(42, result.TotalActive);
    }

    [Fact]
    public async Task ComplianceServiceClient_DeleteResolvesAlert()
    {
        var recordId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler(async (request, _) =>
        {
            capturedRequest = request;
            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("Resolved from the intranet compliance records view.", body, StringComparison.Ordinal);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = new ComplianceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://compliance") });

        var deleted = await client.DeleteComplianceRecordAsync(recordId);

        Assert.True(deleted);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Put, capturedRequest.Method);
        Assert.Equal($"/compliance/v1/compliance-alerts/{recordId}/resolve", capturedRequest.RequestUri?.PathAndQuery);
    }
}
