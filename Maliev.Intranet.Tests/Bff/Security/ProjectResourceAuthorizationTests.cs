using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.Intranet.Bff.Security;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Bff.Hubs;
using Maliev.Intranet.Tests.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace Maliev.Intranet.Tests.Bff.Security;

public sealed class ProjectResourceAuthorizationTests(
    ProjectResourceAuthorizationFactory factory) : IClassFixture<ProjectResourceAuthorizationFactory>
{
    [Fact]
    public async Task Project_update_requires_live_grant_for_the_route_project()
    {
        var allowedProjectId = Guid.NewGuid();
        var deniedProjectId = Guid.NewGuid();
        factory.IamClient.Reset(
            MalievPermissions.Project.Write,
            $"projects/{allowedProjectId:D}");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken(
                "project-editor",
                MalievPermissions.Project.Read,
                MalievPermissions.Project.Write));

        var denied = await client.PutAsJsonAsync($"/api/v1/projects/{deniedProjectId:D}", new { title = "Denied" });
        var allowed = await client.PutAsJsonAsync($"/api/v1/projects/{allowedProjectId:D}", new { title = "Allowed" });

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
        Assert.Contains(
            factory.IamClient.LiveChecks,
            check => check == new PermissionCheck(
                MalievPermissions.Project.Write,
                $"projects/{deniedProjectId:D}"));
        Assert.Contains(
            factory.IamClient.LiveChecks,
            check => check == new PermissionCheck(
                MalievPermissions.Project.Write,
                $"projects/{allowedProjectId:D}"));
    }

    [Fact]
    public async Task Project_detail_requires_live_read_grant_for_the_route_project()
    {
        var allowedProjectId = Guid.NewGuid();
        var deniedProjectId = Guid.NewGuid();
        factory.IamClient.Reset(
            MalievPermissions.Project.Read,
            $"projects/{allowedProjectId:D}");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken("project-reader", MalievPermissions.Project.Read));

        var denied = await client.GetAsync($"/api/v1/projects/{deniedProjectId:D}");
        var allowed = await client.GetAsync($"/api/v1/projects/{allowedProjectId:D}");

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, allowed.StatusCode);
        Assert.Contains(
            factory.IamClient.LiveChecks,
            check => check == new PermissionCheck(
                MalievPermissions.Project.Read,
                $"projects/{deniedProjectId:D}"));
        Assert.Contains(
            factory.IamClient.LiveChecks,
            check => check == new PermissionCheck(
                MalievPermissions.Project.Read,
                $"projects/{allowedProjectId:D}"));
    }

    [Fact]
    public async Task Project_detail_fails_closed_when_live_iam_check_throws()
    {
        var projectId = Guid.NewGuid();
        factory.IamClient.Reset(
            MalievPermissions.Project.Read,
            $"projects/{projectId:D}",
            throwOnLiveCheck: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken("stale-project-reader", MalievPermissions.Project.Read));

        var response = await client.GetAsync($"/api/v1/projects/{projectId:D}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(
            factory.IamClient.LiveChecks,
            check => check == new PermissionCheck(
                MalievPermissions.Project.Read,
                $"projects/{projectId:D}"));
    }

    [Fact]
    public void Authorization_configuration_rejects_resource_scope_with_iam_fail_open()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:ResourceScopedAuthEnabled"] = "true",
                ["Features:FailOpenOnIAMError"] = "true"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            AuthorizationSafetyConfiguration.EnsureSafe(configuration));

        Assert.Contains("must remain false", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public void Authorization_configuration_requires_resource_scope(string? configuredValue)
    {
        var values = new Dictionary<string, string?>();
        if (configuredValue is not null)
        {
            values["Features:ResourceScopedAuthEnabled"] = configuredValue;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            AuthorizationSafetyConfiguration.EnsureSafe(configuration));

        Assert.Contains("must be true", exception.Message, StringComparison.Ordinal);
    }
}

public sealed class ProjectResourceAuthorizationFactory : SignalRTestFactory
{
    public RecordingIamServiceClient IamClient { get; } = new();

    protected override void ConfigureAdditionalServices(IServiceCollection services)
    {
        base.ConfigureAdditionalServices(services);
        services.RemoveAll<IIamServiceClient>();
        services.AddSingleton<IIamServiceClient>(IamClient);
        services.PostConfigureAll<HttpClientFactoryOptions>(options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(httpBuilder =>
            {
                httpBuilder.PrimaryHandler = new MockHttpMessageHandler((request, _) =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    if (request.Method == HttpMethod.Get)
                    {
                        response.Content = new StringContent("null", Encoding.UTF8, "application/json");
                    }

                    return Task.FromResult(response);
                });
            });
        });
    }
}

public sealed record PermissionCheck(string Permission, string? ResourcePath);

public sealed class RecordingIamServiceClient : IIamServiceClient
{
    private readonly ConcurrentQueue<PermissionCheck> _liveChecks = new();
    private string _allowedPermission = string.Empty;
    private string _allowedResourcePath = string.Empty;
    private bool _throwOnLiveCheck;

    public IReadOnlyCollection<PermissionCheck> LiveChecks => _liveChecks.ToArray();

    public void Reset(
        string allowedPermission,
        string allowedResourcePath,
        bool throwOnLiveCheck = false)
    {
        _allowedPermission = allowedPermission;
        _allowedResourcePath = allowedResourcePath;
        _throwOnLiveCheck = throwOnLiveCheck;
        _liveChecks.Clear();
    }

    public Task<IEnumerable<string>> GetUserPermissionsAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<string>>([]);

    public Task<bool> CheckPermissionAsync(
        string principalId,
        string permissionId,
        string? resourcePath = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(permissionId == MalievPermissions.Project.Read);

    public Task<bool> CheckPermissionLiveAsync(
        string principalId,
        string permissionId,
        string? resourcePath = null,
        CancellationToken cancellationToken = default)
    {
        _liveChecks.Enqueue(new PermissionCheck(permissionId, resourcePath));
        if (_throwOnLiveCheck)
        {
            throw new HttpRequestException("IAM is unavailable.");
        }

        return Task.FromResult(
            permissionId == _allowedPermission &&
            string.Equals(resourcePath, _allowedResourcePath, StringComparison.Ordinal));
    }

    public Task<Dictionary<string, bool>> CheckPermissionsAsync(
        string principalId,
        IEnumerable<PermissionCheckRequest> requests,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new Dictionary<string, bool>(StringComparer.Ordinal));

    public Task<IEnumerable<string>> GetAuthorizedResourcesAsync(
        string principalId,
        string permissionId,
        string resourceType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<string>>([]);
}
