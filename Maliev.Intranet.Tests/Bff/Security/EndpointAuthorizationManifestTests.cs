using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Security;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Bff.Hubs;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

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
}
