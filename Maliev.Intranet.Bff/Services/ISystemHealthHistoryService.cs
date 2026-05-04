using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Stores and queries historical system health samples.
/// </summary>
public interface ISystemHealthHistoryService
{
    /// <summary>
    /// Stores one sample row for each current service status.
    /// </summary>
    /// <param name="sampledAtUtc">UTC sample timestamp shared by all rows in the run.</param>
    /// <param name="statuses">Service statuses to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task StoreSamplesAsync(DateTime sampledAtUtc, IReadOnlyList<ServiceHealthStatus> statuses, CancellationToken ct);

    /// <summary>
    /// Deletes samples older than the provided cutoff.
    /// </summary>
    /// <param name="cutoffUtc">UTC retention cutoff.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteSamplesOlderThanAsync(DateTime cutoffUtc, CancellationToken ct);

    /// <summary>
    /// Returns an aggregated health history report.
    /// </summary>
    /// <param name="days">Number of visible days, clamped by the implementation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Historical system health report.</returns>
    Task<SystemHealthHistoryDto> GetHistoryAsync(int days, CancellationToken ct);
}
