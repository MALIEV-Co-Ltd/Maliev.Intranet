using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Bff.Hubs;
using Maliev.Intranet.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace Maliev.Intranet.Tests.Bff.Security;

public sealed class DashboardAuthorizationTests(DashboardAuthorizationFactory factory)
    : IClassFixture<DashboardAuthorizationFactory>
{
    public static TheoryData<string> DashboardRoutes => new()
    {
        "/api/v1/dashboard",
        "/api/v1/dashboard/action-items"
    };

    [Theory]
    [MemberData(nameof(DashboardRoutes))]
    public async Task Route_fails_closed_before_downstream_for_missing_wrong_stale_denied_or_unavailable_IAM(
        string route)
    {
        var scenarios = new[]
        {
            new AuthorizationScenario("missing", [], [], []),
            new AuthorizationScenario(
                "wrong",
                [MalievPermissions.Project.Read],
                [MalievPermissions.Project.Read],
                []),
            new AuthorizationScenario("stale", [MalievPermissions.Dashboard.View], [], []),
            new AuthorizationScenario("denied", [], [], []),
            new AuthorizationScenario(
                "unavailable",
                [MalievPermissions.Dashboard.View],
                [],
                [MalievPermissions.Dashboard.View])
        };

        foreach (var scenario in scenarios)
        {
            factory.Reset(scenario.LivePermissions, scenario.UnavailablePermissions);
            using var client = CreateClient(scenario.Name, scenario.Claims);

            var response = await client.GetAsync(route);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(
                [new DashboardPermissionCheck(MalievPermissions.Dashboard.View, "global")],
                factory.IamClient.LiveChecks);
            Assert.Empty(factory.IamClient.StandardChecks);
            Assert.Empty(factory.DownstreamRequests);
        }
    }

    [Fact]
    public async Task Get_allows_one_live_decision_and_preserves_default_widget_fan_out()
    {
        factory.Reset([MalievPermissions.Dashboard.View]);
        using var client = CreateClient("current-dashboard-reader", []);

        var response = await client.GetAsync("/api/v1/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            [new DashboardPermissionCheck(MalievPermissions.Dashboard.View, "global")],
            factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
        AssertRequests(
            new DashboardDownstreamRequest("GET", "/payment/v1/metrics/stats"),
            new DashboardDownstreamRequest("GET", "/order/v1/metrics/active-count"),
            new DashboardDownstreamRequest("GET", "/quotation/v1/metrics/pending-count"),
            new DashboardDownstreamRequest("GET", "/employee/v1/metrics/headcount"));
    }

    [Fact]
    public async Task Get_respects_widget_query_and_does_not_fan_out_to_unrequested_sources()
    {
        factory.Reset([MalievPermissions.Dashboard.View]);
        using var client = CreateClient("current-revenue-reader", []);

        var response = await client.GetAsync("/api/v1/dashboard?widgets=Revenue%2COrderTrend");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            [new DashboardPermissionCheck(MalievPermissions.Dashboard.View, "global")],
            factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
        AssertRequests(new DashboardDownstreamRequest("GET", "/payment/v1/metrics/stats"));
    }

    [Fact]
    public async Task GetActionItems_allows_one_live_decision_and_preserves_non_employee_fan_out()
    {
        factory.Reset([MalievPermissions.Dashboard.View]);
        using var client = CreateClient("current-action-items-reader", []);

        var response = await client.GetAsync("/api/v1/dashboard/action-items");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            [new DashboardPermissionCheck(MalievPermissions.Dashboard.View, "global")],
            factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
        AssertRequests(
            new DashboardDownstreamRequest("GET", "/order/v1/metrics/on-hold-count"),
            new DashboardDownstreamRequest("GET", "/quotation/v1/metrics/aging-count?minAgeDays=7"),
            new DashboardDownstreamRequest("GET", "/invoice/v1/metrics/overdue-count"),
            new DashboardDownstreamRequest("GET", "/project/v1/projects/stats"),
            new DashboardDownstreamRequest("GET", "/project/v1/projects/stats"),
            new DashboardDownstreamRequest("GET", "/job/v1/jobs/kanban"));
    }

    [Fact]
    public async Task GetActionItems_preserves_principal_to_employee_and_manager_leave_fan_out()
    {
        factory.Reset([MalievPermissions.Dashboard.View]);
        using var client = CreateClient(DashboardAuthorizationFactory.PrincipalId.ToString("D"), []);

        var response = await client.GetAsync("/api/v1/dashboard/action-items");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            [new DashboardPermissionCheck(MalievPermissions.Dashboard.View, "global")],
            factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
        AssertRequests(
            new DashboardDownstreamRequest("GET", "/order/v1/metrics/on-hold-count"),
            new DashboardDownstreamRequest("GET", "/quotation/v1/metrics/aging-count?minAgeDays=7"),
            new DashboardDownstreamRequest("GET", "/invoice/v1/metrics/overdue-count"),
            new DashboardDownstreamRequest("GET", "/project/v1/projects/stats"),
            new DashboardDownstreamRequest("GET", "/project/v1/projects/stats"),
            new DashboardDownstreamRequest("GET", "/job/v1/jobs/kanban"),
            new DashboardDownstreamRequest(
                "GET",
                $"/employee/v1/employees/by-principal/{DashboardAuthorizationFactory.PrincipalId:D}"),
            new DashboardDownstreamRequest(
                "GET",
                $"/leave/v1/LeaveRequests/pending-count?managerId={DashboardAuthorizationFactory.EmployeeId:D}"));
    }

    private HttpClient CreateClient(string principalId, string[] permissions)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken(principalId, permissions));
        return client;
    }

    private void AssertRequests(params DashboardDownstreamRequest[] expected)
    {
        Assert.Equal(
            expected.OrderBy(RequestSortKey, StringComparer.Ordinal),
            factory.DownstreamRequests.OrderBy(RequestSortKey, StringComparer.Ordinal));
    }

    private static string RequestSortKey(DashboardDownstreamRequest request) =>
        $"{request.Method} {request.PathAndQuery}";

    private sealed record AuthorizationScenario(
        string Name,
        string[] Claims,
        string[] LivePermissions,
        string[] UnavailablePermissions);
}

public sealed class DashboardAuthorizationFactory : SignalRTestFactory
{
    public static readonly Guid PrincipalId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid EmployeeId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly ConcurrentQueue<DashboardDownstreamRequest> _downstreamRequests = new();

    public RecordingDashboardIamServiceClient IamClient { get; } = new();

    public IReadOnlyCollection<DashboardDownstreamRequest> DownstreamRequests => _downstreamRequests.ToArray();

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

    private Task<HttpResponseMessage> HandleDownstreamAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var pathAndQuery = request.RequestUri?.PathAndQuery ?? string.Empty;
        _downstreamRequests.Enqueue(new DashboardDownstreamRequest(request.Method.Method, pathAndQuery));

        var json = pathAndQuery switch
        {
            "/payment/v1/metrics/stats" => """{"todayTotal":120}""",
            "/project/v1/projects/stats" =>
                """{"activeCount":0,"configuringCount":0,"customerReviewCount":0,"quotedCount":0,"inProductionCount":0}""",
            "/job/v1/jobs/kanban" =>
                """{"pending":[],"queued":[],"inProgress":[],"finishing":[],"completed":[],"cancelled":[]}""",
            var value when value.StartsWith("/employee/v1/employees/by-principal/", StringComparison.Ordinal) =>
                $$"""{"id":"{{EmployeeId:D}}","employeeNumber":"EMP-1","firstName":"Ada","lastName":"Lovelace","fullName":"Ada Lovelace","workEmail":"ada@maliev.com","employmentType":"FullTime","employmentStatus":"Active","emergencyContacts":[]}""",
            _ => """{"count":0}"""
        };

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    }
}

public sealed record DashboardDownstreamRequest(string Method, string PathAndQuery);

public sealed record DashboardPermissionCheck(string Permission, string? ResourcePath);

public sealed class RecordingDashboardIamServiceClient : IIamServiceClient
{
    private readonly ConcurrentQueue<DashboardPermissionCheck> _liveChecks = new();
    private readonly ConcurrentQueue<DashboardPermissionCheck> _standardChecks = new();
    private HashSet<string> _allowedPermissions = new(StringComparer.Ordinal);
    private HashSet<string> _unavailablePermissions = new(StringComparer.Ordinal);

    public IReadOnlyCollection<DashboardPermissionCheck> LiveChecks => _liveChecks.ToArray();
    public IReadOnlyCollection<DashboardPermissionCheck> StandardChecks => _standardChecks.ToArray();

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
        _standardChecks.Enqueue(new DashboardPermissionCheck(permissionId, resourcePath));
        return Task.FromResult(_allowedPermissions.Contains(permissionId));
    }

    public Task<bool> CheckPermissionLiveAsync(
        string principalId,
        string permissionId,
        string? resourcePath = null,
        CancellationToken cancellationToken = default)
    {
        _liveChecks.Enqueue(new DashboardPermissionCheck(permissionId, resourcePath));
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
