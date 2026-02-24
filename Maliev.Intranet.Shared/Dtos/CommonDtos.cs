using System.Net;
using System.Text.Json.Serialization;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Generic response wrapper for all API responses.
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
public class MalievResponse<T>
{
    /// <summary>Gets or sets the response data.</summary>
    public T? Data { get; set; }
    /// <summary>Gets or sets an optional message.</summary>
    public string? Message { get; set; }
    /// <summary>Gets or sets a value indicating whether the request was successful.</summary>
    public bool Success { get; set; } = true;
}

/// <summary>
/// Paged response wrapper.
/// </summary>
/// <typeparam name="T">The type of items in the data collection.</typeparam>
public class PagedResponse<T>
{
    /// <summary>Gets or sets the collection of data items.</summary>
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    /// <summary>Gets or sets the pagination metadata.</summary>
    public PaginationMeta Meta { get; set; } = new();
}

/// <summary>
/// Metadata for paged responses.
/// </summary>
public class PaginationMeta
{
    /// <summary>Gets or sets the current page number.</summary>
    public int CurrentPage { get; set; }
    /// <summary>Gets or sets the total number of pages.</summary>
    public int TotalPages { get; set; }
    /// <summary>Gets or sets the total number of items.</summary>
    public int TotalItems { get; set; }
    /// <summary>Gets or sets the total count.</summary>
    public int TotalCount { get; set; }
    /// <summary>Gets or sets the size of the page.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets a value indicating whether there is a previous page.</summary>
    public bool HasPrevious => CurrentPage > 1;
    /// <summary>Gets a value indicating whether there is a next page.</summary>
    public bool HasNext => CurrentPage < TotalPages;
}

/// <summary>
/// Represents a response from a downstream service, preserving the original HTTP status code
/// so controllers can forward meaningful error codes to callers instead of defaulting to 500.
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
public class DownstreamResponse<T>
{
    /// <summary>Gets or sets the response data.</summary>
    public T? Data { get; set; }
    /// <summary>Gets or sets the error message, if any.</summary>
    public string? Error { get; set; }
    /// <summary>Gets or sets the HTTP status code returned by the downstream service.</summary>
    public System.Net.HttpStatusCode StatusCode { get; set; } = System.Net.HttpStatusCode.OK;
    /// <summary>Gets a value indicating whether the request was successful.</summary>
    public bool IsSuccess => string.IsNullOrEmpty(Error);

    /// <summary>
    /// Creates a successful downstream response with the given data.
    /// </summary>
    /// <param name="data">The data payload returned by the downstream service.</param>
    /// <returns>A successful <see cref="DownstreamResponse{T}"/>.</returns>
    public static DownstreamResponse<T> Ok(T? data) =>
        new() { Data = data, StatusCode = System.Net.HttpStatusCode.OK };

    /// <summary>
    /// Creates a failed downstream response preserving the original HTTP status code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned by the downstream service.</param>
    /// <returns>A failed <see cref="DownstreamResponse{T}"/>.</returns>
    public static DownstreamResponse<T> Fail(System.Net.HttpStatusCode statusCode) =>
        new() { Error = statusCode.ToString(), StatusCode = statusCode };
}

/// <summary>
/// Represents the health status of a specific service.
/// </summary>
public class ServiceHealthStatus
{
    /// <summary>Gets or sets the name of the service.</summary>
    public string ServiceName { get; set; } = string.Empty;
    /// <summary>Gets or sets the health status.</summary>
    public string Status { get; set; } = "Unknown";
    /// <summary>Gets or sets the service version.</summary>
    public string? Version { get; set; }
    /// <summary>Gets or sets the service description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the response time in milliseconds.</summary>
    public double ResponseTimeMs { get; set; }
    /// <summary>Gets or sets the timestamp of the last check.</summary>
    public DateTime LastCheck { get; set; }
    /// <summary>Gets or sets the error message, if any.</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Aggregated health status for the entire system.
/// </summary>
public class SystemHealthDto
{
    /// <summary>Gets or sets the overall system status.</summary>
    public string OverallStatus { get; set; } = "Healthy";
    /// <summary>Gets or sets the overall status timestamp.</summary>
    public DateTime OverallTimestamp { get; set; }
    /// <summary>Gets or sets the collection of individual service health statuses.</summary>
    public List<ServiceHealthStatus> Services { get; set; } = new();
    /// <summary>Gets or sets the timestamp when the check was performed.</summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response model for BFF file uploads.
/// </summary>
public class BffUploadResponse
{
    /// <summary>Gets or sets the unique upload identifier.</summary>
    [JsonPropertyName("uploadId")]
    public string UploadId { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the uploaded file.</summary>
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the size of the file in bytes.</summary>
    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    /// <summary>Gets or sets the path where the file is stored.</summary>
    [JsonPropertyName("storagePath")]
    public string? StoragePath { get; set; }

    /// <summary>Gets or sets the cloud storage file reference.</summary>
    [JsonPropertyName("fileReference")]
    public string? FileReference { get; set; }
}

/// <summary>
/// Response model for AI-extracted NDA dates.
/// </summary>
public class ExtractedNdaDatesResponse
{
    /// <summary>Gets or sets the extracted expiration date.</summary>
    public DateTime? ExpirationDate { get; set; }
    /// <summary>Gets or sets the extracted effective date.</summary>
    public DateTime? EffectiveDate { get; set; }
    /// <summary>Gets or sets the extracted signing date.</summary>
    public DateTime? SignedDate { get; set; }
}

/// <summary>
/// Response model for AI-generated NDA document summary.
/// </summary>
public class NdaSummaryResponse
{
    /// <summary>Gets or sets the textual summary of the NDA.</summary>
    public string Summary { get; set; } = string.Empty;
    /// <summary>Gets or sets the list of key terms extracted.</summary>
    public List<string> KeyTerms { get; set; } = [];
    /// <summary>Gets or sets the confidentiality scope.</summary>
    public string? ConfidentialityScope { get; set; }
    /// <summary>Gets or sets the duration of the agreement.</summary>
    public string? Duration { get; set; }
    /// <summary>Gets or sets the governing law.</summary>
    public string? GoverningLaw { get; set; }
}

/// <summary>
/// Standard error response for API failures.
/// </summary>
public class ApiErrorResponse
{
    /// <summary>Gets or sets the error code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the error message.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>Gets or sets the detailed error information.</summary>
    public Dictionary<string, string[]>? Details { get; set; }
    /// <summary>Gets or sets the trace identifier.</summary>
    public string? TraceId { get; set; }
    /// <summary>Gets or sets the timestamp of the error.</summary>
    public DateTime Timestamp { get; set; }
    /// <summary>Gets or sets the error title.</summary>
    public string? Title { get; set; }
    /// <summary>Gets or sets the HTTP status code.</summary>
    public int? Status { get; set; }
}
