namespace Maliev.Intranet.Shared;

/// <summary>
/// Represents the aggregated data for the operational dashboard.
/// </summary>
public class DashboardViewModel
{
    /// <summary>
    /// Gets or sets the collection of widget data to be displayed on the dashboard.
    /// </summary>
    public List<WidgetData> Widgets { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of active system alerts and notifications.
    /// </summary>
    public List<SystemAlert> Alerts { get; set; } = new();
}

/// <summary>
/// Represents data for a single dashboard widget.
/// </summary>
public class WidgetData
{
    /// <summary>
    /// Gets or sets the display title of the widget.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the widget type which determines its visual representation (e.g., "Chart", "Numeric").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw data payload for the widget in JSON format.
    /// </summary>
    public System.Text.Json.JsonElement Data { get; set; }

    /// <summary>
    /// Gets or sets the name of the service that provided this widget data.
    /// </summary>
    public string SourceService { get; set; } = string.Empty;

    /// <summary>Gets or sets the trend direction: "up", "down", or null for no trend.</summary>
    public string? Trend { get; set; }
    /// <summary>Gets or sets the trend percentage change (e.g. 12.5 for +12.5%).</summary>
    public double? TrendPercent { get; set; }
    /// <summary>Gets or sets the navigation URL when the widget is clicked.</summary>
    public string? NavigateTo { get; set; }
}

/// <summary>
/// Represents a system-wide alert or notification.
/// </summary>
public class SystemAlert
{
    /// <summary>
    /// Gets or sets the unique identifier for the alert.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the descriptive message of the alert.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity level of the alert (e.g., "Info", "Warning", "Error").
    /// </summary>
    public string Severity { get; set; } = "Info";

    /// <summary>
    /// Gets or sets the UTC timestamp when the alert was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets an optional navigation link related to the alert.
    /// </summary>
    public string? ActionLink { get; set; }
}

/// <summary>
/// Aggregated action items requiring the current user's attention, sourced from multiple services.
/// </summary>
public class DashboardActionItemsDto
{
    /// <summary>Gets or sets the list of action item categories.</summary>
    public List<ActionItemCategoryDto> Categories { get; set; } = new();
}

/// <summary>
/// A category of action items with a count and navigation target.
/// </summary>
public class ActionItemCategoryDto
{
    /// <summary>Gets or sets the display label, e.g. "Projects waiting for pricing".</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>Gets or sets the MudBlazor icon string.</summary>
    public string Icon { get; set; } = string.Empty;
    /// <summary>Gets or sets the item count.</summary>
    public int Count { get; set; }
    /// <summary>Gets or sets the URL to navigate to when the item is clicked.</summary>
    public string NavigateTo { get; set; } = string.Empty;
    /// <summary>Gets or sets the severity colour: "Error", "Warning", "Info".</summary>
    public string Severity { get; set; } = "Info";
    /// <summary>Gets or sets whether this item required manager permissions to be populated.</summary>
    public bool ManagerOnly { get; set; }
}

