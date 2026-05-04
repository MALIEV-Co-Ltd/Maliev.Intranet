namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Registered service target for system health probing.
/// </summary>
/// <param name="ServiceName">The Aspire service discovery name.</param>
/// <param name="DomainGroup">The domain group shown on the health page.</param>
/// <param name="RoutePrefix">The HTTP route prefix for service endpoints.</param>
/// <param name="IsCritical">Whether the service is critical for core operations.</param>
public sealed record SystemHealthTarget(
    string ServiceName,
    string DomainGroup,
    string RoutePrefix,
    bool IsCritical)
{
    /// <summary>
    /// Service liveness endpoint path.
    /// </summary>
    public string LivenessPath { get; } = $"/{RoutePrefix}/liveness";

    /// <summary>
    /// Service readiness endpoint path.
    /// </summary>
    public string ReadinessPath { get; } = $"/{RoutePrefix}/readiness";
}
