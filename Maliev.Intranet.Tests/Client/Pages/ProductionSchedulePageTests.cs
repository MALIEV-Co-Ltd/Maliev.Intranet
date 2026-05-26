using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

using Bunit;

using Maliev.Intranet.Client.Components.Production;
using Maliev.Intranet.Client.Pages.Manufacturing;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Pages;

public sealed class ProductionSchedulePageTests : BunitContext, IAsyncLifetime
{
    private readonly Guid _projectId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private readonly Guid _partId = Guid.Parse("22222222-3333-4444-5555-666666666666");
    private readonly Guid _jobId = Guid.Parse("33333333-4444-5555-6666-777777777777");
    private readonly Guid _holdId = Guid.Parse("44444444-5555-6666-7777-888888888888");
    private readonly List<string> _requestedRequests = [];
    private JsonDocument? _rescheduleRequest;
    private DateTime? _boardRangeStart;

    public ProductionSchedulePageTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<LayoutService>(new LayoutService(JSInterop.JSRuntime, NullLogger<LayoutService>.Instance));

        var handler = new MockHttpMessageHandler(HandleRequestAsync);
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://test/") });
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void ProductionSchedule_RendersAllMachineBoardAndLockedSlots()
    {
        var cut = Render<ProductionSchedule>();

        cut.WaitForAssertion(() => Assert.Contains("production-schedule-board", cut.Markup));

        Assert.Contains("Production schedule", cut.Markup);
        Assert.Contains("Scheduled work centers", cut.Markup);
        Assert.Contains("data-machine-id=\"CNC-01\"", cut.Markup);
        Assert.Contains("data-machine-id=\"FDM-01\"", cut.Markup);
        Assert.Contains("CNC Mill 01", cut.Markup);
        Assert.Contains("FDM Printer 01", cut.Markup);
        Assert.Contains("JOB-2001", cut.Markup);
        Assert.Contains("HOLD-4001", cut.Markup);
        Assert.Contains("Maintenance due", cut.Markup);
        Assert.Contains("psb-slot-maintenance", cut.Markup);
        Assert.Contains("> Maintenance</span>", cut.Markup);
        Assert.Contains("psb-slot-locked", cut.Markup);
        Assert.Contains("aria-disabled=\"true\"", cut.Markup);
        Assert.Contains("Move", cut.Markup);
        Assert.Contains(_requestedRequests, request => request.StartsWith("GET /api/v1/jobs/schedule?from=", StringComparison.Ordinal));
    }

    [Fact]
    public void ProductionSchedule_TimelineZoomExpandsQueueFromWheelAndControls()
    {
        var cut = Render<ProductionSchedule>();

        cut.WaitForAssertion(() => Assert.Contains("data-board-zoom=\"100\"", cut.Markup));

        Assert.Contains("aria-label=\"Zoom out schedule timeline\"", cut.Markup);
        Assert.Contains("aria-label=\"Reset schedule zoom\"", cut.Markup);
        Assert.Contains("aria-label=\"Zoom in schedule timeline\"", cut.Markup);
        Assert.Contains("Scroll over the queue to zoom", cut.Markup);

        cut.Find(".production-schedule-board-shell").TriggerEvent("onwheel", new WheelEventArgs { DeltaY = -120 });
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-board-zoom=\"125\"", cut.Markup);
            Assert.Contains("minmax(225px, 275px)", cut.Markup);
        });

        cut.Find(".production-schedule-board-shell").TriggerEvent("onwheel", new WheelEventArgs { DeltaY = 120 });
        cut.WaitForAssertion(() => Assert.Contains("data-board-zoom=\"100\"", cut.Markup));

        cut.Find("button[aria-label='Zoom in schedule timeline']").Click();
        cut.WaitForAssertion(() => Assert.Contains("data-board-zoom=\"125\"", cut.Markup));

        cut.Find("button[aria-label='Reset schedule zoom']").Click();
        cut.WaitForAssertion(() => Assert.Contains("data-board-zoom=\"100\"", cut.Markup));
    }

    [Fact]
    public void ProductionSchedule_MoveQueuedJob_PatchesScheduleAndRefreshesBoard()
    {
        var cut = Render<ProductionSchedule>();

        cut.WaitForAssertion(() => Assert.Contains("JOB-2001", cut.Markup));
        cut.Find($"button[data-job-id='{_jobId}'] .psb-slot-move").Click();

        cut.WaitForAssertion(() => Assert.Contains("Reschedule slot", cut.Markup));
        var machineSelect = cut.Find(".production-move-panel select");
        Assert.True(machineSelect.HasAttribute("disabled"));
        Assert.Equal("production-move-machine-lock", machineSelect.GetAttribute("aria-describedby"));
        Assert.Contains("Locked to the selected slot.", cut.Markup);

        cut.Find(".production-move-grid input[type='number']").Input("2");
        cut.Find("button.production-move-save").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(_requestedRequests, request => request == $"PATCH /api/v1/jobs/{_jobId}/schedule");
            Assert.NotNull(_rescheduleRequest);
        });

        var root = _rescheduleRequest!.RootElement;
        Assert.Equal("CNC-01", root.GetProperty("machineId").GetString());
        Assert.True(root.GetProperty("cascadeFollowingJobs").GetBoolean());
        Assert.Contains(_requestedRequests, request => request.StartsWith("GET /api/v1/jobs/schedule?from=", StringComparison.Ordinal));
    }

    [Fact]
    public void ProductionSchedule_MovePastSlot_DisablesSaveAndShowsInlineValidation()
    {
        _boardRangeStart = DateTime.UtcNow.Date.AddDays(-1);
        var cut = Render<ProductionSchedule>();

        cut.WaitForAssertion(() => Assert.Contains("JOB-2001", cut.Markup));
        cut.Find($"button[data-job-id='{_jobId}'] .psb-slot-move").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Choose a future start time before saving this schedule.", cut.Markup);
            Assert.True(cut.Find("button.production-move-save").HasAttribute("disabled"));
        });
        Assert.DoesNotContain(_requestedRequests, request => request == $"PATCH /api/v1/jobs/{_jobId}/schedule");
    }

    [Fact]
    public void ProductionSchedule_ClickJobSlot_ShowsJobAndOrderDetails()
    {
        var cut = Render<ProductionSchedule>();

        cut.WaitForAssertion(() => Assert.Contains("JOB-2001", cut.Markup));
        cut.Find($"button[data-job-id='{_jobId}']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Job information", cut.Markup);
            Assert.Contains("Bangkok Precision Parts", cut.Markup);
            Assert.Contains("SO-5005", cut.Markup);
            Assert.Contains("Bracket left machining", cut.Markup);
            Assert.Contains("12 pcs", cut.Markup);
            Assert.Contains("CNC milling", cut.Markup);
            Assert.Contains("CNC Mill 01", cut.Markup);
            Assert.Contains("bracket-left.stl", cut.Markup);
            Assert.Contains("View project", cut.Markup);
        });
        Assert.Contains(_requestedRequests, request => request == $"GET /api/v1/jobs/{_jobId}");
    }

    [Fact]
    public void ProductionSchedule_ClickPlanningHold_ShowsHoldDetailsWithoutJobFetch()
    {
        var cut = Render<ProductionSchedule>();

        cut.WaitForAssertion(() => Assert.Contains("HOLD-4001", cut.Markup));
        cut.Find($"button[data-hold-id='{_holdId}']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Planning hold", cut.Markup);
            Assert.Contains("sensor-cover.3mf", cut.Markup);
            Assert.Contains("Project", cut.Markup);
            Assert.Contains("Expires", cut.Markup);
            Assert.Contains("Move slot", cut.Markup);
        });
        Assert.DoesNotContain(_requestedRequests, request => request == $"GET /api/v1/jobs/{_holdId}");
    }

    [Fact]
    public void ProductionScheduleBoard_CurrentTimeMarker_RendersWithinVisibleRangeAndHighlightsRunningSlot()
    {
        var now = new DateTime(2026, 5, 7, 10, 30, 0, DateTimeKind.Utc);
        _boardRangeStart = now.Date;
        var board = BuildBoard();

        var cut = Render<ProductionScheduleBoard>(parameters => parameters
            .Add(component => component.Board, board)
            .Add(component => component.CurrentTimeUtc, now));

        Assert.Contains("psb-now-overlay", cut.Markup);
        Assert.Contains("data-current-time=\"2026-05-07T10:30:00.0000000Z\"", cut.Markup);
        Assert.Contains("data-current-position=\"6.25\"", cut.Markup);
        Assert.Contains("psb-slot-running-now", cut.Markup);
        Assert.Contains("psb-slot-live", cut.Markup);
        cut.WaitForAssertion(() => Assert.Contains(
            JSInterop.Invocations,
            invocation => invocation.Identifier == "malievProductionSchedule.scrollCurrentTimeIntoView"));
    }

    [Fact]
    public void ProductionScheduleBoard_LateCurrentTime_RequestsInitialScrollToNowMarker()
    {
        var now = new DateTime(2026, 5, 7, 19, 0, 0, DateTimeKind.Utc);
        _boardRangeStart = now.Date;
        var board = BuildBoard();

        var cut = Render<ProductionScheduleBoard>(parameters => parameters
            .Add(component => component.Board, board)
            .Add(component => component.CurrentTimeUtc, now));

        Assert.Contains("psb-now-overlay", cut.Markup);
        Assert.Contains("data-current-time=\"2026-05-07T19:00:00.0000000Z\"", cut.Markup);
        cut.FindAll("button").Single(button => button.TextContent.Trim().Equals("Day", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.Contains("data-current-position=\"79.167\"", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains(
            JSInterop.Invocations,
            invocation => invocation.Identifier == "malievProductionSchedule.scrollCurrentTimeIntoView"));
    }

    [Fact]
    public void ProductionScheduleBoard_SlotsHaveHoverPreviewAndStayBehindMachineColumn()
    {
        var board = BuildBoard();

        var cut = Render<ProductionScheduleBoard>(parameters => parameters
            .Add(component => component.Board, board));
        var css = ReadRepoFile("Maliev.Intranet.Client", "Components", "Production", "ProductionScheduleBoard.razor.css")
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains("psb-slot-preview", cut.Markup);
        Assert.Contains("role=\"tooltip\"", cut.Markup);
        Assert.Contains("Queue #1", cut.Markup);
        Assert.Contains("bracket-left.stl", cut.Markup);
        Assert.Contains("CNC Mill 01", cut.Markup);
        Assert.Contains("CNC milling", cut.Markup);
        Assert.Contains("2h", cut.Markup);

        Assert.Contains("isolation: isolate;", ExtractCssBlock(css, ".production-schedule-board-shell {"), StringComparison.Ordinal);
        Assert.Contains("z-index: 30;", ExtractCssBlock(css, "\n.psb-machine-cell {"), StringComparison.Ordinal);
        Assert.Contains("overflow: visible;", ExtractCssBlock(css, ".psb-track {"), StringComparison.Ordinal);
        Assert.Contains("z-index: 18;", ExtractCssBlock(css, ".psb-slot:hover,"), StringComparison.Ordinal);
        Assert.Contains(".psb-slot:hover .psb-slot-preview,", css, StringComparison.Ordinal);
        Assert.Contains(".psb-slot:focus-visible .psb-slot-preview", css, StringComparison.Ordinal);
    }

    private Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request, CancellationToken _)
    {
        var pathAndQuery = request.RequestUri?.PathAndQuery ?? string.Empty;
        _requestedRequests.Add($"{request.Method.Method} {pathAndQuery}");

        if (request.Method == HttpMethod.Get
            && pathAndQuery.StartsWith("/api/v1/jobs/schedule", StringComparison.Ordinal))
        {
            return Json(BuildBoard());
        }

        if (request.Method == HttpMethod.Get
            && pathAndQuery.Equals($"/api/v1/jobs/{_jobId}", StringComparison.Ordinal))
        {
            return Json(BuildJobDetail());
        }

        if (request.Method == HttpMethod.Patch
            && pathAndQuery.Equals($"/api/v1/jobs/{_jobId}/schedule", StringComparison.Ordinal))
        {
            _rescheduleRequest = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }

        if (request.Method == HttpMethod.Patch
            && pathAndQuery.Equals($"/api/v1/jobs/planning-holds/{_holdId}", StringComparison.Ordinal))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private ProductionScheduleBoardDto BuildBoard()
    {
        var rangeStart = _boardRangeStart ?? DateTime.UtcNow.Date.AddDays(1);
        return new ProductionScheduleBoardDto
        {
            RangeStart = rangeStart,
            RangeEnd = rangeStart.AddDays(7),
            Machines =
            [
                new ProductionScheduleMachineDto
                {
                    MachineId = "CNC-01",
                    MachineName = "CNC Mill 01",
                    Category = "CncMachine",
                    Technology = "CNC_MILL",
                    Slots =
                    [
                        new ProductionScheduleSlotDto
                        {
                            SlotId = _jobId,
                            JobId = _jobId,
                            ProjectId = _projectId,
                            ProjectPartId = _partId,
                            FileName = "bracket-left.stl",
                            MachineId = "CNC-01",
                            MachineName = "CNC Mill 01",
                            Technology = "CNC_MILL",
                            ScheduledStart = rangeStart.AddHours(10),
                            ScheduledEnd = rangeStart.AddHours(12),
                            SetupMinutes = 30,
                            ProductionMinutes = 90,
                            QueuePosition = 1,
                            Status = "Queued",
                            Label = "JOB-2001",
                            CanMove = true
                        },
                        new ProductionScheduleSlotDto
                        {
                            SlotId = Guid.Parse("55555555-6666-7777-8888-999999999999"),
                            JobId = Guid.Parse("55555555-6666-7777-8888-999999999999"),
                            MachineId = "CNC-01",
                            MachineName = "CNC Mill 01",
                            Technology = "CNC_MILL",
                            ScheduledStart = rangeStart.AddHours(14),
                            ScheduledEnd = rangeStart.AddHours(16),
                            SetupMinutes = 30,
                            ProductionMinutes = 90,
                            QueuePosition = 2,
                            Status = "InProduction",
                            Label = "JOB-LOCKED",
                            CanMove = false
                        },
                        new ProductionScheduleSlotDto
                        {
                            SlotId = Guid.Parse("77777777-8888-9999-aaaa-bbbbbbbbbbbb"),
                            MachineId = "CNC-01",
                            MachineName = "CNC Mill 01",
                            Technology = "CNC_MILL",
                            ScheduledStart = rangeStart.AddDays(2),
                            ScheduledEnd = rangeStart.AddDays(3),
                            Status = "Maintenance",
                            Label = "Maintenance due",
                            IsMaintenance = true,
                            CanMove = false
                        }
                    ]
                },
                new ProductionScheduleMachineDto
                {
                    MachineId = "FDM-01",
                    MachineName = "FDM Printer 01",
                    Category = "FdmPrinter",
                    Technology = "FDM",
                    Slots =
                    [
                        new ProductionScheduleSlotDto
                        {
                            SlotId = _holdId,
                            HoldId = _holdId,
                            ProjectId = _projectId,
                            ProjectPartId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa"),
                            FileName = "sensor-cover.3mf",
                            MachineId = "FDM-01",
                            MachineName = "FDM Printer 01",
                            Technology = "FDM",
                            ScheduledStart = rangeStart.AddHours(9),
                            ScheduledEnd = rangeStart.AddHours(13),
                            SetupMinutes = 30,
                            ProductionMinutes = 210,
                            QueuePosition = 1,
                            Status = "ActiveHold",
                            Label = "HOLD-4001",
                            ExpiresAt = rangeStart.AddDays(3),
                            IsHold = true,
                            CanMove = true
                        }
                    ]
                }
            ]
        };
    }

    private JobDetailDto BuildJobDetail() => new()
    {
        Id = _jobId,
        JobNumber = "JOB-2001",
        CustomerName = "Bangkok Precision Parts",
        OrderId = Guid.Parse("88888888-9999-aaaa-bbbb-cccccccccccc"),
        OrderNumber = "SO-5005",
        PartDescription = "Bracket left machining",
        ProcessType = "CNC_MILL",
        Material = "Aluminum 6061-T6",
        Priority = "High",
        Status = "Queued",
        MachineName = "CNC Mill 01",
        MachineId = Guid.Parse("99999999-aaaa-bbbb-cccc-dddddddddddd"),
        Quantity = 12,
        ScheduledStartTime = new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc),
        ScheduledEndTime = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc),
        QueuePosition = 1,
        AssignedTo = "Natt Operator",
        UpdatedAt = new DateTime(2026, 5, 7, 8, 0, 0, DateTimeKind.Utc)
    };

    private static Task<HttpResponseMessage> Json<T>(T body) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(body)
        });

    private static string ReadRepoFile(params string[] path)
    {
        var startDirectories = new[]
        {
            GetSourceDirectory(),
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        };

        foreach (var startDirectory in startDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var current = new DirectoryInfo(startDirectory);
            while (current is not null)
            {
                var candidate = Path.Combine(new[] { current.FullName }.Concat(path).ToArray());
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                current = current.Parent;
            }
        }

        throw new FileNotFoundException($"Unable to locate {Path.Combine(path)}.");
    }

    private static string ExtractCssBlock(string source, string marker)
    {
        var blockStart = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(blockStart >= 0, $"Expected CSS block '{marker}' to exist.");

        var braceStart = source.IndexOf('{', blockStart);
        Assert.True(braceStart >= blockStart, $"Expected CSS block '{marker}' to open with a brace.");

        var depth = 0;
        for (var index = braceStart; index < source.Length; index++)
        {
            depth += source[index] switch
            {
                '{' => 1,
                '}' => -1,
                _ => 0
            };

            if (depth == 0)
            {
                return source[blockStart..(index + 1)];
            }
        }

        throw new InvalidOperationException($"Unable to find the end of CSS block '{marker}'.");
    }

    private static string GetSourceDirectory([CallerFilePath] string sourceFile = "")
        => Path.GetDirectoryName(sourceFile) ?? Directory.GetCurrentDirectory();
}
