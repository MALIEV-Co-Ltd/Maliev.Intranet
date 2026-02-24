using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for company-related operations.
/// </summary>
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[Route("api/[controller]")]
public class CompaniesController(CustomerServiceClient client, IAMServiceClient iamClient, ILogger<CompaniesController> logger) : ControllerBase
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

    /// <summary>
    /// Updates editable fields on a company.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateCompanyRequest request, CancellationToken ct)
    {
        try
        {
            await client.UpdateCompanyAsync(id, request, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update company {CompanyId}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Failed to update company." });
        }
    }

    /// <summary>
    /// Gets company history with resolved actor names from IAM.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<PagedResponse<CustomerActivityResponse>>> GetHistory(
        Guid id,
        [FromQuery] int? skip = null,
        [FromQuery] int? take = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await client.GetCompanyActivityAsync(id, skip, take, page, pageSize, ct);

        // Resolve actor display names from IAM principals (same pattern as CustomersController.GetNdaHistory)
        try
        {
            var principals = await iamClient.GetPrincipalsAsync(ct);
            var principalLookup = principals.ToDictionary(p => p.PrincipalId);

            result.Data = result.Data.Select(activity =>
            {
                if (Guid.TryParse(activity.ActorId, out var principalId) &&
                    principalLookup.TryGetValue(principalId, out var principal))
                {
                    return activity with { ActorName = principal.DisplayName, ActorEmail = principal.Email };
                }
                return activity;
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to resolve actor names from IAM for company history {CompanyId}", id);
        }

        return Ok(result);
    }
}
