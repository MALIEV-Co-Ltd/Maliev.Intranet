namespace Maliev.Intranet.Shared;

/// <summary>
/// Represents the aggregated data for the operational dashboard.
/// </summary>
public class DashboardViewModel
{
    public List<WidgetData> Widgets { get; set; } = new();
    public List<SystemAlert> Alerts { get; set; } = new();
}

/// <summary>
/// Represents data for a single dashboard widget.
/// </summary>
public class WidgetData
{
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public System.Text.Json.JsonElement Data { get; set; }
    public string SourceService { get; set; } = string.Empty;
}

/// <summary>
/// Represents a system-wide alert or notification.
/// </summary>
public class SystemAlert
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public DateTime Timestamp { get; set; }
    public string? ActionLink { get; set; }
}
