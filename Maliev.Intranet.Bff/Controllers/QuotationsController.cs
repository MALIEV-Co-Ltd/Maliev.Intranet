using System.Security.Claims;
using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for quotation-related operations, proxying to the Quotation Service.
/// </summary>
/// <param name="client">The quotation service client.</param>
/// <param name="pdfClient">The PDF service client.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class QuotationsController(QuotationServiceClient client, PdfServiceClient pdfClient) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of quotations.
    /// </summary>
    /// <param name="customerId">Optional customer ID filter.</param>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paged list of quotations.</returns>
    [RequirePermission(MalievPermissions.Quotation.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<QuotationSummaryDto>>> Get([FromQuery] Guid? customerId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await client.GetQuotationsAsync(customerId, page, pageSize);
        return result != null ? Ok(result) : Ok(new PagedResponse<QuotationSummaryDto>());
    }

    /// <summary>
    /// Retrieves detailed information for a single quotation.
    /// </summary>
    /// <param name="id">The quotation ID.</param>
    /// <returns>The quotation details.</returns>
    [RequirePermission(MalievPermissions.Quotation.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuotationDetailDto>> GetById(Guid id)
    {
        var result = await client.GetQuotationByIdAsync(id);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Creates a new quotation.
    /// </summary>
    /// <param name="request">The creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created quotation.</returns>
    [RequirePermission(MalievPermissions.Quotation.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<ActionResult<QuotationSummaryDto>> Create([FromBody] CreateQuotationRequest request, CancellationToken ct)
    {
        var result = await client.CreateQuotationAsync(request, ct);
        return result != null ? CreatedAtAction(nameof(GetById), new { id = result.Id }, result) : BadRequest();
    }

    /// <summary>
    /// Updates an existing quotation.
    /// </summary>
    /// <param name="id">The quotation ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated quotation.</returns>
    [RequirePermission(MalievPermissions.Quotation.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<QuotationDetailDto>> Update(Guid id, [FromBody] UpdateQuotationRequest request, CancellationToken ct)
    {
        var result = await client.UpdateQuotationAsync(id, request, ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Deletes a quotation.
    /// </summary>
    /// <param name="id">The quotation ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [RequirePermission(MalievPermissions.Quotation.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var success = await client.DeleteQuotationAsync(id, ct);
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Updates the status of a quotation.
    /// </summary>
    /// <param name="id">The quotation ID.</param>
    /// <param name="status">The new status.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [RequirePermission(MalievPermissions.Quotation.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult> UpdateStatus(Guid id, [FromBody] string status, CancellationToken ct)
    {
        var success = await client.UpdateStatusAsync(id, status, ct);
        return success ? NoContent() : BadRequest();
    }

    /// <summary>
    /// Adds an internal note to a quotation.
    /// </summary>
    /// <param name="id">The quotation ID.</param>
    /// <param name="request">The note content.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created status if successful.</returns>
    [RequirePermission(MalievPermissions.Quotation.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/notes")]
    public async Task<IActionResult> AddNote(Guid id, [FromBody] AddQuotationNoteRequest request, CancellationToken ct)
    {
        var response = await client.AddNoteAsync(id, request, ct);
        return response.IsSuccessStatusCode ? StatusCode(201) : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Generates a PDF for a quotation.
    /// </summary>
    /// <param name="id">The quotation ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The PDF URL.</returns>
    [RequirePermission(MalievPermissions.Quotation.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/pdf")]
    public async Task<ActionResult<string>> GeneratePdf(Guid id, CancellationToken ct)
    {
        var quotation = await client.GetQuotationByIdAsync(id);
        if (quotation == null) return NotFound();

        var currentVersion = quotation.Versions?
            .OrderByDescending(version => version.VersionNumber == quotation.CurrentVersionNumber)
            .ThenByDescending(version => version.VersionNumber)
            .FirstOrDefault();
        var lineSubtotal = currentVersion?.LineItems?.Sum(item => item.Quantity * item.UnitPrice) ?? quotation.SubTotal;
        var versionDiscount = ResolveDiscountAmount(currentVersion?.DiscountStructure, lineSubtotal);
        var manualDiscount = Math.Max(0m, currentVersion?.ManualDiscountAmount ?? 0m);
        var totalDiscount = Math.Min(lineSubtotal, versionDiscount + manualDiscount);
        var shippingCost = Math.Max(0m, currentVersion?.ShippingCost ?? 0m);
        var taxableSubtotal = Math.Max(0m, lineSubtotal - totalDiscount + shippingCost);

        var pdfData = new QuotationPdfData
        {
            QuotationNumber = quotation.QuotationNumber,
            VersionNumber = currentVersion?.VersionNumber ?? quotation.CurrentVersionNumber,
            CustomerName = quotation.CustomerName,
            CustomerType = "Corporate",
            QuotationDate = quotation.CreatedAt,
            ValidityStart = quotation.ValidityPeriodStart,
            ValidityEnd = quotation.ValidityPeriodEnd,
            SubtotalBeforeDiscount = lineSubtotal,
            TotalDiscount = totalDiscount,
            ManualDiscountAmount = manualDiscount,
            ShippingCost = shippingCost,
            Discounts = BuildDiscounts(currentVersion?.DiscountStructure, versionDiscount),
            Subtotal = taxableSubtotal,
            TaxAmount = currentVersion?.TaxAmount ?? quotation.Tax,
            TotalAmount = quotation.Total,
            Currency = !string.IsNullOrEmpty(quotation.CurrencyCode) ? quotation.CurrencyCode : "THB",
            DeliveryExpectations = quotation.DeliveryExpectations,
            SpecialTerms = currentVersion?.SpecialTerms,
            ChangeSummary = currentVersion?.ChangeSummary,
            Items = currentVersion?.LineItems?.Select((item, index) => new QuotationPdfItem
            {
                Index = index + 1,
                MaterialName = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.Quantity * item.UnitPrice
            }).ToList() ?? []
        };

        ApplyQuotedByMetadata(pdfData);

        var pdfUrl = await pdfClient.GeneratePdfAsync(
            PdfDocumentType.Quotation,
            id.ToString(),
            pdfData,
            ct: ct);

        return pdfUrl != null ? Ok(pdfUrl) : BadRequest("Failed to generate PDF");
    }

    private static decimal ResolveDiscountAmount(SalesDiscountStructureDto? discount, decimal lineSubtotal)
    {
        if (discount == null || discount.DiscountValue <= 0m || lineSubtotal <= 0m)
            return 0m;

        var amount = discount.DiscountType switch
        {
            SalesDiscountType.FixedAmount => discount.DiscountValue,
            SalesDiscountType.Percentage or SalesDiscountType.VolumeBased => lineSubtotal * discount.DiscountValue / 100m,
            _ => 0m,
        };

        return Math.Min(lineSubtotal, Math.Max(0m, amount));
    }

    private static List<QuotationPdfDiscount> BuildDiscounts(SalesDiscountStructureDto? discount, decimal amount)
    {
        if (discount == null || amount <= 0m)
            return [];

        return
        [
            new()
            {
                DiscountType = discount.DiscountType.ToString(),
                DiscountValue = discount.DiscountValue,
                Conditions = discount.Conditions ?? discount.AuthorizationReason,
            }
        ];
    }

    /// <summary>
    /// Generates a draft quotation PDF from the current project state (no quotation ID required).
    /// </summary>
    /// <param name="pdfData">The quotation data to render.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The PDF storage URL.</returns>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("draft-pdf")]
    public async Task<ActionResult> GenerateDraftPdf([FromBody] QuotationPdfData pdfData, CancellationToken ct)
    {
        var referenceId = Guid.NewGuid().ToString();
        ApplyQuotedByMetadata(pdfData);
        var pdfUrl = await pdfClient.GeneratePdfAsync(
            PdfDocumentType.Quotation,
            referenceId,
            pdfData,
            ct: ct);
        return pdfUrl != null ? Ok(new { storageUrl = pdfUrl }) : BadRequest("Failed to generate PDF");
    }

    private void ApplyQuotedByMetadata(QuotationPdfData pdfData)
    {
        var user = HttpContext?.User;

        pdfData.QuotedByName = FirstNonEmpty(
            user?.Identity?.Name,
            user?.FindFirst("name")?.Value,
            user?.FindFirst("preferred_username")?.Value,
            user?.FindFirst("email")?.Value,
            user?.FindFirst(ClaimTypes.Email)?.Value,
            pdfData.QuotedByName);

        pdfData.QuotedByEmail = FirstNonEmpty(
            user?.FindFirst("email")?.Value,
            user?.FindFirst(ClaimTypes.Email)?.Value,
            pdfData.QuotedByEmail);

        pdfData.QuotedAt = DateTime.UtcNow;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
