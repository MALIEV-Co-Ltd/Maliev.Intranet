using System.Net;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class SystemHealthControllerTests
{
    [Fact]
    public async Task GetSystemHealth_UsesServiceLivenessAndReadinessEndpoints()
    {
        var requestedPaths = new List<string>();
        var controller = CreateController(request =>
        {
            requestedPaths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var result = await controller.GetSystemHealth(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var health = Assert.IsType<SystemHealthDto>(okResult.Value);
        Assert.All(health.Services, service =>
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
        var controller = CreateController(request =>
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

        var result = await controller.GetSystemHealth(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var health = Assert.IsType<SystemHealthDto>(okResult.Value);
        Assert.Equal("Unhealthy", health.OverallStatus);
        Assert.All(health.Services, service =>
        {
            Assert.Equal("Unhealthy", service.Status);
            Assert.Contains(service.ReadinessPath, service.ErrorMessage, StringComparison.Ordinal);
            Assert.Equal("""{"status":"Unhealthy"}""", service.ErrorBody);
        });
    }

    private static SystemHealthController CreateController(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory
            .Setup(x => x.CreateClient("ServiceHealthCheck"))
            .Returns(() => new HttpClient(new MockHttpMessageHandler((request, _) =>
                Task.FromResult(handler(request)))));

        var configuration = new ConfigurationBuilder().Build();
        return new SystemHealthController(factory.Object, configuration);
    }
}
