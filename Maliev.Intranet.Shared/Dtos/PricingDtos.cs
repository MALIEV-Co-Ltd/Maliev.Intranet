using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Represents a point-in-time snapshot of pricing details for an order or quotation.
/// </summary>
public sealed record PricingSnapshotDto
{
    /// <summary>The unique identifier of the pricing snapshot.</summary>
    public Guid Id { get; set; }

    /// <summary>The associated order identifier.</summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>The optional quotation identifier this snapshot was derived from.</summary>
    public Guid? QuotationId { get; set; }

    /// <summary>The identifier of the employee who generated or accepted the pricing.</summary>
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>The manufacturing technology used (e.g., FDM, SLA, CNC).</summary>
    public string Technology { get; set; } = string.Empty;

    /// <summary>The material code used for the calculation.</summary>
    public string? MaterialCode { get; set; }

    /// <summary>The brand or manufacturer of the material.</summary>
    public string? MaterialBrand { get; set; }

    /// <summary>The layer thickness in millimeters used for 3D printing.</summary>
    public decimal? LayerHeight { get; set; }

    /// <summary>The internal density percentage for 3D printed parts.</summary>
    public decimal? InfillPercentage { get; set; }

    /// <summary>The type of support structure material or geometry.</summary>
    public string? SupportType { get; set; }

    /// <summary>The orientation of the part during the manufacturing process.</summary>
    public string? PrintOrientation { get; set; }

    /// <summary>The system-calculated price based on the selected parameters.</summary>
    public decimal CalculatedPrice { get; set; }

    /// <summary>A manually adjusted price that overrides the calculated value.</summary>
    public decimal? ManualOverridePrice { get; set; }

    /// <summary>The identifier of the audit record tracking this pricing event.</summary>
    public Guid PricingAuditRecordId { get; set; }

    /// <summary>The current status of the pricing snapshot (e.g., Draft, Accepted, Expired).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>The date and time when the snapshot was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>The date and time when the pricing was officially accepted by a user.</summary>
    public DateTime? AcceptedAt { get; set; }
}

/// <summary>
/// Data required to create a new pricing snapshot for an order.
/// </summary>
public sealed record CreatePricingSnapshotRequest
{
    /// <summary>The associated order identifier.</summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>The optional quotation identifier to link with this snapshot.</summary>
    public Guid? QuotationId { get; set; }

    /// <summary>The manufacturing technology to apply.</summary>
    public string Technology { get; set; } = string.Empty;

    /// <summary>The material code to use for calculation.</summary>
    public string? MaterialCode { get; set; }

    /// <summary>The material brand preference.</summary>
    public string? MaterialBrand { get; set; }

    /// <summary>The requested layer height in millimeters.</summary>
    public decimal? LayerHeight { get; set; }

    /// <summary>The requested infill percentage.</summary>
    public decimal? InfillPercentage { get; set; }

    /// <summary>The requested support structure type.</summary>
    public string? SupportType { get; set; }

    /// <summary>The intended part orientation.</summary>
    public string? PrintOrientation { get; set; }

    /// <summary>The initial calculated price.</summary>
    public decimal CalculatedPrice { get; set; }

    /// <summary>An optional manual price override.</summary>
    public decimal? ManualOverridePrice { get; set; }

    /// <summary>The identifier for the initial audit record.</summary>
    public Guid PricingAuditRecordId { get; set; }

    /// <summary>The initial status for the new snapshot.</summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents a system-wide pricing configuration setting.
/// </summary>
public sealed record PricingConfigurationDto
{
    /// <summary>The unique identifier of the configuration entry.</summary>
    public Guid Id { get; set; }

    /// <summary>The unique key identifying the configuration setting (e.g., "BaseMarkup").</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>The current value of the configuration setting.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>A detailed description of what this setting controls and its impact.</summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents an audit trail entry for pricing-related actions.
/// </summary>
public sealed record PricingAuditRecordDto
{
    /// <summary>The unique identifier of the audit record.</summary>
    public Guid Id { get; set; }

    /// <summary>The identifier of the pricing snapshot that was affected.</summary>
    public Guid SnapshotId { get; set; }

    /// <summary>The type of action performed (e.g., "Created", "Modified", "Accepted").</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>The identifier of the user or system actor who performed the action.</summary>
    public Guid ActorId { get; set; }

    /// <summary>The date and time when the action occurred.</summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Data required to create or update a pricing configuration setting.
/// </summary>
public sealed record CreatePricingConfigurationRequest
{
    /// <summary>The unique key identifying the configuration setting.</summary>
    [Required]
    public string Key { get; set; } = string.Empty;

    /// <summary>The value to assign to the configuration setting.</summary>
    [Required]
    public string Value { get; set; } = string.Empty;

    /// <summary>A description of the configuration setting's purpose.</summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Specifies the computational strategy used to determine part pricing.
/// </summary>
public enum PricingStrategy
{
    /// <summary>Pricing derived from a set of deterministic business rules and static lookups.</summary>
    RuleBased = 1,

    /// <summary>Pricing enhanced or predicted by a machine learning model based on historical data.</summary>
    MLEnhanced = 2,

    /// <summary>Pricing manually entered or overridden by an authorized employee.</summary>
    Manual = 3,

    /// <summary>A combination of rule-based logic and manual adjustments.</summary>
    Hybrid = 4
}

/// <summary>
/// Input data for requesting a real-time pricing calculation for a manufactured part.
/// </summary>
public record PricingRequestDto
{
    /// <summary>The identifier of the uploaded geometry file to analyze.</summary>
    public required Guid FileId { get; init; }

    /// <summary>The identifier of the customer for whom the pricing is calculated.</summary>
    public required Guid CustomerId { get; init; }

    /// <summary>The identifier of the specific material requested.</summary>
    public required Guid MaterialId { get; init; }

    /// <summary>The internal code for the selected material.</summary>
    public required string MaterialCode { get; init; }

    /// <summary>The identifier of the manufacturing process to be used.</summary>
    public required Guid ManufacturingProcessId { get; init; }

    /// <summary>The display name of the manufacturing process.</summary>
    public required string ManufacturingProcessName { get; init; }

    /// <summary>The number of units to be produced, used for volume discounting.</summary>
    public int Quantity { get; init; } = 1;

    /// <summary>Physical geometry metrics extracted from the source file.</summary>
    public required GeometryMetricsDto Geometry { get; init; }

    /// <summary>An optional identifier for tracing the request across microservices.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>The GCS storage path of the 3D file, used as a join key for SignalR events.</summary>
    public string? StoragePath { get; init; }
}

/// <summary>
/// Physical metrics and analysis results for a 3D geometry model.
/// </summary>
public record GeometryMetricsDto
{
    /// <summary>The total displacement volume of the part in cubic centimeters.</summary>
    public required decimal VolumeCm3 { get; init; }

    /// <summary>The estimated volume of sacrificial support material required in cubic centimeters.</summary>
    public required decimal SupportVolumeCm3 { get; init; }

    /// <summary>The total external surface area of the geometry in square centimeters.</summary>
    public required decimal SurfaceAreaCm2 { get; init; }

    /// <summary>The bounding box dimension along the X-axis in millimeters.</summary>
    public required decimal BoundingBoxX { get; init; }

    /// <summary>The bounding box dimension along the Y-axis in millimeters.</summary>
    public required decimal BoundingBoxY { get; init; }

    /// <summary>The bounding box dimension along the Z-axis in millimeters.</summary>
    public required decimal BoundingBoxZ { get; init; }

    /// <summary>Indicates if the geometry is a closed, water-tight manifold suitable for manufacturing.</summary>
    public required bool IsManifold { get; init; }

    /// <summary>The total number of triangular facets in the mesh.</summary>
    public required int TriangleCount { get; init; }
}

/// <summary>
/// The detailed output of a pricing calculation, including cost breakdowns and margins.
/// </summary>
public record PricingResultDto
{
    /// <summary>The strategy used to calculate this result.</summary>
    public required PricingStrategy Strategy { get; init; }

    /// <summary>The version of the machine learning model used, if applicable.</summary>
    public string? MLModelVersion { get; init; }

    /// <summary>The raw cost of the primary manufacturing material.</summary>
    public required decimal MaterialCost { get; init; }

    /// <summary>The cost of the support material required for the build.</summary>
    public required decimal SupportMaterialCost { get; init; }

    /// <summary>The estimated cost derived from machine utilization time.</summary>
    public required decimal MachineTimeCost { get; init; }

    /// <summary>The fixed setup or preparation cost for the manufacturing job.</summary>
    public required decimal SetupCost { get; init; }

    /// <summary>An additional charge based on the geometrical complexity of the part.</summary>
    public required decimal ComplexitySurcharge { get; init; }

    /// <summary>The total internal cost before applying business margins.</summary>
    public required decimal SubtotalBeforeMargin { get; init; }

    /// <summary>The profit margin amount added to the internal cost.</summary>
    public required decimal MarginAmount { get; init; }

    /// <summary>The final calculated price for a single unit.</summary>
    public required decimal TotalUnitPrice { get; init; }

    /// <summary>The total price for the requested quantity, including all line items.</summary>
    public required decimal TotalPrice { get; init; }

    /// <summary>A value between 0 and 1 indicating the system's confidence in the price accuracy.</summary>
    public required decimal ConfidenceLevel { get; init; }

    /// <summary>The date and time until which this pricing result remains valid.</summary>
    public required DateTime ValidUntil { get; init; }

    /// <summary>The time taken by the system to perform the pricing calculation.</summary>
    public required TimeSpan CalculationDuration { get; init; }

    /// <summary>Estimated days for production and shipping, based on current machine capacity.</summary>
    public int? EstimatedLeadTimeDays { get; init; }
}

/// <summary>
/// The raw response from the PricingService calculate endpoint.
/// This is a simplified result that the PricingService currently returns.
/// </summary>
public record PricingServiceCalculateResponse
{
    /// <summary>The unit price calculated by the PricingService.</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>The total amount for the requested quantity.</summary>
    public decimal TotalAmount { get; init; }

    /// <summary>The confidence score of the pricing calculation.</summary>
    public decimal ConfidenceScore { get; init; }

    /// <summary>The name of the pricing engine used.</summary>
    public string EngineName { get; init; } = string.Empty;

    /// <summary>The audit record identifier.</summary>
    public Guid AuditId { get; init; }

    /// <summary>Estimated lead time in days.</summary>
    public int EstimatedLeadTimeDays { get; init; }
}
