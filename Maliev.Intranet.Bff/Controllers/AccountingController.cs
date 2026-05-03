using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for accounting-related operations, proxying to AccountingService.
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
    public async Task<ActionResult<List<ChartOfAccountDto>>> GetAccountsTree(
        [FromQuery] string? accountType = null,
        CancellationToken ct = default)
    {
        var result = await client.GetAccountsTreeAsync(accountType, ct);
        return result is not null ? Ok(result) : StatusCode(StatusCodes.Status502BadGateway, "Chart of accounts could not be loaded.");
    }

    /// <summary>
    /// Retrieves a paged list of journal entries.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("journal-entries")]
    public async Task<ActionResult<PagedResponse<JournalEntryDto>>> GetJournalEntries(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? accountId = null,
        CancellationToken ct = default)
    {
        var result = await client.GetJournalEntriesAsync(page, pageSize, status, startDate, endDate, accountId, ct);
        return result is not null ? Ok(result) : StatusCode(StatusCodes.Status502BadGateway, "Journal entries could not be loaded.");
    }

    /// <summary>
    /// Creates a new journal entry.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("journal-entries")]
    public async Task<ActionResult<JournalEntryDto>> CreateJournalEntry([FromBody] CreateJournalEntryRequest request, CancellationToken ct)
    {
        var result = await client.CreateJournalEntryAsync(request, ct);
        return result is not null ? Ok(result) : BadRequest();
    }

    /// <summary>
    /// Posts a draft journal entry.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("journal-entries/{id:guid}/post")]
    public async Task<ActionResult<JournalEntryDto>> PostJournalEntry(Guid id, CancellationToken ct)
    {
        var result = await client.PostJournalEntryAsync(id, ct);
        return result is not null ? Ok(result) : BadRequest();
    }

    /// <summary>
    /// Retrieves a financial report.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("reports/{type}")]
    public async Task<ActionResult<FinancialReportDto>> GetReport(string type, [FromQuery] DateTime start, [FromQuery] DateTime end, CancellationToken ct)
    {
        var result = await client.GetReportAsync(type, start, end, ct);
        return result is not null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Retrieves accounting periods.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("periods")]
    public async Task<ActionResult<List<AccountingPeriodDto>>> GetPeriods(CancellationToken ct)
    {
        var result = await client.GetPeriodsAsync(ct);
        return result is not null ? Ok(result) : StatusCode(StatusCodes.Status502BadGateway, "Accounting periods could not be loaded.");
    }

    /// <summary>
    /// Opens or creates the accounting period containing the specified date.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("periods/open")]
    public async Task<IActionResult> OpenPeriod([FromQuery] DateTime date, CancellationToken ct)
    {
        return await client.OpenPeriodAsync(date, ct) ? NoContent() : BadRequest();
    }

    /// <summary>
    /// Closes an accounting period.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("periods/{id:guid}/close")]
    public async Task<IActionResult> ClosePeriod(Guid id, CancellationToken ct)
    {
        return await client.ClosePeriodAsync(id, ct) ? NoContent() : BadRequest();
    }

    /// <summary>
    /// Reopens an accounting period.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("periods/{id:guid}/reopen")]
    public async Task<IActionResult> ReopenPeriod(Guid id, CancellationToken ct)
    {
        return await client.ReopenPeriodAsync(id, ct) ? NoContent() : BadRequest();
    }

    /// <summary>
    /// Runs accounting reconciliation for a source system and period.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("reconciliation/run")]
    public async Task<ActionResult<ReconciliationResultDto>> RunReconciliation(
        [FromQuery] string sourceSystem,
        [FromQuery] Guid periodId,
        CancellationToken ct)
    {
        var result = await client.RunReconciliationAsync(sourceSystem, periodId, ct);
        return result is not null ? Ok(result) : BadRequest();
    }
}
