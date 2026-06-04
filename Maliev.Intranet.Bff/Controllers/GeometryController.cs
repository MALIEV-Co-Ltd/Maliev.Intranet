using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for on-demand geometry analysis operations.
/// Proxies requests to GeometryService for lazy DFM analysis when user selects a manufacturing process.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/geometry")]
public class GeometryController(
    GeometryServiceClient geometryServiceClient,
    UploadServiceClient uploadServiceClient,
    BffMetrics bffMetrics,
    ILogger<GeometryController> logger) : ControllerBase
{
    /// <summary>
    /// Proxies the browser advisory geometry runtime manifest from GeometryService.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The runtime manifest JSON.</returns>
    [RequirePermission(MalievPermissions.Project.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("runtime/manifest")]
    public async Task<IActionResult> GetRuntimeManifest(CancellationToken ct = default)
    {
        using var response = await geometryServiceClient.GetRuntimeManifestAsync(ct);
        return await ProxyRuntimeResponseAsync(
            response,
            "application/json; charset=utf-8",
            ct);
    }

    /// <summary>
    /// Proxies a content-hashed browser advisory geometry runtime asset from GeometryService.
    /// </summary>
    /// <param name="assetName">The content-hashed runtime asset file name.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The runtime asset content.</returns>
    [RequirePermission(MalievPermissions.Project.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("runtime/assets/{assetName}")]
    public async Task<IActionResult> GetRuntimeAsset(
        string assetName,
        CancellationToken ct = default)
    {
        if (assetName.Contains('/', StringComparison.Ordinal) ||
            assetName.Contains('\\', StringComparison.Ordinal))
        {
            return NotFound();
        }

        using var response = await geometryServiceClient.GetRuntimeAssetAsync(assetName, ct);
        return await ProxyRuntimeAssetResponseAsync(
            response,
            "text/javascript; charset=utf-8",
            ct);
    }

    /// <summary>
    /// Records that the browser-first local DFM runtime completed on the client.
    /// </summary>
    /// <param name="request">The browser runtime telemetry payload.</param>
    /// <returns>A no-content acknowledgement.</returns>
    [RequirePermission(MalievPermissions.Project.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("runtime/telemetry")]
    public IActionResult RecordRuntimeTelemetry([FromBody] BrowserGeometryRuntimeTelemetryRequest request)
    {
        if (request.IsStarted)
        {
            bffMetrics.RecordBrowserDfmRuntimeStart(
                request.ProcessCode,
                request.Authority,
                request.ExecutionMode,
                request.InputByteCount,
                request.InputTriangleCount);

            logger.LogInformation(
                "Browser-first intranet DFM local runtime started for process {ProcessCode}; inputBytes={InputByteCount}; inputTriangles={InputTriangleCount}",
                request.ProcessCode,
                request.InputByteCount,
                request.InputTriangleCount);

            return NoContent();
        }

        if (request.IsTerminalUnavailable)
        {
            bffMetrics.RecordBrowserDfmRuntimeTerminalAttempt(
                request.ProcessCode,
                request.Reason,
                request.Authority,
                request.ExecutionMode);

            logger.LogInformation(
                "Browser-first intranet DFM local runtime ended before report for process {ProcessCode}; reason={Reason}",
                request.ProcessCode,
                request.Reason);

            return NoContent();
        }

        bffMetrics.RecordBrowserDfmRuntimeCompletion(
            request.ProcessCode,
            request.Accepted,
            request.Authority,
            request.ExecutionMode);

        logger.LogInformation(
            "Browser-first intranet DFM completed locally for process {ProcessCode}; accepted={Accepted}; issues={IssueCount}; warnings={WarningCount}; faces={FaceCount}",
            request.ProcessCode,
            request.Accepted,
            request.IssueCount,
            request.WarningCount,
            request.FaceCount);

        return NoContent();
    }

    /// <summary>
    /// Runs DFM analysis for a specific manufacturing process on an uploaded file.
    /// This is the on-demand endpoint that only runs when the user selects a process.
    /// Supports cache-miss recovery by re-downloading from GCS when needed.
    /// </summary>
    /// <param name="uploadId">The upload identifier from the file upload.</param>
    /// <param name="processCode">Manufacturing process code (e.g., "FDM", "SLA", "CNC_MILL").</param>
    /// <param name="request">Request body with storage_path and optional download_url.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>DFM analysis response with issues found for the selected process.</returns>
    [RequirePermission(MalievPermissions.Project.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{uploadId}/dfm/{processCode}")]
    public async Task<ActionResult<DfmAnalysisResponse>> AnalyzeForProcess(
        string uploadId,
        string processCode,
        [FromBody] GeometryAnalysisRequest request,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "DFM analysis requested for upload {UploadId}, process {ProcessCode}",
            uploadId, processCode);

        request ??= new();

        var clientStoragePath = request.StoragePath;

        // Resolve the authoritative current storage path from UploadService by uploadId.
        // The client's StoragePath may be stale (temp lifecycle expiry, post-migration path change,
        // or geometry-service restart clearing the in-memory cache).
        var currentPath = await uploadServiceClient.GetStoragePathAsync(uploadId, ct);
        if (string.IsNullOrEmpty(currentPath))
        {
            if (string.IsNullOrWhiteSpace(clientStoragePath))
            {
                logger.LogWarning(
                    "Upload {UploadId} not found in UploadService and no storage path fallback was provided",
                    uploadId);
                return StatusCode(410, BuildFileMissingResponse(uploadId, processCode));
            }

            logger.LogWarning(
                "Upload {UploadId} not found in UploadService; falling back to client storage path {StoragePath}",
                uploadId,
                clientStoragePath);
            currentPath = clientStoragePath;
        }

        request.StoragePath = currentPath;

        var (resolvedPath, signedUrl) = await ResolveSignedUrlForCurrentUploadPathAsync(uploadId, currentPath, ct);
        if (string.IsNullOrEmpty(signedUrl))
        {
            logger.LogWarning(
                "UploadService could not produce a signed URL for upload {UploadId} at path {StoragePath}",
                uploadId,
                resolvedPath);
            return StatusCode(410, BuildFileMissingResponse(uploadId, processCode));
        }

        request.StoragePath = resolvedPath;
        request.DownloadUrl = signedUrl;

        bffMetrics.RecordServerDfmProxyRequest(processCode, "browser_local_miss");
        var result = await geometryServiceClient.AnalyzeForProcessAsync(uploadId, processCode, request, ct);

        if (result == null)
        {
            return StatusCode(500, new DfmAnalysisResponse
            {
                UploadId = uploadId,
                ProcessCode = processCode,
                Status = "error",
                DfmReport = new DfmReport
                {
                    ReportType = processCode,
                    Issues = new List<DfmIssue>
                    {
                        new DfmIssue
                        {
                            Category = "system",
                            Severity = "error",
                            Title = "Service Error",
                            Description = "GeometryService returned null response",
                            Value = 0,
                            Threshold = 0
                        }
                    }
                }
            });
        }

        // Return appropriate status code based on response status
        return result.Status switch
        {
            "analysis_complete" => Ok(result),
            "file_missing" => StatusCode(410, result),
            "timeout" => StatusCode(504, result),
            "error" when result.DfmReport?.Issues.Any(i => i.Severity == "error") == true =>
                StatusCode(500, result),
            "error" => Ok(result),
            _ => Ok(result)
        };
    }

    private static DfmAnalysisResponse BuildFileMissingResponse(string uploadId, string processCode) =>
        new()
        {
            UploadId = uploadId,
            ProcessCode = processCode,
            Status = "file_missing",
            DfmReport = new DfmReport
            {
                ReportType = processCode,
                Issues =
                [
                    new DfmIssue
                    {
                        Category = "system",
                        Severity = "error",
                        Title = "File no longer in storage",
                        Description = "The uploaded CAD file is no longer available. Please re-upload the file to run DFM analysis.",
                        Value = 0,
                        Threshold = 0
                    }
                ]
            }
        };

    private async Task<(string StoragePath, string? SignedUrl)> ResolveSignedUrlForCurrentUploadPathAsync(
        string uploadId,
        string currentPath,
        CancellationToken ct)
    {
        var signedUrl = await TryGetDownloadUrlByPathAsync(currentPath, ct);
        if (!string.IsNullOrEmpty(signedUrl))
        {
            logger.LogDebug("Generated signed URL for storage path {StoragePath}", currentPath);
            return (currentPath, signedUrl);
        }

        var refreshedPath = await uploadServiceClient.GetStoragePathAsync(uploadId, ct);
        if (string.IsNullOrEmpty(refreshedPath))
        {
            return (currentPath, null);
        }

        if (!string.Equals(refreshedPath, currentPath, StringComparison.Ordinal))
        {
            logger.LogInformation(
                "Upload {UploadId} storage path changed while resolving DFM signed URL: {OldPath} -> {NewPath}",
                uploadId,
                currentPath,
                refreshedPath);
            currentPath = refreshedPath;
        }

        signedUrl = await TryGetDownloadUrlByPathAsync(currentPath, ct);
        if (!string.IsNullOrEmpty(signedUrl))
        {
            logger.LogDebug("Generated signed URL for refreshed storage path {StoragePath}", currentPath);
        }

        return (currentPath, signedUrl);
    }

    private async Task<string?> TryGetDownloadUrlByPathAsync(string storagePath, CancellationToken ct)
    {
        try
        {
            return await uploadServiceClient.GetDownloadUrlByPathAsync(storagePath, ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(
                "Signed URL generation timed out for storage path {StoragePath}; will retry after re-resolving upload metadata",
                storagePath);
            return null;
        }
    }

    private async Task<ContentResult> ProxyRuntimeResponseAsync(
        HttpResponseMessage response,
        string fallbackContentType,
        CancellationToken ct)
    {
        var content = await response.Content.ReadAsStringAsync(ct);
        var cacheControl = response.Headers.CacheControl?.ToString();
        if (!string.IsNullOrWhiteSpace(cacheControl))
        {
            Response.Headers.CacheControl = cacheControl;
        }

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = response.Content.Headers.ContentType?.ToString()
                ?? fallbackContentType
        };
    }

    private async Task<IActionResult> ProxyRuntimeAssetResponseAsync(
        HttpResponseMessage response,
        string fallbackContentType,
        CancellationToken ct)
    {
        var cacheControl = response.Headers.CacheControl?.ToString();
        if (!string.IsNullOrWhiteSpace(cacheControl))
        {
            Response.Headers.CacheControl = cacheControl;
        }

        var contentType = response.Content.Headers.ContentType?.ToString()
            ?? fallbackContentType;
        if (!response.IsSuccessStatusCode)
        {
            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = await response.Content.ReadAsStringAsync(ct),
                ContentType = contentType
            };
        }

        return new FileContentResult(
            await response.Content.ReadAsByteArrayAsync(ct),
            contentType);
    }
}

/// <summary>
/// Browser-first local DFM runtime telemetry emitted by the Intranet viewer.
/// </summary>
public sealed class BrowserGeometryRuntimeTelemetryRequest
{
    /// <summary>The manufacturing process code analyzed by the browser runtime.</summary>
    public string? ProcessCode { get; set; }

    /// <summary>The browser runtime package version.</summary>
    public string? RuntimeVersion { get; set; }

    /// <summary>The browser DFM algorithm version.</summary>
    public string? AlgorithmVersion { get; set; }

    /// <summary>The runtime authority marker.</summary>
    public string? Authority { get; set; }

    /// <summary>The runtime execution mode marker.</summary>
    public string? ExecutionMode { get; set; }

    /// <summary>Telemetry event status, for example <c>started</c>, <c>complete</c>, or <c>unavailable</c>.</summary>
    public string? Status { get; set; }

    /// <summary>Low-cardinality reason why a local attempt ended before producing a report.</summary>
    public string? Reason { get; set; }

    /// <summary>Whether Blazor accepted and applied the local DFM result.</summary>
    public bool Accepted { get; set; }

    /// <summary>The number of local DFM issues produced by the browser runtime.</summary>
    public int? IssueCount { get; set; }

    /// <summary>The number of warning-or-higher local DFM issues.</summary>
    public int? WarningCount { get; set; }

    /// <summary>The number of triangle faces analyzed locally.</summary>
    public double? FaceCount { get; set; }

    /// <summary>The browser runtime input hash, logged only for correlation and never used as a metric tag.</summary>
    public string? InputHash { get; set; }

    /// <summary>The approximate browser-local runtime input size in bytes.</summary>
    public long? InputByteCount { get; set; }

    /// <summary>The approximate browser-local runtime triangle workload.</summary>
    public long? InputTriangleCount { get; set; }

    /// <summary>Whether this payload represents a terminal local runtime attempt.</summary>
    public bool IsTerminalUnavailable =>
        string.Equals(Status, "unavailable", StringComparison.OrdinalIgnoreCase)
        || !string.IsNullOrWhiteSpace(Reason);

    /// <summary>Whether this payload represents the start of a local runtime attempt.</summary>
    public bool IsStarted => string.Equals(Status, "started", StringComparison.OrdinalIgnoreCase);
}
