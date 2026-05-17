using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the AccountingService.
/// </summary>
public interface IAccountingServiceClient
{
    /// <summary>
    /// Retrieves the chart of accounts as a tree structure.
    /// </summary>
    Task<List<ChartOfAccountDto>?> GetAccountsTreeAsync(string? accountType = null, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a paged list of journal entries.
    /// </summary>
    Task<PagedResponse<JournalEntryDto>?> GetJournalEntriesAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? accountId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new journal entry.
    /// </summary>
    Task<JournalEntryDto?> CreateJournalEntryAsync(CreateJournalEntryRequest request, CancellationToken ct = default);

    /// <summary>
    /// Posts a draft journal entry to the ledger.
    /// </summary>
    Task<JournalEntryDto?> PostJournalEntryAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a financial report.
    /// </summary>
    Task<FinancialReportDto?> GetReportAsync(string type, DateTime start, DateTime end, CancellationToken ct = default);

    /// <summary>
    /// Retrieves accounting periods.
    /// </summary>
    Task<List<AccountingPeriodDto>?> GetPeriodsAsync(CancellationToken ct = default);

    /// <summary>
    /// Opens or creates the period containing the specified date.
    /// </summary>
    Task<bool> OpenPeriodAsync(DateTime date, CancellationToken ct = default);

    /// <summary>
    /// Closes the specified accounting period.
    /// </summary>
    Task<bool> ClosePeriodAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Reopens the specified accounting period.
    /// </summary>
    Task<bool> ReopenPeriodAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Runs reconciliation for a source system and period.
    /// </summary>
    Task<ReconciliationResultDto?> RunReconciliationAsync(string sourceSystem, Guid periodId, CancellationToken ct = default);
}

/// <summary>
/// Default implementation of the accounting service client.
/// </summary>
public class AccountingServiceClient(HttpClient httpClient) : IAccountingServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public async Task<List<ChartOfAccountDto>?> GetAccountsTreeAsync(string? accountType = null, CancellationToken ct = default)
    {
        var path = "/accounting/v1/chart-of-accounts/hierarchy";
        if (!string.IsNullOrWhiteSpace(accountType))
        {
            path += $"?accountType={Uri.EscapeDataString(accountType)}";
        }

        var response = await httpClient.GetFromJsonAsync<List<DownstreamChartOfAccount>>(path, JsonOptions, ct);
        return response?.Select(account => account.ToDto()).ToList();
    }

    /// <inheritdoc />
    public async Task<PagedResponse<JournalEntryDto>?> GetJournalEntriesAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? accountId = null,
        CancellationToken ct = default)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var query = new List<string>
        {
            $"pageNumber={normalizedPage.ToString(CultureInfo.InvariantCulture)}",
            $"pageSize={normalizedPageSize.ToString(CultureInfo.InvariantCulture)}"
        };

        AddIfPresent(query, "status", status);
        AddIfPresent(query, "startDate", startDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AddIfPresent(query, "endDate", endDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AddIfPresent(query, "accountId", accountId?.ToString());

        var path = "/accounting/v1/journal-entries?" + string.Join('&', query);
        var entries = await httpClient.GetFromJsonAsync<List<DownstreamJournalEntry>>(path, JsonOptions, ct);
        if (entries is null)
        {
            return null;
        }

        var items = entries.Select(entry => entry.ToDto()).ToList();
        var hasNext = items.Count == normalizedPageSize;
        return new PagedResponse<JournalEntryDto>
        {
            Data = items,
            Meta = new PaginationMeta
            {
                CurrentPage = normalizedPage,
                PageSize = normalizedPageSize,
                TotalCount = hasNext ? normalizedPage * normalizedPageSize + 1 : ((normalizedPage - 1) * normalizedPageSize) + items.Count,
                TotalItems = hasNext ? normalizedPage * normalizedPageSize + 1 : ((normalizedPage - 1) * normalizedPageSize) + items.Count,
                TotalPages = hasNext ? normalizedPage + 1 : normalizedPage
            }
        };
    }

    /// <inheritdoc />
    public async Task<JournalEntryDto?> CreateJournalEntryAsync(CreateJournalEntryRequest request, CancellationToken ct = default)
    {
        var payload = new
        {
            entryDate = request.Date,
            request.Description,
            request.Reference,
            request.CurrencyCode,
            request.ExchangeRateToBase,
            lines = request.Lines.Select(line => new
            {
                line.AccountId,
                debitAmount = line.Debit,
                creditAmount = line.Credit,
                transactionDebitAmount = ResolveTransactionAmount(line.TransactionDebit, line.Debit),
                transactionCreditAmount = ResolveTransactionAmount(line.TransactionCredit, line.Credit),
                line.Description,
                line.Reference
            }).ToList()
        };

        using var response = await httpClient.PostAsJsonAsync("/accounting/v1/journal-entries", payload, JsonOptions, ct);
        return await ReadJournalEntryAsync(response, ct);
    }

    /// <inheritdoc />
    public async Task<JournalEntryDto?> PostJournalEntryAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsync($"/accounting/v1/journal-entries/{id}/post", null, ct);
        return await ReadJournalEntryAsync(response, ct);
    }

    /// <inheritdoc />
    public async Task<FinancialReportDto?> GetReportAsync(string type, DateTime start, DateTime end, CancellationToken ct = default)
    {
        var path = NormalizeReportType(type) switch
        {
            "balance-sheet" => $"/accounting/v1/reports/balance-sheet?asOfDate={end:yyyy-MM-dd}",
            "trial-balance" => $"/accounting/v1/reports/trial-balance?startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}",
            _ => $"/accounting/v1/reports/income-statement?startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}"
        };

        using var response = await httpClient.GetAsync(path, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        return MapReport(NormalizeReportType(type), json);
    }

    /// <inheritdoc />
    public Task<List<AccountingPeriodDto>?> GetPeriodsAsync(CancellationToken ct = default) =>
        httpClient.GetFromJsonAsync<List<AccountingPeriodDto>>("/accounting/v1/periods", JsonOptions, ct);

    /// <inheritdoc />
    public async Task<bool> OpenPeriodAsync(DateTime date, CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsync($"/accounting/v1/periods/open?date={date:yyyy-MM-dd}", null, ct);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<bool> ClosePeriodAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsync($"/accounting/v1/periods/{id}/close", null, ct);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<bool> ReopenPeriodAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsync($"/accounting/v1/periods/{id}/reopen", null, ct);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<ReconciliationResultDto?> RunReconciliationAsync(string sourceSystem, Guid periodId, CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync(
            $"/accounting/v1/reconciliation/run?sourceSystem={Uri.EscapeDataString(sourceSystem)}&periodId={periodId}",
            ct);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        return new ReconciliationResultDto
        {
            SourceSystem = sourceSystem,
            PeriodId = periodId,
            Status = TryGetString(root, "status") ?? "Completed",
            DifferenceCount = TryGetInt(root, "differenceCount") ?? TryGetInt(root, "mismatchCount") ?? 0,
            DifferenceAmount = TryGetDecimal(root, "differenceAmount") ?? TryGetDecimal(root, "netDifference") ?? 0,
            DetailsJson = json
        };
    }

    private static async Task<JournalEntryDto?> ReadJournalEntryAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var downstream = await response.Content.ReadFromJsonAsync<DownstreamJournalEntry>(JsonOptions, ct);
        return downstream?.ToDto();
    }

    private static void AddIfPresent(List<string> query, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            query.Add($"{key}={Uri.EscapeDataString(value)}");
        }
    }

    private static string NormalizeReportType(string type) => type switch
    {
        "balance-sheet" => "balance-sheet",
        "trial-balance" => "trial-balance",
        _ => "income-statement"
    };

    private static FinancialReportDto MapReport(string type, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var generatedAt = TryGetDate(root, "generatedAt") ?? DateTime.UtcNow;

        return new FinancialReportDto
        {
            ReportName = type switch
            {
                "balance-sheet" => "Balance Sheet",
                "trial-balance" => "Trial Balance",
                _ => "Income Statement"
            },
            GeneratedAt = generatedAt,
            Sections = type switch
            {
                "balance-sheet" => MapBalanceSheet(root),
                "trial-balance" => MapTrialBalance(root),
                _ => MapIncomeStatement(root)
            }
        };
    }

    private static List<ReportSectionDto> MapTrialBalance(JsonElement root)
    {
        var rows = new List<ReportRowDto>();
        if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            rows.AddRange(items.EnumerateArray().Select(item => new ReportRowDto
            {
                Label = $"{TryGetString(item, "accountNumber")} {TryGetString(item, "accountName")}".Trim(),
                Amount = (TryGetDecimal(item, "debitBalance") ?? 0) - (TryGetDecimal(item, "creditBalance") ?? 0)
            }));
        }

        return [new ReportSectionDto { Title = "Trial Balance", Rows = rows, Total = rows.Sum(row => row.Amount) }];
    }

    private static List<ReportSectionDto> MapBalanceSheet(JsonElement root)
    {
        return
        [
            MapBalanceSheetGroup(root, "assets", "Assets"),
            MapBalanceSheetGroup(root, "liabilities", "Liabilities"),
            MapBalanceSheetGroup(root, "equity", "Equity")
        ];
    }

    private static ReportSectionDto MapBalanceSheetGroup(JsonElement root, string property, string title)
    {
        var rows = new List<ReportRowDto>();
        if (root.TryGetProperty(property, out var sections) && sections.ValueKind == JsonValueKind.Array)
        {
            foreach (var section in sections.EnumerateArray())
            {
                if (section.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    rows.AddRange(items.EnumerateArray().Select(item => new ReportRowDto
                    {
                        Label = $"{TryGetString(item, "accountNumber")} {TryGetString(item, "accountName")}".Trim(),
                        Amount = TryGetDecimal(item, "balance") ?? 0
                    }));
                }
            }
        }

        return new ReportSectionDto { Title = title, Rows = rows, Total = rows.Sum(row => row.Amount) };
    }

    private static List<ReportSectionDto> MapIncomeStatement(JsonElement root)
    {
        return
        [
            MapIncomeStatementGroup(root, "revenues", "Revenue"),
            MapIncomeStatementGroup(root, "expenses", "Expenses")
        ];
    }

    private static ReportSectionDto MapIncomeStatementGroup(JsonElement root, string property, string title)
    {
        var rows = new List<ReportRowDto>();
        if (root.TryGetProperty(property, out var sections) && sections.ValueKind == JsonValueKind.Array)
        {
            foreach (var section in sections.EnumerateArray())
            {
                if (section.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    rows.AddRange(items.EnumerateArray().Select(item => new ReportRowDto
                    {
                        Label = $"{TryGetString(item, "accountNumber")} {TryGetString(item, "accountName")}".Trim(),
                        Amount = TryGetDecimal(item, "amount") ?? 0
                    }));
                }
            }
        }

        return new ReportSectionDto { Title = title, Rows = rows, Total = rows.Sum(row => row.Amount) };
    }

    private static string? TryGetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.ToString()
            : null;

    private static int? TryGetInt(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.TryGetInt32(out var parsed) ? parsed : null;

    private static decimal? TryGetDecimal(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.TryGetDecimal(out var parsed) ? parsed : null;

    private static DateTime? TryGetDate(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.TryGetDateTime(out var parsed) ? parsed : null;

    private sealed class DownstreamChartOfAccount
    {
        public Guid Id { get; set; }

        public string AccountNumber { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string? Category { get; set; }

        public Guid? ParentAccountId { get; set; }

        public bool IsActive { get; set; }

        public List<DownstreamChartOfAccount>? Children { get; set; }

        public ChartOfAccountDto ToDto() => new()
        {
            Id = Id,
            Code = AccountNumber,
            AccountNumber = AccountNumber,
            Name = Name,
            Type = Type,
            Category = Category,
            ParentAccountId = ParentAccountId,
            IsActive = IsActive,
            Children = Children?.Select(child => child.ToDto()).ToList() ?? []
        };
    }

    private sealed class DownstreamJournalEntry
    {
        public Guid Id { get; set; }

        public string EntryNumber { get; set; } = string.Empty;

        public DateTime EntryDate { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public Guid? PeriodId { get; set; }

        public string? PeriodName { get; set; }

        public decimal TotalDebit { get; set; }

        public decimal TotalCredit { get; set; }

        public string CurrencyCode { get; set; } = "THB";

        public decimal ExchangeRateToBase { get; set; } = 1m;

        public decimal TransactionTotalDebit { get; set; }

        public decimal TransactionTotalCredit { get; set; }

        public string? Reference { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? PostedAt { get; set; }

        public List<DownstreamJournalEntryLine> Lines { get; set; } = [];

        public JournalEntryDto ToDto() => new()
        {
            Id = Id,
            EntryNumber = EntryNumber,
            Date = EntryDate,
            EntryDate = EntryDate,
            Description = Description,
            Status = Status,
            PeriodId = PeriodId,
            PeriodName = PeriodName,
            TotalDebit = TotalDebit,
            TotalCredit = TotalCredit,
            CurrencyCode = string.IsNullOrWhiteSpace(CurrencyCode) ? "THB" : CurrencyCode,
            ExchangeRateToBase = ExchangeRateToBase <= 0m ? 1m : ExchangeRateToBase,
            TransactionTotalDebit = ResolveTransactionAmount(TransactionTotalDebit, TotalDebit),
            TransactionTotalCredit = ResolveTransactionAmount(TransactionTotalCredit, TotalCredit),
            Reference = Reference,
            CreatedAt = CreatedAt,
            PostedAt = PostedAt,
            Lines = Lines.Select(line => line.ToDto()).ToList()
        };
    }

    private sealed class DownstreamJournalEntryLine
    {
        public Guid Id { get; set; }

        public int LineNumber { get; set; }

        public Guid AccountId { get; set; }

        public string AccountNumber { get; set; } = string.Empty;

        public string AccountName { get; set; } = string.Empty;

        public decimal DebitAmount { get; set; }

        public decimal CreditAmount { get; set; }

        public decimal TransactionDebitAmount { get; set; }

        public decimal TransactionCreditAmount { get; set; }

        public string? Description { get; set; }

        public string? Reference { get; set; }

        public JournalEntryLineDto ToDto() => new()
        {
            Id = Id,
            LineNumber = LineNumber,
            AccountId = AccountId,
            AccountNumber = AccountNumber,
            AccountName = AccountName,
            Debit = DebitAmount,
            Credit = CreditAmount,
            TransactionDebit = ResolveTransactionAmount(TransactionDebitAmount, DebitAmount),
            TransactionCredit = ResolveTransactionAmount(TransactionCreditAmount, CreditAmount),
            Description = Description ?? string.Empty,
            Reference = Reference
        };
    }

    private static decimal ResolveTransactionAmount(decimal transactionAmount, decimal baseAmount) =>
        transactionAmount != 0m || baseAmount == 0m ? transactionAmount : baseAmount;
}
