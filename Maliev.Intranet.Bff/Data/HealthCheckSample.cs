namespace Maliev.Intranet.Bff.Data;

/// <summary>
/// Persisted service health probe result for one service at one sample time.
/// </summary>
public class HealthCheckSample
{
    /// <summary>
    /// Unique sample row identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// UTC timestamp when the sampler started probing the service.
    /// </summary>
    public DateTime SampledAtUtc { get; set; }

    /// <summary>
    /// Unique service name from the system health registry.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Domain grouping shown in the system health report.
    /// </summary>
    public string DomainGroup { get; set; } = string.Empty;

    /// <summary>
    /// HTTP route prefix used by the service.
    /// </summary>
    public string RoutePrefix { get; set; } = string.Empty;

    /// <summary>
    /// Classified service status for this sample.
    /// </summary>
    public string Status { get; set; } = "Unknown";

    /// <summary>
    /// Indicates whether the service is critical for core operations.
    /// </summary>
    public bool IsCritical { get; set; }

    /// <summary>
    /// Liveness endpoint path used for this sample.
    /// </summary>
    public string LivenessPath { get; set; } = string.Empty;

    /// <summary>
    /// Readiness endpoint path used for this sample.
    /// </summary>
    public string ReadinessPath { get; set; } = string.Empty;

    /// <summary>
    /// Liveness probe response time in milliseconds.
    /// </summary>
    public double LivenessResponseTimeMs { get; set; }

    /// <summary>
    /// Readiness probe response time in milliseconds.
    /// </summary>
    public double ReadinessResponseTimeMs { get; set; }

    /// <summary>
    /// Optional failure detail captured from the failed probe.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Optional trimmed response body captured from the failed probe.
    /// </summary>
    public string? ErrorBody { get; set; }
}
