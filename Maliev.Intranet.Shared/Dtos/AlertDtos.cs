namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Lightweight alert summary that travels from the BFF to the Blazor client
/// over both the REST API (/api/v1/alerts) and the SignalR hub (ReceiveAlert).
/// </summary>
public class AlertSummaryDto
{
    /// <summary>Unique alert identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Alert category, such as QuoteAccepted or ProjectPaid.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Project identifier — used to build the navigation URL.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Human-readable project number, e.g. PRJ-2026-0027.</summary>
    public string ProjectNumber { get; set; } = string.Empty;

    /// <summary>Customer display name (empty when not available in the event payload).</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Number of active parts at acceptance time.</summary>
    public int PartCount { get; set; }

    /// <summary>Comma-separated unique process types, e.g. "CNC, FDM".</summary>
    public string ProcessTypes { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the event arrived.</summary>
    public DateTime OccurredAtUtc { get; set; }
}
