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

public sealed class ProductionCollectionAuthorizationTests(ProductionCollectionAuthorizationFactory factory)
    : IClassFixture<ProductionCollectionAuthorizationFactory>
{
    public static TheoryData<string> HttpSurfaces => new()
    {
        "/api/v1/jobs/queue",
        "/api/v1/jobs/stats",
        "/api/v1/jobs?status=Queued&processType=FDM&priority=High&page=2",
        "/api/v1/jobs/machine/FDM-01/schedule?from=2026-07-01T00:00:00Z&to=2026-07-02T00:00:00Z",
        "/api/v1/jobs/schedule?from=2026-07-01T00:00:00Z&to=2026-07-02T00:00:00Z"
    };

    [Theory]
    [MemberData(nameof(HttpSurfaces))]
    public async Task Http_surface_fails_closed_before_downstream_calls_for_invalid_authorization(string route)
    {
        var scenarios = new[]
        {
            new AuthorizationScenario("no-capability", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()),
            new AuthorizationScenario("wrong-capability", [MalievPermissions.Job.Write], [MalievPermissions.Job.Write], Array.Empty<string>()),
            new AuthorizationScenario("stale-read-claim", [MalievPermissions.Job.Read], Array.Empty<string>(), Array.Empty<string>()),
            new AuthorizationScenario("iam-outage", [MalievPermissions.Job.Read], Array.Empty<string>(), [MalievPermissions.Job.Read])
        };

        foreach (var scenario in scenarios)
        {
            factory.Reset(scenario.LivePermissions, scenario.UnavailablePermissions);
            using var client = CreateClient(scenario.Name, scenario.Claims);

            var response = await client.GetAsync(route);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(
                [new ProductionPermissionCheck(MalievPermissions.Job.Read, "global")],
                factory.IamClient.LiveChecks);
            Assert.Empty(factory.IamClient.StandardChecks);
            Assert.Empty(factory.DownstreamRequests);
        }
    }

    [Theory]
    [MemberData(nameof(HttpSurfaces))]
    public async Task Http_surface_allows_one_current_live_read_decision_and_preserves_downstream_call(string route)
    {
        factory.Reset([MalievPermissions.Job.Read]);
        using var client = CreateClient("current-job-reader", []);

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            [new ProductionPermissionCheck(MalievPermissions.Job.Read, "global")],
            factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
        Assert.Equal(ExpectedDownstreamRequests(route), factory.DownstreamRequests);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Production_hub_negotiate_fails_closed_for_stale_claim_or_iam_outage(bool iamUnavailable)
    {
        factory.Reset(
            [],
            iamUnavailable ? [MalievPermissions.Job.Read] : []);
        using var client = CreateClient("stale-production-reader", [MalievPermissions.Job.Read]);

        var response = await client.PostAsync("/hubs/production/negotiate", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            [new ProductionPermissionCheck(MalievPermissions.Job.Read, "global")],
            factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
    }

    [Fact]
    public async Task Production_hub_negotiate_allows_one_current_live_read_decision()
    {
        factory.Reset([MalievPermissions.Job.Read]);
        using var client = CreateClient("current-production-reader", []);

        var response = await client.PostAsync("/hubs/production/negotiate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            [new ProductionPermissionCheck(MalievPermissions.Job.Read, "global")],
            factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
    }

    private HttpClient CreateClient(string principalId, string[] permissions)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken(principalId, permissions));
        return client;
    }

    private static ProductionDownstreamRequest[] ExpectedDownstreamRequests(string route) => route switch
    {
        "/api/v1/jobs/queue" or "/api/v1/jobs/stats" =>
            [new("GET", "/job/v1/jobs/kanban")],
        "/api/v1/jobs?status=Queued&processType=FDM&priority=High&page=2" =>
            [new("GET", "/job/v1/jobs?page=2&status=Queued&processType=FDM&priority=High")],
        var value when value.StartsWith("/api/v1/jobs/machine/", StringComparison.Ordinal) =>
            [new("GET", "/job/v1/jobs/machine/FDM-01/schedule?from=2026-07-01T00:00:00.0000000Z&to=2026-07-02T00:00:00.0000000Z")],
        var value when value.StartsWith("/api/v1/jobs/schedule", StringComparison.Ordinal) =>
            [
                new("GET", "/facility/v1/equipments?page=1&pageSize=200"),
                new("GET", "/job/v1/jobs/schedule?from=2026-07-01T00%3A00%3A00.0000000Z&to=2026-07-02T00%3A00%3A00.0000000Z")
            ],
        _ => throw new InvalidOperationException($"No downstream contract registered for {route}.")
    };

    private sealed record AuthorizationScenario(
        string Name,
        string[] Claims,
        string[] LivePermissions,
        string[] UnavailablePermissions);
}

public sealed class ProductionCollectionAuthorizationFactory : SignalRTestFactory
{
    private readonly ConcurrentQueue<ProductionDownstreamRequest> _downstreamRequests = new();

    public RecordingProductionIamServiceClient IamClient { get; } = new();

    public IReadOnlyCollection<ProductionDownstreamRequest> DownstreamRequests => _downstreamRequests.ToArray();

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
        _downstreamRequests.Enqueue(new ProductionDownstreamRequest(request.Method.Method, pathAndQuery));
        var json = pathAndQuery switch
        {
            var value when value.StartsWith("/job/v1/jobs/kanban", StringComparison.Ordinal) =>
                """{"pending":[],"queued":[],"inProgress":[],"finishing":[],"completed":[],"cancelled":[]}""",
            var value when value.StartsWith("/job/v1/jobs/machine/", StringComparison.Ordinal) => "[]",
            var value when value.StartsWith("/job/v1/jobs/schedule", StringComparison.Ordinal) => "[]",
            var value when value.StartsWith("/job/v1/jobs", StringComparison.Ordinal) => "{}",
            var value when value.StartsWith("/facility/v1/equipments", StringComparison.Ordinal) => "{}",
            _ => "{}"
        };

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    }
}

public sealed record ProductionDownstreamRequest(string Method, string PathAndQuery);

public sealed record ProductionPermissionCheck(string Permission, string? ResourcePath);

public sealed class RecordingProductionIamServiceClient : IIamServiceClient
{
    private readonly ConcurrentQueue<ProductionPermissionCheck> _liveChecks = new();
    private readonly ConcurrentQueue<ProductionPermissionCheck> _standardChecks = new();
    private HashSet<string> _allowedPermissions = new(StringComparer.Ordinal);
    private HashSet<string> _unavailablePermissions = new(StringComparer.Ordinal);

    public IReadOnlyCollection<ProductionPermissionCheck> LiveChecks => _liveChecks.ToArray();
    public IReadOnlyCollection<ProductionPermissionCheck> StandardChecks => _standardChecks.ToArray();

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
        _standardChecks.Enqueue(new ProductionPermissionCheck(permissionId, resourcePath));
        return Task.FromResult(_allowedPermissions.Contains(permissionId));
    }

    public Task<bool> CheckPermissionLiveAsync(
        string principalId,
        string permissionId,
        string? resourcePath = null,
        CancellationToken cancellationToken = default)
    {
        _liveChecks.Enqueue(new ProductionPermissionCheck(permissionId, resourcePath));
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
