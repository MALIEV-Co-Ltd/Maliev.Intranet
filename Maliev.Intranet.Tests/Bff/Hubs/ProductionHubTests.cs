using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Hubs;
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
        Assert.Equal($"Permission:{MalievPermissions.Job.Read}", attribute.Policy);
    }
}
