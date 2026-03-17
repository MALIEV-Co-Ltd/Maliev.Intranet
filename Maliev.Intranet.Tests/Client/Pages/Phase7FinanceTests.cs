using Bunit;
using Maliev.Intranet.Client.Pages.Finance;
using Maliev.Intranet.Client.Pages;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Tests.Testing;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;

namespace Maliev.Intranet.Tests.Client.Pages;

/// <summary>
/// Tests for Phase 7: Finance Page Consolidation.
/// Verifies Accounting (Ledger+Reports+Budget), PaymentsAndReceipts, and legacy redirect stubs.
/// </summary>
public class Phase7FinanceConsolidationTests : BunitContext, IAsyncLifetime
{
    private NavigationManager? _nav;

    public Phase7FinanceConsolidationTests()
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

    // ── Payments & Receipts page ──────────────────────────────────────────────

    [Fact]
    public void PaymentsAndReceipts_ShouldRender_WithoutException()
    {
        var cut = Render<PaymentsAndReceipts>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void PaymentsAndReceipts_ShouldContain_PaymentsTab()
    {
        var cut = Render<PaymentsAndReceipts>();
        Assert.Contains("Payments", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PaymentsAndReceipts_ShouldContain_ReceiptsTab()
    {
        var cut = Render<PaymentsAndReceipts>();
        Assert.Contains("Receipts", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // ── Accounting page ───────────────────────────────────────────────────────

    [Fact]
    public void Accounting_ShouldRender_WithoutException()
    {
        var cut = Render<Accounting>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void Accounting_ShouldContain_LedgerTab()
    {
        var cut = Render<Accounting>();
        Assert.Contains("Ledger", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Accounting_ShouldContain_ReportsTab()
    {
        var cut = Render<Accounting>();
        Assert.Contains("Reports", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Accounting_ShouldContain_BudgetTab()
    {
        var cut = Render<Accounting>();
        Assert.Contains("Budget", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // ── Legacy redirect stubs ─────────────────────────────────────────────────

    [Fact]
    public void ReceiptsStub_ShouldRedirectTo_PaymentsReceiptsTab()
    {
        Render<Receipts>();
        Assert.Contains("/finance/payments?tab=receipts", _nav?.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LedgerStub_ShouldRedirectTo_AccountingLedgerTab()
    {
        Render<Ledger>();
        Assert.Contains("/finance/accounting?tab=ledger", _nav?.Uri, StringComparison.OrdinalIgnoreCase);
    }
}
