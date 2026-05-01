using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for billing notes.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/billing-notes")]
public class BillingNotesController(InvoiceServiceClient invoiceClient) : ControllerBase
{
    /// <summary>
    /// Gets a paged list of billing notes, optionally filtered by invoice ID.
    /// </summary>
    [RequirePermission(MalievPermissions.BillingNote.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<BillingNoteDto>>> GetBillingNotes([FromQuery] Guid? invoiceId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await invoiceClient.GetBillingNotesAsync(invoiceId, page, pageSize, ct);
        return Ok(result ?? new PagedResponse<BillingNoteDto>());
    }

    /// <summary>
    /// Creates a billing note.
    /// </summary>
    [RequirePermission(MalievPermissions.BillingNote.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<ActionResult<BillingNoteDto>> CreateBillingNote(CreateBillingNoteRequest request, CancellationToken ct)
    {
        var result = await invoiceClient.CreateBillingNoteAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets a billing note by ID.
    /// </summary>
    [RequirePermission(MalievPermissions.BillingNote.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BillingNoteDto>> GetBillingNote(Guid id, CancellationToken ct)
    {
        var result = await invoiceClient.GetBillingNoteByIdAsync(id, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }
}
