using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Summary information for a delivery note.
/// </summary>
public sealed record DeliveryNoteSummaryDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the delivery note number.</summary>
    public string DeliveryNoteNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the associated order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the customer name.</summary>
    public string CustomerName { get; set; } = string.Empty;
    /// <summary>Gets or sets the delivery date.</summary>
    public DateTime DeliveryDate { get; set; }
    /// <summary>Gets or sets the current status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the tracking number.</summary>
    public string TrackingNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the count of items in the delivery.</summary>
    public int ItemCount { get; set; }
}

/// <summary>
/// Detailed information for a delivery note.
/// </summary>
public sealed record DeliveryNoteDetailDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the delivery note number.</summary>
    public string DeliveryNoteNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the associated order ID.</summary>
    public string? OrderId { get; set; }
    /// <summary>Gets or sets the associated order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the customer ID.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Gets or sets the customer name.</summary>
    public string CustomerName { get; set; } = string.Empty;
    /// <summary>Gets or sets the customer address.</summary>
    public string CustomerAddress { get; set; } = string.Empty;

    /// <summary>Gets or sets the scheduled delivery date.</summary>
    public DateTime DeliveryDate { get; set; }
    /// <summary>Gets or sets the actual delivery time.</summary>
    public DateTime? ActualDeliveryTime { get; set; }
    /// <summary>Gets or sets the current status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the tracking number.</summary>
    public string? TrackingNumber { get; set; }
    /// <summary>Gets or sets the carrier name.</summary>
    public string? CarrierName { get; set; }
    /// <summary>Gets or sets the delivery contact name.</summary>
    public string? DeliveryContactName { get; set; }
    /// <summary>Gets or sets the delivery contact phone number.</summary>
    public string? DeliveryContactPhone { get; set; }

    /// <summary>Gets or sets shipping address line 1.</summary>
    public string? ShippingAddressLine1 { get; set; }
    /// <summary>Gets or sets shipping address line 2.</summary>
    public string? ShippingAddressLine2 { get; set; }
    /// <summary>Gets or sets the shipping city.</summary>
    public string? ShippingCity { get; set; }
    /// <summary>Gets or sets the shipping province.</summary>
    public string? ShippingProvince { get; set; }
    /// <summary>Gets or sets the shipping postal code.</summary>
    public string? ShippingPostalCode { get; set; }
    /// <summary>Gets or sets the shipping country.</summary>
    public string? ShippingCountry { get; set; }

    /// <summary>Gets or sets special delivery instructions.</summary>
    public string? DeliveryInstructions { get; set; }
    /// <summary>Gets or sets internal notes.</summary>
    public string? InternalNotes { get; set; }
    /// <summary>Gets or sets the name of the person who received the delivery.</summary>
    public string? ReceivedByName { get; set; }

    /// <summary>Gets or sets the list of items in the delivery.</summary>
    public List<DeliveryNoteItemDto> Items { get; set; } = [];

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the creator identifier.</summary>
    public string CreatedBy { get; set; } = string.Empty;
    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTime? UpdatedAt { get; set; }
    /// <summary>Gets or sets the last updater identifier.</summary>
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Information for a single item in a delivery note.
/// </summary>
public sealed record DeliveryNoteItemDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public long Id { get; set; }
    /// <summary>Gets or sets the product code.</summary>
    public string? ProductCode { get; set; }
    /// <summary>Gets or sets the product name.</summary>
    public string? ProductName { get; set; }
    /// <summary>Gets or sets the product description.</summary>
    public string? ProductDescription { get; set; }
    /// <summary>Gets or sets the quantity ordered.</summary>
    public decimal QuantityOrdered { get; set; }
    /// <summary>Gets or sets the quantity manufactured.</summary>
    public decimal QuantityManufactured { get; set; }
    /// <summary>Gets or sets the quantity delivered.</summary>
    public decimal QuantityDelivered { get; set; }
    /// <summary>Gets or sets the unit of measure.</summary>
    public string UnitOfMeasure { get; set; } = string.Empty;
    /// <summary>Gets or sets optional item-specific notes.</summary>
    public string? ItemNotes { get; set; }
}

/// <summary>
/// Request model for creating a delivery note.
/// </summary>
public sealed record CreateDeliveryNoteRequest
{
    /// <summary>Gets or sets the associated order ID.</summary>
    [Required]
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the scheduled delivery date.</summary>
    [Required]
    public DateTime DeliveryDate { get; set; }

    /// <summary>Gets or sets the tracking number.</summary>
    public string? TrackingNumber { get; set; }
    /// <summary>Gets or sets the carrier name.</summary>
    public string? CarrierName { get; set; }
    /// <summary>Gets or sets the delivery contact person.</summary>
    public string? DeliveryContact { get; set; }
    /// <summary>Gets or sets the delivery contact phone.</summary>
    public string? DeliveryPhone { get; set; }
    /// <summary>Gets or sets optional notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the list of items to include in the delivery note.</summary>
    public List<CreateDeliveryNoteItemRequest> Items { get; set; } = [];
}

/// <summary>
/// Request model for adding an item to a delivery note.
/// </summary>
public sealed record CreateDeliveryNoteItemRequest
{
    /// <summary>Gets or sets the associated order item ID.</summary>
    [Required]
    public Guid OrderItemId { get; set; }

    /// <summary>Gets or sets the quantity to deliver.</summary>
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

/// <summary>
/// Request model for updating delivery status.
/// </summary>
public sealed record UpdateDeliveryStatusRequest
{
    /// <summary>Gets or sets the new status.</summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets who received the delivery.</summary>
    public string? ReceivedBy { get; set; }
    /// <summary>Gets or sets when the delivery was received.</summary>
    public DateTime? ReceivedAt { get; set; }
    /// <summary>Gets or sets optional notes.</summary>
    public string? Notes { get; set; }
}
