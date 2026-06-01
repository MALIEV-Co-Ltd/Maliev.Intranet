using System.Text.Json;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Geometry microservice.
/// Provides on-demand DFM analysis for 3D manufacturing files.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class GeometryServiceClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    // Python reads JSON keys in snake_case (e.g. "storage_path", "download_url").
    // The default .NET serializer uses PascalCase, so we must opt in to snake_case explicitly.
    private static readonly JsonSerializerOptions _pythonJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <summary>
    /// Runs DFM analysis for a specific manufacturing process on an uploaded file.
    /// This is the lazy/on-demand endpoint that only runs when the user selects a process.
    /// </summary>
    /// <param name="uploadId">The upload identifier from the file upload.</param>
    /// <param name="processCode">Manufacturing process code (e.g., "FDM", "SLA", "CNC_MILL").</param>
    /// <param name="request">Optional request body with storage_path and download_url for cache-miss recovery.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>DFM analysis response with issues found for the selected process.</returns>
    public async Task<DfmAnalysisResponse?> AnalyzeForProcessAsync(
        string uploadId,
        string processCode,
        GeometryAnalysisRequest request,
        CancellationToken ct = default)
    {
        // Build the request URL
        var url = $"/geometry/uploads/{uploadId}/dfm/{processCode}";

        try
        {
            // POST with snake_case JSON so Python's request.get("storage_path") works correctly.
            var response = await _httpClient.PostAsJsonAsync(url, request, _pythonJsonOptions, ct);

            // Check for success status
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DfmAnalysisResponse>(_pythonJsonOptions, ct);
                return result;
            }

            // Python returns structured JSON bodies for all error codes — always try to deserialise.
            if (!response.IsSuccessStatusCode)
            {
                var errorResult = await response.Content.ReadFromJsonAsync<DfmAnalysisResponse>(_pythonJsonOptions, ct);
                if (errorResult != null) return errorResult;
            }

            // Fallback for unexpected / non-JSON responses
            return new DfmAnalysisResponse
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
                            Title = "Unexpected Error",
                            Description = $"Unexpected HTTP status code: {response.StatusCode}",
                            Value = 0,
                            Threshold = 0
                        }
                    }
                }
            };
        }
        catch (HttpRequestException ex)
        {
            // Network/transport error
            return new DfmAnalysisResponse
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
                            Title = "Network Error",
                            Description = $"Failed to reach GeometryService: {ex.Message}",
                            Value = 0,
                            Threshold = 0
                        }
                    }
                }
            };
        }
        catch (TaskCanceledException)
        {
            // Request was cancelled (timeout or user cancellation)
            return new DfmAnalysisResponse
            {
                UploadId = uploadId,
                ProcessCode = processCode,
                Status = "timeout",
                DfmReport = new DfmReport
                {
                    ReportType = processCode,
                    Issues = new List<DfmIssue>
                    {
                        new DfmIssue
                        {
                            Category = "system",
                            Severity = "error",
                            Title = "Request Timeout",
                            Description = "The request to GeometryService timed out",
                            Value = 0,
                            Threshold = 0
                        }
                    }
                }
            };
        }
    }

    /// <summary>
    /// Gets the browser advisory geometry runtime manifest from GeometryService.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The raw HTTP response so the BFF can preserve cache headers.</returns>
    public Task<HttpResponseMessage> GetRuntimeManifestAsync(CancellationToken ct = default)
    {
        return _httpClient.GetAsync(
            "/geometry/client-runtime/manifest.json",
            HttpCompletionOption.ResponseHeadersRead,
            ct);
    }

    /// <summary>
    /// Gets a content-hashed browser advisory geometry runtime asset from GeometryService.
    /// </summary>
    /// <param name="assetName">The content-hashed runtime asset file name.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The raw HTTP response so the BFF can preserve cache headers.</returns>
    public Task<HttpResponseMessage> GetRuntimeAssetAsync(
        string assetName,
        CancellationToken ct = default)
    {
        return _httpClient.GetAsync(
            $"/geometry/client-runtime/assets/{Uri.EscapeDataString(assetName)}",
            HttpCompletionOption.ResponseHeadersRead,
            ct);
    }
}
