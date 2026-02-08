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
[Route("api/[controller]")]
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
}
