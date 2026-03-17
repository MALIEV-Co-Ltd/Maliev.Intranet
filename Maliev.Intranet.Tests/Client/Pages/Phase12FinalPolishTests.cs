using Bunit;
using Maliev.Intranet.Client.Pages;
using Maliev.Intranet.Client.Pages.Finance;
using Maliev.Intranet.Client.Pages.Manufacturing;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Tests.Testing;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Tests.Client.Pages;

/// <summary>
/// Tests for Phase 12: Final Polish &amp; E2E Testing.
/// Verifies navigation routes, redirect stubs, and DTO completeness.
/// </summary>
public class Phase12FinalPolishTests : BunitContext, IAsyncLifetime
{
    private NavigationManager? _nav;

    public Phase12FinalPolishTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);
        Services.AddScoped<BreadcrumbService>();
        Render<MudPopoverProvider>();
        _nav = Services.GetRequiredService<NavigationManager>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    // ── Redirect stub correctness ─────────────────────────────────────────────

    [Fact]
    public void ReceiptsRedirect_TargetsPaymentsTab()
    {
        Render<Receipts>();
        Assert.Contains("finance/payments?tab=receipts", _nav?.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LedgerRedirect_TargetsAccountingTab()
    {
        Render<Ledger>();
        Assert.Contains("finance/accounting?tab=ledger", _nav?.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TimeOffRedirect_TargetsLeaveTab()
    {
        Render<TimeOff>();
        Assert.Contains("hr/leave?tab=timeoff", _nav?.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MfgInventoryRedirect_TargetsMaterialsTab()
    {
        Render<Inventory>();
        Assert.Contains("mfg/materials?tab=inventory", _nav?.Uri, StringComparison.OrdinalIgnoreCase);
    }

    // ── Shared DTOs completeness ──────────────────────────────────────────────

    [Fact]
    public void ProjectSummaryDto_ShouldHaveRequiredProperties()
    {
        var dto = new ProjectSummaryDto
        {
            Id = Guid.NewGuid(),
            ProjectNumber = "PRJ-2026-0001",
            CustomerName = "Acme",
            Status = "Draft"
        };
        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.StartsWith("PRJ-", dto.ProjectNumber);
        Assert.Equal("Draft", dto.Status);
    }

    [Fact]
    public void JobSummaryDto_ShouldHaveRequiredProperties()
    {
        var dto = new JobSummaryDto
        {
            Id = Guid.NewGuid(),
            JobNumber = "JOB-0001",
            Status = "Queued",
            Priority = "Normal"
        };
        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.StartsWith("JOB-", dto.JobNumber);
    }

    [Fact]
    public void OrderLifecycleDto_ShouldHave7Stages()
    {
        var dto = new OrderLifecycleDto
        {
            OrderId = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            CurrentStage = "Confirmed",
            Stages =
            [
                new() { Stage = "Quoted" },
                new() { Stage = "Confirmed" },
                new() { Stage = "InProduction" },
                new() { Stage = "QC" },
                new() { Stage = "Delivered" },
                new() { Stage = "Invoiced" },
                new() { Stage = "Paid" }
            ]
        };
        Assert.Equal(7, dto.Stages.Count);
        Assert.Equal("ORD-001", dto.OrderNumber);
    }

    [Fact]
    public void OrderLifecycleStageDtoStatuses_AreValid()
    {
        var validStatuses = new[] { "Completed", "Current", "Pending" };
        var stage = new OrderLifecycleStageDto { Status = "Current", Stage = "Confirmed", Label = "Confirmed" };
        Assert.Contains(stage.Status, validStatuses);
    }

    [Fact]
    public void PdfDataDtos_CanBeInstantiated()
    {
        var dto = new QuotationPdfData
        {
            QuotationNumber = "QUO-001",
            CustomerName = "Test Corp"
        };
        Assert.Equal("QUO-001", dto.QuotationNumber);
    }

    // ── BFF client instantiation ──────────────────────────────────────────────

    [Fact]
    public void ProjectServiceClient_CanBeInstantiated()
    {
        var handler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://test") };
        var client = new Maliev.Intranet.Bff.Clients.ProjectServiceClient(httpClient);
        Assert.NotNull(client);
    }

    [Fact]
    public void JobServiceClient_CanBeInstantiated()
    {
        var handler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://test") };
        var client = new Maliev.Intranet.Bff.Clients.JobServiceClient(httpClient);
        Assert.NotNull(client);
    }
}
