using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Accounting microservice.
/// </summary>
public interface IAccountingServiceClient
{
    /// <summary>
    /// Retrieves the chart of accounts as a tree structure.
    /// </summary>
    Task<List<ChartOfAccountDto>?> GetAccountsTreeAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a paged list of journal entries.
    /// </summary>
    Task<PagedResponse<JournalEntryDto>?> GetJournalEntriesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);

    /// <summary>
    /// Creates a new journal entry.
    /// </summary>
    Task<JournalEntryDto?> CreateJournalEntryAsync(CreateJournalEntryRequest request, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a financial report.
    /// </summary>
    Task<FinancialReportDto?> GetReportAsync(string type, DateTime start, DateTime end, CancellationToken ct = default);
}

/// <summary>
/// Default implementation of the accounting service client.
/// </summary>
public class AccountingServiceClient(HttpClient httpClient) : IAccountingServiceClient
{
    /// <inheritdoc />
    public async Task<List<ChartOfAccountDto>?> GetAccountsTreeAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<ChartOfAccountDto>>("/accounting/v1/chart-of-accounts/hierarchy", ct);
    }

    /// <inheritdoc />
    public async Task<PagedResponse<JournalEntryDto>?> GetJournalEntriesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PagedResponse<JournalEntryDto>>($"/accounting/v1/journal-entries?page={page}&pageSize={pageSize}", ct);
    }

    /// <inheritdoc />
    public async Task<JournalEntryDto?> CreateJournalEntryAsync(CreateJournalEntryRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/accounting/v1/journal-entries", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<JournalEntryDto>(cancellationToken: ct);
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<FinancialReportDto?> GetReportAsync(string type, DateTime start, DateTime end, CancellationToken ct = default)
    {
        if (type == "ProfitAndLoss")
        {
            var result = await httpClient.GetFromJsonAsync<IncomeStatementResponse>($"/accounting/v1/reports/income-statement?startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}", ct);
            if (result == null) return null;

            var report = new FinancialReportDto
            {
                ReportName = "Income Statement (Profit & Loss)",
                GeneratedAt = result.GeneratedAt,
                Sections = []
            };

            var revenueSection = new ReportSectionDto { Title = "Revenues", Rows = [], Total = result.TotalRevenue };
            foreach (var s in result.Revenues)
            {
                foreach (var item in s.Items)
                {
                    revenueSection.Rows.Add(new ReportRowDto { Label = $"{item.AccountName} ({item.AccountNumber})", Amount = item.Amount });
                }
            }
            report.Sections.Add(revenueSection);

            var expenseSection = new ReportSectionDto { Title = "Expenses", Rows = [], Total = result.TotalExpense };
            foreach (var s in result.Expenses)
            {
                foreach (var item in s.Items)
                {
                    expenseSection.Rows.Add(new ReportRowDto { Label = $"{item.AccountName} ({item.AccountNumber})", Amount = item.Amount });
                }
            }
            report.Sections.Add(expenseSection);

            report.Sections.Add(new ReportSectionDto { Title = "Summary", Rows = [new ReportRowDto { Label = "Net Income", Amount = result.NetIncome }], Total = result.NetIncome });

            return report;
        }

        if (type == "BalanceSheet")
        {
            var result = await httpClient.GetFromJsonAsync<BalanceSheetResponse>($"/accounting/v1/reports/balance-sheet?asOfDate={end:yyyy-MM-dd}", ct);
            if (result == null) return null;

            var report = new FinancialReportDto
            {
                ReportName = "Balance Sheet",
                GeneratedAt = result.GeneratedAt,
                Sections = []
            };

            var assetSection = new ReportSectionDto { Title = "Assets", Rows = [], Total = result.TotalAssets };
            foreach (var s in result.Assets)
            {
                foreach (var item in s.Items)
                {
                    assetSection.Rows.Add(new ReportRowDto { Label = $"{item.AccountName} ({item.AccountNumber})", Amount = item.Balance });
                }
            }
            report.Sections.Add(assetSection);

            var liabilitySection = new ReportSectionDto { Title = "Liabilities", Rows = [], Total = result.TotalLiabilities };
            foreach (var s in result.Liabilities)
            {
                foreach (var item in s.Items)
                {
                    liabilitySection.Rows.Add(new ReportRowDto { Label = $"{item.AccountName} ({item.AccountNumber})", Amount = item.Balance });
                }
            }
            report.Sections.Add(liabilitySection);

            var equitySection = new ReportSectionDto { Title = "Equity", Rows = [], Total = result.TotalEquity };
            foreach (var s in result.Equity)
            {
                foreach (var item in s.Items)
                {
                    equitySection.Rows.Add(new ReportRowDto { Label = $"{item.AccountName} ({item.AccountNumber})", Amount = item.Balance });
                }
            }
            report.Sections.Add(equitySection);

            return report;
        }

        return null;
    }

    #region Downstream Models
    private class IncomeStatementResponse
    {
        public DateTime GeneratedAt { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetIncome => TotalRevenue - TotalExpense;
        public List<IncomeStatementSection> Revenues { get; set; } = [];
        public List<IncomeStatementSection> Expenses { get; set; } = [];
    }

    private class IncomeStatementSection
    {
        public string Category { get; set; } = string.Empty;
        public List<IncomeStatementItem> Items { get; set; } = [];
        public decimal Subtotal { get; set; }
    }

    private class IncomeStatementItem
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    private class BalanceSheetResponse
    {
        public DateTime GeneratedAt { get; set; }
        public decimal TotalAssets { get; set; }
        public decimal TotalLiabilities { get; set; }
        public decimal TotalEquity { get; set; }
        public List<BalanceSheetSection> Assets { get; set; } = [];
        public List<BalanceSheetSection> Liabilities { get; set; } = [];
        public List<BalanceSheetSection> Equity { get; set; } = [];
    }

    private class BalanceSheetSection
    {
        public string Category { get; set; } = string.Empty;
        public List<BalanceSheetItem> Items { get; set; } = [];
        public decimal Subtotal { get; set; }
    }

    private class BalanceSheetItem
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }
    #endregion
}
