using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Maliev.Intranet.Tests.Testing;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

/// <summary>
/// Unit tests for <see cref="JobsController"/> covering BFF proxy and SignalR broadcast logic.
/// </summary>
public class JobsControllerTests
{
    // ── Fixtures ──────────────────────────────────────────────────────────────

    private static readonly Guid JobId     = Guid.NewGuid();
    private static readonly Guid MachineId = Guid.NewGuid();

    private static JobSummaryDto SampleJob() => new()
    {
        Id          = JobId,
        JobNumber   = "JOB-1042",
        Status      = "InProgress",
        Priority    = "High",
        ProcessType = "FDM",
        CustomerName = "Acme Corp"
    };

    private static ProductionQueueDto SampleQueue() => new()
    {
        Jobs  = new() { SampleJob() },
        Stats = new() { InProgressCount = 1 }
    };

    private static JobServiceClient MakeClient<T>(T responseBody, HttpStatusCode code = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(code)
                { Content = JsonContent.Create(responseBody) }));
        return new JobServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    private static JobServiceClient MakeClientEmpty(HttpStatusCode code)
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(code)));
        return new JobServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    private static (IHubContext<Maliev.Intranet.Bff.Hubs.ProductionHub> Hub, Mock<IClientProxy> ClientProxyMock) MockHub()
    {
        var allProxy = new Mock<IClientProxy>();
        allProxy.Setup(p => p.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.All).Returns(allProxy.Object);

        var hub = new Mock<IHubContext<Maliev.Intranet.Bff.Hubs.ProductionHub>>();
        hub.Setup(h => h.Clients).Returns(clients.Object);

        return (hub.Object, allProxy);
    }

    private static JobsController Make(JobServiceClient client) => new(client);

    // ── GET /queue ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetQueue_WhenServiceReturnsQueue_ShouldReturnOkWithJobs()
    {
        var controller = Make(MakeClient(SampleQueue()));
        var result = await controller.GetQueue(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProductionQueueDto>(ok.Value);
        Assert.Single(dto.Jobs);
    }

    [Fact]
    public async Task GetQueue_WhenServiceFails_ShouldReturnEmptyQueue()
    {
        var controller = Make(MakeClientEmpty(HttpStatusCode.InternalServerError));
        var result = await controller.GetQueue(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProductionQueueDto>(ok.Value);
        Assert.Empty(dto.Jobs);
    }

    // ── GET /stats ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStats_WhenServiceReturnsStats_ShouldReturnCorrectCounts()
    {
        var stats = new JobStatsDto { QueuedCount = 5, InProgressCount = 3, OverdueCount = 1 };
        var controller = Make(MakeClient(stats));
        var result = await controller.GetStats(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<JobStatsDto>(ok.Value);
        Assert.Equal(5, dto.QueuedCount);
        Assert.Equal(1, dto.OverdueCount);
    }

    // ── GET /{id} ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenJobExists_ShouldReturn200()
    {
        var detail = new JobDetailDto { Id = JobId, JobNumber = "JOB-1042" };
        var controller = Make(MakeClient(detail));
        var result = await controller.GetById(JobId, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<JobDetailDto>(ok.Value);
        Assert.Equal(JobId, dto.Id);
    }

    [Fact]
    public async Task GetById_WhenJobNotFound_ShouldReturn404()
    {
        var controller = Make(MakeClientEmpty(HttpStatusCode.NotFound));
        var result = await controller.GetById(JobId, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── PATCH /{id}/status ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_WhenSuccess_ShouldReturn204AndBroadcast()
    {
        var controller = Make(MakeClientEmpty(HttpStatusCode.NoContent));
        var (hub, allProxy) = MockHub();
        var result = await controller.UpdateStatus(
            JobId,
            new UpdateJobStatusRequest { Status = "Complete" },
            hub,
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);

        allProxy.Verify(c => c.SendCoreAsync(
            "JobStatusChanged",
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_WhenDownstreamFails_ShouldForwardStatusCode()
    {
        var controller = Make(MakeClientEmpty(HttpStatusCode.UnprocessableEntity));
        var (hub, allProxy) = MockHub();
        var result = await controller.UpdateStatus(
            JobId,
            new UpdateJobStatusRequest { Status = "Complete" },
            hub,
            CancellationToken.None);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(422, statusResult.StatusCode);

        // Verify NO broadcast when downstream fails
        allProxy.Verify(c => c.SendCoreAsync(
            "JobStatusChanged",
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── POST /{id}/assign-machine ─────────────────────────────────────────────

    [Fact]
    public async Task AssignMachine_WhenSuccess_ShouldReturn204AndBroadcast()
    {
        var controller = Make(MakeClientEmpty(HttpStatusCode.NoContent));
        var (hub, allProxy) = MockHub();
        var result = await controller.AssignMachine(
            JobId,
            new AssignMachineRequest { MachineId = MachineId },
            hub,
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);

        allProxy.Verify(c => c.SendCoreAsync(
            "JobAssigned",
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
