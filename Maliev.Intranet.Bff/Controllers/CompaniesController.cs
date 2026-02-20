using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for company-related operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CompaniesController(CustomerServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of companies.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CompanySummaryDto>>> Get([FromQuery] string? query = null, [FromQuery] int page = 1, CancellationToken ct = default)
    {
        var result = await client.GetCompaniesAsync(query, page, ct);
        return result != null ? Ok(result) : Ok(new PagedResponse<CompanySummaryDto>());
    }

    /// <summary>
    /// Retrieves a single company by ID.
    /// </summary>
    /// <param name="id">The company ID.</param>
    /// <returns>The company details.</returns>
    [RequirePermission(MalievPermissions.Customer.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyResponse>> GetById(Guid id)
    {
        var result = await client.GetCompanyByIdAsync(id);
        return result != null ? Ok(result) : NotFound();
    }
}
