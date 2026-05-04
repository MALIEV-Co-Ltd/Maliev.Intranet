using Maliev.Intranet.Bff.Data;
using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Builds bucketed system health history DTOs from persisted probe samples.
/// </summary>
public static class SystemHealthHistoryAggregator
{
    /// <summary>
    /// Builds an ordered history report from service targets and persisted samples.
    /// </summary>
    /// <param name="targets">Registered service targets.</param>
    /// <param name="samples">Persisted samples in the requested period.</param>
    /// <param name="checkedAtUtc">UTC time the report was generated.</param>
    /// <param name="periodStartUtc">Inclusive UTC period start.</param>
    /// <param name="periodEndUtc">Exclusive UTC period end.</param>
    /// <param name="bucketMinutes">Bucket width in minutes.</param>
    /// <returns>Aggregated history report.</returns>
    public static SystemHealthHistoryDto BuildHistory(
        IReadOnlyList<SystemHealthTarget> targets,
        IReadOnlyList<HealthCheckSample> samples,
        DateTime checkedAtUtc,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        int bucketMinutes)
    {
        var bucketSpan = TimeSpan.FromMinutes(bucketMinutes);
        var samplesByServiceBucket = samples
            .GroupBy(sample => (sample.ServiceName, BucketStart(sample.SampledAtUtc, bucketSpan)))
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(sample => sample.SampledAtUtc).First());

        var services = targets
            .OrderBy(target => target.DomainGroup)
            .ThenBy(target => target.ServiceName)
            .Select(target => BuildService(target, samplesByServiceBucket, periodStartUtc, periodEndUtc, bucketSpan))
            .ToList();

        return new SystemHealthHistoryDto
        {
            CheckedAt = checkedAtUtc,
            PeriodStartUtc = periodStartUtc,
            PeriodEndUtc = periodEndUtc,
            BucketMinutes = bucketMinutes,
            Services = services
        };
    }

    private static SystemHealthHistoryServiceDto BuildService(
        SystemHealthTarget target,
        IReadOnlyDictionary<(string ServiceName, DateTime BucketStart), HealthCheckSample> samplesByServiceBucket,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        TimeSpan bucketSpan)
    {
        var buckets = new List<SystemHealthHistoryBucketDto>();
        for (var bucketStart = periodStartUtc; bucketStart < periodEndUtc; bucketStart = bucketStart.Add(bucketSpan))
        {
            if (samplesByServiceBucket.TryGetValue((target.ServiceName, bucketStart), out var sample))
            {
                buckets.Add(new SystemHealthHistoryBucketDto
                {
                    StartedAtUtc = bucketStart,
                    EndedAtUtc = bucketStart.Add(bucketSpan),
                    Status = sample.Status,
                    HasSample = true,
                    LivenessResponseTimeMs = sample.LivenessResponseTimeMs,
                    ReadinessResponseTimeMs = sample.ReadinessResponseTimeMs
                });
            }
            else
            {
                buckets.Add(new SystemHealthHistoryBucketDto
                {
                    StartedAtUtc = bucketStart,
                    EndedAtUtc = bucketStart.Add(bucketSpan),
                    Status = "NoData",
                    HasSample = false
                });
            }
        }

        var sampledBuckets = buckets.Where(bucket => bucket.HasSample).ToList();
        var healthyBuckets = sampledBuckets.Count(bucket => bucket.Status == "Healthy");
        var uptime = sampledBuckets.Count == 0
            ? 0m
            : Math.Round(healthyBuckets * 100m / sampledBuckets.Count, 2);
        var currentStatus = sampledBuckets.LastOrDefault()?.Status ?? "NoData";

        return new SystemHealthHistoryServiceDto
        {
            ServiceName = target.ServiceName,
            DomainGroup = target.DomainGroup,
            RoutePrefix = target.RoutePrefix,
            IsCritical = target.IsCritical,
            CurrentStatus = currentStatus,
            UptimePercentage = uptime,
            Buckets = buckets
        };
    }

    private static DateTime BucketStart(DateTime timestampUtc, TimeSpan bucketSpan)
    {
        var ticks = timestampUtc.Ticks - timestampUtc.Ticks % bucketSpan.Ticks;
        return new DateTime(ticks, DateTimeKind.Utc);
    }
}
