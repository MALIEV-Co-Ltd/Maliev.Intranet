using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Services;
using Maliev.MessagingContracts.Contracts.Shared;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for customer management operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CustomersController(
    CustomerServiceClient client,
    RegistryServiceClient registryClient,
    IReferenceDataService referenceDataService,
    IAMServiceClient iamClient,
    IPublishEndpoint publishEndpoint,
    IHubContext<NotificationHub> hubContext,
    ILogger<CustomersController> logger) : ControllerBase
{
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;

    /// <summary>Gets all customers</summary>
    [RequirePermission(MalievPermissions.Customer.List)]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CustomerSummaryDto>>> Get(
        [FromQuery] string? query = null,
        [FromQuery] string? segment = null,
        [FromQuery] string? tier = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await client.GetCustomersAsync(query, segment, tier, includeDeleted, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Gets customer by ID</summary>
    [RequirePermission(MalievPermissions.Customer.Read)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await client.GetCustomerByIdAsync(id, ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>Gets payment terms available for customer profiles.</summary>
    [RequirePermission(MalievPermissions.Customer.Read)]
    [HttpGet("payment-terms")]
    public async Task<ActionResult<IReadOnlyList<PaymentTermDto>>> GetPaymentTerms(CancellationToken ct)
    {
        var result = await client.GetPaymentTermsAsync(ct);
        return Ok(result);
    }

    /// <summary>Sends an email notification to a customer through NotificationService.</summary>
    [RequirePermission(MalievPermissions.Customer.Read)]
    [HttpPost("{id:guid}/email")]
    public async Task<IActionResult> SendEmail(Guid id, [FromBody] CustomerEmailRequest request, CancellationToken ct)
    {
        var customer = await client.GetCustomerByIdAsync(id, ct);
        if (customer is null)
        {
            return NotFound();
        }

        if (customer.PrincipalId is null || customer.PrincipalId == Guid.Empty)
        {
            return BadRequest(new ApiErrorResponse { Message = "Customer does not have an IAM principal for notification delivery." });
        }

        if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body))
        {
            return BadRequest(new ApiErrorResponse { Message = "Subject and body are required." });
        }

        var messageId = Guid.NewGuid();
        var notification = new NotificationEvent(
            MessageId: messageId,
            MessageName: nameof(NotificationEvent),
            MessageType: Maliev.MessagingContracts.Contracts.Shared.MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "Maliev.IntranetBff",
            ConsumedBy: ["Maliev.NotificationService"],
            CorrelationId: messageId,
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new NotificationEventPayload(
                NotificationType: request.Subject.Trim(),
                Priority: "normal",
                TargetUsers: [new NotificationEventPayloadTargetUsersItem(customer.PrincipalId.Value.ToString(), "customer")],
                TemplateId: string.Empty,
                Parameters: new Dictionary<string, string>
                {
                    ["subject"] = request.Subject.Trim(),
                    ["message"] = request.Body.Trim()
                },
                Metadata: new NotificationEventPayloadMetadata(customer.PreferredLanguage, "intranet-customer-detail")));

        await publishEndpoint.Publish(notification, ct);
        return Accepted(new { messageId });
    }

    /// <summary>Gets customer history</summary>
    [RequirePermission(MalievPermissions.Customer.Read)]
    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<PagedResponse<CustomerActivityResponse>>> GetHistory(
        Guid id,
        [FromQuery] int? skip = null,
        [FromQuery] int? take = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await client.GetCustomerActivityAsync(id, skip, take, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Creates a customer with basic details (Company + Customer + Note)</summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write)]
    [HttpPost("create-basic")]
    public async Task<IActionResult> CreateBasic([FromBody] CustomerOnboardingRequest request, CancellationToken ct)
    {
        try
        {
            var result = await client.CreateCustomerBasicAsync(request, ct);
            if (result != null)
            {
                await _hubContext.Clients.All.SendAsync("CustomerChanged", cancellationToken: ct);
                return Ok(result);
            }
            return BadRequest(new ApiErrorResponse { Message = "Failed to create customer basic profile." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CreateBasic");
            return StatusCode(500, new ApiErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>Adds multiple addresses to a customer</summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write)]
    [HttpPost("{id:guid}/addresses")]
    public async Task<IActionResult> CreateAddresses(Guid id, [FromBody] List<CreateAddressRequest> addresses, CancellationToken ct)
    {
        var result = await client.CreateAddressesAsync(id, addresses, ct: ct);
        return Ok(result);
    }

    /// <summary>Adds an NDA to a customer</summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write)]
    [HttpPost("{id:guid}/nda")]
    public async Task<IActionResult> CreateNda(Guid id, [FromBody] CreateNdaStepRequest request, CancellationToken ct)
    {
        await client.CreateNdaWithDocumentsAsync(id, request.Nda, request.Documents, ct);
        return Ok();
    }

    /// <summary>Creates a new customer</summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var result = await client.CreateCustomerAsync(request, ct);
        if (result != null)
        {
            await _hubContext.Clients.All.SendAsync("CustomerChanged", cancellationToken: ct);
            return Ok(result);
        }
        return BadRequest();
    }

    /// <summary>Updates a customer with full details including addresses</summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write)]
    [HttpPut("{id:guid}/full")]
    public async Task<IActionResult> UpdateFull(Guid id, [FromBody] CustomerOnboardingRequest request, CancellationToken ct)
    {
        try
        {
            var result = await client.UpdateCustomerFullAsync(id, request, ct);
            if (result != null)
            {
                await _hubContext.Clients.All.SendAsync("CustomerChanged", cancellationToken: ct);
                return Ok(result);
            }
            return BadRequest(new ApiErrorResponse { Message = "Failed to update customer." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateFull for customer {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>Updates a single address</summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write)]
    [HttpPatch("addresses/{id:guid}")]
    public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdateAddressRequest request, CancellationToken ct)
    {
        var result = await client.UpdateAddressAsync(id, request, ct);
        if (result)
        {
            await _hubContext.Clients.All.SendAsync("CustomerChanged", cancellationToken: ct);
            return Ok();
        }
        return BadRequest();
    }

    /// <summary>Deletes a single address</summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write)]
    [HttpDelete("addresses/{id:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid id, [FromQuery] uint xmin, CancellationToken ct)
    {
        var result = await client.DeleteAddressAsync(id, xmin, ct);
        if (result)
        {
            await _hubContext.Clients.All.SendAsync("CustomerChanged", cancellationToken: ct);
            return NoContent();
        }

        return BadRequest();
    }

    /// <summary>Creates a standalone NDA record</summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write)]
    [HttpPost("ndas")]
    public async Task<IActionResult> CreateNda([FromBody] object request, CancellationToken ct)
    {
        var result = await client.CreateNdaAsync(request, ct);
        if (result)
        {
            await _hubContext.Clients.All.SendAsync("CustomerChanged", cancellationToken: ct);
            return Ok();
        }
        return BadRequest();
    }

    /// <summary>Updates a customer</summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write)]
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] object request, CancellationToken ct)
    {
        var result = await client.UpdateCustomerAsync(id, request, ct);
        if (result != null)
        {
            await _hubContext.Clients.All.SendAsync("CustomerChanged", cancellationToken: ct);
            return Ok(result);
        }
        return BadRequest();
    }

    /// <summary>Adds an internal note</summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write)]
    [HttpPost("{id:guid}/notes")]
    public async Task<IActionResult> AddInternalNote(Guid id, [FromBody] CreateInternalNoteRequest request, CancellationToken ct)
    {
        var result = await client.AddInternalNoteAsync(id, request, ct);
        if (result != null)
        {
            await _hubContext.Clients.All.SendAsync("CustomerChanged", cancellationToken: ct);
            return Ok(result);
        }
        return BadRequest();
    }

    /// <summary>Updates an internal note</summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write)]
    [HttpPatch("notes/{id:guid}")]
    public async Task<IActionResult> UpdateInternalNote(Guid id, [FromBody] UpdateInternalNoteRequest request, CancellationToken ct)
    {
        var result = await client.UpdateInternalNoteAsync(id, request, ct);
        if (result != null)
        {
            await _hubContext.Clients.All.SendAsync("CustomerChanged", cancellationToken: ct);
            return Ok(result);
        }
        return BadRequest();
    }

    /// <summary>Adds a comment to a note</summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write)]
    [HttpPost("notes/{id:guid}/comments")]
    public async Task<IActionResult> AddInternalNoteComment(Guid id, [FromBody] CreateInternalNoteCommentRequest request, CancellationToken ct)
    {
        var result = await client.AddInternalNoteCommentAsync(id, request, ct);
        if (result != null)
        {
            await _hubContext.Clients.All.SendAsync("CustomerChanged", cancellationToken: ct);
            return Ok(result);
        }
        return BadRequest();
    }

    /// <summary>Gets activity for a note</summary>
    [RequirePermission(MalievPermissions.Customer.Read)]
    [HttpGet("notes/{id:guid}/activity")]
    public async Task<IActionResult> GetInternalNoteActivity(Guid id, CancellationToken ct)
    {
        var activity = await client.GetInternalNoteActivityAsync(id, ct);
        return Ok(activity);
    }

    /// <summary>Deletes a document</summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write)]
    [HttpDelete("documents/{documentId:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid documentId, [FromQuery] string version, CancellationToken ct)
    {
        var rowVersion = Convert.FromBase64String(version);
        var success = await client.DeleteDocumentAsync(documentId, rowVersion, ct);
        if (success)
        {
            await _hubContext.Clients.All.SendAsync("CustomerChanged", cancellationToken: ct);
            return NoContent();
        }
        return BadRequest();
    }

    /// <summary>Deletes an NDA record</summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write)]
    [HttpDelete("ndas/{ndaId:guid}")]
    public async Task<IActionResult> DeleteNda(Guid ndaId, [FromQuery] string version, CancellationToken ct)
    {
        var rowVersion = Convert.FromBase64String(version);
        var success = await client.DeleteNdaAsync(ndaId, rowVersion, ct);
        if (success)
        {
            await _hubContext.Clients.All.SendAsync("CustomerChanged", cancellationToken: ct);
            return NoContent();
        }
        return BadRequest();
    }

    /// <summary>Updates NDA status</summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write)]
    [HttpPatch("ndas/{ndaId:guid}/status")]
    public async Task<IActionResult> UpdateNdaStatus(Guid ndaId, [FromBody] object request, CancellationToken ct)
    {
        var success = await client.UpdateNdaStatusAsync(ndaId, request, ct);
        if (success)
        {
            await _hubContext.Clients.All.SendAsync("CustomerChanged", cancellationToken: ct);
            return Ok();
        }
        return BadRequest();
    }

    /// <summary>Updates NDA details (expiration, etc.)</summary>
    [RequirePermission(MalievPermissions.Customer.Profile.Write)]
    [HttpPatch("ndas/{ndaId:guid}")]
    public async Task<IActionResult> UpdateNda(Guid ndaId, [FromBody] object request, CancellationToken ct)
    {
        var success = await client.UpdateNdaAsync(ndaId, request, ct);
        if (success)
        {
            await _hubContext.Clients.All.SendAsync("CustomerChanged", cancellationToken: ct);
            return Ok();
        }
        return BadRequest();
    }

    /// <summary>Gets countries</summary>
    [RequirePermission(MalievPermissions.Customer.Read)]
    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries(CancellationToken ct)
    {
        var result = await referenceDataService.GetCountriesAsync(ct);
        return Ok(result);
    }

    /// <summary>Gets Thai locations</summary>
    [RequirePermission(MalievPermissions.Registry.LocationsRead)]
    [HttpGet("locations/thai")]
    public async Task<ActionResult<List<RegistryThaiLocation>>> GetThaiLocations([FromQuery] string query, [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        var result = await registryClient.AutocompleteLocationsAsync(query, limit, ct);
        return Ok(result);
    }

    /// <summary>Searches for Thai companies by Tax ID or name</summary>
    [RequirePermission(MalievPermissions.Registry.CompaniesRead)]
    [HttpGet("companies/search")]
    public async Task<ActionResult<List<RegistryCompanyProfile>>> SearchCompanies([FromQuery] string query, [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        try
        {
            var result = await registryClient.SearchCompaniesAsync(query, limit, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching companies with query: {Query}", query);
            return StatusCode(500, new ApiErrorResponse { Message = "Failed to search companies" });
        }
    }

    /// <summary>Gets NDA audit history</summary>
    [RequirePermission(MalievPermissions.Customer.Read)]
    [HttpGet("ndas/{ndaId:guid}/history")]
    public async Task<ActionResult<List<NDAAuditLogResponse>>> GetNdaHistory(Guid ndaId, CancellationToken ct)
    {
        var result = await client.GetNdaHistoryAsync(ndaId, ct);

        // Resolve actor display names from IAM principals
        try
        {
            var principals = await iamClient.GetPrincipalsAsync(ct);
            var principalLookup = principals.ToDictionary(p => p.PrincipalId);

            foreach (var log in result)
            {
                if (Guid.TryParse(log.ActorId, out var principalId) &&
                    principalLookup.TryGetValue(principalId, out var principal))
                {
                    log.ActorName = principal.DisplayName;
                    log.ActorEmail = principal.Email;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to resolve actor names from IAM for NDA history {NdaId}", ndaId);
        }

        return Ok(result);
    }
}
