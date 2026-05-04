using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Probes registered MALIEV services and classifies their current health state.
/// </summary>
public interface ISystemHealthProbeService
{
    /// <summary>
    /// Registered service targets that are included in system health reports.
    /// </summary>
    IReadOnlyList<SystemHealthTarget> Targets { get; }

    /// <summary>
    /// Probes all registered service targets.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Ordered service health statuses.</returns>
    Task<IReadOnlyList<ServiceHealthStatus>> CheckAllAsync(CancellationToken ct);
}
