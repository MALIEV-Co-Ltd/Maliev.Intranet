using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Services;

internal sealed class UnavailableSystemHealthHistoryService : ISystemHealthHistoryService
{
    public Task StoreSamplesAsync(DateTime sampledAtUtc, IReadOnlyList<ServiceHealthStatus> statuses, CancellationToken ct) =>
        Task.CompletedTask;

    public Task DeleteSamplesOlderThanAsync(DateTime cutoffUtc, CancellationToken ct) =>
        Task.CompletedTask;

    public Task<SystemHealthHistoryDto> GetHistoryAsync(int days, CancellationToken ct) =>
        Task.FromResult(new SystemHealthHistoryDto
        {
            CheckedAt = DateTime.UtcNow,
            PeriodStartUtc = DateTime.UtcNow.AddDays(-Math.Clamp(days, 1, 7)),
            PeriodEndUtc = DateTime.UtcNow,
            BucketMinutes = 5
        });
}
