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
    CustomerServiceClient? customerClient = null) : ControllerBase
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
        if (result is not null && uploadClient is not null)
            await EnrichProjectDetailArtifactsAsync(result, uploadClient, analysisStatusService, ct);

        if (result is not null && customerClient is not null)
            await EnrichProjectCustomerAsync(result, customerClient, ct);

        return result != null ? Ok(result) : NotFound();
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
            Currency = string.IsNullOrWhiteSpace(source.Currency) ? "THB" : source.Currency
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
        var response = await client.GenerateQuotationAsync(id, request ?? new GenerateQuotationRequest(), ct);
        if (response.IsSuccessStatusCode)
            return NoContent();

        var errorContent = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(errorContent))
            return StatusCode((int)response.StatusCode);

        return StatusCode((int)response.StatusCode, errorContent);
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

        var queueAhead = 0;
        try
        {
            var queueDepths = await jobClient.GetQueueDepthAsync(processType, ct);
            queueDepths?.TryGetValue(processType, out queueAhead);
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

        var category = MapProcessToEquipmentCategory(processType);
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
                        JobReference: s.JobId.ToString("N")[..8].ToUpperInvariant(),
                        Status: s.Status,
                        JobId: s.JobId,
                        MachineName: best.Machine.Name,
                        SetupTimeMinutes: s.SetupMinutes,
                        PrintTimeMinutes: s.PrintMinutes
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
        var setupDays = processType.StartsWith("CNC", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
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
            var defaultSetupMin = processType.StartsWith("CNC", StringComparison.OrdinalIgnoreCase) ? 60
                : processType is "SLA_DLP" or "SLA" ? 30
                : 15;
            proposedSlotEnd = proposedSlotStart.Value.AddMinutes(defaultSetupMin + 30);
        }

        return Ok(new ProductionRoutingDto(
            MachineId: machine?.Id ?? Guid.Empty,
            MachineCode: machine?.AssetCode ?? "TBD",
            MachineName: machine?.Name ?? "Unassigned",
            QueueAhead: queueAhead,
            EstimatedStartDate: estimatedStart,
            ScheduleItems: scheduleItems,
            ProposedSlotStart: proposedSlotStart,
            ProposedSlotEnd: proposedSlotEnd));
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

            var shippingAddress = SelectAddress(detail.Addresses, "Shipping")
                ?? SelectAddress(detail.Addresses, "Delivery")
                ?? SelectAddress(detail.Addresses, "Billing")
                ?? detail.Addresses.FirstOrDefault();
            var billingAddress = SelectAddress(detail.Addresses, "Billing") ?? detail.CompanyBillingAddress;

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
