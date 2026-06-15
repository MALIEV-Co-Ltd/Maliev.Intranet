using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class SeedRealtimeSourceTests
{
    [Fact]
    public void SeedController_RequiresCustomerWritePermission()
    {
        Assert.Empty(typeof(SeedController).GetCustomAttributes<AllowAnonymousAttribute>(inherit: false));

        var method = typeof(SeedController).GetMethod(nameof(SeedController.SeedCustomers))!;
        var attribute = Assert.Single(method.GetCustomAttributes<RequirePermissionAttribute>(inherit: false));

        Assert.Equal(MalievPermissions.Customer.Write, attribute.Permission);
        Assert.Equal("Bearer,Cookies", attribute.AuthenticationSchemes);
    }

    [Fact]
    public void SeedController_BroadcastsCustomerChangedAfterCustomerSeed()
    {
        var source = ReadRepoFile("Maliev.Intranet.Bff", "Controllers", "SeedController.cs");

        Assert.Contains("IHubContext<NotificationHub>", source);
        Assert.Contains("CustomerChanged", source);
        Assert.Contains("SendAsync", source);
    }

    [Fact]
    public async Task SeedCustomers_ProductionEnvironment_ReturnsNotFoundBeforeCreatingSeedClients()
    {
        var httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var hubContext = new Mock<IHubContext<NotificationHub>>(MockBehavior.Strict);
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns("Production");
        var controller = new SeedController(
            httpClientFactory.Object,
            hubContext.Object,
            NullLogger<SeedController>.Instance,
            environment.Object);

        var result = await controller.SeedCustomers(CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        httpClientFactory.VerifyNoOtherCalls();
        hubContext.VerifyNoOtherCalls();
    }

    private static string ReadRepoFile(params string[] relativeParts)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Unable to locate {Path.Combine(relativeParts)} from {AppContext.BaseDirectory}.");
    }
}
