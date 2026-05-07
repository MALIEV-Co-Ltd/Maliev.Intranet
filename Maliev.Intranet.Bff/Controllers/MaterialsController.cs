using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for material-related operations, proxying to the Material Service.
/// </summary>
/// <param name="client">The material service client.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class MaterialsController(MaterialServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of materials.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paged list of materials.</returns>
    [RequirePermission(MalievPermissions.Material.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<MaterialSummaryDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await client.GetMaterialsAsync(page, pageSize);
        return result != null ? Ok(result) : Ok(new PagedResponse<MaterialSummaryDto>());
    }

    /// <summary>
    /// Retrieves detailed information for a single material.
    /// </summary>
    /// <param name="id">The material ID.</param>
    /// <returns>The material details.</returns>
    [RequirePermission(MalievPermissions.Material.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MaterialDetailDto>> GetById(Guid id)
    {
        var result = await client.GetMaterialByIdAsync(id);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Creates a new material.
    /// </summary>
    /// <param name="request">The creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created material.</returns>
    [RequirePermission(MalievPermissions.Material.Create, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<ActionResult<MaterialSummaryDto>> Create([FromBody] CreateMaterialRequest request, CancellationToken ct)
    {
        var result = await client.CreateMaterialAsync(request, ct);
        return result != null ? CreatedAtAction(nameof(GetById), new { id = result.Id }, result) : BadRequest();
    }

    /// <summary>
    /// Updates an existing material.
    /// </summary>
    /// <param name="id">The material ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated material.</returns>
    [RequirePermission(MalievPermissions.Material.Update, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MaterialDetailDto>> Update(Guid id, [FromBody] UpdateMaterialRequest request, CancellationToken ct)
    {
        var result = await client.UpdateMaterialAsync(id, request, ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Deletes a material.
    /// </summary>
    /// <param name="id">The material ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    [RequirePermission(MalievPermissions.Material.Delete, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var success = await client.DeleteMaterialAsync(id, ct);
        return success ? NoContent() : NotFound();
    }
}
