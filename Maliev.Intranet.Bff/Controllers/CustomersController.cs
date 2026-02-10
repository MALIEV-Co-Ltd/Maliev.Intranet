using System.Text.Json;
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
/// Controller for customer management operations.
/// </summary>
/// <param name="client">The customer service client.</param>
/// <param name="registryClient">The registry service client.</param>
/// <param name="referenceDataService">The reference data service.</param>
/// <param name="hubContext">The SignalR hub context for notifications.</param>
/// <param name="logger">The logger instance.</param>
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[Route("api/[controller]")]
public class CustomersController(
    CustomerServiceClient client, 
    RegistryServiceClient registryClient,
    IReferenceDataService referenceDataService,
    IHubContext<NotificationHub> hubContext, 
    ILogger<CustomersController> logger) : ControllerBase
{
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;

    /// <summary>
    /// Gets all customers.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CustomerSummaryDto>>> Get(string? query = null, int page = 1)
    {
        var result = await client.GetCustomersAsync(query, page);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single customer by ID.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDetailDto>> GetById(Guid id)
    {
        var result = await client.GetCustomerByIdAsync(id);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Gets activity history for a customer.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<List<CustomerActivityResponse>>> GetHistory(Guid id)
    {
        var result = await client.GetCustomerActivityAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create([FromBody] CreateCustomerRequest request)
    {
        var result = await client.CreateCustomerAsync(request);
        if (result != null)
        {
            await _hubContext.Clients.All.SendAsync("CustomerChanged");
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
                await _hubContext.Clients.All.SendAsync("CustomerChanged");
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
    /// Creates company (optional) + customer + internal note. Returns the created customer.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("create-basic")]
    public async Task<IActionResult> CreateBasic([FromBody] CustomerOnboardingRequest request)
    {
        try
        {
            var result = await client.CreateBasicAsync(request);
            if (result != null)
            {
                await _hubContext.Clients.All.SendAsync("CustomerChanged");
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            return BadRequest(new ApiErrorResponse { Message = "Failed to create customer." });
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Downstream service error during create-basic");
            return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError),
                new ApiErrorResponse
                {
                    Message = ex.Message,
                    Title = "Downstream Service Error",
                    Status = (int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError)
                });
        }
    }

    /// <summary>
    /// Creates addresses for a customer (batch).
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/addresses")]
    public async Task<IActionResult> CreateAddresses(Guid id, [FromBody] List<CreateAddressRequest> addresses)
    {
        try
        {
            var result = await client.CreateAddressesAsync(id, addresses);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Downstream service error during address creation for customer {CustomerId}", id);
            return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError),
                new ApiErrorResponse
                {
                    Message = ex.Message,
                    Title = "Address Creation Failed",
                    Status = (int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError)
                });
        }
    }

    /// <summary>
    /// Creates NDA for a customer, linking documents.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/nda")]
    public async Task<IActionResult> CreateNda(Guid id, [FromBody] CreateNdaStepRequest request)
    {
        try
        {
            await client.CreateNdaWithDocumentsAsync(id, request.Nda, request.Documents);
            return Ok();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Downstream service error during NDA creation for customer {CustomerId}", id);
            return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError),
                new ApiErrorResponse
                {
                    Message = ex.Message,
                    Title = "NDA Creation Failed",
                    Status = (int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError)
                });
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
                await _hubContext.Clients.All.SendAsync("CustomerChanged");
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
    /// Updates a customer profile.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> Update(Guid id, [FromBody] object request)
    {
        var result = await client.UpdateCustomerAsync(id, request);
        if (result != null)
        {
            await _hubContext.Clients.All.SendAsync("CustomerChanged");
            return Ok(result);
        }
        return BadRequest();
    }

    /// <summary>
    /// Gets a list of countries.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("countries")]
    public async Task<ActionResult<List<CountryDto>>> GetCountries()
    {
        var result = await referenceDataService.GetCountriesAsync();
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
    [RequirePermission(MalievPermissions.Customer.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("check-email")]
    public async Task<ActionResult<bool>> CheckEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return Ok(false);
        var exists = await client.CheckEmailExistsAsync(email.Trim());
        return Ok(exists);
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
    /// Creates an NDA record for a customer.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("ndas")]
    public async Task<IActionResult> CreateNda([FromBody] object request)
    {
        var success = await client.CreateNdaAsync(request);
        if (success)
        {
            await _hubContext.Clients.All.SendAsync("CustomerChanged");
            return Ok();
        }
        return BadRequest();
    }

    /// <summary>
    /// Updates NDA status for a customer.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("ndas/{ndaId:guid}/status")]
    public async Task<IActionResult> UpdateNdaStatus(Guid ndaId, [FromBody] JsonElement body)
    {
        if (body.TryGetProperty("status", out var statusProp))
        {
            var status = statusProp.GetString();
            if (!string.IsNullOrEmpty(status))
            {
                var success = await client.UpdateNdaStatusAsync(ndaId, status);
                if (success)
                {
                    await _hubContext.Clients.All.SendAsync("CustomerChanged");
                    return Ok();
                }
            }
        }
        return BadRequest();
    }

    /// <summary>
    /// Adds an internal note to a customer.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/notes")]
    public async Task<ActionResult<InternalNoteResponse>> AddNote(Guid id, [FromBody] string noteText)
    {
        if (string.IsNullOrWhiteSpace(noteText)) return BadRequest("Note text cannot be empty.");

        var request = new CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = id,
            NoteText = noteText
        };

        var result = await client.CreateInternalNoteAsync(request);
        if (result != null)
        {
            await _hubContext.Clients.All.SendAsync("CustomerChanged");
            return Ok(result);
        }
        return BadRequest("Failed to add note.");
    }

    /// <summary>
    /// Updates an internal note.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("notes/{noteId:guid}")]
    public async Task<ActionResult<InternalNoteResponse>> UpdateNote(Guid noteId, [FromBody] UpdateInternalNoteRequest request)
    {
        var result = await client.UpdateInternalNoteAsync(noteId, request);
        if (result != null)
        {
            await _hubContext.Clients.All.SendAsync("CustomerChanged");
            return Ok(result);
        }
        return BadRequest("Failed to update note.");
    }

    /// <summary>
    /// Deletes an internal note.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpDelete("notes/{noteId:guid}")]
    public async Task<IActionResult> DeleteNote(Guid noteId)
    {
        var success = await client.DeleteInternalNoteAsync(noteId);
        if (success)
        {
            await _hubContext.Clients.All.SendAsync("CustomerChanged");
            return NoContent();
        }
        return BadRequest("Failed to delete note.");
    }
}
