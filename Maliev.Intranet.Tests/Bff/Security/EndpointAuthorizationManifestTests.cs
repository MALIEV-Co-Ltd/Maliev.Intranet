using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Security;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Bff.Hubs;
using Maliev.Aspire.ServiceDefaults.Authorization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Maliev.Intranet.Tests.Bff.Security;

public class EndpointAuthorizationManifestTests(SignalRTestFactory factory) : IClassFixture<SignalRTestFactory>
{
    private readonly EndpointDataSource _endpointDataSource = factory.Services.GetRequiredService<EndpointDataSource>();

    [Theory]
    [InlineData(ResourceOwnershipKind.NotDeclared, 0)]
    [InlineData(ResourceOwnershipKind.BffValidated, 1)]
    [InlineData(ResourceOwnershipKind.DownstreamValidated, 2)]
    [InlineData(ResourceOwnershipKind.SignedCapability, 3)]
    [InlineData(ResourceOwnershipKind.GlobalPermission, 4)]
    public void ResourceOwnershipKind_PreservesStableNumericWireValues(
        ResourceOwnershipKind kind,
        int expectedValue)
    {
        var manifest = kind switch
        {
            ResourceOwnershipKind.NotDeclared or ResourceOwnershipKind.GlobalPermission =>
                new ResourceOwnershipManifest(kind, null, null),
            _ => new ResourceOwnershipManifest(kind, "authority", "resourceId")
        };
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(manifest));

        Assert.Equal(expectedValue, (int)kind);
        Assert.Equal(expectedValue, json.RootElement.GetProperty("Kind").GetInt32());
    }

    [Fact]
    public void ResourceOwnershipManifest_PreservesFormerClrConstructionSurface()
    {
        var constructor = Assert.Single(typeof(ResourceOwnershipManifest).GetConstructors());

        Assert.Equal(
            ["Kind", "Authority", "ResourceParameter"],
            constructor.GetParameters().Select(parameter => parameter.Name));
        Assert.All(
            new[]
            {
                nameof(ResourceOwnershipManifest.Kind),
                nameof(ResourceOwnershipManifest.Authority),
                nameof(ResourceOwnershipManifest.ResourceParameter)
            },
            propertyName =>
            {
                var property = typeof(ResourceOwnershipManifest).GetProperty(propertyName);
                Assert.NotNull(property?.SetMethod);
                Assert.Contains(
                    typeof(System.Runtime.CompilerServices.IsExternalInit),
                    property!.SetMethod!.ReturnParameter.GetRequiredCustomModifiers());
            });

        var deconstruct = typeof(ResourceOwnershipManifest).GetMethod(
            "Deconstruct",
            [
                typeof(ResourceOwnershipKind).MakeByRefType(),
                typeof(string).MakeByRefType(),
                typeof(string).MakeByRefType()
            ]);
        Assert.NotNull(deconstruct);
    }

    [Fact]
    public void ResourceOwnershipManifest_JsonRoundTripsValidStateAndRejectsInvalidState()
    {
        var source = new ResourceOwnershipManifest(
            ResourceOwnershipKind.DownstreamValidated,
            "UploadService",
            "fileId");
        var json = JsonSerializer.Serialize(source);

        Assert.Equal(source, JsonSerializer.Deserialize<ResourceOwnershipManifest>(json));
        Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<ResourceOwnershipManifest>(
            """{"Kind":2,"Authority":"UploadService","ResourceParameter":""}"""));
    }

    [Fact]
    public void ResourceOwnershipManifest_SupportsNamedConstructionDeconstructionAndSafeWithUpdates()
    {
        var source = new ResourceOwnershipManifest(
            Kind: ResourceOwnershipKind.DownstreamValidated,
            Authority: "UploadService",
            ResourceParameter: "fileId");

        var (kind, authority, resourceParameter) = source;
        var updated = source with
        {
            Kind = ResourceOwnershipKind.SignedCapability,
            Authority = "CapabilityService",
            ResourceParameter = "capabilityId"
        };

        Assert.Equal(ResourceOwnershipKind.DownstreamValidated, kind);
        Assert.Equal("UploadService", authority);
        Assert.Equal("fileId", resourceParameter);
        Assert.Equal(
            new ResourceOwnershipManifest(
                ResourceOwnershipKind.SignedCapability,
                "CapabilityService",
                "capabilityId"),
            updated);
    }

    [Fact]
    public void ResourceOwnershipManifest_WithCannotBypassInvariants()
    {
        var objectBound = new ResourceOwnershipManifest(
            ResourceOwnershipKind.BffValidated,
            "IAMService",
            "id");
        var global = new ResourceOwnershipManifest(
            ResourceOwnershipKind.GlobalPermission,
            null,
            null);

        Assert.Throws<ArgumentException>(() => objectBound with { Authority = " " });
        Assert.Throws<ArgumentException>(() => objectBound with { ResourceParameter = null });
        Assert.Throws<ArgumentException>(() => objectBound with { Kind = ResourceOwnershipKind.GlobalPermission });
        Assert.Throws<ArgumentException>(() => global with
        {
            Kind = ResourceOwnershipKind.BffValidated,
            Authority = "IAMService",
            ResourceParameter = "id"
        });
    }

    [Fact]
    public void ResourceOwnershipAttribute_OnlyAllowsGlobalPermissionWithoutObjectBinding()
    {
        var ownership = new ResourceOwnershipAttribute(ResourceOwnershipKind.GlobalPermission);

        Assert.Equal(ResourceOwnershipKind.GlobalPermission, ownership.Kind);
        Assert.Null(ownership.Authority);
        Assert.Null(ownership.ResourceParameter);
        Assert.Throws<ArgumentException>(() => new ResourceOwnershipAttribute(ResourceOwnershipKind.NotDeclared));
        Assert.Throws<ArgumentException>(() => new ResourceOwnershipAttribute(ResourceOwnershipKind.BffValidated));
    }

    [Theory]
    [InlineData(ResourceOwnershipKind.NotDeclared, "IAMService", "id")]
    [InlineData(ResourceOwnershipKind.GlobalPermission, "IAMService", "id")]
    [InlineData(ResourceOwnershipKind.BffValidated, "", "id")]
    [InlineData(ResourceOwnershipKind.BffValidated, "IAMService", " ")]
    [InlineData((ResourceOwnershipKind)99, "IAMService", "id")]
    public void ResourceOwnershipAttribute_RejectsInvalidObjectBinding(
        ResourceOwnershipKind kind,
        string authority,
        string resourceParameter)
    {
        Assert.Throws<ArgumentException>(() =>
            new ResourceOwnershipAttribute(kind, authority, resourceParameter));
    }

    [Theory]
    [InlineData(ResourceOwnershipKind.NotDeclared, "unexpected", null)]
    [InlineData(ResourceOwnershipKind.GlobalPermission, null, "unexpected")]
    [InlineData(ResourceOwnershipKind.BffValidated, null, "id")]
    [InlineData(ResourceOwnershipKind.DownstreamValidated, "UploadService", "")]
    [InlineData((ResourceOwnershipKind)99, null, null)]
    public void ResourceOwnershipManifest_RejectsInvalidDisposition(
        ResourceOwnershipKind kind,
        string? authority,
        string? resourceParameter)
    {
        Assert.Throws<ArgumentException>(() =>
            new ResourceOwnershipManifest(kind, authority, resourceParameter));
    }

    [Fact]
    public void Build_CoversEveryControllerAndHubSurfaceWithExplicitAuthorizationMetadata()
    {
        var manifest = EndpointAuthorizationManifestBuilder.Build(
            _endpointDataSource,
            EndpointAuthorizationManifestBuilder.IntranetHubs);

        Assert.NotEmpty(manifest.Entries);
        Assert.True(manifest.Entries.Count > 300);
        Assert.All(manifest.Entries, entry =>
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Surface));
            Assert.False(string.IsNullOrWhiteSpace(entry.Member));
            Assert.False(string.IsNullOrWhiteSpace(entry.Audience));
            Assert.False(string.IsNullOrWhiteSpace(entry.Authentication));
            Assert.False(string.IsNullOrWhiteSpace(entry.Authorization));
            Assert.False(string.IsNullOrWhiteSpace(entry.RateLimit));
            Assert.True(Enum.IsDefined(entry.Ownership.Kind));
            Assert.Equal("uncovered", entry.Coverage);
            Assert.NotEqual("UNCLASSIFIED", entry.Authorization);
        });
    }

    [Fact]
    public void Build_RecordsProtectedManifestEndpointAndOwnedHubSubscriptions()
    {
        var manifest = EndpointAuthorizationManifestBuilder.Build(
            _endpointDataSource,
            EndpointAuthorizationManifestBuilder.IntranetHubs);

        Assert.Contains(manifest.Entries, entry =>
            entry.Member == nameof(AuthorizationManifestController.Get)
            && entry.Permissions.Contains(MalievPermissions.IAM.Permissions.Read));
        Assert.Contains(manifest.Entries, entry =>
            entry.Surface == "/hubs/chat"
            && entry.Member == nameof(ChatHub.JoinSession)
            && entry.Ownership == new ResourceOwnershipManifest(
                ResourceOwnershipKind.DownstreamValidated, "ChatbotService", "sessionId"));
        Assert.Contains(manifest.Entries, entry =>
            entry.Surface == "/hubs/notifications"
            && entry.Member == nameof(NotificationHub.JoinFileGroup)
            && entry.Ownership == new ResourceOwnershipManifest(
                ResourceOwnershipKind.DownstreamValidated, "UploadService", "fileId"));
        Assert.Contains(manifest.Entries, entry =>
            entry.Member == nameof(ProjectsController.Create)
            && entry.Permissions.SequenceEqual([MalievPermissions.Project.Write]));
        Assert.Contains(manifest.Entries, entry =>
            entry.Member == nameof(ProjectsController.GetById)
            && entry.PermissionRequirements.Contains(new PermissionAuthorizationManifest(
                MalievPermissions.Project.Read,
                "projects/{id}",
                true))
            && entry.Ownership == new ResourceOwnershipManifest(
                ResourceOwnershipKind.BffValidated,
                "IAMService",
                "id"));
        Assert.Contains(manifest.Entries, entry =>
            entry.Member == nameof(ProjectsController.Update)
            && entry.PermissionRequirements.Contains(new PermissionAuthorizationManifest(
                MalievPermissions.Project.Write,
                "projects/{id}",
                true)));
        Assert.Contains(manifest.Entries, entry =>
            entry.Member == nameof(ProjectsController.Get)
            && entry.PermissionRequirements.Contains(new PermissionAuthorizationManifest(
                MalievPermissions.Project.Read,
                null,
                true))
            && entry.Ownership == new ResourceOwnershipManifest(
                ResourceOwnershipKind.GlobalPermission,
                null,
                null));
    }

    [Fact]
    public void Build_RecordsProductionCollectionReadsAsAuthoritativeGlobalPermissions()
    {
        var manifest = EndpointAuthorizationManifestBuilder.Build(
            _endpointDataSource,
            EndpointAuthorizationManifestBuilder.IntranetHubs);
        var expectedMembers = new[]
        {
            nameof(JobsController.GetQueue),
            nameof(JobsController.GetStats),
            nameof(JobsController.Get),
            nameof(JobsController.GetMachineSchedule),
            nameof(JobsController.GetAllMachineSchedules),
            "<connect>"
        };

        foreach (var member in expectedMembers)
        {
            var entry = Assert.Single(
                manifest.Entries,
                candidate => candidate.Member == member
                    && (member == "<connect>"
                        ? candidate.Surface == "/hubs/production"
                        : candidate.Surface.StartsWith(
                            "/api/v{version:apiVersion}/Jobs",
                            StringComparison.OrdinalIgnoreCase)));
            var requirement = Assert.Single(entry.PermissionRequirements);

            Assert.Equal(MalievPermissions.Job.Read, requirement.Permission);
            Assert.Null(requirement.ResourcePathTemplate);
            Assert.True(requirement.RequireLiveCheck);
            Assert.Equal("GlobalPermission", entry.Ownership.Kind.ToString());
            Assert.Null(entry.Ownership.Authority);
            Assert.Null(entry.Ownership.ResourceParameter);
        }
    }

    [Fact]
    public void Build_RecordsDashboardReadsAsAuthoritativeGlobalPermissions()
    {
        var manifest = EndpointAuthorizationManifestBuilder.Build(
            _endpointDataSource,
            EndpointAuthorizationManifestBuilder.IntranetHubs);
        var expectedMembers = new[]
        {
            nameof(DashboardController.Get),
            nameof(DashboardController.GetActionItems)
        };

        foreach (var member in expectedMembers)
        {
            var entry = Assert.Single(
                manifest.Entries,
                candidate => candidate.Member == member
                    && candidate.Surface.StartsWith(
                        "/api/v{version:apiVersion}/Dashboard",
                        StringComparison.OrdinalIgnoreCase));
            var requirement = Assert.Single(entry.PermissionRequirements);

            Assert.Equal(MalievPermissions.Dashboard.View, requirement.Permission);
            Assert.Null(requirement.ResourcePathTemplate);
            Assert.True(requirement.RequireLiveCheck);
            Assert.Equal(ResourceOwnershipKind.GlobalPermission, entry.Ownership.Kind);
            Assert.Null(entry.Ownership.Authority);
            Assert.Null(entry.Ownership.ResourceParameter);
        }
    }

    [Fact]
    public void Build_RecordsEmployeeDirectoryReadAsAuthoritativeGlobalPermission()
    {
        var manifest = EndpointAuthorizationManifestBuilder.Build(
            _endpointDataSource,
            EndpointAuthorizationManifestBuilder.IntranetHubs);
        var entry = Assert.Single(
            manifest.Entries,
            candidate => candidate.Member == nameof(EmployeesController.Get)
                && candidate.Surface.StartsWith(
                    "/api/v{version:apiVersion}/Employees",
                    StringComparison.OrdinalIgnoreCase));
        var requirement = Assert.Single(entry.PermissionRequirements);

        Assert.Equal(MalievPermissions.Employee.Read, requirement.Permission);
        Assert.Null(requirement.ResourcePathTemplate);
        Assert.True(requirement.RequireLiveCheck);
        Assert.Equal(ResourceOwnershipKind.GlobalPermission, entry.Ownership.Kind);
        Assert.Null(entry.Ownership.Authority);
        Assert.Null(entry.Ownership.ResourceParameter);
    }

    [Fact]
    public async Task Endpoint_RequiresPermissionAndReturnsRuntimeManifest()
    {
        var unauthorizedClient = factory.CreateClient();
        unauthorizedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken("manifest-no-read"));
        var forbidden = await unauthorizedClient.GetAsync("/api/v1/system/authorization-manifest");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var authorizedClient = factory.CreateClient();
        authorizedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken("manifest-reader", MalievPermissions.IAM.Permissions.Read));
        var response = await authorizedClient.GetAsync("/api/v1/system/authorization-manifest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var manifest = await response.Content.ReadFromJsonAsync<EndpointAuthorizationManifest>();
        Assert.NotNull(manifest);
        Assert.Contains(manifest.Entries, entry => entry.Member == nameof(AuthorizationManifestController.Get));
    }

    [Theory]
    [InlineData(nameof(ProjectsController.GetById), MalievPermissions.Project.Read)]
    [InlineData(nameof(ProjectsController.GetPartLargeThumbnailUrl), MalievPermissions.Project.Read)]
    [InlineData(nameof(ProjectsController.GetProductionPlan), MalievPermissions.Project.Read)]
    [InlineData(nameof(ProjectsController.GetPartRouting), MalievPermissions.Project.Read)]
    [InlineData(nameof(ProjectsController.AddNote), MalievPermissions.Project.Write)]
    [InlineData(nameof(ProjectsController.Duplicate), MalievPermissions.Project.Write)]
    [InlineData(nameof(ProjectsController.Update), MalievPermissions.Project.Write)]
    [InlineData(nameof(ProjectsController.Delete), MalievPermissions.Project.Write)]
    [InlineData(nameof(ProjectsController.AddPart), MalievPermissions.Project.Write)]
    [InlineData(nameof(ProjectsController.UpdatePart), MalievPermissions.Project.Write)]
    [InlineData(nameof(ProjectsController.DeletePart), MalievPermissions.Project.Write)]
    [InlineData(nameof(ProjectsController.GetPartPrice), MalievPermissions.Project.Write)]
    [InlineData(nameof(ProjectsController.ConfirmPartPrice), MalievPermissions.Project.Write)]
    [InlineData(nameof(ProjectsController.GenerateQuotation), MalievPermissions.Project.Write)]
    [InlineData(nameof(ProjectsController.AcceptQuotation), MalievPermissions.Project.Write)]
    [InlineData(nameof(ProjectsController.CreatePlanningHold), MalievPermissions.Job.Write)]
    [InlineData(nameof(ProjectsController.UpdatePlanningHold), MalievPermissions.Job.Write)]
    [InlineData(nameof(ProjectsController.CancelPlanningHold), MalievPermissions.Job.Write)]
    public void Project_resource_routes_require_authoritative_scoped_permission(
        string actionName,
        string permission)
    {
        var action = Assert.Single(
            typeof(ProjectsController).GetMethods(),
            method => method.Name == actionName);
        var requirement = Assert.Single(action.GetCustomAttributes<RequirePermissionAttribute>(inherit: false));
        var ownership = Assert.IsType<ResourceOwnershipAttribute>(
            Assert.Single(action.GetCustomAttributes<ResourceOwnershipAttribute>(inherit: false)));

        Assert.Equal(permission, requirement.Permission);
        Assert.Equal("projects/{id}", requirement.ResourcePathTemplate);
        Assert.True(requirement.RequireLiveCheck);
        Assert.Equal(ResourceOwnershipKind.BffValidated, ownership.Kind);
        if (actionName == nameof(ProjectsController.CreatePlanningHold))
        {
            Assert.Equal("ProjectService+IAMService", ownership.Authority);
            Assert.Equal("id,partId", ownership.ResourceParameter);
        }
        else if (actionName is nameof(ProjectsController.UpdatePlanningHold) or nameof(ProjectsController.CancelPlanningHold))
        {
            Assert.Equal("JobService+IAMService", ownership.Authority);
            Assert.Equal("id,holdId", ownership.ResourceParameter);
        }
        else
        {
            Assert.Equal("IAMService", ownership.Authority);
            Assert.Equal("id", ownership.ResourceParameter);
        }
    }

    [Fact]
    public void Project_create_route_remains_unscoped()
    {
        var action = Assert.Single(
            typeof(ProjectsController).GetMethods(),
            method => method.Name == nameof(ProjectsController.Create));

        Assert.Empty(action.GetCustomAttributes<ResourceOwnershipAttribute>(inherit: false));
        Assert.DoesNotContain(
            action.GetCustomAttributes<RequirePermissionAttribute>(inherit: false),
            requirement => requirement.ResourcePathTemplate is not null || requirement.RequireLiveCheck);
    }
}
