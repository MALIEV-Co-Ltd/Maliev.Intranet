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

public sealed class EmployeeDetailAuthorizationTests(EmployeeDetailAuthorizationFactory factory)
    : IClassFixture<EmployeeDetailAuthorizationFactory>
{
    [Fact]
    public async Task GetById_fails_closed_before_EmployeeService_for_missing_wrong_stale_denied_or_unavailable_decisions()
    {
        var employeeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var resourcePath = $"employee/{employeeId:D}";
        var scenarios = new[]
        {
            new AuthorizationScenario("missing", [], [], []),
            new AuthorizationScenario(
                "wrong",
                [MalievPermissions.Employee.Write],
                [new EmployeeDetailPermissionCheck(MalievPermissions.Employee.Write, resourcePath)],
                []),
            new AuthorizationScenario("stale", [MalievPermissions.Employee.ProfileRead], [], []),
            new AuthorizationScenario("denied", [], [], []),
            new AuthorizationScenario(
                "unavailable",
                [MalievPermissions.Employee.ProfileRead],
                [],
                [new EmployeeDetailPermissionCheck(MalievPermissions.Employee.ProfileRead, resourcePath)])
        };

        foreach (var scenario in scenarios)
        {
            factory.Reset(scenario.LiveGrants, scenario.UnavailableChecks);
            using var client = CreateClient(scenario.Name, scenario.Claims);

            var response = await client.GetAsync($"/api/v1/employees/{employeeId:D}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(
                [new EmployeeDetailPermissionCheck(MalievPermissions.Employee.ProfileRead, resourcePath)],
                factory.IamClient.LiveChecks);
            Assert.Empty(factory.IamClient.StandardChecks);
            Assert.Empty(factory.DownstreamRequests);
        }
    }

    [Fact]
    public async Task GetById_does_not_accept_a_live_grant_for_a_different_employee()
    {
        var requestedId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var otherId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        factory.Reset(
            [new EmployeeDetailPermissionCheck(MalievPermissions.Employee.ProfileRead, $"employee/{otherId:D}")]);
        using var client = CreateClient("other-employee-reader", []);

        var response = await client.GetAsync($"/api/v1/employees/{requestedId:D}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            [new EmployeeDetailPermissionCheck(
                MalievPermissions.Employee.ProfileRead,
                $"employee/{requestedId:D}")],
            factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
        Assert.Empty(factory.DownstreamRequests);
    }

    [Fact]
    public async Task GetById_allows_the_matching_live_grant_and_preserves_the_exact_EmployeeService_contract()
    {
        var employeeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var expectedCheck = new EmployeeDetailPermissionCheck(
            MalievPermissions.Employee.ProfileRead,
            $"employee/{employeeId:D}");
        factory.Reset([expectedCheck]);
        using var client = CreateClient("matching-employee-reader", []);

        var response = await client.GetAsync($"/api/v1/employees/{employeeId:D}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var employee = await response.Content.ReadFromJsonAsync<EmployeeDetailDto>();
        Assert.NotNull(employee);
        Assert.Equal(employeeId, employee.Id);
        Assert.Equal("Mia", employee.FirstName);
        Assert.Equal("Wong", employee.LastName);
        Assert.Equal("mia.wong@maliev.com", employee.Email);
        Assert.Equal("Sales Manager", employee.Title);
        var emergencyContact = Assert.Single(employee.EmergencyContacts);
        Assert.Equal("Kai Wong", emergencyContact.Name);
        Assert.Equal("Spouse", emergencyContact.Relationship);
        Assert.Equal("+66810000000", emergencyContact.Phone);
        Assert.Equal("kai@example.test", emergencyContact.Email);
        Assert.Equal([expectedCheck], factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
        Assert.Equal(
            [new EmployeeDetailDownstreamRequest("GET", $"/employee/v1/employees/{employeeId:D}")],
            factory.DownstreamRequests);
    }

    [Fact]
    public async Task GetById_returns_not_found_only_after_the_matching_live_grant_and_downstream_lookup()
    {
        var employeeId = EmployeeDetailAuthorizationFactory.MissingEmployeeId;
        var expectedCheck = new EmployeeDetailPermissionCheck(
            MalievPermissions.Employee.ProfileRead,
            $"employee/{employeeId:D}");
        factory.Reset([expectedCheck]);
        using var client = CreateClient("missing-employee-reader", []);

        var response = await client.GetAsync($"/api/v1/employees/{employeeId:D}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal([expectedCheck], factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
        Assert.Equal(
            [new EmployeeDetailDownstreamRequest("GET", $"/employee/v1/employees/{employeeId:D}")],
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
        EmployeeDetailPermissionCheck[] LiveGrants,
        EmployeeDetailPermissionCheck[] UnavailableChecks);
}

public sealed class EmployeeDetailAuthorizationFactory : SignalRTestFactory
{
    public static readonly Guid MissingEmployeeId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private readonly ConcurrentQueue<EmployeeDetailDownstreamRequest> _downstreamRequests = new();

    public RecordingEmployeeDetailIamServiceClient IamClient { get; } = new();

    public IReadOnlyCollection<EmployeeDetailDownstreamRequest> DownstreamRequests => _downstreamRequests.ToArray();

    public void Reset(
        IEnumerable<EmployeeDetailPermissionCheck> liveGrants,
        IEnumerable<EmployeeDetailPermissionCheck>? unavailableChecks = null)
    {
        IamClient.Reset(liveGrants, unavailableChecks ?? []);
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
        _downstreamRequests.Enqueue(new EmployeeDetailDownstreamRequest(request.Method.Method, pathAndQuery));

        var missingPath = $"/employee/v1/employees/{MissingEmployeeId:D}";
        if (string.Equals(pathAndQuery, missingPath, StringComparison.Ordinal))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        var employeeId = pathAndQuery.Split('/').Last();
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""{"id":"{{employeeId}}","firstName":"Mia","lastName":"Wong","email":"mia.wong@maliev.com","department":"Sales","title":"Sales Manager","status":"Active","notes":[],"teams":[],"emergencyContacts":[{"name":"Kai Wong","relationship":"Spouse","phone":"+66810000000","email":"kai@example.test"}],"employmentHistory":[],"documents":[],"createdAt":"2026-01-01T00:00:00Z","updatedAt":"2026-01-01T00:00:00Z"}""",
                Encoding.UTF8,
                "application/json")
        });
    }
}

public sealed record EmployeeDetailDownstreamRequest(string Method, string PathAndQuery);

public sealed record EmployeeDetailPermissionCheck(string Permission, string? ResourcePath);

public sealed class RecordingEmployeeDetailIamServiceClient : IIamServiceClient
{
    private readonly ConcurrentQueue<EmployeeDetailPermissionCheck> _liveChecks = new();
    private readonly ConcurrentQueue<EmployeeDetailPermissionCheck> _standardChecks = new();
    private HashSet<EmployeeDetailPermissionCheck> _liveGrants = [];
    private HashSet<EmployeeDetailPermissionCheck> _unavailableChecks = [];

    public IReadOnlyCollection<EmployeeDetailPermissionCheck> LiveChecks => _liveChecks.ToArray();
    public IReadOnlyCollection<EmployeeDetailPermissionCheck> StandardChecks => _standardChecks.ToArray();

    public void Reset(
        IEnumerable<EmployeeDetailPermissionCheck> liveGrants,
        IEnumerable<EmployeeDetailPermissionCheck> unavailableChecks)
    {
        _liveGrants = new HashSet<EmployeeDetailPermissionCheck>(liveGrants);
        _unavailableChecks = new HashSet<EmployeeDetailPermissionCheck>(unavailableChecks);
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
        var check = new EmployeeDetailPermissionCheck(permissionId, resourcePath);
        _standardChecks.Enqueue(check);
        return Task.FromResult(_liveGrants.Contains(check));
    }

    public Task<bool> CheckPermissionLiveAsync(
        string principalId,
        string permissionId,
        string? resourcePath = null,
        CancellationToken cancellationToken = default)
    {
        var check = new EmployeeDetailPermissionCheck(permissionId, resourcePath);
        _liveChecks.Enqueue(check);
        if (_unavailableChecks.Contains(check))
        {
            throw new HttpRequestException("IAM is unavailable.");
        }

        return Task.FromResult(_liveGrants.Contains(check));
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
