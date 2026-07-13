using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Security;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Bff.Hubs;
using Maliev.Intranet.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace Maliev.Intranet.Tests.Bff.Security;

public sealed class JobResourceAuthorizationTests(
    JobResourceAuthorizationFactory factory) : IClassFixture<JobResourceAuthorizationFactory>
{
    [Fact]
    public async Task Job_detail_uses_only_the_live_grant_for_the_route_job()
    {
        var allowedJobId = Guid.NewGuid();
        var deniedJobId = Guid.NewGuid();
        factory.Reset(MalievPermissions.Job.Read, $"jobs/{allowedJobId:D}");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken("job-resource-reader"));

        var denied = await client.GetAsync($"/api/v1/jobs/{deniedJobId:D}");
        var allowed = await client.GetAsync($"/api/v1/jobs/{allowedJobId:D}");

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, allowed.StatusCode);
        Assert.Contains(
            factory.IamClient.LiveChecks,
            check => check == new PermissionCheck(MalievPermissions.Job.Read, $"jobs/{deniedJobId:D}"));
        Assert.Contains(
            factory.IamClient.LiveChecks,
            check => check == new PermissionCheck(MalievPermissions.Job.Read, $"jobs/{allowedJobId:D}"));
        Assert.DoesNotContain(factory.DownstreamRequests, request => request.Contains(deniedJobId.ToString("D"), StringComparison.Ordinal));
        Assert.Contains(factory.DownstreamRequests, request => request == $"GET /job/v1/jobs/{allowedJobId:D}");
    }

    [Fact]
    public async Task Job_update_accepts_a_scoped_live_write_grant_without_global_claims()
    {
        var jobId = Guid.NewGuid();
        factory.Reset(MalievPermissions.Job.Write, $"jobs/{jobId:D}");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken("job-resource-editor"));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/jobs/{jobId:D}/status",
            new { status = "InProgress" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(
            factory.IamClient.LiveChecks,
            check => check == new PermissionCheck(MalievPermissions.Job.Write, $"jobs/{jobId:D}"));
        Assert.Contains(factory.DownstreamRequests, request => request == $"POST /job/v1/jobs/{jobId:D}/start");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ticket_scan_authorizes_the_parsed_job_before_lookup(bool useTrustedUrl)
    {
        var jobId = Guid.NewGuid();
        factory.Reset(MalievPermissions.Job.Read, $"jobs/{jobId:D}");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken("job-ticket-reader"));
        var code = useTrustedUrl
            ? $"https://intranet.maliev.com/mfg/production-schedule?jobId={jobId:D}"
            : jobId.ToString("D");

        var response = await client.PostAsJsonAsync(
            "/api/v1/jobs/ticket-scan",
            new { code });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(
            factory.IamClient.LiveChecks,
            check => check == new PermissionCheck(MalievPermissions.Job.Read, $"jobs/{jobId:D}"));
        Assert.Contains(factory.DownstreamRequests, request => request == $"GET /job/v1/jobs/{jobId:D}");
    }

    [Fact]
    public async Task Ticket_scan_denial_prevents_the_job_lookup()
    {
        var allowedJobId = Guid.NewGuid();
        var deniedJobId = Guid.NewGuid();
        factory.Reset(MalievPermissions.Job.Read, $"jobs/{allowedJobId:D}");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken("stale-job-ticket-reader", MalievPermissions.Job.Read));

        var response = await client.PostAsJsonAsync(
            "/api/v1/jobs/ticket-scan",
            new { code = deniedJobId.ToString("D") });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(
            factory.IamClient.LiveChecks,
            check => check == new PermissionCheck(MalievPermissions.Job.Read, $"jobs/{deniedJobId:D}"));
        Assert.DoesNotContain(factory.DownstreamRequests, request => request.Contains(deniedJobId.ToString("D"), StringComparison.Ordinal));
    }

    [Fact]
    public async Task Ticket_scan_fails_closed_when_live_iam_is_unavailable()
    {
        var jobId = Guid.NewGuid();
        factory.Reset(
            MalievPermissions.Job.Read,
            $"jobs/{jobId:D}",
            throwOnLiveCheck: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken("stale-job-ticket-reader", MalievPermissions.Job.Read));

        var response = await client.PostAsJsonAsync(
            "/api/v1/jobs/ticket-scan",
            new { code = jobId.ToString("D") });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(
            factory.IamClient.LiveChecks,
            check => check == new PermissionCheck(MalievPermissions.Job.Read, $"jobs/{jobId:D}"));
        Assert.DoesNotContain(factory.DownstreamRequests, request => request.Contains(jobId.ToString("D"), StringComparison.Ordinal));
    }

    [Fact]
    public async Task Authorization_manifest_records_dynamic_ticket_resource_enforcement()
    {
        factory.Reset(MalievPermissions.IAM.Permissions.Read, "global");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken("manifest-reader", MalievPermissions.IAM.Permissions.Read));

        var manifest = await client.GetFromJsonAsync<EndpointAuthorizationManifest>(
            "/api/v1/system/authorization-manifest");

        Assert.NotNull(manifest);
        var entry = Assert.Single(
            manifest.Entries,
            candidate => candidate.Member == nameof(JobsController.ResolveTicketScan));
        Assert.Contains(
            entry.PermissionRequirements,
            requirement => requirement == new PermissionAuthorizationManifest(
                MalievPermissions.Job.Read,
                "jobs/{parsedJobId}",
                true));
        Assert.Equal(
            new ResourceOwnershipManifest(
                ResourceOwnershipKind.BffValidated,
                "IAMService",
                "request.code"),
            entry.Ownership);
    }

    [Fact]
    public async Task Planning_hold_update_authorizes_the_resolved_project_before_mutation()
    {
        var holdId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        factory.Reset(
            MalievPermissions.Job.Write,
            $"projects/{projectId:D}",
            planningHold: CreatePlanningHold(holdId, projectId));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken("project-planner"));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/jobs/planning-holds/{holdId:D}",
            CreatePlanningHoldUpdate());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(
            factory.IamClient.LiveChecks,
            check => check == new PermissionCheck(MalievPermissions.Job.Write, $"projects/{projectId:D}"));
        Assert.Equal(
            [
                $"GET /job/v1/jobs/planning-holds/{holdId:D}",
                $"PATCH /job/v1/jobs/planning-holds/{holdId:D}"
            ],
            factory.DownstreamRequests.Where(request => request.Contains(holdId.ToString("D"), StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Planning_hold_wrong_project_grant_cannot_reach_mutation()
    {
        var holdId = Guid.NewGuid();
        var ownedProjectId = Guid.NewGuid();
        var allowedProjectId = Guid.NewGuid();
        factory.Reset(
            MalievPermissions.Job.Write,
            $"projects/{allowedProjectId:D}",
            planningHold: CreatePlanningHold(holdId, ownedProjectId));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken("stale-global-planner", MalievPermissions.Job.Write));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/jobs/planning-holds/{holdId:D}",
            CreatePlanningHoldUpdate());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(
            factory.IamClient.LiveChecks,
            check => check == new PermissionCheck(MalievPermissions.Job.Write, $"projects/{ownedProjectId:D}"));
        Assert.Contains(factory.DownstreamRequests, request => request == $"GET /job/v1/jobs/planning-holds/{holdId:D}");
        Assert.DoesNotContain(factory.DownstreamRequests, request => request == $"PATCH /job/v1/jobs/planning-holds/{holdId:D}");
    }

    [Fact]
    public async Task Planning_hold_update_fails_closed_when_live_iam_is_unavailable()
    {
        var holdId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        factory.Reset(
            MalievPermissions.Job.Write,
            $"projects/{projectId:D}",
            throwOnLiveCheck: true,
            planningHold: CreatePlanningHold(holdId, projectId));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken("stale-global-planner", MalievPermissions.Job.Write));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/jobs/planning-holds/{holdId:D}",
            CreatePlanningHoldUpdate());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain(factory.DownstreamRequests, request => request == $"PATCH /job/v1/jobs/planning-holds/{holdId:D}");
    }

    [Fact]
    public async Task Missing_planning_hold_returns_not_found_before_authorization_or_mutation()
    {
        var holdId = Guid.NewGuid();
        factory.Reset(MalievPermissions.Job.Write, $"projects/{Guid.NewGuid():D}");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken("global-planner", MalievPermissions.Job.Write));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/jobs/planning-holds/{holdId:D}",
            CreatePlanningHoldUpdate());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(factory.IamClient.LiveChecks);
        Assert.Contains(factory.DownstreamRequests, request => request == $"GET /job/v1/jobs/planning-holds/{holdId:D}");
        Assert.DoesNotContain(factory.DownstreamRequests, request => request == $"PATCH /job/v1/jobs/planning-holds/{holdId:D}");
    }

    [Fact]
    public async Task Authorization_manifest_records_dynamic_planning_hold_project_enforcement()
    {
        factory.Reset(MalievPermissions.IAM.Permissions.Read, "global");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTestToken("manifest-reader", MalievPermissions.IAM.Permissions.Read));

        var manifest = await client.GetFromJsonAsync<EndpointAuthorizationManifest>(
            "/api/v1/system/authorization-manifest");

        Assert.NotNull(manifest);
        var entry = Assert.Single(
            manifest.Entries,
            candidate => candidate.Member == nameof(JobsController.UpdatePlanningHold)
                && candidate.Surface.Contains("/jobs/", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            entry.PermissionRequirements,
            requirement => requirement == new PermissionAuthorizationManifest(
                MalievPermissions.Job.Write,
                "projects/{resolvedProjectId}",
                true));
        Assert.Equal(
            new ResourceOwnershipManifest(
                ResourceOwnershipKind.BffValidated,
                "JobService+IAMService",
                "holdId"),
            entry.Ownership);
    }

    private static ProductionPlanningHoldDto CreatePlanningHold(Guid holdId, Guid projectId) => new()
    {
        Id = holdId,
        ProjectId = projectId,
        ProjectPartId = Guid.NewGuid(),
        MachineId = "MAL-FDM-001",
        Status = "Active"
    };

    private static UpdateProductionPlanningHoldRequest CreatePlanningHoldUpdate() => new()
    {
        MachineId = "MAL-FDM-002",
        ScheduledStartTime = DateTime.UtcNow.AddHours(2),
        ScheduledEndTime = DateTime.UtcNow.AddHours(4),
        ProductionTimeMinutes = 60,
        ExpiresAt = DateTime.UtcNow.AddHours(24)
    };
}

public sealed class JobResourceAuthorizationFactory : SignalRTestFactory
{
    private readonly ConcurrentQueue<string> _downstreamRequests = new();
    private ProductionPlanningHoldDto? _planningHold;

    public RecordingIamServiceClient IamClient { get; } = new();
    public IReadOnlyCollection<string> DownstreamRequests => _downstreamRequests.ToArray();

    public void Reset(
        string permission,
        string resourcePath,
        bool throwOnLiveCheck = false,
        ProductionPlanningHoldDto? planningHold = null)
    {
        IamClient.Reset(permission, resourcePath, throwOnLiveCheck);
        _planningHold = planningHold;
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
                httpBuilder.PrimaryHandler = new MockHttpMessageHandler((request, _) =>
                {
                    var requestUri = request.RequestUri!;
                    _downstreamRequests.Enqueue($"{request.Method} {requestUri.PathAndQuery}");
                    if (request.Method == HttpMethod.Get &&
                        requestUri.AbsolutePath.StartsWith(
                            "/job/v1/jobs/planning-holds/",
                            StringComparison.Ordinal))
                    {
                        return Task.FromResult(_planningHold is null
                            ? new HttpResponseMessage(HttpStatusCode.NotFound)
                            : new HttpResponseMessage(HttpStatusCode.OK)
                            {
                                Content = JsonContent.Create(_planningHold)
                            });
                    }

                    var response = new HttpResponseMessage(
                        request.Method == HttpMethod.Get
                            ? HttpStatusCode.OK
                            : HttpStatusCode.NoContent);
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
