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

public sealed class EmployeeDirectoryAuthorizationTests(EmployeeDirectoryAuthorizationFactory factory)
    : IClassFixture<EmployeeDirectoryAuthorizationFactory>
{
    private const string PublicRoute = "/api/v1/employees?page=2&pageSize=100";

    [Fact]
    public async Task Get_fails_closed_before_EmployeeService_for_missing_wrong_stale_denied_or_unavailable_decisions()
    {
        var scenarios = new[]
        {
            new AuthorizationScenario("missing", [], [], []),
            new AuthorizationScenario(
                "wrong",
                [MalievPermissions.Employee.Write],
                [MalievPermissions.Employee.Write],
                []),
            new AuthorizationScenario("stale", [MalievPermissions.Employee.Read], [], []),
            new AuthorizationScenario("denied", [], [], []),
            new AuthorizationScenario(
                "unavailable",
                [MalievPermissions.Employee.Read],
                [],
                [MalievPermissions.Employee.Read])
        };

        foreach (var scenario in scenarios)
        {
            factory.Reset(scenario.LivePermissions, scenario.UnavailablePermissions);
            using var client = CreateClient(scenario.Name, scenario.Claims);

            var response = await client.GetAsync(PublicRoute);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(
                [new EmployeePermissionCheck(MalievPermissions.Employee.Read, "global")],
                factory.IamClient.LiveChecks);
            Assert.Empty(factory.IamClient.StandardChecks);
            Assert.Empty(factory.DownstreamRequests);
        }
    }

    [Fact]
    public async Task Get_allows_one_current_live_decision_and_preserves_exact_EmployeeService_paged_contract()
    {
        factory.Reset([MalievPermissions.Employee.Read]);
        using var client = CreateClient("current-employee-directory-reader", []);

        var response = await client.GetAsync(PublicRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<EmployeeSummaryDto>>();
        Assert.NotNull(payload);
        var employee = Assert.Single(payload.Data);
        Assert.Equal(Guid.Parse("55555555-5555-5555-5555-555555555555"), employee.Id);
        Assert.Equal("Mia Wong", employee.Name);
        Assert.Equal("Sales Manager", employee.Title);
        Assert.Equal(2, payload.Meta.CurrentPage);
        Assert.Equal(100, payload.Meta.PageSize);
        Assert.Equal(301, payload.Meta.TotalCount);
        Assert.Equal(
            [new EmployeePermissionCheck(MalievPermissions.Employee.Read, "global")],
            factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
        Assert.Equal(
            [new EmployeeDownstreamRequest("GET", "/employee/v1/employees?page=2&pageSize=100")],
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

public sealed class EmployeeDirectoryAuthorizationFactory : SignalRTestFactory
{
    private readonly ConcurrentQueue<EmployeeDownstreamRequest> _downstreamRequests = new();

    public RecordingEmployeeIamServiceClient IamClient { get; } = new();

    public IReadOnlyCollection<EmployeeDownstreamRequest> DownstreamRequests => _downstreamRequests.ToArray();

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
        _downstreamRequests.Enqueue(new EmployeeDownstreamRequest(request.Method.Method, pathAndQuery));

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"data":[{"id":"55555555-5555-5555-5555-555555555555","name":"Mia Wong","email":"mia.wong@maliev.com","department":"Sales","title":"Sales Manager","status":"Active"}],"meta":{"currentPage":2,"totalPages":4,"totalItems":301,"totalCount":301,"pageSize":100}}""",
                Encoding.UTF8,
                "application/json")
        });
    }
}

public sealed record EmployeeDownstreamRequest(string Method, string PathAndQuery);

public sealed record EmployeePermissionCheck(string Permission, string? ResourcePath);

public sealed class RecordingEmployeeIamServiceClient : IIamServiceClient
{
    private readonly ConcurrentQueue<EmployeePermissionCheck> _liveChecks = new();
    private readonly ConcurrentQueue<EmployeePermissionCheck> _standardChecks = new();
    private HashSet<string> _allowedPermissions = new(StringComparer.Ordinal);
    private HashSet<string> _unavailablePermissions = new(StringComparer.Ordinal);

    public IReadOnlyCollection<EmployeePermissionCheck> LiveChecks => _liveChecks.ToArray();
    public IReadOnlyCollection<EmployeePermissionCheck> StandardChecks => _standardChecks.ToArray();

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
        _standardChecks.Enqueue(new EmployeePermissionCheck(permissionId, resourcePath));
        return Task.FromResult(_allowedPermissions.Contains(permissionId));
    }

    public Task<bool> CheckPermissionLiveAsync(
        string principalId,
        string permissionId,
        string? resourcePath = null,
        CancellationToken cancellationToken = default)
    {
        _liveChecks.Enqueue(new EmployeePermissionCheck(permissionId, resourcePath));
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
