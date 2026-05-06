using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using System.Text.Json;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Typed HTTP client for interacting with the ProjectService downstream API.
/// </summary>
/// <param name="httpClient">The HTTP client configured by <c>AddBffServiceClient</c>.</param>
public class ProjectServiceClient(HttpClient httpClient)
{
    // ── Query endpoints ──────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves a paged list of projects with optional filtering.
    /// </summary>
    /// <param name="status">Optional status filter (e.g. "Configuring").</param>
    /// <param name="search">Optional text search (project number, title, or customer name).</param>
    /// <param name="customerId">Optional customer ID filter.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paged response of project summaries.</returns>
    public async Task<PagedResponse<ProjectSummaryDto>> GetProjectsAsync(
        string? status = null, string? search = null, Guid? customerId = null,
        int page = 1, int pageSize = 20,
        CancellationToken ct = default)
    {
        var url = $"/project/v1/projects?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(status)) url += $"&status={Uri.EscapeDataString(status)}";
        if (!string.IsNullOrEmpty(search)) url += $"&query={Uri.EscapeDataString(search)}";
        if (customerId.HasValue) url += $"&customerId={customerId.Value}";

        var response = await httpClient.GetFromJsonAsync<ProjectServicePagedProjectResponse>(url, ct);
        return response?.ToPagedResponse() ?? new PagedResponse<ProjectSummaryDto>();
    }

    /// <summary>
    /// Retrieves full detail for a single project by ID.
    /// </summary>
    /// <param name="id">The project GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The project detail DTO, or <c>null</c> if not found.</returns>
    public async Task<ProjectDetailDto?> GetProjectByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/project/v1/projects/{id}", ct);
        if (!response.IsSuccessStatusCode) return null;
        var project = await response.Content.ReadFromJsonAsync<ProjectServiceProjectDetailResponse>(cancellationToken: ct);
        return project?.ToIntranetDto();
    }

    /// <summary>
    /// Returns project statistics for the dashboard action items panel.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Aggregated project counts by status.</returns>
    public async Task<ProjectStatsDto?> GetProjectStatsAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("/project/v1/projects/stats", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProjectStatsDto>(cancellationToken: ct);
    }

    // ── Mutation endpoints ───────────────────────────────────────────────────

    /// <summary>
    /// Creates a new project.
    /// </summary>
    public async Task<(ProjectDetailDto? Result, string? ErrorContent, int StatusCode)> CreateProjectAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/project/v1/projects", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                return (null, errorContent, (int)response.StatusCode);
            }
            var result = await response.Content.ReadFromJsonAsync<ProjectServiceProjectDetailResponse>(cancellationToken: ct);
            return (result?.ToIntranetDto(), null, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            return (null, $"Exception calling ProjectService: {ex.Message}", 0);
        }
    }

    /// <summary>
    /// Updates an existing project's metadata (title, description, notes, validity).
    /// </summary>
    public async Task<HttpResponseMessage> UpdateProjectAsync(Guid id, object request, CancellationToken ct = default)
        => await httpClient.PutAsJsonAsync($"/project/v1/projects/{id}", request, ct);

    /// <summary>
    /// Deletes a project (only permitted when in Draft status).
    /// </summary>
    public async Task<HttpResponseMessage> DeleteProjectAsync(Guid id, CancellationToken ct = default)
        => await httpClient.DeleteAsync($"/project/v1/projects/{id}", ct);

    /// <summary>
    /// Adds an internal note to a project.
    /// </summary>
    public async Task<(ProjectNoteDto? Result, string? ErrorContent, int StatusCode)> AddNoteAsync(
        Guid projectId,
        AddProjectNoteRequest request,
        CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/project/v1/projects/{projectId}/notes", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            return (null, errorContent, (int)response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<ProjectNoteDto>(cancellationToken: ct);
        return (result, null, (int)response.StatusCode);
    }

    // ── Part endpoints ───────────────────────────────────────────────────────

    /// <summary>
    /// Adds a new part to an existing project.
    /// Maps the Intranet DTO to the ProjectService DTO shape (field name differences).
    /// </summary>
    public async Task<(ProjectPartDto? Result, string? ErrorContent, int StatusCode)> AddPartAsync(Guid projectId, AddProjectPartRequest request, CancellationToken ct = default)
    {
        var payload = new
        {
            request.FileId,
            request.FileReference,
            request.ThumbnailSmallGcsPath,
            request.ThumbnailLargeGcsPath,
            request.GlbStoragePath,
            request.OverlayPaths,
            request.FileName,
            ProcessType = MapManufacturingProcess(request.ProcessType),
            request.MaterialId,
            request.MaterialName,
            request.MaterialCode,
            request.Quantity,
            FinishType = request.Finish,
            request.Color,
            request.Tolerance,
            CustomNotes = request.PartNotes,
            request.RoughnessCode,
            MarkingType = request.MarkingType.ToString(),
            request.MarkingText,
            request.DfmAcknowledged,
            request.HasThreadedHoles,
            request.ThreadedHoleSpec,
            request.ThreadedHoleCount,
            request.HasInserts,
            InsertType = request.InsertType.ToString(),
            request.InsertCount,
            request.BagAndTag,
            InspectionLevel = request.InspectionLevel.ToString(),
            request.Certificates,
            request.DrawingFiles,
            request.SupplementaryFiles,
            request.ProcessConfig,
            request.BodyCount,
            request.BodiesJson,
            request.SelectedBodyIndex,
            request.VolumeCm3,
            request.SupportVolumeCm3,
            request.SurfaceAreaCm2,
            request.BoundingBoxX,
            request.BoundingBoxY,
            request.BoundingBoxZ,
            request.IsManifold,
        };

        var response = await httpClient.PostAsJsonAsync($"/project/v1/projects/{projectId}/parts", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            return (null, errorContent, (int)response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<ProjectServiceProjectPartResponse>(cancellationToken: ct);
        return (result?.ToIntranetDto(), null, (int)response.StatusCode);
    }

    /// <summary>
    /// Updates the configuration of an existing part.
    /// Maps the Intranet DTO to the ProjectService DTO shape (field name differences).
    /// </summary>
    public async Task<HttpResponseMessage> UpdatePartAsync(Guid projectId, Guid partId, UpdateProjectPartRequest request, CancellationToken ct = default)
    {
        var payload = new
        {
            ProcessType = MapManufacturingProcess(request.ProcessType),
            request.MaterialId,
            request.MaterialName,
            request.MaterialCode,
            request.Quantity,
            FinishType = request.Finish,
            request.Color,
            request.Tolerance,
            CustomNotes = request.PartNotes,
            request.RoughnessCode,
            MarkingType = request.MarkingType.ToString(),
            request.MarkingText,
            request.DfmAcknowledged,
            request.HasThreadedHoles,
            request.ThreadedHoleSpec,
            request.ThreadedHoleCount,
            request.HasInserts,
            InsertType = request.InsertType.ToString(),
            request.InsertCount,
            request.BagAndTag,
            InspectionLevel = request.InspectionLevel.ToString(),
            request.Certificates,
            request.DrawingFiles,
            request.SupplementaryFiles,
            request.ProcessConfig,
            request.ThumbnailSmallGcsPath,
            request.ThumbnailLargeGcsPath,
            request.GlbStoragePath,
            request.OverlayPaths,
            request.BodyCount,
            request.BodiesJson,
            request.SelectedBodyIndex,
        };

        return await httpClient.PutAsJsonAsync($"/project/v1/projects/{projectId}/parts/{partId}", payload, ct);
    }

    private static int? MapManufacturingProcess(string? processType)
    {
        if (string.IsNullOrWhiteSpace(processType))
            return null;

        var normalized = processType.Trim()
            .Replace("-", "_", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal)
            .ToUpperInvariant();

        return normalized switch
        {
            "FDM" => 1,
            "SLA" or "DLP" or "SLA_DLP" => 2,
            "SLS" => 3,
            "CNC" or "CNC_MILL" or "CNC_MILLING" or "MILLING" => 10,
            "CNC_TURN" or "CNC_TURNING" or "TURNING" or "LATHE" => 11,
            "CNC_5AXIS" or "CNC_5_AXIS" or "5_AXIS" => 12,
            "SHEET_METAL" or "SHEETMETAL" or "SHEET_METAL_CUTTING" => 20,
            "SHEET_METAL_BENDING" => 21,
            "SHEET_METAL_WELDING" => 22,
            "INJECTION" or "INJECTION_MOLDING" or "INJECTIONMOULDING" => 30,
            "3D_SCANNING" or "THREEDSCANNING" => 40,
            "DESIGN" => 50,
            "ASSEMBLY" or "FINISHING" => 60,
            _ => null,
        };
    }

    /// <summary>
    /// Removes a part from a project.
    /// </summary>
    public async Task<HttpResponseMessage> DeletePartAsync(Guid projectId, Guid partId, CancellationToken ct = default)
        => await httpClient.DeleteAsync($"/project/v1/projects/{projectId}/parts/{partId}", ct);

    // ── Pricing endpoints ────────────────────────────────────────────────────

    /// <summary>
    /// Requests AI price estimation for a specific part. Returns the detailed price breakdown.
    /// </summary>
    public async Task<ProjectPriceBreakdownDto?> GetPartPriceAsync(Guid projectId, Guid partId, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"/project/v1/projects/{projectId}/parts/{partId}/price", null, ct);
        if (!response.IsSuccessStatusCode) return null;
        var part = await response.Content.ReadFromJsonAsync<ProjectServiceProjectPartResponse>(cancellationToken: ct);
        if (part is null) return null;

        return new ProjectPriceBreakdownDto
        {
            TotalPerUnit = part.EffectiveUnitPrice ?? part.ConfirmedUnitPrice ?? part.AiSuggestedPrice ?? 0m
        };
    }

    /// <summary>
    /// Confirms or overrides the price for a specific part.
    /// </summary>
    public async Task<HttpResponseMessage> ConfirmPartPriceAsync(Guid projectId, Guid partId, ConfirmPartPriceRequest request, CancellationToken ct = default)
        => await httpClient.PostAsJsonAsync($"/project/v1/projects/{projectId}/parts/{partId}/confirm-price", request, ct);

    // ── Quotation lifecycle endpoints ────────────────────────────────────────

    /// <summary>
    /// Generates a quotation PDF for the project. Only allowed when all parts have confirmed prices.
    /// </summary>
    public async Task<HttpResponseMessage> GenerateQuotationAsync(
        Guid projectId,
        GenerateQuotationRequest? request = null,
        CancellationToken ct = default)
        => await httpClient.PostAsJsonAsync(
            $"/project/v1/projects/{projectId}/generate-quotation",
            request ?? new GenerateQuotationRequest(),
            ct);

    /// <summary>
    /// Marks a project's quotation as accepted by the customer.
    /// </summary>
    public async Task<HttpResponseMessage> AcceptQuotationAsync(Guid projectId, CancellationToken ct = default)
        => await httpClient.PostAsync($"/project/v1/projects/{projectId}/accept-quotation", null, ct);

    // ── Dashboard helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Returns the count of projects currently in Configuring status (waiting for pricing).
    /// Used by the dashboard action items panel.
    /// </summary>
    public async Task<int> GetConfiguringCountAsync(CancellationToken ct = default)
    {
        var stats = await GetProjectStatsAsync(ct);
        return stats?.ConfiguringCount ?? 0;
    }

    private sealed class ProjectServicePagedProjectResponse
    {
        public List<ProjectServiceProjectSummaryResponse> Data { get; set; } = [];

        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }

        public int TotalCount { get; set; }

        public int PageSize { get; set; }

        public PagedResponse<ProjectSummaryDto> ToPagedResponse() => new()
        {
            Data = Data.Select(item => item.ToIntranetDto()).ToList(),
            Meta = new PaginationMeta
            {
                CurrentPage = CurrentPage,
                TotalPages = TotalPages,
                TotalCount = TotalCount,
                TotalItems = TotalCount,
                PageSize = PageSize
            }
        };
    }

    private sealed class ProjectServiceProjectSummaryResponse
    {
        public Guid Id { get; set; }

        public string ProjectNumber { get; set; } = string.Empty;

        public Guid CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int PartsCount { get; set; }

        public decimal TotalEstimatedPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime CreatedAt { get; set; }

        public ProjectSummaryDto ToIntranetDto() => new()
        {
            Id = Id,
            ProjectNumber = ProjectNumber,
            CustomerId = CustomerId,
            CustomerName = CustomerName,
            Title = Title,
            Status = Status,
            PartsCount = PartsCount,
            TotalPrice = ResolveTotalPrice(TotalEstimatedPrice, TotalPrice),
            CreatedAt = CreatedAt
        };
    }

    private sealed class ProjectServiceProjectDetailResponse
    {
        public Guid Id { get; set; }

        public string ProjectNumber { get; set; } = string.Empty;

        public Guid CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Status { get; set; } = string.Empty;

        public Guid? QuotationId { get; set; }

        public string? QuotationNumber { get; set; }

        public string? QuotationStatus { get; set; }

        public decimal TotalEstimatedPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public string Currency { get; set; } = "THB";

        public DateTime? ValidUntil { get; set; }

        public string? CreatedBy { get; set; }

        public string? CreatedByName { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public List<ProjectServiceProjectPartResponse> Parts { get; set; } = [];

        public List<ProjectNoteDto> Notes { get; set; } = [];

        public ProjectDetailDto ToIntranetDto()
        {
            var parts = Parts.Select(part => part.ToIntranetDto()).ToList();
            var total = ResolveTotalPrice(TotalEstimatedPrice, TotalPrice);
            if (total == 0m)
                total = parts.Sum(part => ResolvePartUnitPrice(part) * Math.Max(part.Quantity, 0));

            return new ProjectDetailDto
            {
                Id = Id,
                ProjectNumber = ProjectNumber,
                CustomerId = CustomerId,
                CustomerName = CustomerName,
                Title = Title,
                Description = Description,
                Status = Status,
                Notes = Notes,
                ValidUntil = ValidUntil,
                TotalPrice = total,
                Currency = string.IsNullOrWhiteSpace(Currency) ? "THB" : Currency,
                QuotationId = QuotationId,
                QuotationNumber = QuotationNumber,
                QuotationStatus = ResolveQuotationStatus(Status, QuotationStatus, QuotationId),
                CreatedBy = CreatedBy,
                CreatedByName = CreatedByName,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                Parts = parts,
                Timeline = BuildTimeline(Status, CreatedAt, UpdatedAt, QuotationId)
            };
        }
    }

    private sealed class ProjectServiceProjectPartResponse
    {
        public Guid Id { get; set; }

        public Guid? FileId { get; set; }

        public string? FileReference { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string? ThumbnailUrl { get; set; }

        public string? ModelPreviewUrl { get; set; }

        public string? ThumbnailSmallGcsPath { get; set; }

        public string? ThumbnailLargeGcsPath { get; set; }

        public string? GlbStoragePath { get; set; }

        public Dictionary<string, string> OverlayPaths { get; set; } = [];

        public string? ProcessType { get; set; }

        public Guid? MaterialId { get; set; }

        public string? MaterialName { get; set; }

        public string? MaterialCode { get; set; }

        public int Quantity { get; set; } = 1;

        public string? FinishType { get; set; }

        public string? Finish { get; set; }

        public string? Color { get; set; }

        public string? Tolerance { get; set; }

        public string? CustomNotes { get; set; }

        public string? RoughnessCode { get; set; }

        public JsonElement? MarkingType { get; set; }

        public string? MarkingText { get; set; }

        public bool DfmAcknowledged { get; set; }

        public bool HasThreadedHoles { get; set; }

        public string? ThreadedHoleSpec { get; set; }

        public int ThreadedHoleCount { get; set; }

        public bool HasInserts { get; set; }

        public JsonElement? InsertType { get; set; }

        public int InsertCount { get; set; }

        public bool BagAndTag { get; set; } = true;

        public JsonElement? InspectionLevel { get; set; }

        public List<string> Certificates { get; set; } = [];

        public List<ProjectPartAttachmentDto> DrawingFiles { get; set; } = [];

        public List<ProjectPartAttachmentDto> SupplementaryFiles { get; set; } = [];

        public Dictionary<string, string> ProcessConfig { get; set; } = [];

        public int? BodyCount { get; set; }

        public string? BodiesJson { get; set; }

        public int? SelectedBodyIndex { get; set; }

        public decimal? AiSuggestedPrice { get; set; }

        public decimal? EstimatedPrice { get; set; }

        public decimal? ConfirmedUnitPrice { get; set; }

        public decimal? ConfirmedPrice { get; set; }

        public decimal? EffectiveUnitPrice { get; set; }

        public string? PriceOverrideReason { get; set; }

        public string? OverrideReason { get; set; }

        public string Status { get; set; } = "Configuring";

        public decimal? BoundingBoxX { get; set; }

        public decimal? BoundingBoxY { get; set; }

        public decimal? BoundingBoxZ { get; set; }

        public bool? IsManifold { get; set; }

        public Guid? JobId { get; set; }

        public string? JobStatus { get; set; }

        public int? JobProgressPercent { get; set; }

        public string? MachineName { get; set; }

        public ProjectPartDto ToIntranetDto()
        {
            var estimatedPrice = EstimatedPrice ?? AiSuggestedPrice;
            var confirmedPrice = ConfirmedPrice ?? ConfirmedUnitPrice;
            var previewUrl = FirstNonEmpty(ModelPreviewUrl, ThumbnailUrl);

            return new ProjectPartDto
            {
                Id = Id,
                FileId = FileId ?? Guid.Empty,
                FileReference = FileReference,
                FileName = FileName,
                ProcessType = ProcessType,
                MaterialId = MaterialId,
                MaterialName = FirstNonEmpty(MaterialName, MaterialCode),
                MaterialCode = MaterialCode,
                Quantity = Quantity,
                Finish = FirstNonEmpty(Finish, FinishType),
                Color = Color,
                Tolerance = Tolerance,
                PartNotes = CustomNotes,
                EstimatedPrice = estimatedPrice,
                ConfirmedPrice = confirmedPrice,
                AiSuggestedPrice = AiSuggestedPrice,
                ConfirmedUnitPrice = ConfirmedUnitPrice,
                OverrideReason = FirstNonEmpty(OverrideReason, PriceOverrideReason),
                Status = Status,
                ModelPreviewUrl = previewUrl,
                ThumbnailUrl = ThumbnailUrl,
                ThumbnailSmallGcsPath = ThumbnailSmallGcsPath,
                ThumbnailLargeGcsPath = ThumbnailLargeGcsPath,
                GlbStoragePath = GlbStoragePath,
                OverlayPaths = new Dictionary<string, string>(OverlayPaths),
                Dimensions = BoundingBoxX.HasValue || BoundingBoxY.HasValue || BoundingBoxZ.HasValue
                    ? new ModelDimensionsDto
                    {
                        X = (double)(BoundingBoxX ?? 0m),
                        Y = (double)(BoundingBoxY ?? 0m),
                        Z = (double)(BoundingBoxZ ?? 0m)
                    }
                    : null,
                IsManifold = IsManifold,
                RoughnessCode = RoughnessCode,
                MarkingType = ParseEnumOrDefault(MarkingType, PartMarkingType.None),
                MarkingText = MarkingText,
                DfmAcknowledged = DfmAcknowledged,
                HasThreadedHoles = HasThreadedHoles,
                ThreadedHoleSpec = ThreadedHoleSpec,
                ThreadedHoleCount = ThreadedHoleCount,
                HasInserts = HasInserts,
                InsertType = ParseEnumOrDefault(InsertType, Maliev.Intranet.Shared.InsertType.None),
                InsertCount = InsertCount,
                BagAndTag = BagAndTag,
                InspectionLevel = ParseEnumOrDefault(InspectionLevel, Maliev.Intranet.Shared.InspectionLevel.Standard),
                Certificates = [.. Certificates],
                DrawingFiles = [.. DrawingFiles],
                SupplementaryFiles = [.. SupplementaryFiles],
                ProcessConfig = new Dictionary<string, string>(ProcessConfig),
                BodyCount = BodyCount,
                BodiesJson = BodiesJson,
                SelectedBodyIndex = SelectedBodyIndex,
                JobId = JobId,
                JobStatus = JobStatus,
                JobProgressPercent = JobProgressPercent,
                MachineName = MachineName
            };
        }
    }

    private static decimal ResolveTotalPrice(decimal preferred, decimal fallback) =>
        preferred != 0m ? preferred : fallback;

    private static decimal ResolvePartUnitPrice(ProjectPartDto part) =>
        part.ConfirmedPrice ?? part.ConfirmedUnitPrice ?? part.EstimatedPrice ?? part.AiSuggestedPrice ?? 0m;

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static TEnum ParseEnumOrDefault<TEnum>(string? value, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static TEnum ParseEnumOrDefault<TEnum>(JsonElement? value, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        if (value is null)
            return defaultValue;

        return value.Value.ValueKind switch
        {
            JsonValueKind.String => ParseEnumOrDefault(value.Value.GetString(), defaultValue),
            JsonValueKind.Number when value.Value.TryGetInt32(out var numeric) && Enum.IsDefined(typeof(TEnum), numeric) =>
                (TEnum)Enum.ToObject(typeof(TEnum), numeric),
            _ => defaultValue
        };
    }

    private static string? ResolveQuotationStatus(string status, string? explicitStatus, Guid? quotationId)
    {
        if (!string.IsNullOrWhiteSpace(explicitStatus))
            return explicitStatus;

        if (quotationId is null)
            return null;

        return status.Trim().ToLowerInvariant() switch
        {
            "quotationgenerated" => "Generated",
            "quotationsent" or "quoted" => "Sent",
            "quotationaccepted" => "Accepted",
            _ => "Generated"
        };
    }

    private static List<ProjectTimelineEventDto> BuildTimeline(
        string status,
        DateTime createdAt,
        DateTime? updatedAt,
        Guid? quotationId)
    {
        var timeline = new List<ProjectTimelineEventDto>();

        if (createdAt != default)
        {
            timeline.Add(new ProjectTimelineEventDto
            {
                Label = "Project created",
                Timestamp = createdAt,
                Icon = "add_circle",
                Completed = true
            });
        }

        if (updatedAt is { } updated && updated != default && updated > createdAt.AddSeconds(1))
        {
            timeline.Add(new ProjectTimelineEventDto
            {
                Label = "Project updated",
                Timestamp = updated,
                Icon = "edit",
                Completed = true
            });
        }

        if (quotationId.HasValue)
        {
            timeline.Add(new ProjectTimelineEventDto
            {
                Label = ResolveQuotationStatus(status, null, quotationId) switch
                {
                    "Accepted" => "Quotation accepted",
                    "Sent" => "Quotation sent",
                    _ => "Quotation generated"
                },
                Timestamp = updatedAt ?? createdAt,
                Icon = "request_quote",
                Completed = true
            });
        }

        return timeline;
    }
}
