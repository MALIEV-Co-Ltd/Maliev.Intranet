using Maliev.Intranet.Bff.Data;
using Maliev.Intranet.Bff.Services;

namespace Maliev.Intranet.Tests.Bff.Services;

public class SystemHealthHistoryAggregatorTests
{
    [Fact]
    public void BuildHistory_ExcludesMissingBucketsFromUptimeAndPreservesChronologicalOrder()
    {
        var start = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMinutes(15);
        var target = new SystemHealthTarget("AuthService", "Platform", "auth", true);
        var samples = new[]
        {
            CreateSample(target, start, "Healthy"),
            CreateSample(target, start.AddMinutes(10), "Unhealthy")
        };

        var history = SystemHealthHistoryAggregator.BuildHistory(
            [target],
            samples,
            checkedAtUtc: end,
            periodStartUtc: start,
            periodEndUtc: end,
            bucketMinutes: 5);

        var service = Assert.Single(history.Services);
        Assert.Equal(50m, service.UptimePercentage);
        Assert.Equal(["Healthy", "NoData", "Unhealthy"], service.Buckets.Select(bucket => bucket.Status).ToArray());
        Assert.Equal([start, start.AddMinutes(5), start.AddMinutes(10)], service.Buckets.Select(bucket => bucket.StartedAtUtc).ToArray());
        Assert.False(service.Buckets[1].HasSample);
    }

    private static HealthCheckSample CreateSample(SystemHealthTarget target, DateTime sampledAtUtc, string status) =>
        new()
        {
            Id = Guid.NewGuid(),
            SampledAtUtc = sampledAtUtc,
            ServiceName = target.ServiceName,
            DomainGroup = target.DomainGroup,
            RoutePrefix = target.RoutePrefix,
            IsCritical = target.IsCritical,
            Status = status,
            LivenessPath = target.LivenessPath,
            ReadinessPath = target.ReadinessPath
        };
}
