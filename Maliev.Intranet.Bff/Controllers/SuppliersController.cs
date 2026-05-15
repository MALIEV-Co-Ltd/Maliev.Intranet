using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for supplier-related operations, proxying to the Supplier Service.
/// </summary>
/// <param name="client">The supplier service client.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class SuppliersController(SupplierServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of suppliers.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paged list of suppliers.</returns>
    [RequirePermission(MalievPermissions.Supplier.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<SupplierSummaryDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await client.GetSuppliersAsync(page, pageSize);
        return result != null ? Ok(result) : Ok(new PagedResponse<SupplierSummaryDto>());
    }

    /// <summary>
    /// Retrieves detailed information for a single supplier.
    /// </summary>
    /// <param name="id">The supplier ID.</param>
    /// <returns>The supplier details.</returns>
    [RequirePermission(MalievPermissions.Supplier.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierDetailDto>> GetById(Guid id)
    {
        var result = await client.GetSupplierByIdAsync(id);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Creates a new supplier.
    /// </summary>
    [RequirePermission(MalievPermissions.Supplier.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request, CancellationToken ct)
    {
        using var response = await client.CreateSupplierAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            return string.IsNullOrWhiteSpace(body)
                ? StatusCode((int)response.StatusCode)
                : StatusCode((int)response.StatusCode, body);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return StatusCode((int)response.StatusCode);
        }

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = body,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
        };
    }

    /// <summary>
    /// Updates an existing supplier.
    /// </summary>
    [RequirePermission(MalievPermissions.Supplier.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupplierRequest request, CancellationToken ct)
    {
        var response = await client.UpdateSupplierAsync(id, request, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Deactivates a supplier.
    /// </summary>
    [RequirePermission(MalievPermissions.Supplier.Delete, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var response = await client.DeactivateSupplierAsync(id, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }
}
