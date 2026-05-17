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

    private static readonly Guid JobId = Guid.NewGuid();
    private static readonly Guid MachineId = Guid.NewGuid();

    private static JobSummaryDto SampleJob() => new()
    {
        Id = JobId,
        JobNumber = "JOB-1042",
        Status = "InProgress",
        Priority = "High",
        ProcessType = "FDM",
        CustomerName = "Acme Corp"
    };

    private static ProductionQueueDto SampleQueue() => new()
    {
        Jobs = new() { SampleJob() },
        Stats = new() { InProgressCount = 1 }
    };

    private static object SampleKanbanQueue() => new
    {
        Queued = new[]
        {
            new
            {
                JobId,
                OrderId = Guid.NewGuid(),
                Technology = "FDM",
                MaterialId = Guid.NewGuid(),
                AssignedMachineId = (string?)null,
                Priority = 4,
                EstimatedPrintTimeMinutes = 120,
                StartedAt = (DateTime?)null
            }
        }
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

    private static OrderServiceClient MakeOrderClient<T>(T responseBody, HttpStatusCode code = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(code)
            { Content = JsonContent.Create(responseBody) }));
        return new OrderServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    private static UploadServiceClient MakeUploadClient<T>(T responseBody, HttpStatusCode code = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(code)
            { Content = JsonContent.Create(responseBody) }));
        return new UploadServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
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

    private static JobsController Make(JobServiceClient client) =>
        new(client, MakeOrderClient(new object()), MakeUploadClient(new object()));

    // ── GET /queue ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetQueue_WhenServiceReturnsQueue_ShouldReturnOkWithJobs()
    {
        var controller = Make(MakeClient(SampleKanbanQueue()));
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
        var controller = Make(MakeClient(SampleKanbanQueue()));
        var result = await controller.GetStats(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<JobStatsDto>(ok.Value);
        Assert.Equal(1, dto.QueuedCount);
        Assert.Equal(0, dto.OverdueCount);
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

    [Fact]
    public async Task GetAllMachineSchedules_ReturnsBoardWithFacilityMetadataAndPreservedSlotIds()
    {
        var holdId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var partId = Guid.NewGuid();
        var start = DateTime.UtcNow.Date.AddDays(1);
        var maintenanceDueDate = DateOnly.FromDateTime(start.AddDays(2));
        string? capturedFacilityUrl = null;
        string? capturedScheduleUrl = null;
        var jobHandler = new MockHttpMessageHandler((req, _) =>
        {
            capturedScheduleUrl = req.RequestUri!.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[]
                {
                    new
                    {
                        MachineId = "MAL-CNC-001",
                        Schedule = new[]
                        {
                            new
                            {
                                JobId = holdId,
                                Technology = "CNC_MILL",
                                ScheduledStart = start.AddHours(2),
                                ScheduledEnd = start.AddHours(4),
                                SetupMinutes = 60,
                                PrintMinutes = 60,
                                QueuePosition = 3,
                                Status = "Planning Hold",
                                OrderId = Guid.Empty,
                                IsHold = true,
                                HoldId = (Guid?)holdId,
                                ProjectId = (Guid?)projectId,
                                ProjectPartId = (Guid?)partId,
                                ExpiresAt = (DateTime?)DateTime.UtcNow.AddHours(72)
                            }
                        }
                    }
                })
            });
        });
        var facilityHandler = new MockHttpMessageHandler((req, _) =>
        {
            capturedFacilityUrl = req.RequestUri!.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new FacilityPagedResult<EquipmentSummaryDto>
                {
                    Items =
                    [
                        new EquipmentSummaryDto
                        {
                            Id = Guid.NewGuid(),
                            AssetCode = "MAL-FDM-001",
                            Name = "Bambulab X1C #1",
                            Category = "FdmPrinter",
                            Status = "Active"
                        },
                        new EquipmentSummaryDto
                        {
                            Id = Guid.NewGuid(),
                            AssetCode = "MAL-CNC-001",
                            Name = "HAAS VF2",
                            Category = "CncMachine",
                            Status = "UnderMaintenance",
                            NextServiceDueDate = maintenanceDueDate
                        }
                    ],
                    TotalCount = 2,
                    Page = 1,
                    PageSize = 200
                })
            });
        });
        var controller = Make(new JobServiceClient(new HttpClient(jobHandler) { BaseAddress = new Uri("http://test") }));
        var facilityClient = new FacilityServiceClient(new HttpClient(facilityHandler) { BaseAddress = new Uri("http://test") });

        var result = await controller.GetAllMachineSchedules(facilityClient, start, start.AddDays(7), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var board = Assert.IsType<ProductionScheduleBoardDto>(ok.Value);
        Assert.Contains(board.Machines, machine => machine.MachineId == "MAL-FDM-001");
        var cncMachine = Assert.Single(board.Machines, machine => machine.MachineId == "MAL-CNC-001");
        Assert.Contains(cncMachine.Slots, slot => slot.HoldId == holdId && slot.ProjectId == projectId && slot.ProjectPartId == partId);
        var maintenance = Assert.Single(cncMachine.Slots, slot => slot.IsMaintenance);
        Assert.Equal("Maintenance due", maintenance.Label);
        Assert.Equal("Maintenance", maintenance.Status);
        Assert.False(maintenance.CanMove);
        Assert.Equal(maintenanceDueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), maintenance.ScheduledStart);
        Assert.Equal(maintenanceDueDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), maintenance.ScheduledEnd);
        Assert.NotNull(capturedFacilityUrl);
        Assert.DoesNotContain("status=Active", capturedFacilityUrl);
        Assert.Contains("machineIds=MAL-FDM-001", capturedScheduleUrl);
        Assert.Contains("machineIds=MAL-CNC-001", capturedScheduleUrl);
    }

    [Fact]
    public async Task Reschedule_WhenSuccess_ShouldReturn204AndBroadcastScheduleChange()
    {
        var controller = Make(MakeClientEmpty(HttpStatusCode.NoContent));
        var (hub, allProxy) = MockHub();

        var result = await controller.Reschedule(
            JobId,
            new RescheduleJobRequest
            {
                MachineId = "MAL-FDM-001",
                ScheduledStartTime = DateTime.UtcNow.AddHours(2),
                ScheduledEndTime = DateTime.UtcNow.AddHours(4),
                QueuePosition = 1
            },
            hub,
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        allProxy.Verify(c => c.SendCoreAsync(
            "ScheduleChanged",
            It.Is<object?[]>(args => args.Length == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
