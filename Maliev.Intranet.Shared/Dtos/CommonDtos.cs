using System.Text.Json.Serialization;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Generic response wrapper for all API responses.
/// </summary>
public class MalievResponse<T>
{
    public T? Data { get; set; }
    public string? Message { get; set; }
    public bool Success { get; set; } = true;
}

/// <summary>
/// Paged response wrapper.
/// </summary>
public class PagedResponse<T>
{
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public PaginationMeta Meta { get; set; } = new();
}

/// <summary>
/// Metadata for paged responses.
/// </summary>
public class PaginationMeta
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;
}

/// <summary>
/// Represents a response from a downstream service.
/// </summary>
public class DownstreamResponse<T>
{
    public T? Data { get; set; }
    public string? Error { get; set; }
    public bool IsSuccess => string.IsNullOrEmpty(Error);
}

/// <summary>
/// Represents the health status of a specific service.
/// </summary>
public class ServiceHealthStatus
{
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = "Unknown";
    public string? Version { get; set; }
    public string? Description { get; set; }
    public double ResponseTimeMs { get; set; }
    public DateTime LastCheck { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Aggregated health status for the entire system.
/// </summary>
public class SystemHealthDto
{
    public string OverallStatus { get; set; } = "Healthy";
    public DateTime OverallTimestamp { get; set; }
    public List<ServiceHealthStatus> Services { get; set; } = new();
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response model for BFF file uploads.
/// </summary>
public class BffUploadResponse
{
    public string FileReference { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? StoragePath { get; set; }
}
