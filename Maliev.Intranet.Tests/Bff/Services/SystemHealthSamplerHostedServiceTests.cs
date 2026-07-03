using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maliev.Intranet.Tests.Bff.Services;

public sealed class SystemHealthSamplerHostedServiceTests
{
    [Fact]
    public async Task StartAsync_DoesNotProbeBeforeFirstSampleInterval()
    {
        var probeService = new CountingProbeService();
        var historyService = new CountingHistoryService();
        await using var provider = CreateProvider(probeService, historyService);
        var sampler = new SystemHealthSamplerHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SystemHealthSamplerHostedService>.Instance,
            TimeSpan.FromMilliseconds(100));

        await sampler.StartAsync(CancellationToken.None);
        await Task.Delay(30);
        await sampler.StopAsync(CancellationToken.None);

        Assert.Equal(0, probeService.CheckCount);
        Assert.Equal(0, historyService.StoreCount);
    }

    [Fact]
    public async Task StartAsync_ProbesAfterFirstSampleInterval()
    {
        var probeService = new CountingProbeService();
        var historyService = new CountingHistoryService();
        await using var provider = CreateProvider(probeService, historyService);
        var sampler = new SystemHealthSamplerHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SystemHealthSamplerHostedService>.Instance,
            TimeSpan.FromMilliseconds(25));

        await sampler.StartAsync(CancellationToken.None);
        await WaitForAsync(() => Volatile.Read(ref probeService.CheckCount) > 0);
        await sampler.StopAsync(CancellationToken.None);

        Assert.True(probeService.CheckCount > 0);
        Assert.True(historyService.StoreCount > 0);
        Assert.True(historyService.DeleteCount > 0);
    }

    private static ServiceProvider CreateProvider(
        ISystemHealthProbeService probeService,
        ISystemHealthHistoryService historyService)
    {
        return new ServiceCollection()
            .AddScoped(_ => probeService)
            .AddScoped(_ => historyService)
            .BuildServiceProvider();
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!condition())
        {
            await Task.Delay(10, cts.Token);
        }
    }

    private sealed class CountingProbeService : ISystemHealthProbeService
    {
        public int CheckCount;

        public IReadOnlyList<SystemHealthTarget> Targets { get; } =
        [
            new("AuthService", "Platform", "auth", true)
        ];

        public Task<IReadOnlyList<ServiceHealthStatus>> CheckAllAsync(CancellationToken ct)
        {
            Interlocked.Increment(ref CheckCount);

            IReadOnlyList<ServiceHealthStatus> statuses =
            [
                new()
                {
                    ServiceName = "AuthService",
                    DomainGroup = "Platform",
                    RoutePrefix = "auth",
                    Status = "Healthy",
                    IsCritical = true,
                    LastCheck = DateTime.UtcNow
                }
            ];
            return Task.FromResult(statuses);
        }
    }

    private sealed class CountingHistoryService : ISystemHealthHistoryService
    {
        public int DeleteCount;
        public int StoreCount;

        public Task StoreSamplesAsync(DateTime sampledAtUtc, IReadOnlyList<ServiceHealthStatus> statuses, CancellationToken ct)
        {
            Interlocked.Increment(ref StoreCount);
            return Task.CompletedTask;
        }

        public Task DeleteSamplesOlderThanAsync(DateTime cutoffUtc, CancellationToken ct)
        {
            Interlocked.Increment(ref DeleteCount);
            return Task.CompletedTask;
        }

        public Task<SystemHealthHistoryDto> GetHistoryAsync(int days, CancellationToken ct)
        {
            return Task.FromResult(new SystemHealthHistoryDto());
        }
    }
}
