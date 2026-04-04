using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// BFF controller that proxies all project lifecycle operations to the downstream ProjectService.
/// </summary>
/// <param name="client">The typed ProjectService HTTP client.</param>
/// <param name="jobClient">The typed JobService HTTP client (used for routing queue depth).</param>
/// <param name="facilityClient">The typed FacilityService HTTP client (used for routing machine lookup).</param>
/// <param name="logger">The logger instance.</param>
[RequirePermission(MalievPermissions.Project.Read, AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[Route("api/[controller]")]
public class ProjectsController(
    ProjectServiceClient client,
    JobServiceClient jobClient,
    FacilityServiceClient facilityClient,
    ILogger<ProjectsController> logger) : ControllerBase
{
    private readonly ILogger<ProjectsController> _logger = logger;
    // ── Query endpoints ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paged list of projects with optional status/search filtering.
    /// </summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="search">Optional full-text search term.</param>
    /// <param name="customerId">Optional customer ID filter.</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Items per page (default 20).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged project summaries.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProjectSummaryDto>>> Get(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await client.GetProjectsAsync(status, search, customerId, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns full detail for a single project.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Project detail or 404 if not found.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await client.GetProjectByIdAsync(id, ct);
        return result != null ? Ok(result) : NotFound();
    }

    // ── Create / Update / Delete ─────────────────────────────────────────────

    /// <summary>
    /// Creates a new project.
    /// </summary>
    /// <param name="request">Project creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created project detail.</returns>
    [HttpPost]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<ProjectDetailDto>> Create([FromBody] CreateProjectRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Creating project: CustomerId={CustomerId}, CustomerName={CustomerName}, Title={Title}",
            request.CustomerId, request.CustomerName, request.Title);

        var (result, errorContent, statusCode) = await client.CreateProjectAsync(request, ct);
        if (result != null)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        _logger.LogError(
            "ProjectService returned HTTP {StatusCode} for POST /project/v1/projects. ErrorContent={ErrorContent}. " +
            "Request: CustomerId={CustomerId}, CustomerName={CustomerName}, Title={Title}. " +
            "If StatusCode=404, check: (1) ProjectService is reachable, (2) route is /project/v1/projects, (3) auth token forwarded.",
            statusCode, errorContent, request.CustomerId, request.CustomerName, request.Title);

        var userMessage = statusCode switch
        {
            0 when !string.IsNullOrEmpty(errorContent) && errorContent.Contains("no such host") =>
                "ProjectService is unreachable. Check network connectivity and Aspire service discovery.",
            0 =>
                "ProjectService is unreachable or returned no response.",
            401 => "Not authorised — the BFF could not authenticate with ProjectService.",
            403 => "Permission denied — you may lack the 'project.projects.create' permission in IAM.",
            404 => $"ProjectService returned HTTP 404 for POST /project/v1/projects. The downstream service may not have this endpoint, or the route is incorrect. Error: {errorContent}",
            _ when !string.IsNullOrEmpty(errorContent) => errorContent,
            _ => $"ProjectService returned HTTP {statusCode}."
        };

        return StatusCode(502, userMessage);
    }

    /// <summary>
    /// Updates a project's metadata fields (title, description, notes, validity period).
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="request">Fields to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPut("{id:guid}")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> Update(Guid id, [FromBody] object request, CancellationToken ct)
    {
        var response = await client.UpdateProjectAsync(id, request, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Deletes a project. Only permitted when the project is in Draft status.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpDelete("{id:guid}")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var response = await client.DeleteProjectAsync(id, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    // ── Parts ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a new part (uploaded 3D file) to an existing project.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="request">Part creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created project part details.</returns>
    [HttpPost("{id:guid}/parts")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<ProjectPartDto>> AddPart(Guid id, [FromBody] AddProjectPartRequest request, CancellationToken ct)
    {
        var result = await client.AddPartAsync(id, request, ct);
        return result != null ? Ok(result) : StatusCode(502, "ProjectService returned an error.");
    }

    /// <summary>
    /// Updates an existing part's configuration (process, material, quantity, finish, etc.).
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="partId">The part GUID.</param>
    /// <param name="request">Configuration update payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPut("{id:guid}/parts/{partId:guid}")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> UpdatePart(Guid id, Guid partId, [FromBody] UpdateProjectPartRequest request, CancellationToken ct)
    {
        var response = await client.UpdatePartAsync(id, partId, request, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Removes a part from a project.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="partId">The part GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpDelete("{id:guid}/parts/{partId:guid}")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> DeletePart(Guid id, Guid partId, CancellationToken ct)
    {
        var response = await client.DeletePartAsync(id, partId, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    // ── Pricing ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Triggers AI price estimation for a specific part and returns the full price breakdown.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="partId">The part GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Price breakdown DTO.</returns>
    [HttpPost("{id:guid}/parts/{partId:guid}/price")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<ProjectPriceBreakdownDto>> GetPartPrice(Guid id, Guid partId, CancellationToken ct)
    {
        var result = await client.GetPartPriceAsync(id, partId, ct);
        return result != null ? Ok(result) : StatusCode(502);
    }

    /// <summary>
    /// Confirms or overrides the AI-estimated price for a specific part.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="partId">The part GUID.</param>
    /// <param name="request">Confirmed price and optional override reason.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPost("{id:guid}/parts/{partId:guid}/confirm-price")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> ConfirmPartPrice(Guid id, Guid partId, [FromBody] ConfirmPartPriceRequest request, CancellationToken ct)
    {
        var response = await client.ConfirmPartPriceAsync(id, partId, request, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    // ── Quotation lifecycle ──────────────────────────────────────────────────

    /// <summary>
    /// Generates the quotation PDF and marks the project as Quoted.
    /// Only allowed when all parts have confirmed prices.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPost("{id:guid}/generate-quotation")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> GenerateQuotation(Guid id, CancellationToken ct)
    {
        var response = await client.GenerateQuotationAsync(id, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Marks the project quotation as accepted by the customer, triggering order/job creation.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPost("{id:guid}/accept-quotation")]
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<IActionResult> AcceptQuotation(Guid id, CancellationToken ct)
    {
        var response = await client.AcceptQuotationAsync(id, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    // ── Routing ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the production routing information for a specific part.
    /// Computed in the BFF by aggregating JobService queue depth and FacilityService machine data.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="partId">The part GUID.</param>
    /// <param name="processType">The manufacturing process code (e.g. "FDM", "CNC_MILL").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Production routing DTO.</returns>
    [HttpGet("{id:guid}/parts/{partId:guid}/routing")]
    public async Task<ActionResult<ProductionRoutingDto>> GetPartRouting(
        Guid id, Guid partId,
        [FromQuery] string? processType,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(processType))
            return BadRequest("processType query parameter is required.");

        var queueDepths = await jobClient.GetQueueDepthAsync(processType, ct);
        var queueAhead = 0;
        queueDepths?.TryGetValue(processType, out queueAhead);

        var category = MapProcessToEquipmentCategory(processType);
        EquipmentSummaryDto? machine = null;
        if (category is not null)
        {
            var equipments = await facilityClient.GetEquipmentsAsync(
                category: category, status: "Active", page: 1, pageSize: 1, ct: ct);
            machine = equipments?.Items?.FirstOrDefault();
        }
        else
        {
            _logger.LogWarning("No equipment category mapping for process type '{ProcessType}'; machine lookup skipped.", processType);
        }

        // CNC requires fixture and toolpath verification; other processes need only 1 setup day.
        var setupDays = processType.StartsWith("CNC", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
        var estimatedStart = DateTimeOffset.UtcNow.AddDays(setupDays + queueAhead);

        return Ok(new ProductionRoutingDto(
            MachineId:          machine?.Id ?? Guid.Empty,
            MachineCode:        machine?.AssetCode ?? "TBD",
            MachineName:        machine?.Name ?? "Unassigned",
            QueueAhead:         queueAhead,
            EstimatedStartDate: estimatedStart,
            ScheduleItems:      []));
    }

    private static string? MapProcessToEquipmentCategory(string processType) => processType switch
    {
        "FDM"      => "FdmPrinter",
        "SLA_DLP"  => "SlaPrinter",
        "SLS"      => "SlsPrinter",
        "MJF"      => "MjfPrinter",
        "MJ"       => "MjPrinter",
        "BJ"       => "BjPrinter",
        "DMLS"     => "DmlsPrinter",
        "CNC_MILL" => "CncMachine",
        "CNC_TURN" => "CncMachine",
        _          => null,
    };
}
