using Maliev.Aspire.ServiceDefaults.Authorization;
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
[Route("api/geometry")]
public class GeometryController(
    GeometryServiceClient geometryServiceClient,
    UploadServiceClient uploadServiceClient,
    ILogger<GeometryController> logger) : ControllerBase
{
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
            "DFM analysis requested for upload {UploadId}, process {ProcessCode}, hasStoragePath: {HasStoragePath}",
            uploadId, processCode, !string.IsNullOrEmpty(request?.StoragePath));

        // If request has StoragePath but no DownloadUrl, generate a signed URL
        if (request != null && !string.IsNullOrEmpty(request.StoragePath) && string.IsNullOrEmpty(request.DownloadUrl))
        {
            try
            {
                var signedUrl = await uploadServiceClient.GetDownloadUrlByPathAsync(request.StoragePath, ct);
                if (!string.IsNullOrEmpty(signedUrl))
                {
                    request.DownloadUrl = signedUrl;
                    logger.LogDebug(
                        "Generated signed URL for storage path {StoragePath}",
                        request.StoragePath);
                }
                else
                {
                    logger.LogWarning(
                        "Failed to generate signed URL for storage path {StoragePath}",
                        request.StoragePath);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error generating signed URL for storage path {StoragePath}",
                    request.StoragePath);
                // Continue without signed URL - GeometryService will return 400 if needed
            }
        }

        var result = await geometryServiceClient.AnalyzeForProcessAsync(uploadId, processCode, request ?? new(), ct);

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
            "timeout" => StatusCode(504, result),
            "error" when result.DfmReport?.Issues.Any(i => i.Severity == "error") == true =>
                StatusCode(500, result),
            "error" => Ok(result), // Non-error status codes return OK
            _ => Ok(result)
        };
    }
}
