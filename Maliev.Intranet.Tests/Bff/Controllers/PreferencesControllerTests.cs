using System.Reflection;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public sealed class PreferencesControllerTests
{
    [Theory]
    [InlineData(nameof(PreferencesController.GetPreference), MalievPermissions.Employee.ProfileRead)]
    [InlineData(nameof(PreferencesController.UpsertPreference), MalievPermissions.Employee.ProfileUpdate)]
    [InlineData(nameof(PreferencesController.DeletePreference), MalievPermissions.Employee.ProfileUpdate)]
    public void PreferenceEndpoints_RequireEmployeeProfilePermissions(string actionName, string expectedPermission)
    {
        var method = typeof(PreferencesController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(candidate => candidate.Name == actionName);

        var attribute = Assert.Single(method.GetCustomAttributes<RequirePermissionAttribute>());

        Assert.Contains(expectedPermission, attribute.Policy, StringComparison.Ordinal);
        Assert.Equal("Bearer,Cookies", attribute.AuthenticationSchemes);
    }
}
