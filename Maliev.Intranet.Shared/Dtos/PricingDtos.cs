using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// DTO for a pricing snapshot.
/// </summary>
public sealed record PricingSnapshotDto
{
    public Guid Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public Guid? QuotationId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string Technology { get; set; } = string.Empty;
    public string? MaterialCode { get; set; }
    public string? MaterialBrand { get; set; }
    public decimal? LayerHeight { get; set; }
    public decimal? InfillPercentage { get; set; }
    public string? SupportType { get; set; }
    public string? PrintOrientation { get; set; }
    public decimal CalculatedPrice { get; set; }
    public decimal? ManualOverridePrice { get; set; }
    public Guid PricingAuditRecordId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
}

/// <summary>
/// Request to create a pricing snapshot.
/// </summary>
public sealed record CreatePricingSnapshotRequest
{
    public string OrderId { get; set; } = string.Empty;
    public Guid? QuotationId { get; set; }
    public string Technology { get; set; } = string.Empty;
    public string? MaterialCode { get; set; }
    public string? MaterialBrand { get; set; }
    public decimal? LayerHeight { get; set; }
    public decimal? InfillPercentage { get; set; }
    public string? SupportType { get; set; }
    public string? PrintOrientation { get; set; }
    public decimal CalculatedPrice { get; set; }
    public decimal? ManualOverridePrice { get; set; }
    public Guid PricingAuditRecordId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed record PricingConfigurationDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed record PricingAuditRecordDto
{
    public Guid Id { get; set; }
    public Guid SnapshotId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public DateTime Timestamp { get; set; }
}

public sealed record CreatePricingConfigurationRequest
{
    [Required]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Pricing strategy used for calculations.
/// </summary>
public enum PricingStrategy
{
    RuleBased = 1,
    MLEnhanced = 2,
    Manual = 3,
    Hybrid = 4
}

/// <summary>
/// Request for pricing calculation.
/// </summary>
public record PricingRequestDto
{
    public required Guid FileId { get; init; }
    public required Guid CustomerId { get; init; }
    public required Guid MaterialId { get; init; }
    public required string MaterialCode { get; init; }
    public required Guid ManufacturingProcessId { get; init; }
    public required string ManufacturingProcessName { get; init; }
    public int Quantity { get; init; } = 1;
    public required GeometryMetricsDto Geometry { get; init; }
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Geometry metrics from file analysis.
/// </summary>
public record GeometryMetricsDto
{
    public required decimal VolumeCm3 { get; init; }
    public required decimal SupportVolumeCm3 { get; init; }
    public required decimal SurfaceAreaCm2 { get; init; }
    public required decimal BoundingBoxX { get; init; }
    public required decimal BoundingBoxY { get; init; }
    public required decimal BoundingBoxZ { get; init; }
    public required bool IsManifold { get; init; }
    public required int TriangleCount { get; init; }
}

/// <summary>
/// Result of pricing calculation.
/// </summary>
public record PricingResultDto
{
    public required PricingStrategy Strategy { get; init; }
    public string? MLModelVersion { get; init; }
    public required decimal MaterialCost { get; init; }
    public required decimal SupportMaterialCost { get; init; }
    public required decimal MachineTimeCost { get; init; }
    public required decimal SetupCost { get; init; }
    public required decimal ComplexitySurcharge { get; init; }
    public required decimal SubtotalBeforeMargin { get; init; }
    public required decimal MarginAmount { get; init; }
    public required decimal TotalUnitPrice { get; init; }
    public required decimal TotalPrice { get; init; }
    public required decimal ConfidenceLevel { get; init; }
    public required DateTime ValidUntil { get; init; }
    public required TimeSpan CalculationDuration { get; init; }
}
