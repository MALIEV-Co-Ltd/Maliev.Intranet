using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Security;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Bff.Hubs;
using Maliev.Aspire.ServiceDefaults.Authorization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Maliev.Intranet.Tests.Bff.Security;

public class EndpointAuthorizationManifestTests(SignalRTestFactory factory) : IClassFixture<SignalRTestFactory>
{
    private readonly EndpointDataSource _endpointDataSource = factory.Services.GetRequiredService<EndpointDataSource>();

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
            && entry.Permissions.Contains(MalievPermissions.Project.Read)
            && entry.Permissions.Contains(MalievPermissions.Project.Write));
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
            && entry.PermissionRequirements.All(requirement =>
                requirement.ResourcePathTemplate is null && !requirement.RequireLiveCheck)
            && entry.Ownership.Kind == ResourceOwnershipKind.NotDeclared);
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

    [Theory]
    [InlineData(nameof(ProjectsController.Get))]
    [InlineData(nameof(ProjectsController.Create))]
    public void Project_collection_routes_remain_unscoped(string actionName)
    {
        var action = Assert.Single(
            typeof(ProjectsController).GetMethods(),
            method => method.Name == actionName);

        Assert.Empty(action.GetCustomAttributes<ResourceOwnershipAttribute>(inherit: false));
        Assert.DoesNotContain(
            action.GetCustomAttributes<RequirePermissionAttribute>(inherit: false),
            requirement => requirement.ResourcePathTemplate is not null || requirement.RequireLiveCheck);
    }
}
