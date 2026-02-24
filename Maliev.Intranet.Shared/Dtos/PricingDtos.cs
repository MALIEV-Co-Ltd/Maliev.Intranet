using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// DTO for a pricing snapshot.
/// </summary>
public sealed record PricingSnapshotDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the associated order ID.</summary>
    public string OrderId { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional associated quotation ID.</summary>
    public Guid? QuotationId { get; set; }
    /// <summary>Gets or sets the identifier of the employee who created the snapshot.</summary>
    public string EmployeeId { get; set; } = string.Empty;
    /// <summary>Gets or sets the manufacturing technology.</summary>
    public string Technology { get; set; } = string.Empty;
    /// <summary>Gets or sets the material code used.</summary>
    public string? MaterialCode { get; set; }
    /// <summary>Gets or sets the material brand.</summary>
    public string? MaterialBrand { get; set; }
    /// <summary>Gets or sets the layer height in mm.</summary>
    public decimal? LayerHeight { get; set; }
    /// <summary>Gets or sets the infill percentage.</summary>
    public decimal? InfillPercentage { get; set; }
    /// <summary>Gets or sets the type of support used.</summary>
    public string? SupportType { get; set; }
    /// <summary>Gets or sets the print orientation.</summary>
    public string? PrintOrientation { get; set; }
    /// <summary>Gets or sets the calculated price based on rules or ML.</summary>
    public decimal CalculatedPrice { get; set; }
    /// <summary>Gets or sets the manual override price, if any.</summary>
    public decimal? ManualOverridePrice { get; set; }
    /// <summary>Gets or sets the identifier of the associated audit record.</summary>
    public Guid PricingAuditRecordId { get; set; }
    /// <summary>Gets or sets the current status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the acceptance timestamp.</summary>
    public DateTime? AcceptedAt { get; set; }
}

/// <summary>
/// Request to create a pricing snapshot.
/// </summary>
public sealed record CreatePricingSnapshotRequest
{
    /// <summary>Gets or sets the order ID.</summary>
    public string OrderId { get; set; } = string.Empty;
    /// <summary>Gets or sets the quotation ID.</summary>
    public Guid? QuotationId { get; set; }
    /// <summary>Gets or sets the technology.</summary>
    public string Technology { get; set; } = string.Empty;
    /// <summary>Gets or sets the material code.</summary>
    public string? MaterialCode { get; set; }
    /// <summary>Gets or sets the material brand.</summary>
    public string? MaterialBrand { get; set; }
    /// <summary>Gets or sets the layer height.</summary>
    public decimal? LayerHeight { get; set; }
    /// <summary>Gets or sets the infill percentage.</summary>
    public decimal? InfillPercentage { get; set; }
    /// <summary>Gets or sets the support type.</summary>
    public string? SupportType { get; set; }
    /// <summary>Gets or sets the print orientation.</summary>
    public string? PrintOrientation { get; set; }
    /// <summary>Gets or sets the calculated price.</summary>
    public decimal CalculatedPrice { get; set; }
    /// <summary>Gets or sets the override price.</summary>
    public decimal? ManualOverridePrice { get; set; }
    /// <summary>Gets or sets the audit record identifier.</summary>
    public Guid PricingAuditRecordId { get; set; }
    /// <summary>Gets or sets the snapshot status.</summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for pricing configuration.
/// </summary>
public sealed record PricingConfigurationDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the configuration key.</summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>Gets or sets the configuration value.</summary>
    public string Value { get; set; } = string.Empty;
    /// <summary>Gets or sets the configuration description.</summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for a pricing audit record.
/// </summary>
public sealed record PricingAuditRecordDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the associated snapshot ID.</summary>
    public Guid SnapshotId { get; set; }
    /// <summary>Gets or sets the action performed.</summary>
    public string Action { get; set; } = string.Empty;
    /// <summary>Gets or sets the identifier of the actor.</summary>
    public Guid ActorId { get; set; }
    /// <summary>Gets or sets the event timestamp.</summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Request model for creating a pricing configuration.
/// </summary>
public sealed record CreatePricingConfigurationRequest
{
    /// <summary>Gets or sets the configuration key.</summary>
    [Required]
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the configuration value.</summary>
    [Required]
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets the configuration description.</summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Pricing strategy used for calculations.
/// </summary>
public enum PricingStrategy
{
    /// <summary>Calculation based on deterministic rules.</summary>
    RuleBased = 1,
    /// <summary>Calculation enhanced by machine learning models.</summary>
    MLEnhanced = 2,
    /// <summary>Price manually entered by an operator.</summary>
    Manual = 3,
    /// <summary>Combination of multiple strategies.</summary>
    Hybrid = 4
}

/// <summary>
/// Request for pricing calculation.
/// </summary>
public record PricingRequestDto
{
    /// <summary>Gets the unique identifier of the 3D file.</summary>
    public required Guid FileId { get; init; }
    /// <summary>Gets the unique identifier of the customer.</summary>
    public required Guid CustomerId { get; init; }
    /// <summary>Gets the unique identifier of the material.</summary>
    public required Guid MaterialId { get; init; }
    /// <summary>Gets the material code.</summary>
    public required string MaterialCode { get; init; }
    /// <summary>Gets the unique identifier of the manufacturing process.</summary>
    public required Guid ManufacturingProcessId { get; init; }
    /// <summary>Gets the name of the manufacturing process.</summary>
    public required string ManufacturingProcessName { get; init; }
    /// <summary>Gets the desired quantity.</summary>
    public int Quantity { get; init; } = 1;
    /// <summary>Gets the geometry metrics of the part.</summary>
    public required GeometryMetricsDto Geometry { get; init; }
    /// <summary>Gets an optional correlation identifier.</summary>
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Geometry metrics from file analysis.
/// </summary>
public record GeometryMetricsDto
{
    /// <summary>Gets the volume in cubic centimeters.</summary>
    public required decimal VolumeCm3 { get; init; }
    /// <summary>Gets the estimated support material volume in cubic centimeters.</summary>
    public required decimal SupportVolumeCm3 { get; init; }
    /// <summary>Gets the surface area in square centimeters.</summary>
    public required decimal SurfaceAreaCm2 { get; init; }
    /// <summary>Gets the bounding box X dimension in mm.</summary>
    public required decimal BoundingBoxX { get; init; }
    /// <summary>Gets the bounding box Y dimension in mm.</summary>
    public required decimal BoundingBoxY { get; init; }
    /// <summary>Gets the bounding box Z dimension in mm.</summary>
    public required decimal BoundingBoxZ { get; init; }
    /// <summary>Gets a value indicating whether the mesh is manifold.</summary>
    public required bool IsManifold { get; init; }
    /// <summary>Gets the total number of triangles in the mesh.</summary>
    public required int TriangleCount { get; init; }
}

/// <summary>
/// Result of pricing calculation.
/// </summary>
public record PricingResultDto
{
    /// <summary>Gets the pricing strategy used.</summary>
    public required PricingStrategy Strategy { get; init; }
    /// <summary>Gets the version of the ML model used, if any.</summary>
    public string? MLModelVersion { get; init; }
    /// <summary>Gets the calculated material cost.</summary>
    public required decimal MaterialCost { get; init; }
    /// <summary>Gets the calculated support material cost.</summary>
    public required decimal SupportMaterialCost { get; init; }
    /// <summary>Gets the calculated machine time cost.</summary>
    public required decimal MachineTimeCost { get; init; }
    /// <summary>Gets the base setup cost.</summary>
    public required decimal SetupCost { get; init; }
    /// <summary>Gets the surcharge for geometry complexity.</summary>
    public required decimal ComplexitySurcharge { get; init; }
    /// <summary>Gets the subtotal before applying margin.</summary>
    public required decimal SubtotalBeforeMargin { get; init; }
    /// <summary>Gets the absolute margin amount.</summary>
    public required decimal MarginAmount { get; init; }
    /// <summary>Gets the calculated unit price.</summary>
    public required decimal TotalUnitPrice { get; init; }
    /// <summary>Gets the total price for the requested quantity.</summary>
    public required decimal TotalPrice { get; init; }
    /// <summary>Gets the confidence level of the calculation (0 to 1).</summary>
    public required decimal ConfidenceLevel { get; init; }
    /// <summary>Gets the timestamp until which this price is guaranteed.</summary>
    public required DateTime ValidUntil { get; init; }
    /// <summary>Gets the duration of the pricing calculation.</summary>
    public required TimeSpan CalculationDuration { get; init; }
}
