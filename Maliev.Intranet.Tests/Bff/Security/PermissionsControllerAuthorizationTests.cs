using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Security;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Bff.Hubs;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace Maliev.Intranet.Tests.Bff.Security;

public sealed class PermissionsControllerAuthorizationTests(
    PermissionCapabilityAuthorizationFactory factory)
    : IClassFixture<PermissionCapabilityAuthorizationFactory>
{
    private const string AuthenticationSchemes = "Bearer,Cookies";

    public static TheoryData<string, string, string> ActionRequirements => new()
    {
        { nameof(PermissionsController.GetAvailablePermissions), MalievPermissions.IAM.Permissions.List, "GET" },
        { nameof(PermissionsController.GetAvailableRoles), MalievPermissions.IAM.Roles.List, "GET" },
        { nameof(PermissionsController.GetUserAssignments), MalievPermissions.IAM.Bindings.List, "GET" },
        { nameof(PermissionsController.UpdateAssignments), MalievPermissions.IAM.Bindings.Create, "POST" }
    };

    public static TheoryData<string, string, string, string, HttpStatusCode> RuntimeRequirements => new()
    {
        {
            "GET",
            "/api/v1/permissions/available",
            MalievPermissions.IAM.Permissions.List,
            MalievPermissions.IAM.Roles.List,
            HttpStatusCode.OK
        },
        {
            "GET",
            "/api/v1/permissions/roles",
            MalievPermissions.IAM.Roles.List,
            MalievPermissions.IAM.Permissions.List,
            HttpStatusCode.OK
        },
        {
            "GET",
            $"/api/v1/permissions/users/{Guid.Parse("8b10a597-67ef-4c64-8516-c8284d5588bc"):D}",
            MalievPermissions.IAM.Bindings.List,
            MalievPermissions.IAM.Bindings.Create,
            HttpStatusCode.NotFound
        },
        {
            "POST",
            "/api/v1/permissions/assignments",
            MalievPermissions.IAM.Bindings.Create,
            MalievPermissions.IAM.Bindings.List,
            HttpStatusCode.OK
        }
    };

    [Fact]
    public void Controller_does_not_apply_a_shared_manage_permission()
    {
        Assert.Empty(
            typeof(PermissionsController)
                .GetCustomAttributes<RequirePermissionAttribute>(inherit: false));
    }

    [Theory]
    [MemberData(nameof(ActionRequirements))]
    public void Action_declares_its_canonical_forced_live_permission(
        string actionName,
        string permission,
        string _)
    {
        var action = Assert.Single(
            typeof(PermissionsController).GetMethods(),
            method => method.Name == actionName);
        var requirement = Assert.Single(
            action.GetCustomAttributes<RequirePermissionAttribute>(inherit: false));

        Assert.Equal(permission, requirement.Permission);
        Assert.Equal(AuthenticationSchemes, requirement.AuthenticationSchemes);
        Assert.True(requirement.RequireLiveCheck);
        Assert.Null(requirement.ResourcePathTemplate);
    }

    [Fact]
    public void Manifest_records_separate_live_capabilities_without_legacy_manage()
    {
        var endpointDataSource = factory.Services.GetRequiredService<EndpointDataSource>();
        var manifest = EndpointAuthorizationManifestBuilder.Build(
            endpointDataSource,
            EndpointAuthorizationManifestBuilder.IntranetHubs);

        foreach (var expected in ActionRequirements)
        {
            var actionName = (string)expected[0];
            var permission = (string)expected[1];
            var method = (string)expected[2];
            var entry = Assert.Single(
                manifest.Entries,
                candidate => candidate.Member == actionName && candidate.Methods.Contains(method));

            Assert.Equal(
                new PermissionAuthorizationManifest(permission, null, true),
                Assert.Single(entry.PermissionRequirements));
            Assert.DoesNotContain(MalievPermissions.Iam.Manage, entry.Permissions);
        }
    }

    [Theory]
    [MemberData(nameof(RuntimeRequirements))]
    public async Task Route_requires_its_own_live_capability_and_rejects_stale_claims(
        string method,
        string route,
        string requiredPermission,
        string unrelatedPermission,
        HttpStatusCode successStatus)
    {
        using var client = factory.CreateClient();

        factory.IamClient.Reset(requiredPermission);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken("live-capability-holder"));

        var allowed = await SendAsync(client, method, route);

        Assert.Equal(successStatus, allowed.StatusCode);
        Assert.Contains(
            new CapabilityPermissionCheck(requiredPermission, "global"),
            factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);

        factory.IamClient.Reset(unrelatedPermission);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken(
                "stale-capability-holder",
                requiredPermission,
                MalievPermissions.Iam.Manage));

        var denied = await SendAsync(client, method, route);

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Contains(
            new CapabilityPermissionCheck(requiredPermission, "global"),
            factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
    }

    [Fact]
    public async Task Assignment_mutation_fails_closed_when_live_iam_is_unavailable()
    {
        factory.IamClient.Reset(
            MalievPermissions.IAM.Bindings.Create,
            throwOnLiveCheck: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken(
                "stale-binding-creator",
                MalievPermissions.IAM.Bindings.Create,
                MalievPermissions.Iam.Manage));

        var response = await SendAsync(
            client,
            "POST",
            "/api/v1/permissions/assignments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(
            new CapabilityPermissionCheck(MalievPermissions.IAM.Bindings.Create, "global"),
            factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
    }

    private static Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        string method,
        string route)
    {
        if (method == "POST")
        {
            return client.PostAsJsonAsync(route, new
            {
                userId = "8b10a597-67ef-4c64-8516-c8284d5588bc",
                roles = Array.Empty<string>(),
                permissions = Array.Empty<string>()
            });
        }

        return client.GetAsync(route);
    }
}

public sealed class PermissionCapabilityAuthorizationFactory : SignalRTestFactory
{
    public RecordingCapabilityIamServiceClient IamClient { get; } = new();

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
                    var json = request.Method == HttpMethod.Get ? "[]" : "null";
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    });
                });
            });
        });
    }
}

public sealed record CapabilityPermissionCheck(string Permission, string? ResourcePath);

public sealed class RecordingCapabilityIamServiceClient : IIamServiceClient
{
    private readonly ConcurrentQueue<CapabilityPermissionCheck> _liveChecks = new();
    private readonly ConcurrentQueue<CapabilityPermissionCheck> _standardChecks = new();
    private string _allowedPermission = string.Empty;
    private bool _throwOnLiveCheck;

    public IReadOnlyCollection<CapabilityPermissionCheck> LiveChecks => _liveChecks.ToArray();
    public IReadOnlyCollection<CapabilityPermissionCheck> StandardChecks => _standardChecks.ToArray();

    public void Reset(string allowedPermission, bool throwOnLiveCheck = false)
    {
        _allowedPermission = allowedPermission;
        _throwOnLiveCheck = throwOnLiveCheck;
        _liveChecks.Clear();
        _standardChecks.Clear();
    }

    public Task<IEnumerable<string>> GetUserPermissionsAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<string>>([]);

    public Task<bool> CheckPermissionAsync(
        string principalId,
        string permissionId,
        string? resourcePath = null,
        CancellationToken cancellationToken = default)
    {
        _standardChecks.Enqueue(new CapabilityPermissionCheck(permissionId, resourcePath));
        return Task.FromResult(permissionId == _allowedPermission);
    }

    public Task<bool> CheckPermissionLiveAsync(
        string principalId,
        string permissionId,
        string? resourcePath = null,
        CancellationToken cancellationToken = default)
    {
        _liveChecks.Enqueue(new CapabilityPermissionCheck(permissionId, resourcePath));
        if (_throwOnLiveCheck)
        {
            throw new HttpRequestException("IAM is unavailable.");
        }

        return Task.FromResult(permissionId == _allowedPermission);
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
