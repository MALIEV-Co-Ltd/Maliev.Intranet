using Maliev.Intranet.Bff.Data;
using Maliev.Intranet.Shared;
using Microsoft.EntityFrameworkCore;

namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Database-backed system health history service.
/// </summary>
public sealed class SystemHealthHistoryService(IntranetDbContext dbContext, ISystemHealthProbeService probeService) : ISystemHealthHistoryService
{
    private const int BucketMinutes = 5;
    private const int MaximumHistoryDays = 7;

    /// <inheritdoc />
    public async Task StoreSamplesAsync(DateTime sampledAtUtc, IReadOnlyList<ServiceHealthStatus> statuses, CancellationToken ct)
    {
        var samples = statuses.Select(status => new HealthCheckSample
        {
            Id = Guid.NewGuid(),
            SampledAtUtc = sampledAtUtc,
            ServiceName = status.ServiceName,
            DomainGroup = status.DomainGroup,
            RoutePrefix = status.RoutePrefix,
            Status = status.Status,
            IsCritical = status.IsCritical,
            LivenessPath = status.LivenessPath,
            ReadinessPath = status.ReadinessPath,
            LivenessResponseTimeMs = status.LivenessResponseTimeMs,
            ReadinessResponseTimeMs = status.ReadinessResponseTimeMs,
            ErrorMessage = status.ErrorMessage,
            ErrorBody = status.ErrorBody
        });

        dbContext.HealthCheckSamples.AddRange(samples);
        await dbContext.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task DeleteSamplesOlderThanAsync(DateTime cutoffUtc, CancellationToken ct)
    {
        await dbContext.HealthCheckSamples
            .Where(sample => sample.SampledAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(ct);
    }

    /// <inheritdoc />
    public async Task<SystemHealthHistoryDto> GetHistoryAsync(int days, CancellationToken ct)
    {
        var visibleDays = Math.Clamp(days, 1, MaximumHistoryDays);
        var checkedAtUtc = DateTime.UtcNow;
        var periodEndUtc = RoundDown(checkedAtUtc, TimeSpan.FromMinutes(BucketMinutes)).AddMinutes(BucketMinutes);
        var periodStartUtc = periodEndUtc.AddDays(-visibleDays);

        var samples = await dbContext.HealthCheckSamples
            .AsNoTracking()
            .Where(sample => sample.SampledAtUtc >= periodStartUtc && sample.SampledAtUtc < periodEndUtc)
            .OrderBy(sample => sample.SampledAtUtc)
            .ToListAsync(ct);

        return SystemHealthHistoryAggregator.BuildHistory(
            probeService.Targets,
            samples,
            checkedAtUtc,
            periodStartUtc,
            periodEndUtc,
            BucketMinutes);
    }

    private static DateTime RoundDown(DateTime timestampUtc, TimeSpan interval)
    {
        var ticks = timestampUtc.Ticks - timestampUtc.Ticks % interval.Ticks;
        return new DateTime(ticks, DateTimeKind.Utc);
    }
}
