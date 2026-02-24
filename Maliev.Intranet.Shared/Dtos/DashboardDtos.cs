namespace Maliev.Intranet.Shared;

/// <summary>
/// Represents the aggregated data for the operational dashboard.
/// </summary>
public class DashboardViewModel
{
    /// <summary>Gets or sets the collection of dashboard widgets.</summary>
    public List<WidgetData> Widgets { get; set; } = new();
    /// <summary>Gets or sets the collection of active system alerts.</summary>
    public List<SystemAlert> Alerts { get; set; } = new();
}

/// <summary>
/// Represents data for a single dashboard widget.
/// </summary>
public class WidgetData
{
    /// <summary>Gets or sets the widget title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the widget type.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Gets or sets the raw JSON data for the widget.</summary>
    public System.Text.Json.JsonElement Data { get; set; }
    /// <summary>Gets or sets the name of the service that provided the data.</summary>
    public string SourceService { get; set; } = string.Empty;
}

/// <summary>
/// Represents a system-wide alert or notification.
/// </summary>
public class SystemAlert
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the alert message content.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>Gets or sets the severity level (e.g., Info, Warning, Error).</summary>
    public string Severity { get; set; } = "Info";
    /// <summary>Gets or sets the timestamp of the alert.</summary>
    public DateTime Timestamp { get; set; }
    /// <summary>Gets or sets an optional link for taking action on the alert.</summary>
    public string? ActionLink { get; set; }
}
