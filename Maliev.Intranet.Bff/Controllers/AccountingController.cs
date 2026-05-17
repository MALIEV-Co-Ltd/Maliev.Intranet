using System.Globalization;
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
public class AccountingController(
    IAccountingServiceClient client,
    UploadServiceClient? uploadClient = null,
    PdfServiceClient? pdfClient = null,
    IHttpClientFactory? httpClientFactory = null) : ControllerBase
{
    private const long MaxAccountingAttachmentBytes = 25 * 1024 * 1024;

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
    [RequirePermission(MalievPermissions.Accounting.Journal.Read, AuthenticationSchemes = "Bearer,Cookies")]
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
    [RequirePermission(MalievPermissions.Accounting.Journal.Create, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("journal-entries")]
    public async Task<ActionResult<JournalEntryDto>> CreateJournalEntry([FromBody] CreateJournalEntryRequest request, CancellationToken ct)
    {
        var result = await client.CreateJournalEntryAsync(request, ct);
        return result is not null ? Ok(result) : BadRequest();
    }

    /// <summary>
    /// Posts a draft journal entry.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.Journal.Post, AuthenticationSchemes = "Bearer,Cookies")]
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
    /// Generates and downloads a financial report as a PDF.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.Reports.Export, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("reports/{type}/pdf")]
    public async Task<IActionResult> DownloadReportPdf(string type, [FromQuery] DateTime start, [FromQuery] DateTime end, CancellationToken ct)
    {
        if (pdfClient is null || httpClientFactory is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "PDF export is not configured.");
        }

        var report = await client.GetReportAsync(type, start, end, ct);
        if (report is null)
        {
            return NotFound();
        }

        var reportType = NormalizeReportType(type);
        var pdfData = BuildFinancialReportPdfData(reportType, report, start, end);
        var pdfUrl = await pdfClient.GeneratePdfAsync(
            PdfDocumentType.Report,
            BuildReportReferenceId(reportType, start, end),
            pdfData,
            ct: ct);

        if (string.IsNullOrWhiteSpace(pdfUrl))
        {
            return StatusCode(StatusCodes.Status502BadGateway, "Report PDF could not be generated.");
        }

        using var response = await httpClientFactory.CreateClient().GetAsync(pdfUrl, ct);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "Generated report PDF could not be downloaded.");
        }

        var pdfBytes = await response.Content.ReadAsByteArrayAsync(ct);
        return File(pdfBytes, "application/pdf", BuildReportDownloadFileName(reportType, start, end));
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
    [RequirePermission(MalievPermissions.Accounting.PeriodsOpen, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("periods/open")]
    public async Task<IActionResult> OpenPeriod([FromQuery] DateTime date, CancellationToken ct)
    {
        return await client.OpenPeriodAsync(date, ct) ? NoContent() : BadRequest();
    }

    /// <summary>
    /// Closes an accounting period.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.PeriodsClose, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("periods/{id:guid}/close")]
    public async Task<IActionResult> ClosePeriod(Guid id, CancellationToken ct)
    {
        return await client.ClosePeriodAsync(id, ct) ? NoContent() : BadRequest();
    }

    /// <summary>
    /// Reopens an accounting period.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.PeriodsReopen, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("periods/{id:guid}/reopen")]
    public async Task<IActionResult> ReopenPeriod(Guid id, CancellationToken ct)
    {
        return await client.ReopenPeriodAsync(id, ct) ? NoContent() : BadRequest();
    }

    /// <summary>
    /// Runs accounting reconciliation for a source system and period.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.ReconciliationRun, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("reconciliation/run")]
    public async Task<ActionResult<ReconciliationResultDto>> RunReconciliation(
        [FromQuery] string sourceSystem,
        [FromQuery] Guid periodId,
        CancellationToken ct)
    {
        var result = await client.RunReconciliationAsync(sourceSystem, periodId, ct);
        return result is not null ? Ok(result) : BadRequest();
    }

    /// <summary>
    /// Uploads an accounting document, such as transfer slip or receipt evidence, to UploadService.
    /// </summary>
    [RequirePermission(MalievPermissions.Accounting.Journal.Create, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("documents")]
    public async Task<ActionResult<BffUploadResponse>> UploadDocument(
        IFormFile file,
        [FromQuery] string category = "JournalEvidence",
        CancellationToken ct = default)
    {
        if (uploadClient is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "UploadServiceClient is not configured.");
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        if (file.Length > MaxAccountingAttachmentBytes)
        {
            return BadRequest($"File exceeds the {MaxAccountingAttachmentBytes / 1024 / 1024} MB limit.");
        }

        var safeFileName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            return BadRequest("File name is required.");
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;
        var safeCategory = string.IsNullOrWhiteSpace(category) ? "JournalEvidence" : category.Trim();
        var uniquePrefix = Guid.NewGuid().ToString("N")[..8];
        var storagePath = $"accounting/{safeCategory}/{DateTime.UtcNow:yyyyMMdd}/{uniquePrefix}_{safeFileName}";

        using var stream = file.OpenReadStream();
        var upload = await uploadClient.UploadFileAsync(safeFileName, stream, contentType, storagePath, true, ct);

        return upload is not null
            ? Ok(upload)
            : StatusCode(StatusCodes.Status502BadGateway, "Upload failed.");
    }

    private static FinancialReportPdfData BuildFinancialReportPdfData(
        string reportType,
        FinancialReportDto report,
        DateTime start,
        DateTime end)
    {
        var summary = CalculateReportSummary(reportType, report);

        return new FinancialReportPdfData
        {
            ReportTitle = string.IsNullOrWhiteSpace(report.ReportName) ? FormatReportTitle(reportType) : report.ReportName,
            ReportNumber = BuildReportNumber(reportType, start, end),
            ReportDate = report.GeneratedAt == default ? DateTime.UtcNow : report.GeneratedAt,
            PeriodStart = start,
            PeriodEnd = end,
            CompanyName = "MALIEV Co., Ltd.",
            CompanyAddress = "36/1 Moo 3, Khlong Khoi, Pak Kret, Nonthaburi 11120",
            Sections = report.Sections.Select(section => new FinancialReportPdfSection
            {
                SectionTitle = section.Title,
                SectionTotal = ToDouble(section.Total),
                LineItems = section.Rows.Select(row => new FinancialReportPdfLineItem
                {
                    Description = row.Label,
                    Amount = ToDouble(row.Amount),
                    IsHighlight = row.Label.Contains("total", StringComparison.OrdinalIgnoreCase)
                }).ToList()
            }).ToList(),
            TotalRevenue = summary.PrimaryTotal,
            TotalExpenses = summary.SecondaryTotal,
            NetProfit = summary.NetTotal,
            Currency = "THB"
        };
    }

    private static ReportSummary CalculateReportSummary(string reportType, FinancialReportDto report)
    {
        if (reportType == "income-statement")
        {
            var revenue = FindSectionTotal(report, "revenue");
            var expenses = FindSectionTotal(report, "expense");
            return new ReportSummary(ToDouble(revenue), ToDouble(expenses), ToDouble(revenue - expenses));
        }

        if (reportType == "balance-sheet")
        {
            var primary = report.Sections.FirstOrDefault()?.Total ?? 0m;
            var secondary = report.Sections.Skip(1).Sum(section => section.Total);
            return new ReportSummary(ToDouble(primary), ToDouble(secondary), ToDouble(primary - secondary));
        }

        var positiveTotal = report.Sections.SelectMany(section => section.Rows).Where(row => row.Amount >= 0m).Sum(row => row.Amount);
        var negativeTotal = Math.Abs(report.Sections.SelectMany(section => section.Rows).Where(row => row.Amount < 0m).Sum(row => row.Amount));
        return new ReportSummary(ToDouble(positiveTotal), ToDouble(negativeTotal), ToDouble(positiveTotal - negativeTotal));
    }

    private static decimal FindSectionTotal(FinancialReportDto report, string titleFragment) =>
        report.Sections.FirstOrDefault(section => section.Title.Contains(titleFragment, StringComparison.OrdinalIgnoreCase))?.Total ?? 0m;

    private static string NormalizeReportType(string type) =>
        string.IsNullOrWhiteSpace(type) ? "income-statement" : type.Trim().ToLowerInvariant();

    private static string FormatReportTitle(string reportType) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(reportType.Replace('-', ' '));

    private static string BuildReportReferenceId(string reportType, DateTime start, DateTime end) =>
        $"{reportType}-{start:yyyyMMdd}-{end:yyyyMMdd}";

    private static string BuildReportNumber(string reportType, DateTime start, DateTime end) =>
        $"RPT-{reportType.ToUpperInvariant()}-{start:yyyyMMdd}-{end:yyyyMMdd}";

    private static string BuildReportDownloadFileName(string reportType, DateTime start, DateTime end) =>
        $"{reportType}_{start:yyyy-MM-dd}_{end:yyyy-MM-dd}.pdf";

    private static double ToDouble(decimal value) => decimal.ToDouble(value);

    private readonly record struct ReportSummary(double PrimaryTotal, double SecondaryTotal, double NetTotal);
}
