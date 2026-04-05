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
    /// The timestamp of the last health check.
    /// </summary>
    public DateTime LastCheck { get; set; }

    /// <summary>
    /// An optional error message if the health check failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
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
