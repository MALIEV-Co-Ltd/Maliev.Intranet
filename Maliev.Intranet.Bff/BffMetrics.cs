using System.Diagnostics.Metrics;

namespace Maliev.Intranet.Bff;

/// <summary>
/// Provides business metrics collection for the Intranet BFF.
/// </summary>
public class BffMetrics
{
    private readonly UpDownCounter<long> _activeSessionsCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="BffMetrics"/> class.
    /// </summary>
    /// <param name="meterFactory">The meter factory to create metrics.</param>
    public BffMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("intranet-portal");
        _activeSessionsCounter = meter.CreateUpDownCounter<long>("intranet_active_sessions", "Number of active user sessions");
    }

    /// <summary>
    /// Records that a user session has started.
    /// </summary>
    public void RecordSessionStarted() => _activeSessionsCounter.Add(1);

    /// <summary>
    /// Records that a user session has ended.
    /// </summary>
    public void RecordSessionEnded() => _activeSessionsCounter.Add(-1);
}
