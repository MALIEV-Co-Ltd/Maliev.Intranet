using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Represents the aggregated data for the operational dashboard.
/// </summary>
public class DashboardViewModel
{
    /// <summary>
    /// Gets or sets the list of dashboard widgets.
    /// </summary>
    public List<WidgetData> Widgets { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of active system alerts.
    /// </summary>
    public List<SystemAlert> Alerts { get; set; } = new();
}

/// <summary>
/// Represents data for a single dashboard widget.
/// </summary>
public class WidgetData
{
    /// <summary>
    /// Gets or sets the widget title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the widget type (e.g., "Stat", "Chart").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data payload for the widget.
    /// </summary>
    public object Data { get; set; } = new();

    /// <summary>
    /// Gets or sets the name of the source microservice.
    /// </summary>
    public string SourceService { get; set; } = string.Empty;
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
    /// Gets or sets the alert message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity level (e.g., "Info", "Warning", "Critical").
    /// </summary>
    public string Severity { get; set; } = "Info";

    /// <summary>
    /// Gets or sets the timestamp when the alert was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets an optional link for user action.
    /// </summary>
    public string? ActionLink { get; set; }
}

/// <summary>
/// Represents a summary of a customer record.
/// </summary>
public class CustomerSummaryDto
{
    /// <summary>
    /// Gets or sets the customer ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer email.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the outstanding balance for the customer.
    /// </summary>
    public decimal OutstandingBalance { get; set; }
}

/// <summary>
/// Represents a summary of an order record.
/// </summary>
public class OrderSummaryDto
{
    /// <summary>
    /// Gets or sets the order ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique order number.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer name associated with the order.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total amount of the order.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the current status of the order.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date when the order was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents the context and permissions of the currently authenticated user.
/// </summary>
public class UserContextDto
{
    /// <summary>
    /// Gets or sets the user's unique identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the user's display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of roles assigned to the user.
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of specific permissions granted to the user.
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// Generic wrapper for a standard Maliev service response.
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
public class MalievResponse<T>
{
    /// <summary>
    /// Gets or sets the data payload.
    /// </summary>
    public T? Data { get; set; }
}

/// <summary>
/// Generic wrapper for a paged response.
/// </summary>
/// <typeparam name="T">The type of the items in the list.</typeparam>
public class PagedResponse<T>
{
    /// <summary>
    /// Gets or sets the list of data items.
    /// </summary>
    public IEnumerable<T> Data { get; set; } = [];

    /// <summary>
    /// Gets or sets the pagination metadata.
    /// </summary>
    public PaginationMeta Meta { get; set; } = new();
}

/// <summary>
/// Metadata for paged responses.
/// </summary>
public class PaginationMeta
{
    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Generic wrapper for a response from a downstream microservice.
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
public class DownstreamResponse<T>
{
    /// <summary>
    /// Gets or sets the data payload.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the request was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the request failed.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
