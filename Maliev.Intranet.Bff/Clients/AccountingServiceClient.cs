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
        return await httpClient.GetFromJsonAsync<List<ChartOfAccountDto>>("/accounting/v1/accounts/tree", ct);
    }

    /// <inheritdoc />
    public async Task<PagedResponse<JournalEntryDto>?> GetJournalEntriesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PagedResponse<JournalEntryDto>>($"/accounting/v1/entries?page={page}&pageSize={pageSize}", ct);
    }

    /// <inheritdoc />
    public async Task<JournalEntryDto?> CreateJournalEntryAsync(CreateJournalEntryRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/accounting/v1/entries", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<JournalEntryDto>(cancellationToken: ct);
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<FinancialReportDto?> GetReportAsync(string type, DateTime start, DateTime end, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<FinancialReportDto>($"/accounting/v1/reports/{type}?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}", ct);
    }
}
