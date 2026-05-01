using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for accounting-related operations, proxying to the Accounting Service.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AccountingController(IAccountingServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves the chart of accounts tree.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("accounts-tree")]
    public async Task<ActionResult<List<ChartOfAccountDto>>> GetAccountsTree(CancellationToken ct)
    {
        var result = await client.GetAccountsTreeAsync(ct);
        return result != null ? Ok(result) : Ok(new List<ChartOfAccountDto>());
    }

    /// <summary>
    /// Retrieves a paged list of journal entries.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("journal-entries")]
    public async Task<ActionResult<PagedResponse<JournalEntryDto>>> GetJournalEntries([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await client.GetJournalEntriesAsync(page, pageSize, ct);
        return result != null ? Ok(result) : Ok(new PagedResponse<JournalEntryDto>());
    }

    /// <summary>
    /// Creates a new journal entry.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("journal-entries")]
    public async Task<ActionResult<JournalEntryDto>> CreateJournalEntry([FromBody] CreateJournalEntryRequest request, CancellationToken ct)
    {
        var result = await client.CreateJournalEntryAsync(request, ct);
        return result != null ? Ok(result) : BadRequest();
    }

    /// <summary>
    /// Retrieves a financial report.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("reports/{type}")]
    public async Task<ActionResult<FinancialReportDto>> GetReport(string type, [FromQuery] DateTime start, [FromQuery] DateTime end, CancellationToken ct)
    {
        var result = await client.GetReportAsync(type, start, end, ct);
        return result != null ? Ok(result) : NotFound();
    }
}
