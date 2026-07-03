namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Periodically probes registered services and stores system health history samples.
/// </summary>
public sealed class SystemHealthSamplerHostedService : BackgroundService
{
    private static readonly TimeSpan SampleInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RetentionWindow = TimeSpan.FromDays(8);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SystemHealthSamplerHostedService> _logger;
    private readonly TimeSpan _sampleInterval;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemHealthSamplerHostedService"/> class.
    /// </summary>
    /// <param name="scopeFactory">The scope factory used to resolve scoped services for each sample.</param>
    /// <param name="logger">The logger used for sampler failures.</param>
    public SystemHealthSamplerHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<SystemHealthSamplerHostedService> logger)
        : this(scopeFactory, logger, SampleInterval)
    {
    }

    internal SystemHealthSamplerHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<SystemHealthSamplerHostedService> logger,
        TimeSpan sampleInterval)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sampleInterval, TimeSpan.Zero);

        _scopeFactory = scopeFactory;
        _logger = logger;
        _sampleInterval = sampleInterval;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_sampleInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunSamplerOnceAsync(stoppingToken);
        }
    }

    private async Task RunSamplerOnceAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var probeService = scope.ServiceProvider.GetRequiredService<ISystemHealthProbeService>();
            var historyService = scope.ServiceProvider.GetRequiredService<ISystemHealthHistoryService>();
            var sampledAtUtc = RoundDown(DateTime.UtcNow, _sampleInterval);

            var statuses = await probeService.CheckAllAsync(ct);
            await historyService.StoreSamplesAsync(sampledAtUtc, statuses, ct);
            await historyService.DeleteSamplesOlderThanAsync(DateTime.UtcNow.Subtract(RetentionWindow), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "System health sampling failed.");
        }
    }

    private static DateTime RoundDown(DateTime timestampUtc, TimeSpan interval)
    {
        var ticks = timestampUtc.Ticks - timestampUtc.Ticks % interval.Ticks;
        return new DateTime(ticks, DateTimeKind.Utc);
    }
}
