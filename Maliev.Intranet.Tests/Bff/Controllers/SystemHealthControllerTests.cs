using System.Net;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class SystemHealthControllerTests
{
    [Fact]
    public void Controller_RequiresSystemHealthReadPermission()
    {
        var attribute = Assert.Single(typeof(SystemHealthController).GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: false));
        var permission = Assert.IsType<RequirePermissionAttribute>(attribute);

        Assert.Equal(MalievPermissions.System.HealthRead, permission.Permission);
    }

    [Fact]
    public async Task GetSystemHealth_UsesServiceLivenessAndReadinessEndpoints()
    {
        var requestedPaths = new List<string>();
        var probeService = CreateProbeService(request =>
        {
            requestedPaths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var services = await probeService.CheckAllAsync(CancellationToken.None);

        Assert.All(services, service =>
        {
            Assert.Equal($"/{service.RoutePrefix}/readiness", service.HealthPath);
            Assert.Equal($"/{service.RoutePrefix}/liveness", service.LivenessPath);
            Assert.Equal($"/{service.RoutePrefix}/readiness", service.ReadinessPath);
        });
        Assert.DoesNotContain(requestedPaths, path => path.EndsWith("/aspire-liveness", StringComparison.Ordinal));
        Assert.Contains("/contact/liveness", requestedPaths);
        Assert.Contains("/contact/readiness", requestedPaths);
        Assert.Contains("/inventory/liveness", requestedPaths);
        Assert.Contains("/inventory/readiness", requestedPaths);
        Assert.Contains("/predictionservice/liveness", requestedPaths);
        Assert.Contains("/predictionservice/readiness", requestedPaths);
    }

    [Fact]
    public async Task GetSystemHealth_WhenReadinessFails_MarksServiceUnhealthyNotUnreachable()
    {
        var probeService = CreateProbeService(request =>
        {
            if (request.RequestUri?.AbsolutePath.EndsWith("/readiness", StringComparison.Ordinal) == true)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    ReasonPhrase = "Service Unavailable",
                    Content = new StringContent("""{"status":"Unhealthy"}""")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var services = await probeService.CheckAllAsync(CancellationToken.None);

        Assert.All(services, service =>
        {
            Assert.Equal("Unhealthy", service.Status);
            Assert.Contains(service.ReadinessPath, service.ErrorMessage, StringComparison.Ordinal);
            Assert.Equal("""{"status":"Unhealthy"}""", service.ErrorBody);
        });
    }

    [Fact]
    public async Task GetSystemHealth_WhenTransientLivenessRequestFails_RetriesBeforeMarkingUnreachable()
    {
        var geometryLivenessAttempts = 0;
        var probeService = CreateProbeService(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/geometry/liveness" &&
                Interlocked.Increment(ref geometryLivenessAttempts) == 1)
            {
                throw new HttpRequestException("The response ended prematurely.");
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var services = await probeService.CheckAllAsync(CancellationToken.None);

        var geometry = Assert.Single(services, service => service.ServiceName == "GeometryService");
        Assert.Equal("Healthy", geometry.Status);
        Assert.Equal(2, geometryLivenessAttempts);
    }

    [Fact]
    public async Task GetSystemHealth_ThrottlesConcurrentServiceProbes()
    {
        var currentConcurrency = 0;
        var maxConcurrency = 0;
        var probeService = CreateProbeService(
            async (_, ct) =>
            {
                var active = Interlocked.Increment(ref currentConcurrency);
                while (true)
                {
                    var observed = Volatile.Read(ref maxConcurrency);
                    if (active <= observed ||
                        Interlocked.CompareExchange(ref maxConcurrency, active, observed) == observed)
                    {
                        break;
                    }
                }

                try
                {
                    await Task.Delay(25, ct);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
                finally
                {
                    Interlocked.Decrement(ref currentConcurrency);
                }
            },
            maxConcurrentProbes: 2);

        var services = await probeService.CheckAllAsync(CancellationToken.None);

        Assert.True(services.Count >= 30);
        Assert.True(maxConcurrency <= 2, $"Expected at most 2 concurrent probes, but observed {maxConcurrency}.");
    }

    [Fact]
    public async Task GetSystemHealth_UsesProbeServiceAndPreservesLiveResponseShape()
    {
        var probeService = new Mock<ISystemHealthProbeService>();
        probeService
            .Setup(x => x.CheckAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ServiceHealthStatus
                {
                    ServiceName = "AuthService",
                    DomainGroup = "Platform",
                    RoutePrefix = "auth",
                    Status = "Healthy",
                    IsCritical = true
                },
                new ServiceHealthStatus
                {
                    ServiceName = "InventoryService",
                    DomainGroup = "Operations",
                    RoutePrefix = "inventory",
                    Status = "Unhealthy",
                    IsCritical = false
                }
            ]);
        var historyService = new Mock<ISystemHealthHistoryService>();
        var controller = new SystemHealthController(probeService.Object, historyService.Object);

        var result = await controller.GetSystemHealth(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var health = Assert.IsType<SystemHealthDto>(okResult.Value);
        Assert.Equal("Degraded", health.OverallStatus);
        Assert.Equal(2, health.Services.Count);
    }

    [Fact]
    public async Task GetSystemHealthHistory_ReturnsHistoryFromHistoryService()
    {
        var probeService = new Mock<ISystemHealthProbeService>();
        var expected = new SystemHealthHistoryDto
        {
            BucketMinutes = 5,
            Services =
            [
                new SystemHealthHistoryServiceDto
                {
                    ServiceName = "AuthService",
                    DomainGroup = "Platform",
                    RoutePrefix = "auth",
                    CurrentStatus = "Healthy",
                    UptimePercentage = 100m
                }
            ]
        };
        var historyService = new Mock<ISystemHealthHistoryService>();
        historyService
            .Setup(x => x.GetHistoryAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new SystemHealthController(probeService.Object, historyService.Object);

        var result = await controller.GetSystemHealthHistory(7, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, okResult.Value);
    }

    private static SystemHealthProbeService CreateProbeService(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        return CreateProbeService(
            (request, _) => Task.FromResult(handler(request)));
    }

    private static SystemHealthProbeService CreateProbeService(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler,
        int? maxConcurrentProbes = null)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory
            .Setup(x => x.CreateClient("ServiceHealthCheck"))
            .Returns(() => new HttpClient(new MockHttpMessageHandler(handler)));

        var configurationBuilder = new ConfigurationBuilder();
        if (maxConcurrentProbes is not null)
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SystemHealth:MaxConcurrentProbes"] = maxConcurrentProbes.Value.ToString()
            });
        }

        var configuration = configurationBuilder.Build();
        return new SystemHealthProbeService(factory.Object, configuration);
    }
}
