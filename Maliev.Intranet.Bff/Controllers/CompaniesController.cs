using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for company-related operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
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
    /// Searches companies by name or tax ID using internal CustomerService records and RegistryService-backed results.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("search")]
    public async Task<ActionResult<List<CompanySearchResultDto>>> Search([FromQuery] string query, [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        var result = await client.SearchCompanyResultsAsync(query, limit, ct);
        return Ok(result);
    }

    /// <summary>
    /// Creates a company.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<ActionResult<CompanyResponse>> Create([FromBody] CreateCompanyRequest request, CancellationToken ct = default)
    {
        var result = await client.CreateCompanyAsync(request, ct);
        return result != null ? Ok(result) : BadRequest();
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

    /// <summary>
    /// Promotes a customer to be the primary contact for their company.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{companyId:guid}/primary-contact/{customerId:guid}")]
    public async Task<IActionResult> PromotePrimaryContact(Guid companyId, Guid customerId, CancellationToken ct = default)
    {
        var success = await client.PromotePrimaryContactAsync(companyId, customerId, ct);
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Updates a company by ID.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<CompanyResponse>> Update(Guid id, [FromBody] UpdateCompanyRequest request, CancellationToken ct = default)
    {
        var result = await client.UpdateCompanyAsync(id, request, ct);
        return result != null ? Ok(result) : NotFound();
    }
}
