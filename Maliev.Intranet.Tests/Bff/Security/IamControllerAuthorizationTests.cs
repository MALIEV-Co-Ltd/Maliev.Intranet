using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Security;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Bff.Hubs;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace Maliev.Intranet.Tests.Bff.Security;

public sealed class IamControllerAuthorizationTests(IamConsoleAuthorizationFactory factory)
    : IClassFixture<IamConsoleAuthorizationFactory>
{
    private static readonly Guid PrincipalId = Guid.Parse("8b10a597-67ef-4c64-8516-c8284d5588bc");
    private static readonly Guid BindingId = Guid.Parse("405be3d2-8533-4ac5-92f8-7af9f7298a44");
    private const string AuthenticationSchemes = "Bearer,Cookies";

    public static TheoryData<string, string, string, string, HttpStatusCode> ActionRequirements => new()
    {
        { nameof(IamController.GetUsers), "GET", "/api/v1/iam/users", MalievPermissions.IAM.Principals.List, HttpStatusCode.OK },
        { nameof(IamController.GetUser), "GET", $"/api/v1/iam/users/{PrincipalId:D}", MalievPermissions.IAM.Principals.Read, HttpStatusCode.NotFound },
        { nameof(IamController.GetRoles), "GET", "/api/v1/iam/roles", MalievPermissions.IAM.Roles.List, HttpStatusCode.OK },
        { nameof(IamController.GetRolesPaged), "GET", "/api/v1/iam/roles/paged", MalievPermissions.IAM.Roles.List, HttpStatusCode.OK },
        { nameof(IamController.GetPermissions), "GET", "/api/v1/iam/permissions", MalievPermissions.IAM.Permissions.List, HttpStatusCode.OK },
        { nameof(IamController.GetPermissionsPaged), "GET", "/api/v1/iam/permissions/paged", MalievPermissions.IAM.Permissions.List, HttpStatusCode.OK },
        { nameof(IamController.GetUserRoles), "GET", $"/api/v1/iam/users/{PrincipalId:D}/roles", MalievPermissions.IAM.Bindings.List, HttpStatusCode.OK },
        { nameof(IamController.GrantRole), "POST", $"/api/v1/iam/users/{PrincipalId:D}/roles", MalievPermissions.IAM.Bindings.Create, HttpStatusCode.OK },
        { nameof(IamController.RevokeRole), "DELETE", $"/api/v1/iam/users/{PrincipalId:D}/roles/{BindingId:D}", MalievPermissions.IAM.Bindings.Delete, HttpStatusCode.OK },
        { nameof(IamController.InviteUser), "POST", "/api/v1/iam/users/invite", MalievPermissions.IAM.Principals.Create, HttpStatusCode.Created },
        { nameof(IamController.PatchUser), "PATCH", $"/api/v1/iam/users/{PrincipalId:D}", MalievPermissions.IAM.Principals.Update, HttpStatusCode.Accepted },
        { nameof(IamController.GetUserActivity), "GET", $"/api/v1/iam/users/{PrincipalId:D}/activity", MalievPermissions.IAM.Audit.List, HttpStatusCode.OK },
        { nameof(IamController.GetRolePermissionsMatrix), "GET", "/api/v1/iam/roles/roles.iam.viewer/permissions-matrix", MalievPermissions.IAM.Roles.Read, HttpStatusCode.NotFound }
    };

    public static TheoryData<string, string, string> MutationRequirements => new()
    {
        { "POST", $"/api/v1/iam/users/{PrincipalId:D}/roles", MalievPermissions.IAM.Bindings.Create },
        { "DELETE", $"/api/v1/iam/users/{PrincipalId:D}/roles/{BindingId:D}", MalievPermissions.IAM.Bindings.Delete },
        { "POST", "/api/v1/iam/users/invite", MalievPermissions.IAM.Principals.Create },
        { "PATCH", $"/api/v1/iam/users/{PrincipalId:D}", MalievPermissions.IAM.Principals.Update }
    };

    [Fact]
    public void Controller_keeps_only_the_authorization_service_needed_for_compound_invites()
    {
        var constructor = Assert.Single(typeof(IamController).GetConstructors());
        var parameterTypes = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

        Assert.Equal([typeof(IAMServiceClient), typeof(IAuthorizationService)], parameterTypes);
        Assert.Null(typeof(IamController).GetMethod("IsAuthorizedAsync", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.DoesNotContain(parameterTypes, type => type.FullName == "Microsoft.AspNetCore.Hosting.IWebHostEnvironment");
    }

    [Theory]
    [MemberData(nameof(ActionRequirements))]
    public void Action_declares_its_exact_canonical_forced_live_permission(
        string actionName,
        string httpMethod,
        string route,
        string permission,
        HttpStatusCode successStatus)
    {
        Assert.NotEmpty(httpMethod);
        Assert.StartsWith("/api/v1/iam/", route, StringComparison.Ordinal);
        Assert.True(Enum.IsDefined(successStatus));
        var action = GetAction(actionName);
        var requirement = Assert.Single(
            action.GetCustomAttributes<RequirePermissionAttribute>(inherit: false));

        Assert.Equal(permission, requirement.Permission);
        Assert.Equal(AuthenticationSchemes, requirement.AuthenticationSchemes);
        Assert.True(requirement.RequireLiveCheck);
        Assert.Null(requirement.ResourcePathTemplate);

        var bffRequirement = action.GetCustomAttributes<BffEnforcedPermissionAttribute>(inherit: false).ToArray();
        if (actionName == nameof(IamController.InviteUser))
        {
            var bindingRequirement = Assert.Single(bffRequirement);
            Assert.Equal(MalievPermissions.IAM.Bindings.Create, bindingRequirement.Permission);
            Assert.Equal("global", bindingRequirement.ResourcePathTemplate);
            Assert.True(bindingRequirement.RequireLiveCheck);
        }
        else
        {
            Assert.Empty(bffRequirement);
        }
    }

    [Fact]
    public void Manifest_records_the_exact_live_capabilities_for_every_iam_action()
    {
        var manifest = EndpointAuthorizationManifestBuilder.Build(
            factory.Services.GetRequiredService<EndpointDataSource>(),
            EndpointAuthorizationManifestBuilder.IntranetHubs);

        foreach (var expected in ActionRequirements)
        {
            var actionName = (string)expected[0];
            var method = (string)expected[1];
            var permission = (string)expected[3];
            var entry = Assert.Single(
                manifest.Entries,
                candidate => candidate.Member == actionName
                    && candidate.Methods.Contains(method)
                    && candidate.Surface.StartsWith(
                        "/api/v{version:apiVersion}/Iam",
                        StringComparison.OrdinalIgnoreCase));
            var expectedRequirements = ExpectedPermissions(actionName, permission)
                .Select(value => new PermissionAuthorizationManifest(value, value == MalievPermissions.IAM.Bindings.Create && actionName == nameof(IamController.InviteUser) ? "global" : null, true))
                .OrderBy(requirement => requirement.Permission, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(
                expectedRequirements,
                entry.PermissionRequirements.OrderBy(requirement => requirement.Permission, StringComparer.Ordinal));
        }
    }

    [Theory]
    [MemberData(nameof(ActionRequirements))]
    public async Task Route_uses_only_its_expected_live_capabilities_when_authorized(
        string actionName,
        string method,
        string route,
        string permission,
        HttpStatusCode successStatus)
    {
        var expectedPermissions = ExpectedPermissions(actionName, permission);
        factory.Reset(expectedPermissions);
        using var client = CreateClient("live-iam-console-user");

        var response = await SendAsync(client, method, route);

        Assert.Equal(successStatus, response.StatusCode);
        Assert.Equal(
            expectedPermissions.Order(StringComparer.Ordinal),
            factory.IamClient.LiveChecks.Select(check => check.Permission).Order(StringComparer.Ordinal));
        Assert.Empty(factory.IamClient.StandardChecks);
    }

    [Theory]
    [MemberData(nameof(ActionRequirements))]
    public async Task Route_rejects_stale_permission_claims_when_live_iam_denies(
        string actionName,
        string method,
        string route,
        string permission,
        HttpStatusCode _)
    {
        var stalePermissions = ExpectedPermissions(actionName, permission);
        var unrelatedPermission = permission == MalievPermissions.IAM.Bindings.Create
            ? MalievPermissions.IAM.Bindings.List
            : MalievPermissions.IAM.Bindings.Create;
        factory.Reset([unrelatedPermission]);
        using var client = CreateClient("stale-iam-console-user", stalePermissions);

        var response = await SendAsync(client, method, route);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            [new IamConsolePermissionCheck(permission, "global")],
            factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
    }

    [Theory]
    [MemberData(nameof(ActionRequirements))]
    public async Task Route_fails_closed_when_its_live_iam_check_is_unavailable(
        string actionName,
        string method,
        string route,
        string permission,
        HttpStatusCode _)
    {
        var stalePermissions = ExpectedPermissions(actionName, permission);
        factory.Reset([], [permission]);
        using var client = CreateClient("stale-iam-console-user", stalePermissions);

        var response = await SendAsync(client, method, route);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            [new IamConsolePermissionCheck(permission, "global")],
            factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
    }

    [Theory]
    [MemberData(nameof(MutationRequirements))]
    public async Task Mutation_fails_closed_when_its_live_iam_check_is_unavailable(
        string method,
        string route,
        string permission)
    {
        factory.Reset([], [permission]);
        using var client = CreateClient("stale-iam-mutator", permission);

        var response = await SendAsync(client, method, route);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            [new IamConsolePermissionCheck(permission, "global")],
            factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Invite_does_not_create_a_principal_when_binding_authorization_fails(bool iamUnavailable)
    {
        factory.Reset(
            [MalievPermissions.IAM.Principals.Create],
            iamUnavailable ? [MalievPermissions.IAM.Bindings.Create] : []);
        using var client = CreateClient(
            "stale-iam-inviter",
            MalievPermissions.IAM.Principals.Create,
            MalievPermissions.IAM.Bindings.Create);

        var response = await SendAsync(client, "POST", "/api/v1/iam/users/invite");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(
            new IamConsolePermissionCheck(MalievPermissions.IAM.Principals.Create, "global"),
            factory.IamClient.LiveChecks);
        Assert.Contains(
            new IamConsolePermissionCheck(MalievPermissions.IAM.Bindings.Create, "global"),
            factory.IamClient.LiveChecks);
        Assert.DoesNotContain(
            factory.DownstreamRequests,
            request => request.Method == "POST" && request.Path == "/iam/v1/principals");
        Assert.Empty(factory.IamClient.StandardChecks);
    }

    [Fact]
    public async Task Invite_uses_the_canonical_global_binding_request_and_requires_binding_success()
    {
        factory.Reset([
            MalievPermissions.IAM.Principals.Create,
            MalievPermissions.IAM.Bindings.Create
        ]);
        using var client = CreateClient(
            "iam-inviter",
            MalievPermissions.IAM.Principals.Create,
            MalievPermissions.IAM.Bindings.Create);

        var response = await SendAsync(client, "POST", "/api/v1/iam/users/invite");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var bindingRequest = Assert.Single(
            factory.DownstreamRequests,
            request => request.Method == "POST" && request.Path.EndsWith("/roles", StringComparison.Ordinal));
        using var json = JsonDocument.Parse(Assert.IsType<string>(bindingRequest.Body));
        Assert.Equal(3, json.RootElement.EnumerateObject().Count());
        Assert.Equal("roles.iam.viewer", json.RootElement.GetProperty("roleId").GetString());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("resourcePath").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("expiresAt").ValueKind);
        Assert.False(json.RootElement.TryGetProperty("roleName", out _));
    }

    [Fact]
    public async Task GrantRole_InvalidModel_IsRejectedBeforeIamCall()
    {
        factory.Reset([MalievPermissions.IAM.Bindings.Create]);
        using var client = CreateClient("iam-binding-validator", MalievPermissions.IAM.Bindings.Create);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/iam/users/{PrincipalId:D}/roles",
            new { roleId = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(
            factory.DownstreamRequests,
            request => request.Method == "POST" && request.Path.EndsWith("/roles", StringComparison.Ordinal));
    }

    private HttpClient CreateClient(string principalId, params string[] permissions)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken(principalId, permissions));
        return client;
    }

    private static MethodInfo GetAction(string actionName) => Assert.Single(
        typeof(IamController).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
        method => method.Name == actionName);

    private static string[] ExpectedPermissions(string actionName, string primaryPermission) =>
        actionName == nameof(IamController.InviteUser)
            ? [primaryPermission, MalievPermissions.IAM.Bindings.Create]
            : [primaryPermission];

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string route) => method switch
    {
        "POST" when route.EndsWith("/invite", StringComparison.Ordinal) => client.PostAsJsonAsync(route, new
        {
            displayName = "New Employee",
            email = "new.employee@maliev.com",
            roleId = "roles.iam.viewer"
        }),
        "POST" => client.PostAsJsonAsync(route, new { roleId = "roles.iam.viewer" }),
        "PATCH" => client.PatchAsJsonAsync(route, new { displayName = "Updated Employee", isEnabled = true }),
        "DELETE" => client.DeleteAsync(route),
        _ => client.GetAsync(route)
    };
}

public sealed class IamConsoleAuthorizationFactory : SignalRTestFactory
{
    private readonly ConcurrentQueue<IamConsoleDownstreamRequest> _downstreamRequests = new();

    public RecordingIamConsoleIamServiceClient IamClient { get; } = new();

    public IReadOnlyCollection<IamConsoleDownstreamRequest> DownstreamRequests => _downstreamRequests.ToArray();

    public void Reset(IEnumerable<string> allowedPermissions, IEnumerable<string>? unavailablePermissions = null)
    {
        IamClient.Reset(allowedPermissions, unavailablePermissions ?? []);
        _downstreamRequests.Clear();
    }

    protected override void ConfigureAdditionalServices(IServiceCollection services)
    {
        base.ConfigureAdditionalServices(services);
        services.RemoveAll<IIamServiceClient>();
        services.AddSingleton<IIamServiceClient>(IamClient);
        services.PostConfigureAll<HttpClientFactoryOptions>(options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(httpBuilder =>
            {
                httpBuilder.PrimaryHandler = new MockHttpMessageHandler(HandleDownstreamAsync);
            });
        });
    }

    private async Task<HttpResponseMessage> HandleDownstreamAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);
        _downstreamRequests.Enqueue(new IamConsoleDownstreamRequest(request.Method.Method, path, body));

        if (request.Method == HttpMethod.Get &&
            path.StartsWith("/iam/v1/principals/", StringComparison.Ordinal) &&
            !path.EndsWith("/roles", StringComparison.Ordinal))
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        if (request.Method == HttpMethod.Post && path == "/iam/v1/principals")
        {
            return JsonResponse(
                HttpStatusCode.Created,
                $$"""{"principalId":"{{Guid.Parse("a1f0d442-d541-42e8-8fcc-8e513ecb8fc3"):D}}","createdAt":"2026-07-15T00:00:00Z"}""");
        }

        if (request.Method == HttpMethod.Post && path.EndsWith("/roles", StringComparison.Ordinal))
        {
            var principalId = Guid.Parse(path.Split('/')[4]);
            return JsonResponse(
                HttpStatusCode.OK,
                $$"""
                {
                  "bindingId": "4d6905e4-adf0-4c0b-966f-ea51df8cbd41",
                  "principalId": "{{principalId:D}}",
                  "roleId": "roles.iam.viewer",
                  "resourcePath": null,
                  "grantedAt": "2026-07-15T00:00:01Z",
                  "expiresAt": null
                }
                """);
        }

        var content = request.Method == HttpMethod.Get ? "[]" : string.Empty;
        return JsonResponse(HttpStatusCode.OK, content);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };
}

public sealed record IamConsoleDownstreamRequest(string Method, string Path, string? Body = null);

public sealed record IamConsolePermissionCheck(string Permission, string? ResourcePath);

public sealed class RecordingIamConsoleIamServiceClient : IIamServiceClient
{
    private readonly ConcurrentQueue<IamConsolePermissionCheck> _liveChecks = new();
    private readonly ConcurrentQueue<IamConsolePermissionCheck> _standardChecks = new();
    private HashSet<string> _allowedPermissions = new(StringComparer.Ordinal);
    private HashSet<string> _unavailablePermissions = new(StringComparer.Ordinal);

    public IReadOnlyCollection<IamConsolePermissionCheck> LiveChecks => _liveChecks.ToArray();
    public IReadOnlyCollection<IamConsolePermissionCheck> StandardChecks => _standardChecks.ToArray();

    public void Reset(IEnumerable<string> allowedPermissions, IEnumerable<string> unavailablePermissions)
    {
        _allowedPermissions = new HashSet<string>(allowedPermissions, StringComparer.Ordinal);
        _unavailablePermissions = new HashSet<string>(unavailablePermissions, StringComparer.Ordinal);
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
        _standardChecks.Enqueue(new IamConsolePermissionCheck(permissionId, resourcePath));
        return Task.FromResult(_allowedPermissions.Contains(permissionId));
    }

    public Task<bool> CheckPermissionLiveAsync(
        string principalId,
        string permissionId,
        string? resourcePath = null,
        CancellationToken cancellationToken = default)
    {
        _liveChecks.Enqueue(new IamConsolePermissionCheck(permissionId, resourcePath));
        if (_unavailablePermissions.Contains(permissionId))
        {
            throw new HttpRequestException("IAM is unavailable.");
        }

        return Task.FromResult(_allowedPermissions.Contains(permissionId));
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
