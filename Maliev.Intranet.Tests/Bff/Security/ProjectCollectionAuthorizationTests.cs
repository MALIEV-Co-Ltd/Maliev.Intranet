using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Bff.Hubs;
using Maliev.Intranet.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace Maliev.Intranet.Tests.Bff.Security;

public sealed class ProjectCollectionAuthorizationTests(ProjectCollectionAuthorizationFactory factory)
    : IClassFixture<ProjectCollectionAuthorizationFactory>
{
    private const string PublicRoute =
        "/api/v1/projects?status=Configuring&search=fixture%20plate&customerId=11111111-1111-1111-1111-111111111111&page=3&pageSize=25";

    [Fact]
    public async Task Get_fails_closed_before_ProjectService_for_missing_wrong_stale_denied_or_unavailable_decisions()
    {
        var scenarios = new[]
        {
            new AuthorizationScenario("missing", [], [], []),
            new AuthorizationScenario("wrong", [MalievPermissions.Project.Write], [MalievPermissions.Project.Write], []),
            new AuthorizationScenario("stale", [MalievPermissions.Project.Read], [], []),
            new AuthorizationScenario("denied", [], [], []),
            new AuthorizationScenario("unavailable", [MalievPermissions.Project.Read], [], [MalievPermissions.Project.Read])
        };

        foreach (var scenario in scenarios)
        {
            factory.Reset(scenario.LivePermissions, scenario.UnavailablePermissions);
            using var client = CreateClient(scenario.Name, scenario.Claims);

            var response = await client.GetAsync(PublicRoute);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(
                [new ProjectPermissionCheck(MalievPermissions.Project.Read, "global")],
                factory.IamClient.LiveChecks);
            Assert.Empty(factory.IamClient.StandardChecks);
            Assert.Empty(factory.DownstreamRequests);
        }
    }

    [Fact]
    public async Task Get_allows_one_current_live_decision_and_preserves_exact_ProjectService_wire_contract()
    {
        factory.Reset([MalievPermissions.Project.Read]);
        using var client = CreateClient("current-project-reader", []);

        var response = await client.GetAsync(PublicRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<ProjectSummaryDto>>();
        Assert.NotNull(payload);
        Assert.Empty(payload.Data);
        Assert.Equal(3, payload.Meta.CurrentPage);
        Assert.Equal(25, payload.Meta.PageSize);
        Assert.Equal(
            [new ProjectPermissionCheck(MalievPermissions.Project.Read, "global")],
            factory.IamClient.LiveChecks);
        Assert.Empty(factory.IamClient.StandardChecks);
        Assert.Equal(
            [new ProjectDownstreamRequest(
                "GET",
                "/project/v1/projects?page=3&pageSize=25&status=Configuring&query=fixture%20plate&customerId=11111111-1111-1111-1111-111111111111")],
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

public sealed class ProjectCollectionAuthorizationFactory : SignalRTestFactory
{
    private readonly ConcurrentQueue<ProjectDownstreamRequest> _downstreamRequests = new();

    public RecordingProjectIamServiceClient IamClient { get; } = new();

    public IReadOnlyCollection<ProjectDownstreamRequest> DownstreamRequests => _downstreamRequests.ToArray();

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
        _downstreamRequests.Enqueue(new ProjectDownstreamRequest(request.Method.Method, pathAndQuery));

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"data":[],"currentPage":3,"totalPages":0,"totalCount":0,"pageSize":25}""",
                Encoding.UTF8,
                "application/json")
        });
    }
}

public sealed record ProjectDownstreamRequest(string Method, string PathAndQuery);

public sealed record ProjectPermissionCheck(string Permission, string? ResourcePath);

public sealed class RecordingProjectIamServiceClient : IIamServiceClient
{
    private readonly ConcurrentQueue<ProjectPermissionCheck> _liveChecks = new();
    private readonly ConcurrentQueue<ProjectPermissionCheck> _standardChecks = new();
    private HashSet<string> _allowedPermissions = new(StringComparer.Ordinal);
    private HashSet<string> _unavailablePermissions = new(StringComparer.Ordinal);

    public IReadOnlyCollection<ProjectPermissionCheck> LiveChecks => _liveChecks.ToArray();
    public IReadOnlyCollection<ProjectPermissionCheck> StandardChecks => _standardChecks.ToArray();

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
        _standardChecks.Enqueue(new ProjectPermissionCheck(permissionId, resourcePath));
        return Task.FromResult(_allowedPermissions.Contains(permissionId));
    }

    public Task<bool> CheckPermissionLiveAsync(
        string principalId,
        string permissionId,
        string? resourcePath = null,
        CancellationToken cancellationToken = default)
    {
        _liveChecks.Enqueue(new ProjectPermissionCheck(permissionId, resourcePath));
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
