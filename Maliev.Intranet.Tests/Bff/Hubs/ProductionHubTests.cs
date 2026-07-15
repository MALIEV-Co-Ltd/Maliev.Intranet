using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Security;
using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Tests.Bff.Hubs;

public class ProductionHubTests
{
    [Fact]
    public void ProductionHub_ShouldRequireJobReadPermission()
    {
        var attribute = Assert.Single(
            typeof(ProductionHub).GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: true)
                .Cast<RequirePermissionAttribute>());

        Assert.Equal(MalievPermissions.Job.Read, attribute.Permission);
        Assert.Equal("Bearer,Cookies", attribute.AuthenticationSchemes);
        Assert.Equal($"Permission:{MalievPermissions.Job.Read}:live_check", attribute.Policy);
        Assert.True(attribute.RequireLiveCheck);
        Assert.Null(attribute.ResourcePathTemplate);

        var ownership = Assert.Single(
            typeof(ProductionHub).GetCustomAttributes(typeof(ResourceOwnershipAttribute), inherit: true)
                .Cast<ResourceOwnershipAttribute>());
        Assert.Equal("GlobalPermission", ownership.Kind.ToString());
        Assert.Null(ownership.Authority);
        Assert.Null(ownership.ResourceParameter);
    }

    [Fact]
    public void ProductionHub_DoesNotExposeClientCallableBroadcastMethods()
    {
        var browserMethods = typeof(ProductionHub)
            .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.DeclaredOnly)
            .Select(method => method.Name);

        Assert.DoesNotContain("NotifyJobStatusChanged", browserMethods);
        Assert.DoesNotContain("NotifyJobAssigned", browserMethods);
        Assert.DoesNotContain("NotifyStatsUpdated", browserMethods);
        Assert.DoesNotContain("NotifyScheduleChanged", browserMethods);
    }
}
