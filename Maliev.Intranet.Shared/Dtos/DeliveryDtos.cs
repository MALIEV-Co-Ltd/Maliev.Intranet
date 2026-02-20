using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

public sealed record DeliveryNoteSummaryDto
{
    public Guid Id { get; set; }
    public string DeliveryNoteNumber { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

public sealed record DeliveryNoteDetailDto
{
    public Guid Id { get; set; }
    public string DeliveryNoteNumber { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;

    public DateTime DeliveryDate { get; set; }
    public DateTime? ActualDeliveryTime { get; set; }
    public string Status { get; set; } = string.Empty;

    public string? TrackingNumber { get; set; }
    public string? CarrierName { get; set; }
    public string? DeliveryContactName { get; set; }
    public string? DeliveryContactPhone { get; set; }

    public string? ShippingAddressLine1 { get; set; }
    public string? ShippingAddressLine2 { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingProvince { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? ShippingCountry { get; set; }

    public string? DeliveryInstructions { get; set; }
    public string? InternalNotes { get; set; }
    public string? ReceivedByName { get; set; }

    public List<DeliveryNoteItemDto> Items { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public sealed record DeliveryNoteItemDto
{
    public long Id { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? ProductDescription { get; set; }
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityManufactured { get; set; }
    public decimal QuantityDelivered { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string? ItemNotes { get; set; }
}

public sealed record CreateDeliveryNoteRequest
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    public DateTime DeliveryDate { get; set; }

    public string? TrackingNumber { get; set; }
    public string? CarrierName { get; set; }
    public string? DeliveryContact { get; set; }
    public string? DeliveryPhone { get; set; }
    public string? Notes { get; set; }

    public List<CreateDeliveryNoteItemRequest> Items { get; set; } = [];
}

public sealed record CreateDeliveryNoteItemRequest
{
    [Required]
    public Guid OrderItemId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

public sealed record UpdateDeliveryStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;

    public string? ReceivedBy { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Notes { get; set; }
}
