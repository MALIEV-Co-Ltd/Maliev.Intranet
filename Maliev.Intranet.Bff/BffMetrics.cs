using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Maliev.Intranet.Bff;

/// <summary>
/// Provides business metrics collection for the Intranet BFF.
/// </summary>
public class BffMetrics
{
    private readonly UpDownCounter<long> _activeSessionsCounter;
    private readonly Counter<long> _browserDfmRuntimeCompletions;
    private readonly Counter<long> _browserDfmRuntimeTerminalAttempts;
    private readonly Counter<long> _serverDfmProxyRequests;

    /// <summary>
    /// Initializes a new instance of the <see cref="BffMetrics"/> class.
    /// </summary>
    /// <param name="meterFactory">The meter factory to create metrics.</param>
    public BffMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("intranet-portal");
        _activeSessionsCounter = meter.CreateUpDownCounter<long>("intranet_active_sessions", "Number of active user sessions");
        _browserDfmRuntimeCompletions = meter.CreateCounter<long>(
            "intranet_browser_dfm_runtime_completions",
            unit: "{completion}",
            description: "Counts browser-first local DFM runtime completions observed by Intranet.");
        _browserDfmRuntimeTerminalAttempts = meter.CreateCounter<long>(
            "intranet_browser_dfm_runtime_terminal_attempts",
            unit: "{attempt}",
            description: "Counts browser-first local DFM runtime attempts that ended before producing a local report.");
        _serverDfmProxyRequests = meter.CreateCounter<long>(
            "intranet_server_dfm_proxy_requests",
            unit: "{request}",
            description: "Counts lazy server DFM requests forwarded to GeometryService after local browser DFM did not satisfy the process.");
    }

    /// <summary>
    /// Records that a user session has started.
    /// </summary>
    public void RecordSessionStarted() => _activeSessionsCounter.Add(1);

    /// <summary>
    /// Records that a user session has ended.
    /// </summary>
    public void RecordSessionEnded() => _activeSessionsCounter.Add(-1);

    /// <summary>
    /// Records a browser-first local DFM runtime completion.
    /// </summary>
    /// <param name="processCode">The process code analyzed by the browser runtime.</param>
    /// <param name="accepted">Whether the Blazor client accepted the local result for the active part.</param>
    /// <param name="authority">The runtime authority marker.</param>
    /// <param name="executionMode">The runtime execution mode marker.</param>
    public void RecordBrowserDfmRuntimeCompletion(
        string? processCode,
        bool accepted,
        string? authority,
        string? executionMode)
    {
        _browserDfmRuntimeCompletions.Add(1, new TagList
        {
            { "process_family", NormalizeProcessFamily(processCode) },
            { "accepted", accepted },
            { "authority", NormalizeMarker(authority, "other") },
            { "execution_mode", NormalizeMarker(executionMode, "other") },
        });
    }

    /// <summary>
    /// Records a browser-first local DFM runtime attempt that ended before producing a report.
    /// </summary>
    /// <param name="processCode">The process code requested for the browser runtime.</param>
    /// <param name="reason">The low-cardinality terminal reason reported by the viewer.</param>
    /// <param name="authority">The runtime authority marker.</param>
    /// <param name="executionMode">The runtime execution mode marker.</param>
    public void RecordBrowserDfmRuntimeTerminalAttempt(
        string? processCode,
        string? reason,
        string? authority,
        string? executionMode)
    {
        _browserDfmRuntimeTerminalAttempts.Add(1, new TagList
        {
            { "process_family", NormalizeProcessFamily(processCode) },
            { "reason", NormalizeMarker(reason, "local_runtime_unavailable") },
            { "authority", NormalizeMarker(authority, "other") },
            { "execution_mode", NormalizeMarker(executionMode, "other") },
        });
    }

    /// <summary>
    /// Records a lazy server DFM request forwarded to GeometryService.
    /// </summary>
    /// <param name="processCode">The requested manufacturing process code.</param>
    /// <param name="fallbackReason">The low-cardinality reason the browser-local path did not satisfy the request.</param>
    public void RecordServerDfmProxyRequest(string? processCode, string? fallbackReason)
    {
        _serverDfmProxyRequests.Add(1, new TagList
        {
            { "process_family", NormalizeProcessFamily(processCode) },
            { "fallback_reason", NormalizeMarker(fallbackReason, "browser_local_miss") },
        });
    }

    private static string NormalizeProcessFamily(string? processCode)
    {
        var normalized = NormalizeMarker(processCode, "unknown")
            .Replace("-", "_", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal)
            .ToUpperInvariant();

        return normalized switch
        {
            "CNC" or "CNC_MILL" or "CNC_MILLING" or "CNC_TURN" or "CNC_TURNING" => "cnc",
            "SLA" or "DLP" or "SLA_DLP" => "sla",
            "FDM" or "FFF" => "fdm",
            "SLS" or "MJF" or "MJ" or "BJ" or "DMLS" => "powder_bed",
            _ => "other"
        };
    }

    private static string NormalizeMarker(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var normalized = value.Trim();
        return normalized.Length <= 40 ? normalized : fallback;
    }
}
