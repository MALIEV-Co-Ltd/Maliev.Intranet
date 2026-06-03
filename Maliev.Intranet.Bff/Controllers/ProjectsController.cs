using System.Globalization;
using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Services;
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
/// <param name="uploadClient">The typed UploadService HTTP client used when duplicating project file artifacts.</param>
/// <param name="analysisStatusService">The file analysis status cache used when restoring duplicated part previews.</param>
/// <param name="customerClient">The typed CustomerService HTTP client used to hydrate quote customer details.</param>
/// <param name="quotationClient">The typed QuotationService HTTP client used to hydrate generated quotation details.</param>
/// <param name="pdfClient">The typed PdfService HTTP client used to generate quotation PDFs.</param>
[RequirePermission(MalievPermissions.Project.Read, AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProjectsController(
    ProjectServiceClient client,
    JobServiceClient jobClient,
    IFacilityServiceClient facilityClient,
    ILogger<ProjectsController> logger,
    UploadServiceClient? uploadClient = null,
    IFileAnalysisStatusService? analysisStatusService = null,
    CustomerServiceClient? customerClient = null,
    QuotationServiceClient? quotationClient = null,
    PdfServiceClient? pdfClient = null) : ControllerBase
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
        if (uploadClient is not null)
            await EnrichProjectSummaryPreviewsAsync(result.Data, uploadClient, analysisStatusService, ct);

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
        if (result is not null && uploadClient is not null)
            await EnrichProjectDetailArtifactsAsync(result, uploadClient, analysisStatusService, ct);

        if (result is not null && customerClient is not null)
            await EnrichProjectCustomerAsync(result, customerClient, ct);

        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Returns a signed large thumbnail URL for a part that belongs to the project.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="partId">The project part GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A signed large thumbnail URL, or 404 when the part or artifact is not available.</returns>
    [HttpGet("{id:guid}/parts/{partId:guid}/thumbnail-large-url")]
    public async Task<ActionResult> GetPartLargeThumbnailUrl(Guid id, Guid partId, CancellationToken ct)
    {
        if (uploadClient is null)
            return StatusCode(StatusCodes.Status500InternalServerError, "UploadServiceClient is not configured.");

        var project = await client.GetProjectByIdAsync(id, ct);
        var part = project?.Parts.FirstOrDefault(candidate => candidate.Id == partId);
        if (project is null || part is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(part.ThumbnailLargeGcsPath))
            return NotFound("Large thumbnail is not available.");

        var signedUrl = await uploadClient.GetDownloadUrlByPathAsync(part.ThumbnailLargeGcsPath, ct);
        if (string.IsNullOrEmpty(signedUrl))
            return NotFound("Download URL not available");

        return Ok(new { Url = signedUrl });
    }

    // ── Create / Update / Delete ─────────────────────────────────────────────

    /// <summary>
    /// Creates a new project.
    /// </summary>
    /// <param name="request">Project creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created project detail.</returns>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost]
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
    /// Adds an internal note to a project.
    /// </summary>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/notes")]
    public async Task<ActionResult<ProjectNoteDto>> AddNote(
        Guid id,
        [FromBody] AddProjectNoteRequest request,
        CancellationToken ct)
    {
        var (result, errorContent, statusCode) = await client.AddNoteAsync(id, request, ct);
        if (result is not null)
            return Ok(result);

        _logger.LogWarning(
            "ProjectService returned HTTP {StatusCode} for POST /project/v1/projects/{ProjectId}/notes. ErrorContent={ErrorContent}",
            statusCode,
            id,
            errorContent);

        return StatusCode(502, string.IsNullOrWhiteSpace(errorContent) ? "Project note could not be saved." : errorContent);
    }

    /// <summary>
    /// Duplicates a project into a new independent reorder draft.
    /// </summary>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/duplicate")]
    public async Task<ActionResult<ProjectDetailDto>> Duplicate(
        Guid id,
        [FromBody] DuplicateProjectRequest? request,
        CancellationToken ct)
    {
        if (uploadClient is null)
            return StatusCode(StatusCodes.Status500InternalServerError, "UploadServiceClient is not configured.");

        var source = await client.GetProjectByIdAsync(id, ct);
        if (source is null)
            return NotFound();

        var createRequest = new CreateProjectRequest
        {
            CustomerId = source.CustomerId,
            CustomerName = source.CustomerName,
            Title = string.IsNullOrWhiteSpace(request?.Title) ? BuildCopyTitle(source.Title) : request!.Title!,
            Description = source.Description,
            Currency = string.IsNullOrWhiteSpace(source.Currency) ? "THB" : source.Currency,
            SourceProjectId = source.Id,
            SourceProjectNumber = source.ProjectNumber
        };

        var (created, errorContent, statusCode) = await client.CreateProjectAsync(createRequest, ct);
        if (created is null)
        {
            return StatusCode(
                statusCode == 0 ? StatusCodes.Status502BadGateway : statusCode,
                errorContent ?? "ProjectService returned an error while creating the duplicate project.");
        }

        var copiedFileIds = new List<Guid>();
        var createdParts = new List<ProjectPartDto>();

        try
        {
            foreach (var sourcePart in source.Parts)
            {
                var copiedPart = await CopyPartForDuplicateAsync(
                    source,
                    created,
                    sourcePart,
                    uploadClient,
                    copiedFileIds,
                    ct);

                var (createdPart, addErrorContent, addStatusCode) = await client.AddPartAsync(created.Id, copiedPart, ct);
                if (createdPart is null)
                {
                    throw new InvalidOperationException(
                        $"ProjectService failed to add duplicated part '{sourcePart.FileName}' with HTTP {(addStatusCode == 0 ? 502 : addStatusCode)}: {addErrorContent}");
                }

                await EnrichSignedArtifactUrlsAsync(createdPart, uploadClient, ct);
                createdParts.Add(createdPart);
            }

            var duplicated = await client.GetProjectByIdAsync(created.Id, ct) ?? created;
            duplicated.Parts = createdParts.Count > 0 ? createdParts : duplicated.Parts;
            return Ok(duplicated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to duplicate project {ProjectId}; cleaning up duplicate project {DuplicateProjectId}", id, created.Id);
            await CleanupDuplicateAsync(created.Id, copiedFileIds, uploadClient, ct);
            return StatusCode(StatusCodes.Status502BadGateway, "Failed to duplicate the project. No reorder draft was created.");
        }
    }

    /// <summary>
    /// Updates a project's metadata fields (title, description, notes, validity period).
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="request">Fields to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPut("{id:guid}")]
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
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpDelete("{id:guid}")]
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
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/parts")]
    public async Task<ActionResult<ProjectPartDto>> AddPart(Guid id, [FromBody] AddProjectPartRequest request, CancellationToken ct)
    {
        var (result, errorContent, statusCode) = await client.AddPartAsync(id, request, ct);
        if (result != null)
            return Ok(result);

        return StatusCode(statusCode == 0 ? 502 : statusCode, errorContent ?? "ProjectService returned an error.");
    }

    /// <summary>
    /// Updates an existing part's configuration (process, material, quantity, finish, etc.).
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="partId">The part GUID.</param>
    /// <param name="request">Configuration update payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPut("{id:guid}/parts/{partId:guid}")]
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
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpDelete("{id:guid}/parts/{partId:guid}")]
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
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/parts/{partId:guid}/price")]
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
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/parts/{partId:guid}/confirm-price")]
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
    /// <param name="request">Quotation validity and delivery expectations.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/generate-quotation")]
    public async Task<IActionResult> GenerateQuotation(
        Guid id,
        [FromBody] GenerateQuotationRequest? request,
        CancellationToken ct)
    {
        var quotationRequest = request ?? new GenerateQuotationRequest();
        var response = await client.GenerateQuotationAsync(id, quotationRequest, ct);
        if (response.IsSuccessStatusCode)
        {
            var generatedProject = await ReadGeneratedProjectAsync(response, ct);
            var pdfGenerated = await TryGenerateQuotationPdfAsync(id, generatedProject, quotationRequest.PdfData, ct);
            if (!pdfGenerated)
                return StatusCode(StatusCodes.Status502BadGateway, "Quotation generated, but automatic PDF generation failed.");

            return NoContent();
        }

        var errorContent = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(errorContent))
            return StatusCode((int)response.StatusCode);

        return StatusCode((int)response.StatusCode, errorContent);
    }

    private static async Task<ProjectDetailDto?> ReadGeneratedProjectAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ProjectDetailDto>(cancellationToken: ct);
        }
        catch
        {
            return null;
        }
    }

    private async Task<bool> TryGenerateQuotationPdfAsync(
        Guid projectId,
        ProjectDetailDto? generatedProject,
        QuotationPdfData? submittedPdfData,
        CancellationToken ct)
    {
        if (quotationClient is null || pdfClient is null)
        {
            _logger.LogWarning("Skipping automatic quotation PDF generation because required clients are not registered.");
            return false;
        }

        var project = generatedProject?.QuotationId is Guid
            ? generatedProject
            : await client.GetProjectByIdAsync(projectId, ct);
        if (project?.QuotationId is not Guid quotationId)
        {
            _logger.LogWarning("Skipping automatic quotation PDF generation because project {ProjectId} has no quotation ID.", projectId);
            return false;
        }

        if (uploadClient is not null)
            await EnrichProjectDetailArtifactsAsync(project, uploadClient, analysisStatusService, ct);

        var quotation = await quotationClient.GetQuotationByIdAsync(quotationId, ct);
        if (quotation is null)
        {
            _logger.LogWarning("Skipping automatic quotation PDF generation because quotation {QuotationId} was not found.", quotationId);
            return false;
        }

        var customerDetail = await TryGetCustomerDetailAsync(project.CustomerId, ct);
        var pdfData = submittedPdfData is null || submittedPdfData.Items.Count == 0
            ? ProjectQuotationPdfDataFactory.Build(project, quotation, customerDetail)
            : ProjectQuotationPdfDataFactory.ApplyFormalQuotationMetadata(submittedPdfData, project, quotation, customerDetail);
        QuotationPdfMetadataApplicator.Apply(pdfData, HttpContext?.User);

        var pdfArtifact = await pdfClient.GeneratePdfArtifactAsync(
            PdfDocumentType.Quotation,
            quotation.QuotationNumber,
            pdfData,
            ct: ct);
        var pdfUrl = pdfArtifact?.StorageUrl;

        if (!string.IsNullOrWhiteSpace(pdfUrl))
        {
            var versionNumber = project.CurrentQuotationVersionNumber ?? quotation.CurrentVersionNumber;
            if (versionNumber > 0)
            {
                var attached = await quotationClient.AttachVersionPdfArtifactAsync(
                    quotationId,
                    versionNumber,
                    pdfUrl,
                    pdfArtifact?.StoragePath,
                    ct);
                if (!attached)
                {
                    _logger.LogWarning(
                        "Generated quotation PDF for project {ProjectId}, but failed to attach it to quotation {QuotationId} version {VersionNumber}.",
                        projectId,
                        quotationId,
                        versionNumber);
                }
            }
        }

        return !string.IsNullOrWhiteSpace(pdfUrl);
    }

    private async Task<CustomerDetailDto?> TryGetCustomerDetailAsync(Guid customerId, CancellationToken ct)
    {
        if (customerClient is null || customerId == Guid.Empty)
            return null;

        try
        {
            return await customerClient.GetCustomerByIdAsync(customerId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not hydrate customer {CustomerId} for automatic quotation PDF.", customerId);
            return null;
        }
    }

    /// <summary>
    /// Marks the project quotation as accepted by the customer, triggering order/job creation.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [RequirePermission(MalievPermissions.Project.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/accept-quotation")]
    public async Task<IActionResult> AcceptQuotation(Guid id, CancellationToken ct)
    {
        var response = await client.AcceptQuotationAsync(id, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    // ── Production planning ──────────────────────────────────────────────────

    /// <summary>
    /// Returns project-scoped production planning data for quoted parts.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The project production plan.</returns>
    [HttpGet("{id:guid}/production-plan")]
    public async Task<ActionResult<ProjectProductionPlanDto>> GetProductionPlan(Guid id, CancellationToken ct)
    {
        var project = await client.GetProjectByIdAsync(id, ct);
        if (project is null)
            return NotFound();

        if (uploadClient is not null)
            await EnrichProjectDetailArtifactsAsync(project, uploadClient, analysisStatusService, ct);

        var holds = await jobClient.GetPlanningHoldsAsync(projectId: id, activeOnly: true, ct: ct);
        var holdByPart = holds
            .GroupBy(hold => hold.ProjectPartId)
            .ToDictionary(group => group.Key, group => group.OrderBy(hold => hold.ExpiresAt).First());

        var partPlans = new List<ProjectProductionPartPlanDto>();
        foreach (var part in project.Parts.Where(part => !string.Equals(part.Status, "Removed", StringComparison.OrdinalIgnoreCase)))
        {
            var processType = part.ProcessType;
            ProductionRoutingDto? routing = null;
            if (!string.IsNullOrWhiteSpace(processType))
            {
                routing = await BuildPartRoutingAsync(id, part.Id, processType, ct);
            }

            holdByPart.TryGetValue(part.Id, out var activeHold);
            var blockReason = ResolvePlanningBlockReason(part);

            partPlans.Add(new ProjectProductionPartPlanDto
            {
                PartId = part.Id,
                FileName = part.FileName,
                ThumbnailUrl = part.ThumbnailUrl,
                Dimensions = FormatDimensions(part.Dimensions),
                ProcessType = part.ProcessType,
                MaterialName = FirstNonEmpty(part.MaterialName, part.MaterialCode),
                Configuration = FormatPartConfiguration(part),
                Quantity = part.Quantity,
                DfmStatus = part.HasDfmWarnings ? part.DfmAcknowledged ? "DFM acknowledged" : "DFM warnings" : "DFM passed",
                Routing = routing,
                ActiveHold = activeHold,
                JobId = part.JobId,
                JobStatus = part.JobStatus,
                MachineName = FirstNonEmpty(part.MachineName, activeHold?.MachineName, routing?.MachineName),
                CanCreateHold = blockReason is null && activeHold is null && part.JobId is null,
                HoldBlockReason = activeHold is not null ? "A planning hold already exists." : blockReason
            });
        }

        var scheduleBoard = await BuildProjectScheduleBoardAsync(id, partPlans, ct);

        return Ok(new ProjectProductionPlanDto
        {
            ProjectId = id,
            Parts = partPlans,
            ScheduleBoard = scheduleBoard
        });
    }

    /// <summary>
    /// Creates a tentative queue hold for a project part.
    /// </summary>
    [RequirePermission(MalievPermissions.Job.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/parts/{partId:guid}/planning-hold")]
    public async Task<ActionResult<ProductionPlanningHoldDto>> CreatePlanningHold(
        Guid id,
        Guid partId,
        [FromBody] CreateProductionPlanningHoldRequest request,
        CancellationToken ct)
    {
        var project = await client.GetProjectByIdAsync(id, ct);
        var part = project?.Parts.FirstOrDefault(candidate => candidate.Id == partId);
        if (project is null || part is null)
            return NotFound();

        var blockReason = ResolvePlanningBlockReason(part);
        if (blockReason is not null)
            return Conflict(new { error = blockReason });

        var forwarded = NormalizeCreateHoldRequest(project, part, request);
        var response = await jobClient.CreatePlanningHoldAsync(forwarded, ct);
        return await ForwardPlanningHoldResponseAsync(response, ct);
    }

    /// <summary>
    /// Updates a tentative queue hold for a project part.
    /// </summary>
    [RequirePermission(MalievPermissions.Job.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("{id:guid}/planning-holds/{holdId:guid}")]
    public async Task<ActionResult<ProductionPlanningHoldDto>> UpdatePlanningHold(
        Guid id,
        Guid holdId,
        [FromBody] UpdateProductionPlanningHoldRequest request,
        CancellationToken ct)
    {
        var project = await client.GetProjectByIdAsync(id, ct);
        if (project is null)
            return NotFound();

        var forwarded = NormalizeUpdateHoldRequest(project, request);
        var response = await jobClient.UpdatePlanningHoldAsync(holdId, forwarded, ct);
        return await ForwardPlanningHoldResponseAsync(response, ct);
    }

    /// <summary>
    /// Cancels a tentative queue hold for a project part.
    /// </summary>
    [RequirePermission(MalievPermissions.Job.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpDelete("{id:guid}/planning-holds/{holdId:guid}")]
    public async Task<ActionResult<ProductionPlanningHoldDto>> CancelPlanningHold(
        Guid id,
        Guid holdId,
        CancellationToken ct)
    {
        var project = await client.GetProjectByIdAsync(id, ct);
        if (project is null)
            return NotFound();

        var response = await jobClient.CancelPlanningHoldAsync(holdId, ct);
        return await ForwardPlanningHoldResponseAsync(response, ct);
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

        return Ok(await BuildPartRoutingAsync(id, partId, processType, ct));
    }

    private async Task<ProductionRoutingDto> BuildPartRoutingAsync(
        Guid id,
        Guid partId,
        string processType,
        CancellationToken ct)
    {
        var normalizedProcessType = NormalizeProductionTechnology(processType);
        var queueAhead = 0;
        try
        {
            var queueDepths = await jobClient.GetQueueDepthAsync(normalizedProcessType, ct);
            queueDepths?.TryGetValue(normalizedProcessType, out queueAhead);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("JobService request cancelled or timed out for '{ProcessType}'; defaulting to 0.", processType);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "JobService request failed for '{ProcessType}'; defaulting to 0.", processType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching queue depth for '{ProcessType}'; defaulting to 0.", processType);
        }

        var category = MapProcessToEquipmentCategory(normalizedProcessType);
        EquipmentSummaryDto? machine = null;
        IReadOnlyList<PlanningScheduleItemDto> scheduleItems = [];

        if (category is not null)
        {
            try
            {
                var equipments = await facilityClient.GetEquipmentsAsync(
                    category: category, status: "Active", page: 1, pageSize: 50, ct: ct);
                var candidates = equipments?.Items ?? [];

                if (candidates.Count > 0)
                {
                    var from = DateTime.UtcNow.Date;
                    var to = from.AddDays(30);

                    // Fetch schedule for all candidate machines in parallel, then pick least-loaded
                    var scheduleTasks = candidates.Select(async m =>
                    {
                        try
                        {
                            var slots = await jobClient.GetMachineScheduleAsync(m.AssetCode, from, to, ct);
                            return (Machine: m, Slots: (IReadOnlyList<MachineScheduleItemDto>)slots);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            _logger.LogWarning(ex, "Failed to fetch schedule for machine '{AssetCode}'.", m.AssetCode);
                            return (Machine: m, Slots: (IReadOnlyList<MachineScheduleItemDto>)[]);
                        }
                    });

                    var results = await Task.WhenAll(scheduleTasks);
                    var best = results.MinBy(r => r.Slots.Count);
                    machine = best.Machine;
                    scheduleItems = best.Slots.Select(s => new PlanningScheduleItemDto(
                        PlannedDate: new DateTimeOffset(s.ScheduledStart, TimeSpan.Zero),
                        PlannedEndDate: new DateTimeOffset(s.ScheduledEnd, TimeSpan.Zero),
                        JobReference: (s.IsHold ? "HOLD-" : string.Empty) + s.JobId.ToString("N")[..8].ToUpperInvariant(),
                        Status: s.Status,
                        JobId: s.JobId,
                        MachineName: best.Machine.Name,
                        SetupTimeMinutes: s.SetupMinutes,
                        PrintTimeMinutes: s.PrintMinutes,
                        IsHold: s.IsHold,
                        HoldId: s.HoldId
                    )).ToList();
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("FacilityService request cancelled or timed out for category '{Category}'; machine lookup skipped.", category);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "FacilityService request failed for category '{Category}'; machine lookup skipped.", category);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unexpected error fetching equipment for category '{Category}'; machine lookup skipped.", category);
            }
        }
        else
        {
            _logger.LogWarning("No equipment category mapping for process type '{ProcessType}'; machine lookup skipped.", processType);
        }

        // CNC requires fixture and toolpath verification; other processes need only 1 setup day.
        var setupDays = normalizedProcessType.StartsWith("CNC", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
        var estimatedStart = DateTimeOffset.UtcNow.AddDays(setupDays + queueAhead);

        // Compute proposed slot: starts right after the last scheduled job ends (or now if queue is empty).
        // Uses default setup/print durations matching TimeEstimationService defaults.
        DateTimeOffset? proposedSlotStart = null;
        DateTimeOffset? proposedSlotEnd = null;
        if (machine is not null)
        {
            var lastEnd = scheduleItems.Count > 0
                ? scheduleItems.Max(s => s.PlannedEndDate)
                : estimatedStart;
            proposedSlotStart = lastEnd;
            var defaultSetupMin = normalizedProcessType.StartsWith("CNC", StringComparison.OrdinalIgnoreCase) ? 60
                : normalizedProcessType is "SLA_DLP" or "SLA" ? 30
                : 15;
            proposedSlotEnd = proposedSlotStart.Value.AddMinutes(defaultSetupMin + 30);
        }

        return new ProductionRoutingDto(
            MachineId: machine?.Id ?? Guid.Empty,
            MachineCode: machine?.AssetCode ?? "TBD",
            MachineName: machine?.Name ?? "Unassigned",
            QueueAhead: queueAhead,
            EstimatedStartDate: estimatedStart,
            ScheduleItems: scheduleItems,
            ProposedSlotStart: proposedSlotStart,
            ProposedSlotEnd: proposedSlotEnd);
    }

    private async Task<ProductionScheduleBoardDto> BuildProjectScheduleBoardAsync(
        Guid projectId,
        IReadOnlyList<ProjectProductionPartPlanDto> partPlans,
        CancellationToken ct)
    {
        var rangeFrom = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var rangeTo = rangeFrom.AddDays(7);
        var machines = await GetProjectScheduleMachinesAsync(partPlans, ct);
        var technologies = partPlans
            .Select(part => NormalizeProductionTechnology(part.ProcessType ?? string.Empty))
            .Where(technology => !string.IsNullOrWhiteSpace(technology))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var machineIds = machines
            .Select(machine => machine.AssetCode)
            .Where(machineId => !string.IsNullOrWhiteSpace(machineId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var scheduleGroups = machineIds.Count == 0
            ? []
            : await jobClient.GetScheduleAsync(rangeFrom, rangeTo, machineIds, technologies, ct);
        var scheduleByMachine = scheduleGroups
            .GroupBy(group => group.MachineId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.SelectMany(item => item.Schedule).ToList(),
                StringComparer.OrdinalIgnoreCase);
        var partFileNames = partPlans.ToDictionary(part => part.PartId, part => part.FileName);

        var boardMachines = machines
            .OrderBy(machine => machine.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(machine => machine.Name, StringComparer.OrdinalIgnoreCase)
            .Select(machine =>
            {
                scheduleByMachine.TryGetValue(machine.AssetCode, out var scheduleSlots);
                var slots = (scheduleSlots ?? [])
                    .OrderBy(slot => slot.ScheduledStart)
                    .Select(slot => MapProjectScheduleSlot(projectId, machine, slot, partFileNames))
                    .ToList();

                slots.AddRange(BuildProjectProposedSlots(projectId, machine, partPlans, rangeFrom, rangeTo));
                AddMissingActiveHoldSlots(projectId, machine, partPlans, slots, partFileNames);

                return new ProductionScheduleMachineDto
                {
                    MachineId = machine.AssetCode,
                    MachineName = machine.Name,
                    Category = machine.Category,
                    Technology = NormalizeEquipmentCategoryTechnology(machine.Category),
                    Slots = slots.OrderBy(slot => slot.ScheduledStart).ToList()
                };
            })
            .ToList();

        return new ProductionScheduleBoardDto
        {
            RangeStart = rangeFrom,
            RangeEnd = rangeTo,
            Machines = boardMachines
        };
    }

    private async Task<List<EquipmentSummaryDto>> GetProjectScheduleMachinesAsync(
        IReadOnlyList<ProjectProductionPartPlanDto> partPlans,
        CancellationToken ct)
    {
        var machinesByCode = new Dictionary<string, EquipmentSummaryDto>(StringComparer.OrdinalIgnoreCase);
        var categories = partPlans
            .Select(part => MapProcessToEquipmentCategory(NormalizeProductionTechnology(part.ProcessType ?? string.Empty)))
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var category in categories)
        {
            try
            {
                var equipments = await facilityClient.GetEquipmentsAsync(category: category, status: "Active", page: 1, pageSize: 50, ct: ct);
                foreach (var machine in equipments?.Items ?? [])
                {
                    if (!string.IsNullOrWhiteSpace(machine.AssetCode))
                    {
                        machinesByCode[machine.AssetCode] = machine;
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to load schedule machines for category '{Category}'.", category);
            }
        }

        foreach (var part in partPlans)
        {
            if (part.Routing is { MachineCode: not "TBD" } routing && !string.IsNullOrWhiteSpace(routing.MachineCode))
            {
                machinesByCode.TryAdd(routing.MachineCode, new EquipmentSummaryDto
                {
                    Id = routing.MachineId,
                    AssetCode = routing.MachineCode,
                    Name = routing.MachineName,
                    Category = MapProcessToEquipmentCategory(NormalizeProductionTechnology(part.ProcessType ?? string.Empty)) ?? string.Empty,
                    Status = "Active"
                });
            }

            if (part.ActiveHold is { } hold && !string.IsNullOrWhiteSpace(hold.MachineId))
            {
                machinesByCode.TryAdd(hold.MachineId, new EquipmentSummaryDto
                {
                    AssetCode = hold.MachineId,
                    Name = FirstNonEmpty(hold.MachineName, hold.MachineId) ?? hold.MachineId,
                    Category = MapProcessToEquipmentCategory(NormalizeProductionTechnology(part.ProcessType ?? string.Empty)) ?? string.Empty,
                    Status = "Active"
                });
            }
        }

        return machinesByCode.Values.ToList();
    }

    private static ProductionScheduleSlotDto MapProjectScheduleSlot(
        Guid projectId,
        EquipmentSummaryDto machine,
        MachineScheduleItemDto slot,
        IReadOnlyDictionary<Guid, string> partFileNames)
    {
        var isCurrentProject = slot.ProjectId == projectId;
        return new ProductionScheduleSlotDto
        {
            SlotId = slot.HoldId ?? slot.JobId,
            JobId = slot.IsHold ? null : slot.JobId,
            HoldId = slot.HoldId,
            ProjectId = slot.ProjectId,
            ProjectPartId = slot.ProjectPartId,
            FileName = slot.ProjectPartId.HasValue && partFileNames.TryGetValue(slot.ProjectPartId.Value, out var fileName) ? fileName : null,
            MachineId = machine.AssetCode,
            MachineName = machine.Name,
            Technology = slot.Technology,
            ScheduledStart = DateTime.SpecifyKind(slot.ScheduledStart, DateTimeKind.Utc),
            ScheduledEnd = DateTime.SpecifyKind(slot.ScheduledEnd, DateTimeKind.Utc),
            SetupMinutes = slot.SetupMinutes,
            ProductionMinutes = slot.PrintMinutes,
            QueuePosition = slot.QueuePosition,
            Status = slot.Status,
            Label = slot.IsHold ? $"Hold #{slot.QueuePosition}" : slot.JobId.ToString("N")[..8].ToUpperInvariant(),
            ExpiresAt = slot.ExpiresAt,
            IsHold = slot.IsHold,
            IsCurrentProject = isCurrentProject,
            CanMove = slot.IsHold || string.Equals(slot.Status, "Queued", StringComparison.OrdinalIgnoreCase)
        };
    }

    private static List<ProductionScheduleSlotDto> BuildProjectProposedSlots(
        Guid projectId,
        EquipmentSummaryDto machine,
        IReadOnlyList<ProjectProductionPartPlanDto> partPlans,
        DateTime rangeFrom,
        DateTime rangeTo)
    {
        var slots = new List<ProductionScheduleSlotDto>();
        foreach (var part in partPlans)
        {
            if (part.ActiveHold is not null || part.JobId is not null || part.Routing is not { } routing)
            {
                continue;
            }

            if (!machine.AssetCode.Equals(routing.MachineCode, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var start = (routing.ProposedSlotStart ?? routing.EstimatedStartDate).UtcDateTime;
            var setupMinutes = DefaultSetupMinutes(part.ProcessType);
            var productionMinutes = Math.Max(30, part.Quantity * 30);
            var end = routing.ProposedSlotEnd?.UtcDateTime ?? start.AddMinutes(setupMinutes + productionMinutes);
            if (end <= rangeFrom || start >= rangeTo)
            {
                continue;
            }

            slots.Add(new ProductionScheduleSlotDto
            {
                SlotId = part.PartId,
                ProjectId = projectId,
                ProjectPartId = part.PartId,
                FileName = part.FileName,
                MachineId = machine.AssetCode,
                MachineName = machine.Name,
                Technology = NormalizeProductionTechnology(part.ProcessType ?? string.Empty),
                ScheduledStart = DateTime.SpecifyKind(start, DateTimeKind.Utc),
                ScheduledEnd = DateTime.SpecifyKind(end, DateTimeKind.Utc),
                SetupMinutes = setupMinutes,
                ProductionMinutes = productionMinutes,
                QueuePosition = routing.QueueAhead + 1,
                Status = "Proposed",
                Label = "Proposed",
                IsProposed = true,
                IsCurrentProject = true,
                CanMove = false
            });
        }

        return slots;
    }

    private static void AddMissingActiveHoldSlots(
        Guid projectId,
        EquipmentSummaryDto machine,
        IReadOnlyList<ProjectProductionPartPlanDto> partPlans,
        List<ProductionScheduleSlotDto> slots,
        IReadOnlyDictionary<Guid, string> partFileNames)
    {
        foreach (var part in partPlans)
        {
            if (part.ActiveHold is not { } hold ||
                !machine.AssetCode.Equals(hold.MachineId, StringComparison.OrdinalIgnoreCase) ||
                slots.Any(slot => slot.HoldId == hold.Id))
            {
                continue;
            }

            slots.Add(new ProductionScheduleSlotDto
            {
                SlotId = hold.Id,
                HoldId = hold.Id,
                ProjectId = projectId,
                ProjectPartId = part.PartId,
                FileName = partFileNames.TryGetValue(part.PartId, out var fileName) ? fileName : part.FileName,
                MachineId = machine.AssetCode,
                MachineName = machine.Name,
                Technology = hold.Technology,
                ScheduledStart = DateTime.SpecifyKind(hold.ScheduledStartTime, DateTimeKind.Utc),
                ScheduledEnd = DateTime.SpecifyKind(hold.ScheduledEndTime, DateTimeKind.Utc),
                SetupMinutes = hold.SetupTimeMinutes,
                ProductionMinutes = hold.ProductionTimeMinutes,
                QueuePosition = hold.QueuePosition,
                Status = "Planning Hold",
                Label = $"Hold #{hold.QueuePosition}",
                ExpiresAt = hold.ExpiresAt,
                IsHold = true,
                IsCurrentProject = true,
                CanMove = true
            });
        }
    }

    private static string NormalizeEquipmentCategoryTechnology(string? category) => category switch
    {
        "FdmPrinter" => "FDM",
        "SlaPrinter" => "SLA",
        "CncMachine" => "CNC_MILL",
        "InjectionMolding" => "INJECTION_MOLDING",
        _ => string.Empty
    };

    private static int DefaultSetupMinutes(string? processType)
    {
        var normalized = NormalizeProductionTechnology(processType ?? string.Empty);
        if (normalized.StartsWith("CNC", StringComparison.OrdinalIgnoreCase))
        {
            return 60;
        }

        return normalized is "SLA" or "SLA_DLP" ? 30 : 15;
    }

    private static CreateProductionPlanningHoldRequest NormalizeCreateHoldRequest(
        ProjectDetailDto project,
        ProjectPartDto part,
        CreateProductionPlanningHoldRequest request)
    {
        var expiresAt = ResolveHoldExpiration(project, request.ExpiresAt);
        return new CreateProductionPlanningHoldRequest
        {
            ProjectId = project.Id,
            ProjectPartId = part.Id,
            Technology = NormalizeProductionTechnology(FirstNonEmpty(request.Technology, part.ProcessType) ?? string.Empty),
            MachineId = request.MachineId,
            MachineName = request.MachineName,
            QueuePosition = request.QueuePosition,
            ScheduledStartTime = EnsureUtc(request.ScheduledStartTime == default ? DateTime.UtcNow.AddDays(1) : request.ScheduledStartTime),
            ScheduledEndTime = request.ScheduledEndTime.HasValue ? EnsureUtc(request.ScheduledEndTime.Value) : null,
            SetupTimeMinutes = request.SetupTimeMinutes,
            ProductionTimeMinutes = request.ProductionTimeMinutes <= 0 ? 30 : request.ProductionTimeMinutes,
            Quantity = Math.Max(1, part.Quantity),
            Notes = request.Notes,
            ExpiresAt = expiresAt,
            AutoSnapToNextAvailable = request.AutoSnapToNextAvailable,
        };
    }

    private static UpdateProductionPlanningHoldRequest NormalizeUpdateHoldRequest(
        ProjectDetailDto project,
        UpdateProductionPlanningHoldRequest request)
    {
        return new UpdateProductionPlanningHoldRequest
        {
            MachineId = request.MachineId,
            MachineName = request.MachineName,
            QueuePosition = request.QueuePosition,
            ScheduledStartTime = EnsureUtc(request.ScheduledStartTime == default ? DateTime.UtcNow.AddDays(1) : request.ScheduledStartTime),
            ScheduledEndTime = request.ScheduledEndTime.HasValue ? EnsureUtc(request.ScheduledEndTime.Value) : null,
            SetupTimeMinutes = request.SetupTimeMinutes,
            ProductionTimeMinutes = request.ProductionTimeMinutes <= 0 ? 30 : request.ProductionTimeMinutes,
            Notes = request.Notes,
            ExpiresAt = ResolveHoldExpiration(project, request.ExpiresAt)
        };
    }

    private static DateTime ResolveHoldExpiration(ProjectDetailDto project, DateTime requestedExpiration)
    {
        var defaultExpiration = DateTime.UtcNow.AddHours(72);
        var requested = requestedExpiration == default ? defaultExpiration : EnsureUtc(requestedExpiration);
        var capped = requested > defaultExpiration ? defaultExpiration : requested;
        if (project.ValidUntil.HasValue)
        {
            var quoteValidUntil = EnsureUtc(project.ValidUntil.Value);
            if (quoteValidUntil > DateTime.UtcNow && quoteValidUntil < capped)
                capped = quoteValidUntil;
        }

        return capped;
    }

    private static async Task<ActionResult<ProductionPlanningHoldDto>> ForwardPlanningHoldResponseAsync(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            var hold = await response.Content.ReadFromJsonAsync<ProductionPlanningHoldDto>(cancellationToken: ct);
            return hold is not null ? new OkObjectResult(hold) : new StatusCodeResult(StatusCodes.Status502BadGateway);
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        return new ObjectResult(string.IsNullOrWhiteSpace(content) ? "Planning hold operation failed." : content)
        {
            StatusCode = (int)response.StatusCode
        };
    }

    private static string? ResolvePlanningBlockReason(ProjectPartDto part)
    {
        if (part.JobId.HasValue)
            return "A production job already exists for this part.";

        if (string.IsNullOrWhiteSpace(part.ProcessType))
            return "Process must be selected before planning.";

        if (string.IsNullOrWhiteSpace(part.MaterialName) && string.IsNullOrWhiteSpace(part.MaterialCode))
            return "Material must be selected before planning.";

        if (part.HasDfmWarnings && !part.DfmAcknowledged)
            return "DFM warnings must be acknowledged before planning.";

        return null;
    }

    private static string? FormatPartConfiguration(ProjectPartDto part)
    {
        var values = new[] { part.Finish, part.Color, part.Tolerance }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => FormatConfigurationToken(value!));

        return string.Join(" / ", values);
    }

    private static string FormatConfigurationToken(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return trimmed;

        return trimmed.ToUpperInvariant() switch
        {
            "AS_MACHINED" => "As Machined",
            "AS_PRINTED" => "As Printed",
            "FDM_STD" => "Standard FDM settings",
            "ISO2768_M" or "ISO2768-M" => "ISO 2768-m",
            _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
                trimmed
                    .Replace('_', ' ')
                    .Replace('-', ' ')
                    .ToLowerInvariant())
        };
    }

    private static string? FormatDimensions(ModelDimensionsDto? dimensions)
    {
        return dimensions is null
            ? null
            : $"{dimensions.X:0.#} x {dimensions.Y:0.#} x {dimensions.Z:0.#} mm";
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, value.Kind == DateTimeKind.Unspecified ? DateTimeKind.Utc : value.Kind).ToUniversalTime();
    }

    private async Task<AddProjectPartRequest> CopyPartForDuplicateAsync(
        ProjectDetailDto sourceProject,
        ProjectDetailDto duplicateProject,
        ProjectPartDto sourcePart,
        UploadServiceClient upload,
        List<Guid> copiedFileIds,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sourcePart.FileReference))
            throw new InvalidOperationException($"Source part '{sourcePart.FileName}' does not have a file reference.");

        var destinationPath = BuildProjectUploadPath(duplicateProject.Id, sourceProject.CustomerId, sourcePart.FileName);
        var copiedFile = await upload.CopyFileWithMetadataAsync(
            sourcePart.FileReference,
            destinationPath,
            sourcePart.FileName,
            metadata: new Dictionary<string, string>
            {
                ["source_project_id"] = sourceProject.Id.ToString(),
                ["source_part_id"] = sourcePart.Id.ToString(),
                ["source_file_id"] = sourcePart.FileId.ToString()
            },
            ct: ct);

        if (copiedFile is null || !Guid.TryParse(copiedFile.FileId, out var copiedFileId))
            throw new InvalidOperationException($"UploadService failed to copy source file for part '{sourcePart.FileName}'.");

        copiedFileIds.Add(copiedFileId);

        var thumbnailSmallPath = await CopyArtifactPathAsync(
            upload,
            sourcePart.FileReference,
            copiedFile.StoragePath,
            sourcePart.ThumbnailSmallGcsPath,
            "thumbnail_small",
            ct);
        var thumbnailLargePath = await CopyArtifactPathAsync(
            upload,
            sourcePart.FileReference,
            copiedFile.StoragePath,
            sourcePart.ThumbnailLargeGcsPath,
            "thumbnail_large",
            ct);
        var glbPath = await CopyArtifactPathAsync(
            upload,
            sourcePart.FileReference,
            copiedFile.StoragePath,
            sourcePart.GlbStoragePath,
            "viewer",
            ct);
        var overlayPaths = await CopyOverlayPathsAsync(
            upload,
            sourcePart.FileReference,
            copiedFile.StoragePath,
            sourcePart.OverlayPaths,
            ct);
        var drawingFiles = await CopyAttachmentsAsync(
            upload,
            duplicateProject.Id,
            sourceProject.CustomerId,
            sourcePart.DrawingFiles,
            copiedFileIds,
            "drawings",
            ct);
        var supplementaryFiles = await CopyAttachmentsAsync(
            upload,
            duplicateProject.Id,
            sourceProject.CustomerId,
            sourcePart.SupplementaryFiles,
            copiedFileIds,
            "supplementary",
            ct);

        var thumbnailSmallUrl = await GetSignedUrlIfPresentAsync(upload, thumbnailSmallPath, ct);
        var thumbnailLargeUrl = await GetSignedUrlIfPresentAsync(upload, thumbnailLargePath, ct);
        var glbSignedUrl = await GetSignedUrlIfPresentAsync(upload, glbPath, ct);

        if (analysisStatusService is not null)
        {
            await analysisStatusService.CloneStatusAsync(
                sourcePart.FileReference,
                copiedFile.StoragePath,
                thumbnailSmallUrl,
                thumbnailLargeUrl,
                thumbnailSmallPath,
                thumbnailLargePath,
                glbPath,
                glbSignedUrl,
                ct);
        }

        return new AddProjectPartRequest
        {
            FileId = copiedFileId,
            FileReference = copiedFile.StoragePath,
            ThumbnailSmallGcsPath = thumbnailSmallPath,
            ThumbnailLargeGcsPath = thumbnailLargePath,
            GlbStoragePath = glbPath,
            OverlayPaths = overlayPaths,
            FileName = sourcePart.FileName,
            ProcessType = sourcePart.ProcessType,
            MaterialId = sourcePart.MaterialId,
            MaterialName = sourcePart.MaterialName,
            MaterialCode = sourcePart.MaterialCode,
            Quantity = sourcePart.Quantity,
            Finish = sourcePart.Finish,
            Color = sourcePart.Color,
            Tolerance = sourcePart.Tolerance,
            PartNotes = sourcePart.PartNotes,
            RoughnessCode = sourcePart.RoughnessCode,
            MarkingType = sourcePart.MarkingType,
            MarkingText = sourcePart.MarkingText,
            DfmAcknowledged = sourcePart.DfmAcknowledged,
            HasThreadedHoles = sourcePart.HasThreadedHoles,
            ThreadedHoleSpec = sourcePart.ThreadedHoleSpec,
            ThreadedHoleCount = sourcePart.ThreadedHoleCount,
            HasInserts = sourcePart.HasInserts,
            InsertType = sourcePart.InsertType,
            InsertCount = sourcePart.InsertCount,
            BagAndTag = sourcePart.BagAndTag,
            InspectionLevel = sourcePart.InspectionLevel,
            Certificates = [.. sourcePart.Certificates],
            DrawingFiles = drawingFiles,
            SupplementaryFiles = supplementaryFiles,
            ProcessConfig = new Dictionary<string, string>(sourcePart.ProcessConfig),
            BodyCount = sourcePart.BodyCount,
            BodiesJson = sourcePart.BodiesJson,
            SelectedBodyIndex = sourcePart.SelectedBodyIndex,
            VolumeCm3 = null,
            BoundingBoxX = sourcePart.Dimensions is null ? null : (decimal)sourcePart.Dimensions.X,
            BoundingBoxY = sourcePart.Dimensions is null ? null : (decimal)sourcePart.Dimensions.Y,
            BoundingBoxZ = sourcePart.Dimensions is null ? null : (decimal)sourcePart.Dimensions.Z,
            IsManifold = sourcePart.IsManifold
        };
    }

    private static async Task<string?> CopyArtifactPathAsync(
        UploadServiceClient upload,
        string sourceFilePath,
        string destinationFilePath,
        string? sourceArtifactPath,
        string artifactKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sourceArtifactPath))
            return null;

        var destinationPath = BuildArtifactDestinationPath(
            sourceFilePath,
            destinationFilePath,
            sourceArtifactPath,
            artifactKey);

        return await upload.CopyFileAsync(sourceArtifactPath, destinationPath, ct)
            ? destinationPath
            : null;
    }

    private static async Task<Dictionary<string, string>> CopyOverlayPathsAsync(
        UploadServiceClient upload,
        string sourceFilePath,
        string destinationFilePath,
        Dictionary<string, string> sourceOverlayPaths,
        CancellationToken ct)
    {
        var copied = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, path) in sourceOverlayPaths)
        {
            var copiedPath = await CopyArtifactPathAsync(upload, sourceFilePath, destinationFilePath, path, key, ct);
            if (!string.IsNullOrWhiteSpace(copiedPath))
                copied[key] = copiedPath;
        }

        return copied;
    }

    private static async Task<List<ProjectPartAttachmentDto>> CopyAttachmentsAsync(
        UploadServiceClient upload,
        Guid duplicateProjectId,
        Guid customerId,
        List<ProjectPartAttachmentDto> sourceAttachments,
        List<Guid> copiedFileIds,
        string attachmentFolder,
        CancellationToken ct)
    {
        var copied = new List<ProjectPartAttachmentDto>();
        foreach (var source in sourceAttachments)
        {
            if (string.IsNullOrWhiteSpace(source.StoragePath))
                continue;

            var destinationPath = BuildAttachmentPath(duplicateProjectId, customerId, attachmentFolder, source.FileName);
            var copiedFile = await upload.CopyFileWithMetadataAsync(
                source.StoragePath,
                destinationPath,
                source.FileName,
                metadata: new Dictionary<string, string>
                {
                    ["source_attachment_file_id"] = source.FileId?.ToString() ?? string.Empty,
                    ["source_attachment_path"] = source.StoragePath
                },
                ct: ct);

            if (copiedFile is null || !Guid.TryParse(copiedFile.FileId, out var copiedFileId))
                throw new InvalidOperationException($"UploadService failed to copy attachment '{source.FileName}'.");

            copiedFileIds.Add(copiedFileId);
            copied.Add(new ProjectPartAttachmentDto
            {
                FileId = copiedFileId,
                FileName = source.FileName,
                StoragePath = copiedFile.StoragePath,
                SignedUrl = await upload.GetDownloadUrlByPathAsync(copiedFile.StoragePath, ct),
                SizeBytes = copiedFile.SizeBytes,
                ContentType = copiedFile.ContentType,
                UploadedAt = copiedFile.UploadedAt
            });
        }

        return copied;
    }

    private static async Task EnrichSignedArtifactUrlsAsync(
        ProjectPartDto part,
        UploadServiceClient upload,
        CancellationToken ct)
    {
        await EnrichPartArtifactsAsync(part, upload, analysisStatusService: null, ct);
    }

    private static async Task EnrichProjectSummaryPreviewsAsync(
        IEnumerable<ProjectSummaryDto> projects,
        UploadServiceClient upload,
        IFileAnalysisStatusService? analysisStatusService,
        CancellationToken ct)
    {
        var previews = projects
            .SelectMany(project => project.PartPreviews)
            .ToList();

        await Task.WhenAll(previews.Select(preview =>
            EnrichProjectSummaryPreviewAsync(preview, upload, analysisStatusService, ct)));
    }

    private static async Task EnrichProjectSummaryPreviewAsync(
        ProjectPartPreviewDto preview,
        UploadServiceClient upload,
        IFileAnalysisStatusService? analysisStatusService,
        CancellationToken ct)
    {
        if (analysisStatusService is not null && !string.IsNullOrWhiteSpace(preview.FileReference))
        {
            var status = await analysisStatusService.GetStatusAsync(preview.FileReference, ct);
            if (status is not null)
            {
                preview.ThumbnailUrl = FirstNonEmpty(
                    preview.ThumbnailUrl,
                    status.PreviewUrls?.ThumbnailSmall,
                    status.ThumbnailUrl,
                    status.HiResThumbnailUrl);
                preview.ThumbnailSmallGcsPath = FirstNonEmpty(preview.ThumbnailSmallGcsPath, status.PreviewUrls?.ThumbnailSmallGcsPath);
                preview.ThumbnailLargeGcsPath = FirstNonEmpty(preview.ThumbnailLargeGcsPath, status.PreviewUrls?.ThumbnailLargeGcsPath);
            }
        }

        if (string.IsNullOrWhiteSpace(preview.ThumbnailUrl))
            preview.ThumbnailUrl = await GetSignedUrlIfPresentAsync(upload, preview.ThumbnailSmallGcsPath, ct)
                ?? await GetSignedUrlIfPresentAsync(upload, preview.ThumbnailLargeGcsPath, ct);
    }

    private static async Task EnrichProjectDetailArtifactsAsync(
        ProjectDetailDto project,
        UploadServiceClient upload,
        IFileAnalysisStatusService? analysisStatusService,
        CancellationToken ct)
    {
        foreach (var part in project.Parts)
        {
            await EnrichPartArtifactsAsync(part, upload, analysisStatusService, ct);
        }
    }

    private async Task EnrichProjectCustomerAsync(
        ProjectDetailDto project,
        CustomerServiceClient customer,
        CancellationToken ct)
    {
        try
        {
            var detail = await customer.GetCustomerByIdAsync(project.CustomerId, ct);
            if (detail is null)
                return;

            project.CustomerName = FirstNonEmpty(detail.Name, project.CustomerName) ?? project.CustomerName;
            project.CustomerProfileImageUrl = FirstNonEmpty(detail.ProfileImageUrl, project.CustomerProfileImageUrl);
            project.CustomerEmail = FirstNonEmpty(detail.Email);
            project.CustomerPhone = FirstNonEmpty(detail.Mobile, detail.Landline, detail.CompanyPhone);
            project.CustomerStatus = FirstNonEmpty(detail.Status);
            project.CustomerSegment = FirstNonEmpty(detail.CompanySegment, detail.Segment);
            project.CustomerTier = FirstNonEmpty(detail.CompanyTier, detail.Tier);
            project.CustomerPreferredLanguage = FirstNonEmpty(detail.PreferredLanguage);
            project.CustomerTimezone = FirstNonEmpty(detail.Timezone);
            project.CustomerCompanyId = detail.CompanyId;
            project.CustomerCompanyName = FirstNonEmpty(detail.CompanyName);
            project.CustomerCompanyPhone = FirstNonEmpty(detail.CompanyPhone);
            project.CustomerCompanyEmail = FirstNonEmpty(detail.CompanyContactEmail);
            project.CustomerTaxId = FirstNonEmpty(project.CustomerTaxId, detail.CompanyVatNumber, detail.CompanyRegistrationNumber);

            var shippingAddress = SelectAddress(detail.Addresses, "Shipping")
                ?? SelectAddress(detail.Addresses, "Delivery")
                ?? SelectAddress(detail.Addresses, "Billing")
                ?? detail.Addresses.FirstOrDefault();
            var billingAddress = SelectAddress(detail.Addresses, "Billing") ?? detail.CompanyBillingAddress;
            project.CustomerBranch = FirstNonEmpty(project.CustomerBranch, ResolveCustomerBranch(detail, billingAddress));

            project.ShippingAddressLine = FormatAddress(shippingAddress);
            project.ShippingRecipientName = FirstNonEmpty(shippingAddress?.RecipientName, detail.Name);
            project.ShippingRecipientPhone = FirstNonEmpty(shippingAddress?.RecipientPhone, detail.Mobile, detail.Landline, detail.CompanyPhone);
            project.BillingAddressLine = FormatAddress(billingAddress);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to hydrate customer details for project {ProjectId}", project.Id);
        }
    }

    private static async Task EnrichPartArtifactsAsync(
        ProjectPartDto part,
        UploadServiceClient upload,
        IFileAnalysisStatusService? analysisStatusService,
        CancellationToken ct)
    {
        if (analysisStatusService is not null && !string.IsNullOrWhiteSpace(part.FileReference))
        {
            var status = await analysisStatusService.GetStatusAsync(part.FileReference, ct);
            if (status is not null)
            {
                part.ThumbnailUrl = FirstNonEmpty(
                    part.ThumbnailUrl,
                    status.PreviewUrls?.ThumbnailSmall,
                    status.ThumbnailUrl,
                    status.HiResThumbnailUrl);
                part.ThumbnailSmallGcsPath = FirstNonEmpty(part.ThumbnailSmallGcsPath, status.PreviewUrls?.ThumbnailSmallGcsPath);
                part.ThumbnailLargeGcsPath = FirstNonEmpty(part.ThumbnailLargeGcsPath, status.PreviewUrls?.ThumbnailLargeGcsPath);
                part.GlbStoragePath = FirstNonEmpty(part.GlbStoragePath, status.GlbStoragePath);
                part.ViewerStoragePath = FirstNonEmpty(part.ViewerStoragePath, status.ViewerStoragePath, status.GlbStoragePath);
                part.ViewerFileExtension = FirstNonEmpty(part.ViewerFileExtension, status.ViewerFileExtension);
                part.ModelPreviewUrl = FirstNonEmpty(part.ModelPreviewUrl, status.GlbSignedUrl);

                if (part.Dimensions is null && status.Dimensions is not null)
                {
                    part.Dimensions = new ModelDimensionsDto
                    {
                        X = status.Dimensions.X,
                        Y = status.Dimensions.Y,
                        Z = status.Dimensions.Z
                    };
                }

                part.IsManifold ??= status.IsManifold;
            }
        }

        if (string.IsNullOrWhiteSpace(part.ThumbnailUrl))
            part.ThumbnailUrl = await GetSignedUrlIfPresentAsync(upload, part.ThumbnailSmallGcsPath, ct)
                ?? await GetSignedUrlIfPresentAsync(upload, part.ThumbnailLargeGcsPath, ct);

        if (string.IsNullOrWhiteSpace(part.ModelPreviewUrl))
            part.ModelPreviewUrl = await GetSignedUrlIfPresentAsync(upload, part.GlbStoragePath, ct);

        await EnrichAttachmentUrlsAsync(part.DrawingFiles, upload, ct);
        await EnrichAttachmentUrlsAsync(part.SupplementaryFiles, upload, ct);
    }

    private static async Task EnrichAttachmentUrlsAsync(
        IEnumerable<ProjectPartAttachmentDto> attachments,
        UploadServiceClient upload,
        CancellationToken ct)
    {
        foreach (var attachment in attachments)
        {
            if (string.IsNullOrWhiteSpace(attachment.SignedUrl))
                attachment.SignedUrl = await GetSignedUrlIfPresentAsync(upload, attachment.StoragePath, ct);
        }
    }

    private static async Task<string?> GetSignedUrlIfPresentAsync(
        UploadServiceClient upload,
        string? storagePath,
        CancellationToken ct)
    {
        return string.IsNullOrWhiteSpace(storagePath)
            ? null
            : await upload.GetDownloadUrlByPathAsync(storagePath, ct);
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static AddressResponse? SelectAddress(IEnumerable<AddressResponse> addresses, string type) =>
        addresses.FirstOrDefault(address => address.IsDefault && MatchesAddressType(address, type))
            ?? addresses.FirstOrDefault(address => MatchesAddressType(address, type));

    private static bool MatchesAddressType(AddressResponse address, string type) =>
        string.Equals(address.Type, type, StringComparison.OrdinalIgnoreCase);

    private static string? ResolveCustomerBranch(CustomerDetailDto detail, AddressResponse? billingAddress)
    {
        var isCorporateBilling = detail.CompanyId.HasValue
            || !string.IsNullOrWhiteSpace(detail.CompanyName)
            || string.Equals(billingAddress?.OwnerType, "Company", StringComparison.OrdinalIgnoreCase);

        return isCorporateBilling ? "Head Office" : null;
    }

    private static string? FormatAddress(AddressResponse? address)
    {
        if (address is null)
            return null;

        var parts = new[]
        {
            address.AddressLine1,
            address.AddressLine2,
            address.AddressLine3,
            address.District,
            address.City,
            address.StateProvince,
            address.PostalCode
        }
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value!.Trim());

        return string.Join(", ", parts);
    }

    private async Task CleanupDuplicateAsync(
        Guid duplicateProjectId,
        IReadOnlyCollection<Guid> copiedFileIds,
        UploadServiceClient upload,
        CancellationToken ct)
    {
        try
        {
            await client.DeleteProjectAsync(duplicateProjectId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to clean up duplicate project {DuplicateProjectId}", duplicateProjectId);
        }

        foreach (var fileId in copiedFileIds)
        {
            try
            {
                await upload.DeleteFileAsync(fileId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to clean up copied file {FileId}", fileId);
            }
        }
    }

    private static string BuildCopyTitle(string title)
    {
        return string.IsNullOrWhiteSpace(title) ? "Copy" : $"{title} (Copy)";
    }

    private static string BuildProjectUploadPath(Guid projectId, Guid customerId, string fileName)
    {
        var uniquePrefix = Guid.NewGuid().ToString("N")[..8];
        return $"customers/{customerId}/projects/{projectId}/{uniquePrefix}_{fileName}";
    }

    private static string BuildAttachmentPath(Guid projectId, Guid customerId, string folder, string fileName)
    {
        var uniquePrefix = Guid.NewGuid().ToString("N")[..8];
        return $"customers/{customerId}/projects/{projectId}/{folder}/{uniquePrefix}_{fileName}";
    }

    private static string BuildArtifactDestinationPath(
        string sourceFilePath,
        string destinationFilePath,
        string sourceArtifactPath,
        string artifactKey)
    {
        if (sourceArtifactPath.StartsWith(sourceFilePath, StringComparison.OrdinalIgnoreCase))
            return destinationFilePath + sourceArtifactPath[sourceFilePath.Length..];

        var extension = GetStoragePathExtension(sourceArtifactPath);
        var safeKey = string.Concat(artifactKey.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));
        return $"{destinationFilePath}_{safeKey}{extension}";
    }

    private static string GetStoragePathExtension(string storagePath)
    {
        var fileName = storagePath.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? storagePath;
        var extension = Path.GetExtension(fileName);
        return string.IsNullOrWhiteSpace(extension) ? string.Empty : extension;
    }

    private static string NormalizeProductionTechnology(string processType)
    {
        if (string.IsNullOrWhiteSpace(processType))
            return string.Empty;

        var normalized = processType.Trim()
            .Replace("-", "_", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal)
            .ToUpperInvariant();

        return normalized switch
        {
            "CNC" or "CNC_MILL" or "CNC_MILLING" or "MILLING" => "CNC_MILL",
            "CNC_TURN" or "CNC_TURNING" or "TURNING" or "LATHE" => "CNC_TURN",
            _ => normalized,
        };
    }

    private static string? MapProcessToEquipmentCategory(string processType) => processType switch
    {
        "FDM" => "FdmPrinter",
        "SLA_DLP" => "SlaPrinter",
        "SLS" => "SlsPrinter",
        "MJF" => "MjfPrinter",
        "MJ" => "MjPrinter",
        "BJ" => "BjPrinter",
        "DMLS" => "DmlsPrinter",
        "CNC_MILL" => "CncMachine",
        "CNC_TURN" => "CncMachine",
        _ => null,
    };
}
