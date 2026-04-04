using System.Net.Http.Json;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using Microsoft.Extensions.Logging;

namespace Maliev.Intranet.Client.Pages.Finance;

/// <summary>Finance page for general ledger, financial reports, and budget management.</summary>
/// <summary>Finance page for general ledger, financial reports, and budget management.</summary>
public partial class Accounting : ComponentBase
{
    /// <summary>HTTP client for API calls.</summary>
    [Inject] public HttpClient Http { get; set; } = null!;
    /// <summary>Navigation manager for routing.</summary>
    [Inject] public NavigationManager Navigation { get; set; } = null!;
    /// <summary>Snackbar service for transient notifications.</summary>
    [Inject] public ISnackbar Snackbar { get; set; } = null!;
    /// <summary>Dialog service for modal dialogs.</summary>
    [Inject] public IDialogService DialogService { get; set; } = null!;
    /// <summary>JavaScript runtime for browser interop.</summary>
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    
    
    private int _activeTab = 0;

    // ─── General Ledger ────────────────────────────────────────────────────────
    private bool _ledgerLoading = true;
    private string _ledgerPeriod = "January 2026";
    private List<ChartOfAccountDto> _accountsTree = new();
    private List<JournalEntryDto> _journalEntries = new();

    // ─── Financial Reports ────────────────────────────────────────────────────
    private bool _reportLoading = false;
    private string _selectedReport = "ProfitAndLoss";
    private DateRange _reportDateRange = new DateRange(DateTime.Today.AddMonths(-1), DateTime.Today);
    private FinancialReportDto? _financialReport;

    // ─── Budget ───────────────────────────────────────────────────────────────
    private string _selectedBudgetYear = "2026";
    private List<BudgetCategoryModel> _budgetCategories = new();

    // ─── Report Builder ───────────────────────────────────────────────────────
    private string _builderReportName = string.Empty;
    private string _builderReportType = "table";
    private string _builderGroupBy = "";
    private bool _builderHasRun = false;
    private DateRange _builderDateRange = new DateRange(DateTime.Today.AddMonths(-1), DateTime.Today);
    private HashSet<string> _selectedMetrics = new() { "Revenue", "Orders" };

    private readonly List<string> _availableMetrics = new()
    {
        "Revenue", "Orders", "Invoices", "Payments",
        "Customers", "New Customers", "Headcount", "Materials Used"
    };

    /// <summary>
    /// Initializes the accounting page, checking URL tab query parameter and loading ledger data.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        var uri = new Uri(Navigation.Uri);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        _activeTab = query["tab"] switch
        {
            "ledger" => 0,
            "reports" => 1,
            "budget" => 2,
            "builder" => 3,
            _ => 0
        };

        await LoadLedgerData();
    }

    private async Task LoadLedgerData()
    {
        try
        {
            _ledgerLoading = true;
            var accountsTask = Http.GetFromJsonAsync<List<ChartOfAccountDto>>("api/accounting/accounts-tree");
            var entriesTask = Http.GetFromJsonAsync<PagedResponse<JournalEntryDto>>("api/accounting/journal-entries");
            await Task.WhenAll(accountsTask, entriesTask);
            _accountsTree = await accountsTask ?? new();
            var response = await entriesTask;
            _journalEntries = response?.Data.ToList() ?? new();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading ledger data: {ex.Message}", Severity.Error);
        }
        finally
        {
            _ledgerLoading = false;
        }
    }

    private async Task OpenNewEntryDialog()
    {
        var parameters = new DialogParameters<JournalEntryDialog> { { x => x.Accounts, _accountsTree } };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Large, FullWidth = true };
        var dialog = await DialogService.ShowAsync<JournalEntryDialog>("New Journal Entry", parameters, options);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            await LoadLedgerData();
            Snackbar.Add("Journal entry created", Severity.Success);
        }
    }

    private async Task ViewJournalEntry(JournalEntryDto entry)
    {
        var parameters = new DialogParameters<JournalEntryDialog>
        {
            { x => x.ExistingEntry, entry },
            { x => x.Accounts, _accountsTree },
            { x => x.IsReadOnly, true }
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Large, FullWidth = true };
        await DialogService.ShowAsync<JournalEntryDialog>($"Journal Entry {entry.EntryNumber}", parameters, options);
    }

    private async Task EditJournalEntry(JournalEntryDto entry)
    {
        if (entry.Status != "Draft") { Snackbar.Add("Only Draft entries can be edited", Severity.Warning); return; }
        var parameters = new DialogParameters<JournalEntryDialog>
        {
            { x => x.ExistingEntry, entry },
            { x => x.Accounts, _accountsTree },
            { x => x.IsReadOnly, false }
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Large, FullWidth = true };
        var dialog = await DialogService.ShowAsync<JournalEntryDialog>($"Edit Entry {entry.EntryNumber}", parameters, options);
        var result = await dialog.Result;
        if (result is { Canceled: false }) { await LoadLedgerData(); Snackbar.Add("Journal entry updated", Severity.Success); }
    }

    private async Task LoadTrialBalance()
    {
        try
        {
            var now = DateTime.Today;
            var start = new DateTime(now.Year, now.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            var report = await Http.GetFromJsonAsync<FinancialReportDto>($"api/accounting/reports/TrialBalance?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}");
            if (report != null)
            {
                var parameters = new DialogParameters<TrialBalanceDialog> { { x => x.Report, report } };
                var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Large, FullWidth = true };
                await DialogService.ShowAsync<TrialBalanceDialog>("Trial Balance", parameters, options);
            }
        }
        catch (Exception ex) { Snackbar.Add($"Failed to load trial balance: {ex.Message}", Severity.Error); }
    }

    private async Task ExportLedger()
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"MALIEV General Ledger - {_ledgerPeriod}");
            sb.AppendLine($"Exported: {DateTime.Now:f}");
            sb.AppendLine();
            foreach (var entry in _journalEntries)
                sb.AppendLine($"{entry.EntryNumber,-12} {entry.Date:dd MMM yyyy,-15} {entry.Description,-30} {entry.TotalDebit,12:N2} {entry.TotalCredit,12:N2} {entry.Status,-10}");
            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var base64 = Convert.ToBase64String(bytes);
            await JSRuntime.InvokeVoidAsync("eval", $"const l=document.createElement('a');l.href='data:text/plain;charset=utf-8;base64,{base64}';l.download='ledger-{DateTime.Now:yyyyMMdd}.csv';l.click();");
            Snackbar.Add("Ledger exported successfully", Severity.Success);
        }
        catch (Exception ex) { Snackbar.Add($"Export failed: {ex.Message}", Severity.Error); }
    }

    private async Task LoadReport()
    {
        if (_reportDateRange.Start == null || _reportDateRange.End == null) { Snackbar.Add("Please select a date range", Severity.Warning); return; }
        try
        {
            _reportLoading = true;
            _financialReport = await Http.GetFromJsonAsync<FinancialReportDto>($"api/accounting/reports/{_selectedReport}?start={_reportDateRange.Start:yyyy-MM-dd}&end={_reportDateRange.End:yyyy-MM-dd}");
        }
        catch (Exception ex) { Snackbar.Add($"Error generating report: {ex.Message}", Severity.Error); }
        finally { _reportLoading = false; }
    }

    private async Task DownloadReport()
    {
        if (_financialReport == null) return;
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"MALIEV - {_financialReport.ReportName}");
            foreach (var reportSection in _financialReport.Sections)
            {
                sb.AppendLine(reportSection.Title.ToUpper());
                foreach (var row in reportSection.Rows) sb.AppendLine($"{row.Label,-40} {row.Amount,12:C2}");
                sb.AppendLine($"{"Total " + reportSection.Title,-40} {reportSection.Total,12:C2}");
            }
            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var base64 = Convert.ToBase64String(bytes);
            await JSRuntime.InvokeVoidAsync("eval", $"const l=document.createElement('a');l.href='data:text/plain;base64,{base64}';l.download='report-{_selectedReport}-{DateTime.Now:yyyyMMdd}.txt';l.click();");
            Snackbar.Add("Report downloaded", Severity.Success);
        }
        catch (Exception ex) { Snackbar.Add($"Download failed: {ex.Message}", Severity.Error); }
    }

    private void ToggleMetric(string metric, bool selected)
    {
        if (selected) _selectedMetrics.Add(metric);
        else _selectedMetrics.Remove(metric);
    }

    private void RunBuilderReport()
    {
        if (!_selectedMetrics.Any()) { Snackbar.Add("Please select at least one metric", Severity.Warning); return; }
        _builderHasRun = true;
    }

    private Color GetLedgerStatusColor(string status) => status switch
    {
        "Draft" => Color.Default,
        "Posted" => Color.Success,
        "Void" => Color.Error,
        _ => Color.Default
    };

    /// <summary>Budget category model for the budget planning tab.</summary>
    private class BudgetCategoryModel
    {
        /// <summary>Gets or sets the budget category name.</summary>
        public string Category { get; set; } = string.Empty;
        /// <summary>Gets or sets the department.</summary>
        public string Department { get; set; } = string.Empty;
        /// <summary>Gets or sets the budget amount in THB.</summary>
        public decimal BudgetAmount { get; set; }
        /// <summary>Gets or sets the actual amount spent in THB.</summary>
        public decimal ActualAmount { get; set; }
    }

}
