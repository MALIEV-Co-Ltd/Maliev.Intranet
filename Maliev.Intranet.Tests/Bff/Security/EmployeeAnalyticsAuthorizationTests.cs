using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Bff.Hubs;
using Maliev.Intranet.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace Maliev.Intranet.Tests.Bff.Security;

public sealed class EmployeeAnalyticsAuthorizationTests(EmployeeAnalyticsAuthorizationFactory factory)
    : IClassFixture<EmployeeAnalyticsAuthorizationFactory>
{
    private const string ReportsViewPermission = MalievPermissions.Employee.ReportsView;
    private const string PublicRoute = "/api/v1/employees/analytics";

    [Fact]
    public async Task GetAnalytics_fails_closed_before_EmployeeService_for_missing_wrong_stale_denied_or_unavailable_decisions()
    {
        var scenarios = new[]
        {
            new AuthorizationScenario("missing", [], [], []),
            new AuthorizationScenario(
                "wrong",
                [MalievPermissions.Employee.Read],
                [MalievPermissions.Employee.Read],
                []),
            new AuthorizationScenario("stale", [ReportsViewPermission], [], []),
            new AuthorizationScenario("denied", [], [], []),
            new AuthorizationScenario(
                "unavailable",
                [ReportsViewPermission],
                [],
                [ReportsViewPermission])
        };

        foreach (var scenario in scenarios)
        {
            factory.Reset(scenario.LivePermissions, scenario.UnavailablePermissions);
            using var client = CreateClient(scenario.Name, scenario.Claims);

            var response = await client.GetAsync(PublicRoute);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(
                [new EmployeePermissionCheck(ReportsViewPermission, "global")],
                factory.IamClient.LiveChecks);
            Assert.Empty(factory.IamClient.StandardChecks);
            Assert.Empty(factory.DownstreamRequests);
        }
    }

    [Fact]
    public async Task GetAnalytics_allows_one_current_live_decision_and_preserves_exact_EmployeeService_contract()
    {
        factory.Reset([ReportsViewPermission]);
        using var client = CreateClient("current-hr-report-reader", []);

        var response = await client.GetAsync(PublicRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var analytics = await response.Content.ReadFromJsonAsync<HrAnalyticsDto>();
        Assert.NotNull(analytics);
        Assert.Equal(42, analytics.TotalHeadcount);
        Assert.Equal(39, analytics.ActiveEmployees);
        Assert.Equal(2, analytics.OnboardingCount);
        Assert.Equal(3.5m, analytics.TurnoverRate);
        var department = Assert.Single(analytics.DepartmentDistribution);
        Assert.Equal("Engineering", department.Department);
        Assert.Equal(17, department.Count);
        Assert.Equal(43.59, department.Percentage);
        var hireTrend = Assert.Single(analytics.HireTrend);
        Assert.Equal("2026-06", hireTrend.Month);
        Assert.Equal(4, hireTrend.Count);
        Assert.Equal(
            [new EmployeePermissionCheck(ReportsViewPermission, "global")],
            factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
        Assert.Equal(
            [new EmployeeDownstreamRequest("GET", "/employee/v1/analytics/summary")],
            factory.DownstreamRequests);
    }

    [Fact]
    public async Task GetAnalytics_preserves_empty_success_fallback_after_authorized_downstream_not_found()
    {
        factory.Reset([ReportsViewPermission], downstreamNotFound: true);
        using var client = CreateClient("current-hr-report-reader", []);

        var response = await client.GetAsync(PublicRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var analytics = await response.Content.ReadFromJsonAsync<HrAnalyticsDto>();
        Assert.NotNull(analytics);
        Assert.Equal(0, analytics.TotalHeadcount);
        Assert.Empty(analytics.DepartmentDistribution);
        Assert.Empty(analytics.HireTrend);
        Assert.Equal(
            [new EmployeePermissionCheck(ReportsViewPermission, "global")],
            factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
        Assert.Equal(
            [new EmployeeDownstreamRequest("GET", "/employee/v1/analytics/summary")],
            factory.DownstreamRequests);
    }

    private HttpClient CreateClient(string principalId, string[] permissions)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken(principalId, permissions));
        return client;
    }

    private sealed record AuthorizationScenario(
        string Name,
        string[] Claims,
        string[] LivePermissions,
        string[] UnavailablePermissions);
}

public sealed class EmployeeAnalyticsAuthorizationFactory : SignalRTestFactory
{
    private readonly ConcurrentQueue<EmployeeDownstreamRequest> _downstreamRequests = new();
    private bool _downstreamNotFound;

    public RecordingEmployeeIamServiceClient IamClient { get; } = new();

    public IReadOnlyCollection<EmployeeDownstreamRequest> DownstreamRequests => _downstreamRequests.ToArray();

    public void Reset(
        IEnumerable<string> allowedPermissions,
        IEnumerable<string>? unavailablePermissions = null,
        bool downstreamNotFound = false)
    {
        IamClient.Reset(allowedPermissions, unavailablePermissions ?? []);
        _downstreamRequests.Clear();
        _downstreamNotFound = downstreamNotFound;
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
        _downstreamRequests.Enqueue(new EmployeeDownstreamRequest(request.Method.Method, pathAndQuery));

        if (_downstreamNotFound)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"totalHeadcount":42,"activeEmployees":39,"onboardingCount":2,"turnoverRate":3.5,"departmentDistribution":[{"department":"Engineering","count":17,"percentage":43.59}],"hireTrend":[{"month":"2026-06","count":4}]}""",
                Encoding.UTF8,
                "application/json")
        });
    }
}
