using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for quotation-related operations, proxying to the Quotation Service.
/// </summary>
/// <param name="client">The quotation service client.</param>
[ApiController]
[Route("api/[controller]")]
public class QuotationsController(QuotationServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of quotations.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paged list of quotations.</returns>
    [RequirePermission(MalievPermissions.Quotation.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<QuotationSummaryDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await client.GetQuotationsAsync(page, pageSize);
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
}
