using Maliev.Intranet.Bff.Data;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Maliev.Intranet.Tests.Bff.Services;

public sealed class IntranetDbContextTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("intranet_app_db_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task HealthCheckSamples_PersistAndFilterByServiceAndSampledTime()
    {
        var options = new DbContextOptionsBuilder<IntranetDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var setup = new IntranetDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.HealthCheckSamples.AddRange(
                CreateSample("AuthService", new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)),
                CreateSample("AuthService", new DateTime(2026, 5, 1, 0, 5, 0, DateTimeKind.Utc)),
                CreateSample("InventoryService", new DateTime(2026, 5, 1, 0, 5, 0, DateTimeKind.Utc)));
            await setup.SaveChangesAsync();
        }

        await using var db = new IntranetDbContext(options);
        var samples = await db.HealthCheckSamples
            .Where(sample => sample.ServiceName == "AuthService" && sample.SampledAtUtc >= new DateTime(2026, 5, 1, 0, 5, 0, DateTimeKind.Utc))
            .OrderBy(sample => sample.SampledAtUtc)
            .ToListAsync();

        var indexes = db.Model.FindEntityType(typeof(HealthCheckSample))?.GetIndexes().ToList();

        Assert.Single(samples);
        Assert.Contains(indexes!, index => index.Properties.Select(property => property.Name).SequenceEqual(["ServiceName", "SampledAtUtc"]));
        Assert.Contains(indexes!, index => index.Properties.Select(property => property.Name).SequenceEqual(["DomainGroup", "SampledAtUtc"]));
    }

    [Fact]
    public async Task SystemHealthHistoryService_ReturnsSevenDaysOfFiveMinuteBucketsAndDeletesExpiredSamples()
    {
        var options = new DbContextOptionsBuilder<IntranetDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        await using var db = new IntranetDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var target = new SystemHealthTarget("AuthService", "Platform", "auth", true);
        var historyService = new SystemHealthHistoryService(db, new StaticProbeService([target]));
        db.HealthCheckSamples.AddRange(
            CreateSample("AuthService", DateTime.UtcNow.AddDays(-9)),
            CreateSample("AuthService", DateTime.UtcNow.AddMinutes(-5)));
        await db.SaveChangesAsync();

        await historyService.DeleteSamplesOlderThanAsync(DateTime.UtcNow.AddDays(-8), CancellationToken.None);
        var history = await historyService.GetHistoryAsync(7, CancellationToken.None);

        Assert.Equal(5, history.BucketMinutes);
        Assert.Equal(2016, Assert.Single(history.Services).Buckets.Count);
        Assert.Equal(1, await db.HealthCheckSamples.CountAsync());
    }

    private static HealthCheckSample CreateSample(string serviceName, DateTime sampledAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            SampledAtUtc = sampledAtUtc,
            ServiceName = serviceName,
            DomainGroup = serviceName == "AuthService" ? "Platform" : "Operations",
            RoutePrefix = serviceName == "AuthService" ? "auth" : "inventory",
            Status = "Healthy",
            LivenessPath = serviceName == "AuthService" ? "/auth/liveness" : "/inventory/liveness",
            ReadinessPath = serviceName == "AuthService" ? "/auth/readiness" : "/inventory/readiness"
        };

    private sealed class StaticProbeService(IReadOnlyList<SystemHealthTarget> targets) : ISystemHealthProbeService
    {
        public IReadOnlyList<SystemHealthTarget> Targets { get; } = targets;

        public Task<IReadOnlyList<ServiceHealthStatus>> CheckAllAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ServiceHealthStatus>>([]);
    }
}
