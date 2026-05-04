using System.Text.Json.Serialization;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Generic response wrapper for all API responses, providing a standardized structure for data, messages, and success status.
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
public class MalievResponse<T>
{
    /// <summary>
    /// The actual data payload of the response.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// An optional message providing additional information or error details.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; set; } = true;
}

/// <summary>
/// Paged response wrapper for returning collections of data with associated pagination metadata.
/// </summary>
/// <typeparam name="T">The type of the items in the collection.</typeparam>
public class PagedResponse<T>
{
    /// <summary>
    /// The collection of data items for the current page.
    /// </summary>
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();

    /// <summary>
    /// The pagination metadata containing page numbers and item counts.
    /// </summary>
    public PaginationMeta Meta { get; set; } = new();
}

/// <summary>
/// Metadata for paged responses, including current page, total pages, and total items.
/// </summary>
public class PaginationMeta
{
    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// The total number of pages available.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// The total number of items available across all pages.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// The total count of items in the current response.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Indicates whether a previous page exists.
    /// </summary>
    public bool HasPrevious => CurrentPage > 1;

    /// <summary>
    /// Indicates whether a next page exists.
    /// </summary>
    public bool HasNext => CurrentPage < TotalPages;
}

/// <summary>
/// Represents a response received from a downstream internal microservice.
/// </summary>
/// <typeparam name="T">The type of the downstream data payload.</typeparam>
public class DownstreamResponse<T>
{
    /// <summary>
    /// The data payload returned by the downstream service.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// The error message returned by the downstream service, if any.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Indicates whether the downstream request was successful based on the absence of errors.
    /// </summary>
    public bool IsSuccess => string.IsNullOrEmpty(Error);
}

/// <summary>
/// Represents the operational health status and diagnostic information of a specific microservice.
/// </summary>
public class ServiceHealthStatus
{
    /// <summary>
    /// The unique name of the service being monitored.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// The domain group this service belongs to.
    /// </summary>
    public string DomainGroup { get; set; } = string.Empty;

    /// <summary>
    /// The service route prefix used for HTTP endpoints.
    /// </summary>
    public string RoutePrefix { get; set; } = string.Empty;

    /// <summary>
    /// The health endpoint path used for liveness checks.
    /// </summary>
    public string HealthPath { get; set; } = string.Empty;

    /// <summary>
    /// The endpoint path used to verify the service process is reachable.
    /// </summary>
    public string LivenessPath { get; set; } = string.Empty;

    /// <summary>
    /// The endpoint path used to verify service dependencies are ready.
    /// </summary>
    public string ReadinessPath { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the service is critical for core business operations.
    /// </summary>
    public bool IsCritical { get; set; }

    /// <summary>
    /// The current status of the service (e.g., Healthy, Unhealthy, Unknown).
    /// </summary>
    public string Status { get; set; } = "Unknown";

    /// <summary>
    /// The deployed version of the service.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// A brief description of the service's purpose or current state.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The response time of the health check in milliseconds.
    /// </summary>
    public double ResponseTimeMs { get; set; }

    /// <summary>
    /// The response time of the liveness check in milliseconds.
    /// </summary>
    public double LivenessResponseTimeMs { get; set; }

    /// <summary>
    /// The response time of the readiness check in milliseconds.
    /// </summary>
    public double ReadinessResponseTimeMs { get; set; }

    /// <summary>
    /// The timestamp of the last health check.
    /// </summary>
    public DateTime LastCheck { get; set; }

    /// <summary>
    /// An optional error message if the health check failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The response body returned by the health endpoint when the check fails.
    /// </summary>
    public string? ErrorBody { get; set; }
}

/// <summary>
/// Aggregated health status report for the entire system ecosystem.
/// </summary>
public class SystemHealthDto
{
    /// <summary>
    /// The overall calculated health status of the system.
    /// </summary>
    public string OverallStatus { get; set; } = "Healthy";

    /// <summary>
    /// The timestamp indicating when the overall health status was determined.
    /// </summary>
    public DateTime OverallTimestamp { get; set; }

    /// <summary>
    /// The list of health statuses for each individual service in the system.
    /// </summary>
    public List<ServiceHealthStatus> Services { get; set; } = new();

    /// <summary>
    /// The timestamp when the health check aggregation was initiated.
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Bucketed system health history for the requested period.
/// </summary>
public class SystemHealthHistoryDto
{
    /// <summary>
    /// The UTC timestamp when the history report was generated.
    /// </summary>
    public DateTime CheckedAt { get; set; }

    /// <summary>
    /// Inclusive UTC start of the returned history period.
    /// </summary>
    public DateTime PeriodStartUtc { get; set; }

    /// <summary>
    /// Exclusive UTC end of the returned history period.
    /// </summary>
    public DateTime PeriodEndUtc { get; set; }

    /// <summary>
    /// Width of each history bucket in minutes.
    /// </summary>
    public int BucketMinutes { get; set; }

    /// <summary>
    /// Service rows included in the history report.
    /// </summary>
    public List<SystemHealthHistoryServiceDto> Services { get; set; } = new();
}

/// <summary>
/// Bucketed health history for one service.
/// </summary>
public class SystemHealthHistoryServiceDto
{
    /// <summary>
    /// Unique service name from the health registry.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Domain group shown on the health report.
    /// </summary>
    public string DomainGroup { get; set; } = string.Empty;

    /// <summary>
    /// HTTP route prefix for the service.
    /// </summary>
    public string RoutePrefix { get; set; } = string.Empty;

    /// <summary>
    /// Whether the service is critical for core operations.
    /// </summary>
    public bool IsCritical { get; set; }

    /// <summary>
    /// Most recent sampled status in the requested period, or NoData.
    /// </summary>
    public string CurrentStatus { get; set; } = "NoData";

    /// <summary>
    /// Uptime percentage calculated from sampled buckets only.
    /// </summary>
    public decimal UptimePercentage { get; set; }

    /// <summary>
    /// Chronologically ordered health buckets for the requested period.
    /// </summary>
    public List<SystemHealthHistoryBucketDto> Buckets { get; set; } = new();
}

/// <summary>
/// One rendered history bucket for a service.
/// </summary>
public class SystemHealthHistoryBucketDto
{
    /// <summary>
    /// Inclusive UTC bucket start timestamp.
    /// </summary>
    public DateTime StartedAtUtc { get; set; }

    /// <summary>
    /// Exclusive UTC bucket end timestamp.
    /// </summary>
    public DateTime EndedAtUtc { get; set; }

    /// <summary>
    /// Bucket status, including NoData for missing samples.
    /// </summary>
    public string Status { get; set; } = "NoData";

    /// <summary>
    /// Indicates whether the bucket has a persisted sample.
    /// </summary>
    public bool HasSample { get; set; }

    /// <summary>
    /// Liveness response time in milliseconds for sampled buckets.
    /// </summary>
    public double LivenessResponseTimeMs { get; set; }

    /// <summary>
    /// Readiness response time in milliseconds for sampled buckets.
    /// </summary>
    public double ReadinessResponseTimeMs { get; set; }
}

/// <summary>
/// Response model containing metadata for a successfully uploaded file via the Backend-for-Frontend (BFF).
/// </summary>
public class BffUploadResponse
{
    /// <summary>
    /// The unique identifier assigned to the upload operation.
    /// </summary>
    [JsonPropertyName("uploadId")]
    public string UploadId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the uploaded file.
    /// </summary>
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The size of the uploaded file in bytes.
    /// </summary>
    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    /// <summary>
    /// The path where the file is stored in the underlying storage system.
    /// </summary>
    [JsonPropertyName("storagePath")]
    public string? StoragePath { get; set; }

    /// <summary>
    /// A public or internal reference used to retrieve or link the file.
    /// </summary>
    [JsonPropertyName("fileReference")]
    public string? FileReference { get; set; }
}

/// <summary>
/// Request model for initiating a BFF-mediated resumable upload.
/// </summary>
public class BffInitiateResumableUploadRequest
{
    /// <summary>
    /// The original file name selected by the user.
    /// </summary>
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The content type reported by the browser.
    /// </summary>
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    /// <summary>
    /// The file size in bytes.
    /// </summary>
    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    /// <summary>
    /// The temporary or persisted project identifier that scopes the upload path.
    /// </summary>
    [JsonPropertyName("projectId")]
    public Guid ProjectId { get; set; }

    /// <summary>
    /// The customer identifier that owns the project, when already selected.
    /// </summary>
    [JsonPropertyName("customerId")]
    public Guid? CustomerId { get; set; }
}

/// <summary>
/// Response model containing a direct GCS resumable upload session.
/// </summary>
public class BffResumableUploadSessionResponse
{
    /// <summary>
    /// The UploadService identifier for the resumable upload session.
    /// </summary>
    [JsonPropertyName("uploadId")]
    public string UploadId { get; set; } = string.Empty;

    /// <summary>
    /// The short-lived GCS session URI used by the browser to upload bytes.
    /// </summary>
    [JsonPropertyName("sessionUri")]
    public string SessionUri { get; set; } = string.Empty;

    /// <summary>
    /// The storage path assigned to the upload.
    /// </summary>
    [JsonPropertyName("storagePath")]
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// The original file name selected by the user.
    /// </summary>
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The file size in bytes.
    /// </summary>
    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    /// <summary>
    /// The timestamp when the GCS resumable session expires.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Response model containing specific dates extracted from a Non-Disclosure Agreement (NDA) document by AI.
/// </summary>
public class ExtractedNdaDatesResponse
{
    /// <summary>
    /// The date when the NDA document is scheduled to expire.
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// The date the NDA becomes legally effective.
    /// </summary>
    public DateTime? EffectiveDate { get; set; }

    /// <summary>
    /// The date the NDA was signed by the parties involved.
    /// </summary>
    public DateTime? SignedDate { get; set; }
}

/// <summary>
/// Response model containing a summary and key attributes extracted from an NDA document by AI.
/// </summary>
public class NdaSummaryResponse
{
    /// <summary>
    /// A concise summary of the NDA's contents.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// A list of key legal or business terms identified in the document.
    /// </summary>
    public List<string> KeyTerms { get; set; } = [];

    /// <summary>
    /// The defined scope of confidentiality as stated in the agreement.
    /// </summary>
    public string? ConfidentialityScope { get; set; }

    /// <summary>
    /// The duration for which the confidentiality obligations remain in effect.
    /// </summary>
    public string? Duration { get; set; }

    /// <summary>
    /// The jurisdiction or governing law specified in the agreement.
    /// </summary>
    public string? GoverningLaw { get; set; }
}

/// <summary>
/// Standardized error response model for API failures, including code, message, and diagnostic details.
/// </summary>
public class ApiErrorResponse
{
    /// <summary>
    /// A machine-readable error code for programmatically handling the error.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable message describing the error.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional dictionary containing detailed validation errors or diagnostic information.
    /// </summary>
    public Dictionary<string, string[]>? Details { get; set; }

    /// <summary>
    /// The trace identifier for correlating the request with server-side logs.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// The timestamp when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// A short title or summary of the error type.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The HTTP status code associated with the error.
    /// </summary>
    public int? Status { get; set; }
}

/// <summary>Exchange rate returned by the BFF currency rate proxy endpoint.</summary>
/// <param name="From">Source currency code (e.g. "THB").</param>
/// <param name="To">Target currency code (e.g. "USD").</param>
/// <param name="Rate">Multiplier to convert an amount from <paramref name="From"/> to <paramref name="To"/>.</param>
public sealed record ExchangeRateResponse(string From, string To, decimal Rate);
