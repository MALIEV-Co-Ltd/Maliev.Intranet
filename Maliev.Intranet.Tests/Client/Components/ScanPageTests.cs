using Bunit;
using Maliev.Intranet.Client.Pages.Manufacturing;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Shared.Enums;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Tests.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;

namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>Tests for the Scan page component.</summary>
public class ScanPageTests : BunitContext, IAsyncLifetime
{
    private readonly MockHttpMessageHandler _httpHandler = new();
    private readonly Mock<ILogger<Scan>> _loggerMock = new();

    /// <summary>Initializes a new instance of the <see cref="ScanPageTests"/> class.</summary>
    public ScanPageTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Render<MudPopoverProvider>();

        var client = new HttpClient(_httpHandler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);
        Services.AddSingleton(_loggerMock.Object);
    }

    /// <summary>Initializes the test asynchronously.</summary>
    public Task InitializeAsync() => Task.CompletedTask;
    /// <summary>Disposes resources used by the test asynchronously.</summary>
    public new async Task DisposeAsync()
    {
        _httpHandler.Dispose();
        await base.DisposeAsync();
    }

    [Fact]
    /// <summary>Verifies that the scan page title is rendered.</summary>
    public void ShouldRenderScanTitle()
    {
        var cut = Render<Scan>();

        Assert.Contains("Scan Job QR Code", cut.Markup);
    }

    [Fact]
    /// <summary>Verifies that the QR reader element is rendered on the scan page.</summary>
    public void ShouldRenderQrReaderElement()
    {
        var cut = Render<Scan>();

        Assert.Contains("qr-reader", cut.Markup);
    }

    [Fact]
    /// <summary>Verifies that a job is loaded and displayed when a job identifier is provided as a route parameter.</summary>
    public async Task ShouldLoadJobFromRouteParameter()
    {
        var jobId = Guid.NewGuid();
        var job = new JobDto
        {
            Id = jobId,
            OrderId = Guid.NewGuid(),
            MaterialId = Guid.NewGuid(),
            VolumeCm3 = 25.5m,
            Technology = "FDM",
            Status = JobStatus.Pending,
            AssignedMachineId = "PRUSA-01",
            Priority = 1,
            CreatedAt = DateTime.UtcNow
        };

        _httpHandler.When($"/api/jobs/{jobId}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(job));

        var cut = Render<Scan>(parameters => parameters
            .Add(p => p.JobId, jobId.ToString()));

        cut.WaitForState(() => cut.Markup.Contains("Job Found"), TimeSpan.FromSeconds(5));

        Assert.Contains("Job Found", cut.Markup);
        Assert.Contains("FDM", cut.Markup);
    }

    [Fact]
    /// <summary>Verifies that the correct status action is displayed for a job in the Pending status.</summary>
    public async Task ShouldDisplayCorrectStatusColorForPending()
    {
        var jobId = Guid.NewGuid();
        var job = new JobDto
        {
            Id = jobId,
            OrderId = Guid.NewGuid(),
            MaterialId = Guid.NewGuid(),
            VolumeCm3 = 25.5m,
            Technology = "FDM",
            Status = JobStatus.Pending,
            Priority = 1,
            CreatedAt = DateTime.UtcNow
        };

        _httpHandler.When($"/api/jobs/{jobId}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(job));

        var cut = Render<Scan>(parameters => parameters
            .Add(p => p.JobId, jobId.ToString()));

        cut.WaitForState(() => cut.Markup.Contains("Queue Job"), TimeSpan.FromSeconds(5));

        Assert.Contains("Queue Job", cut.Markup);
    }

    [Fact]
    /// <summary>Verifies that the Start button is displayed for a job in the Queued status.</summary>
    public async Task ShouldDisplayStartButtonForQueuedJob()
    {
        var jobId = Guid.NewGuid();
        var job = new JobDto
        {
            Id = jobId,
            OrderId = Guid.NewGuid(),
            MaterialId = Guid.NewGuid(),
            VolumeCm3 = 25.5m,
            Technology = "SLA",
            Status = JobStatus.Queued,
            AssignedMachineId = "RESIN-01",
            Priority = 1,
            CreatedAt = DateTime.UtcNow
        };

        _httpHandler.When($"/api/jobs/{jobId}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(job));

        var cut = Render<Scan>(parameters => parameters
            .Add(p => p.JobId, jobId.ToString()));

        cut.WaitForState(() => cut.Markup.Contains("Start"), TimeSpan.FromSeconds(5));

        Assert.Contains("Start", cut.Markup);
        Assert.DoesNotContain("Queue Job", cut.Markup);
    }

    [Fact]
    /// <summary>Verifies that the Complete button is displayed for a job in the Finishing status.</summary>
    public async Task ShouldDisplayCompleteButtonForFinishingJob()
    {
        var jobId = Guid.NewGuid();
        var job = new JobDto
        {
            Id = jobId,
            OrderId = Guid.NewGuid(),
            MaterialId = Guid.NewGuid(),
            VolumeCm3 = 25.5m,
            Technology = "CNC",
            Status = JobStatus.Finishing,
            AssignedMachineId = "HAAS-VF2",
            Priority = 1,
            CreatedAt = DateTime.UtcNow
        };

        _httpHandler.When($"/api/jobs/{jobId}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(job));

        var cut = Render<Scan>(parameters => parameters
            .Add(p => p.JobId, jobId.ToString()));

        cut.WaitForState(() => cut.Markup.Contains("Complete"), TimeSpan.FromSeconds(5));

        Assert.Contains("Complete", cut.Markup);
    }

    [Fact]
    /// <summary>Verifies that the assigned machine identifier is displayed when a machine is assigned to the job.</summary>
    public async Task ShouldDisplayMachineIdWhenAssigned()
    {
        var jobId = Guid.NewGuid();
        var job = new JobDto
        {
            Id = jobId,
            OrderId = Guid.NewGuid(),
            MaterialId = Guid.NewGuid(),
            VolumeCm3 = 25.5m,
            Technology = "FDM",
            Status = JobStatus.Queued,
            AssignedMachineId = "PRUSA-MK4",
            Priority = 1,
            CreatedAt = DateTime.UtcNow
        };

        _httpHandler.When($"/api/jobs/{jobId}")
            .Respond(HttpStatusCode.OK, JsonContent.Create(job));

        var cut = Render<Scan>(parameters => parameters
            .Add(p => p.JobId, jobId.ToString()));

        cut.WaitForState(() => cut.Markup.Contains("PRUSA-MK4"), TimeSpan.FromSeconds(5));

        Assert.Contains("PRUSA-MK4", cut.Markup);
    }
}
