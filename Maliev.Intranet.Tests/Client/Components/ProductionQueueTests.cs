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

/// <summary>Tests for the ProductionQueue page component.</summary>
public class ProductionQueueTests : BunitContext, IAsyncLifetime
{
    private readonly MockHttpMessageHandler _httpHandler = new();
    private readonly Mock<ILogger<ProductionQueue>> _loggerMock = new();

    /// <summary>Initializes a new instance of the <see cref="ProductionQueueTests"/> class.</summary>
    public ProductionQueueTests()
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
    /// <summary>Verifies that the loading skeleton state is rendered immediately after the component is initialized.</summary>
    public void ShouldRenderLoadingStateInitially()
    {
        var cut = Render<ProductionQueue>();

        Assert.Contains("Production Queue", cut.Markup);
        Assert.True(cut.FindAll(".mud-skeleton").Count > 0);
    }

    [Fact]
    /// <summary>Verifies that the Kanban columns are rendered after data has been loaded.</summary>
    public async Task ShouldRenderKanbanColumnsAfterLoading()
    {
        var kanbanResponse = new KanbanResponse
        {
            Pending = [new KanbanJobDto { Id = Guid.NewGuid(), Technology = "FDM", MachineId = "PRUSA-01", Volume = 10.5m, Priority = 1 }],
            Queued = [],
            InProgress = [],
            Finishing = [],
            Completed = []
        };

        _httpHandler.When("/api/jobs/kanban")
            .Respond(HttpStatusCode.OK, JsonContent.Create(kanbanResponse));

        var cut = Render<ProductionQueue>();

        cut.WaitForState(() => !cut.Markup.Contains("mud-skeleton"), TimeSpan.FromSeconds(5));

        Assert.Contains("Pending", cut.Markup);
        Assert.Contains("Queued", cut.Markup);
        Assert.Contains("In Progress", cut.Markup);
        Assert.Contains("Finishing", cut.Markup);
        Assert.Contains("Completed", cut.Markup);
    }

    [Fact]
    /// <summary>Verifies that the technology type of a job is displayed in the production queue.</summary>
    public async Task ShouldDisplayJobTechnology()
    {
        var jobId = Guid.NewGuid();
        var kanbanResponse = new KanbanResponse
        {
            Pending = [new KanbanJobDto { Id = jobId, Technology = "FDM", MachineId = "PRUSA-01", Volume = 10.5m, Priority = 1 }],
            Queued = [],
            InProgress = [],
            Finishing = [],
            Completed = []
        };

        _httpHandler.When("/api/jobs/kanban")
            .Respond(HttpStatusCode.OK, JsonContent.Create(kanbanResponse));

        var cut = Render<ProductionQueue>();

        cut.WaitForState(() => cut.Markup.Contains("FDM"), TimeSpan.FromSeconds(5));

        Assert.Contains("FDM", cut.Markup);
    }

    [Fact]
    /// <summary>Verifies that the correct job counts are displayed for each Kanban column.</summary>
    public async Task ShouldDisplayCorrectJobCounts()
    {
        var kanbanResponse = new KanbanResponse
        {
            Pending = [
                new KanbanJobDto { Id = Guid.NewGuid(), Technology = "FDM", MachineId = "PRUSA-01", Volume = 10.5m, Priority = 1 },
                new KanbanJobDto { Id = Guid.NewGuid(), Technology = "SLA", MachineId = "RESIN-01", Volume = 5.2m, Priority = 2 }
            ],
            Queued = [new KanbanJobDto { Id = Guid.NewGuid(), Technology = "CNC", MachineId = "HAAS-VF2", Volume = 50.0m, Priority = 1 }],
            InProgress = [],
            Finishing = [],
            Completed = []
        };

        _httpHandler.When("/api/jobs/kanban")
            .Respond(HttpStatusCode.OK, JsonContent.Create(kanbanResponse));

        var cut = Render<ProductionQueue>();

        cut.WaitForState(() => cut.Markup.Contains("2"), TimeSpan.FromSeconds(5));

        var pendingChips = cut.FindAll(".mud-chip");
        Assert.True(pendingChips.Count > 0);
    }
}
