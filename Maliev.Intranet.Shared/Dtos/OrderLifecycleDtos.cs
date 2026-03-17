namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Represents a single stage in the order lifecycle, with its entity reference and timestamps.
/// </summary>
public class OrderLifecycleStageDto
{
    /// <summary>Gets or sets the stage key (e.g. "Quoted", "Confirmed", "InProduction", "QC", "Delivered", "Invoiced", "Paid").</summary>
    public string Stage { get; set; } = string.Empty;

    /// <summary>Gets or sets the display label for the stage.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the MudBlazor icon for the stage.</summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>Gets or sets the current status of this stage: "Completed", "Current", "Pending", or "Failed".</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>Gets or sets the related entity ID (QuotationId, OrderId, JobId, DeliveryNoteId, InvoiceId, PaymentId).</summary>
    public Guid? EntityId { get; set; }

    /// <summary>Gets or sets the optional display reference number for the entity.</summary>
    public string? EntityReference { get; set; }

    /// <summary>Gets or sets the optional navigation URL for the entity.</summary>
    public string? NavigateTo { get; set; }

    /// <summary>Gets or sets when this stage was completed.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Gets or sets an optional short note for the stage (e.g. overdue days, job machine).</summary>
    public string? Note { get; set; }
}

/// <summary>
/// Aggregated lifecycle data for an order, used by the OrderLifecycleTracker component.
/// </summary>
public class OrderLifecycleDto
{
    /// <summary>Gets or sets the order ID.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the full ordered list of lifecycle stages.</summary>
    public List<OrderLifecycleStageDto> Stages { get; set; } = new();

    /// <summary>Gets or sets the key of the current active stage.</summary>
    public string CurrentStage { get; set; } = string.Empty;
}
