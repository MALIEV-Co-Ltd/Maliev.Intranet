namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Periodically probes registered services and stores system health history samples.
/// </summary>
public sealed class SystemHealthSamplerHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<SystemHealthSamplerHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan SampleInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RetentionWindow = TimeSpan.FromDays(8);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunSamplerOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(SampleInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunSamplerOnceAsync(stoppingToken);
        }
    }

    private async Task RunSamplerOnceAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var probeService = scope.ServiceProvider.GetRequiredService<ISystemHealthProbeService>();
            var historyService = scope.ServiceProvider.GetRequiredService<ISystemHealthHistoryService>();
            var sampledAtUtc = RoundDown(DateTime.UtcNow, SampleInterval);

            var statuses = await probeService.CheckAllAsync(ct);
            await historyService.StoreSamplesAsync(sampledAtUtc, statuses, ct);
            await historyService.DeleteSamplesOlderThanAsync(DateTime.UtcNow.Subtract(RetentionWindow), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "System health sampling failed.");
        }
    }

    private static DateTime RoundDown(DateTime timestampUtc, TimeSpan interval)
    {
        var ticks = timestampUtc.Ticks - timestampUtc.Ticks % interval.Ticks;
        return new DateTime(ticks, DateTimeKind.Utc);
    }
}
