using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for customer-related operations, proxying to the Customer Service.
/// </summary>
/// <param name="client">The customer service client.</param>
/// <param name="registryClient">The registry service client.</param>
/// <param name="referenceDataService">Reference data service for countries, etc.</param>
/// <param name="hubContext">The SignalR hub context for broadcasting real-time updates.</param>
/// <param name="logger">The logger instance.</param>
[ApiController]
[Route("api/[controller]")]
public class CustomersController(
    CustomerServiceClient client,
    RegistryServiceClient registryClient,
    IReferenceDataService referenceDataService,
    IHubContext<NotificationHub> hubContext,
    ILogger<CustomersController> logger) : ControllerBase
{

    /// <summary>
    /// Retrieves a paged list of customers.
    /// </summary>
    /// <param name="query">Optional search query.</param>
    /// <param name="page">The page number.</param>
    /// <returns>A paged list of customers.</returns>
    [RequirePermission(MalievPermissions.Customer.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CustomerSummaryDto>>> Get([FromQuery] string? query, [FromQuery] int page = 1)
    {
        var result = await client.GetCustomersAsync(query, page);
        return result != null ? Ok(result) : Ok(new PagedResponse<CustomerSummaryDto>());
    }

    /// <summary>
    /// Retrieves detailed information for a single customer by ID.
    /// </summary>
    /// <param name="id">The customer ID.</param>
    /// <returns>The customer details.</returns>
    [RequirePermission(MalievPermissions.Customer.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDetailDto>> GetById(Guid id)
    {
        var result = await client.GetCustomerByIdAsync(id);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="request">The customer creation request.</param>
    /// <returns>The created customer.</returns>
    [RequirePermission(MalievPermissions.Customer.Profile.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create([FromBody] CreateCustomerRequest request)
    {
        var result = await client.CreateCustomerAsync(request);
        if (result != null)
        {
            // Notify all connected clients that customer data changed
            await hubContext.Clients.All.SendAsync("CustomerChanged");
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        return BadRequest();
    }

    /// <summary>
    /// Onboards a new customer with full details.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("onboard")]
    public async Task<IActionResult> Onboard([FromBody] CustomerOnboardingRequest request)
    {
        try
        {
            var result = await client.OnboardCustomerAsync(request);
            if (result != null)
            {
                // Notify all connected clients that customer data changed
                await hubContext.Clients.All.SendAsync("CustomerChanged");
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            return BadRequest(new ApiErrorResponse { Message = "Failed to onboard customer. Unknown error." });
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Downstream service error during onboarding");
            return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                new ApiErrorResponse { 
                    Message = ex.Message, 
                    Title = "Downstream Service Error",
                    Status = (int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError)
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during onboarding");
            return StatusCode(500, new ApiErrorResponse { Message = ex.Message, Title = "Internal Server Error", Status = 500 });
        }
    }

    /// <summary>
    /// Updates an existing customer with full details.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPut("{id:guid}/full")]
    public async Task<IActionResult> UpdateFull(Guid id, [FromBody] CustomerOnboardingRequest request)
    {
        try
        {
            var result = await client.UpdateCustomerFullAsync(id, request);
            if (result != null)
            {
                // Notify all connected clients that customer data changed
                await hubContext.Clients.All.SendAsync("CustomerChanged");
                return Ok(result);
            }
            return NotFound(new ApiErrorResponse { Message = "Customer not found or update failed." });
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Downstream service error during update");
            return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), 
                new ApiErrorResponse { 
                    Message = ex.Message, 
                    Title = "Downstream Service Error",
                    Status = (int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError)
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during update");
            return StatusCode(500, new ApiErrorResponse { Message = ex.Message, Title = "Internal Server Error", Status = 500 });
        }
    }

    /// <summary>
    /// Gets all countries. Requires authentication but uses service account for downstream calls.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("countries")]
    public async Task<ActionResult<List<CountryDto>>> GetCountries()
    {
        var countries = await referenceDataService.GetCountriesAsync();
        return Ok(countries);
    }

    /// <summary>
    /// Searches for companies.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("companies")]
    public async Task<ActionResult<List<CompanySummaryDto>>> SearchCompanies([FromQuery] string? query)
    {
        var result = await client.SearchCompaniesAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Proxies Thai location autocomplete to Registry Service.
    /// </summary>
    [RequirePermission(MalievPermissions.Registry.LocationsRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("locations/thai")]
    public async Task<ActionResult<List<RegistryThaiLocation>>> GetThaiLocations([FromQuery] string query, [FromQuery] int limit = 10)
    {
        try
        {
            var result = await registryClient.AutocompleteLocationsAsync(query, limit);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Registry Service returned error for query {Query}: {StatusCode}", query, ex.StatusCode);
            return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), ex.Message);
        }
    }

    /// <summary>
    /// Proxies Thai company search to Registry Service.
    /// </summary>
    [RequirePermission(MalievPermissions.Registry.CompaniesRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("locations/thai/companies")]
    public async Task<ActionResult<List<RegistryCompanyProfile>>> SearchThaiCompanies([FromQuery] string query, [FromQuery] int limit = 10)
    {
        var result = await registryClient.SearchCompaniesAsync(query, limit);
        return Ok(result);
    }

    /// <summary>
    /// Checks if a customer with the specified email already exists.
    /// </summary>
    /// <param name="email">Email address to check.</param>
    /// <returns>Email existence check result.</returns>
    [RequirePermission(MalievPermissions.Customer.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("check-email")]
    public async Task<ActionResult<bool>> CheckEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Ok(false);
        }

        var exists = await client.CheckEmailExistsAsync(email.Trim());
        return Ok(exists);
    }
}
