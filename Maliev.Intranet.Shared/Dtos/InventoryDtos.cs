using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Material summary DTO for high-level stock and material directory lists.
/// </summary>
public class MaterialSummaryDto
{
    /// <summary>The unique identifier for the material record.</summary>
    public Guid Id { get; set; }
    /// <summary>The display name of the material (e.g., "ABS Filament White").</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The unique Stock Keeping Unit identifier for tracking.</summary>
    public string SKU { get; set; } = string.Empty;
    /// <summary>The broad classification of the material (e.g., Filament, Resin).</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>The current physical quantity available in stock.</summary>
    public int QuantityOnHand { get; set; }
    /// <summary>The minimum stock level before a reorder is triggered.</summary>
    public int ReorderLevel { get; set; }
    /// <summary>The current unit price for material costing.</summary>
    public decimal UnitPrice { get; set; }
    /// <summary>The current status of the material (e.g., InStock, OutOfStock, Discontinued).</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>The unit of measurement for the material (e.g., "kg", "pcs").</summary>
    public string Unit { get; set; } = "pcs";
}

/// <summary>
/// Detailed material information for inventory management and procurement.
/// </summary>
public class MaterialDetailDto
{
    /// <summary>The unique identifier for the material record.</summary>
    public Guid Id { get; set; }
    /// <summary>The display name of the material.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The unique Stock Keeping Unit identifier.</summary>
    public string SKU { get; set; } = string.Empty;
    /// <summary>The broad classification category.</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>A detailed description of the material's properties and use cases.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>The current unit price for procurement.</summary>
    public decimal UnitPrice { get; set; }
    /// <summary>The current physical quantity available in stock.</summary>
    public int QuantityOnHand { get; set; }
    /// <summary>The minimum stock level before a reorder is triggered.</summary>
    public int ReorderLevel { get; set; }
    /// <summary>The unit of measurement.</summary>
    public string Unit { get; set; } = "pcs";
    /// <summary>The current availability status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Manufacturing process names that support this material.</summary>
    public List<string> ManufacturingProcesses { get; set; } = [];
    /// <summary>Specific type of material (e.g., PLA, PETG).</summary>
    public string? MaterialType { get; set; }
    /// <summary>The color of the material, if applicable.</summary>
    public string? Color { get; set; }
    /// <summary>Available material color options with display swatches.</summary>
    public List<MaterialColorDto> Colors { get; set; } = [];
    /// <summary>The manufacturer or brand of the material.</summary>
    public string? Brand { get; set; }
    /// <summary>The weight of a single unit of material, if applicable.</summary>
    public decimal? Weight { get; set; }
    /// <summary>List of technical properties or specifications (e.g., Print Temperature).</summary>
    public List<MaterialPropertyDto> Properties { get; set; } = [];
    /// <summary>List of suppliers that provide this material.</summary>
    public List<SupplierSummaryDto> Suppliers { get; set; } = [];
    /// <summary>List of recent stock movement transactions (e.g., receipts, issues).</summary>
    public List<StockTransactionDto> RecentTransactions { get; set; } = [];
    /// <summary>The date and time when the material record was created.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>The date and time when the record was last updated.</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Represents an available material color and its display color.
/// </summary>
public class MaterialColorDto
{
    /// <summary>The unique identifier for the color option.</summary>
    public Guid Id { get; set; }
    /// <summary>The display name of the color.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The optional CSS-compatible hex color code.</summary>
    public string? HexCode { get; set; }
}

/// <summary>
/// Represents a technical property or specification for a material.
/// </summary>
public class MaterialPropertyDto
{
    /// <summary>The name of the property (e.g., "Density").</summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>The value of the property (e.g., "1.25").</summary>
    public string Value { get; set; } = string.Empty;
    /// <summary>The optional unit of measurement for the property value (e.g., "g/cm3").</summary>
    public string? Unit { get; set; }
}

/// <summary>
/// Represents a single movement or adjustment of material stock.
/// </summary>
public class StockTransactionDto
{
    /// <summary>The unique identifier for the transaction record.</summary>
    public Guid Id { get; set; }
    /// <summary>The type of stock movement (e.g., Receipt, Issue, Adjustment, Transfer).</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>The quantity of material moved. Positive for additions, negative for reductions.</summary>
    public int Quantity { get; set; }
    /// <summary>A reference to a source document or event (e.g., Purchase Order ID).</summary>
    public string Reference { get; set; } = string.Empty;
    /// <summary>The name or identifier of the user who performed the transaction.</summary>
    public string PerformedBy { get; set; } = string.Empty;
    /// <summary>The date and time when the transaction occurred.</summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Request model for receiving a managed inventory batch from purchasing, supplier delivery, or manual stock intake.
/// </summary>
public sealed record CreateInventoryBatchRequest
{
    /// <summary>The material identifier this physical batch belongs to.</summary>
    [Required]
    public Guid MaterialId { get; set; }

    /// <summary>The received batch quantity measured in grams for traceable raw stock deduction.</summary>
    [Range(0.01, double.MaxValue)]
    public decimal InitialWeightGrams { get; set; }

    /// <summary>The storage location where the batch is physically kept.</summary>
    [Required]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    /// <summary>The optional low-stock threshold for the batch in grams.</summary>
    [Range(0, double.MaxValue)]
    public decimal? LowStockThresholdGrams { get; set; }
}

/// <summary>
/// Response DTO for a traceable physical material batch managed by InventoryService.
/// </summary>
public sealed record InventoryBatchDto
{
    /// <summary>The unique batch identifier used for QR or barcode labels.</summary>
    public Guid Id { get; init; }

    /// <summary>The material identifier this batch belongs to.</summary>
    public Guid MaterialId { get; init; }

    /// <summary>The originally received batch weight in grams.</summary>
    public decimal InitialWeightGrams { get; init; }

    /// <summary>The current remaining batch weight in grams.</summary>
    public decimal RemainingWeightGrams { get; init; }

    /// <summary>The current batch lifecycle status.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>The physical storage location for this batch.</summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>The low-stock threshold for alerting on this batch.</summary>
    public decimal LowStockThresholdGrams { get; init; }

    /// <summary>The timestamp when the batch was received.</summary>
    public DateTimeOffset ReceivedAt { get; init; }
}

/// <summary>
/// Request model for receiving one QR-tracked physical material item.
/// </summary>
public sealed record CreateInventoryItemRequest
{
    /// <summary>The material identifier this physical item belongs to.</summary>
    [Required]
    public Guid MaterialId { get; set; }

    /// <summary>The original quantity received for this item.</summary>
    [Range(0.01, double.MaxValue)]
    public decimal InitialQuantity { get; set; }

    /// <summary>The quantity unit, such as g, kg, m, pcs, spool, or block.</summary>
    [Required]
    [StringLength(16)]
    public string QuantityUnit { get; set; } = "g";

    /// <summary>The physical storage location where this item is kept.</summary>
    [Required]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    /// <summary>The physical form factor, such as spool, block, sheet, bar, bottle, or bag.</summary>
    [Required]
    [StringLength(32)]
    public string FormFactor { get; set; } = "Spool";

    /// <summary>The optional low-stock alert threshold in the same unit as the item quantity.</summary>
    [Range(0, double.MaxValue)]
    public decimal? LowStockThresholdQuantity { get; set; }

    /// <summary>The supplier that provided this item, when known.</summary>
    public Guid? SupplierId { get; set; }

    /// <summary>The purchase order linked to this item, when known.</summary>
    public Guid? PurchaseOrderId { get; set; }

    /// <summary>The supplier or manufacturer lot number.</summary>
    [StringLength(100)]
    public string? LotNumber { get; set; }

    /// <summary>The manufacturer SKU or catalog code.</summary>
    [StringLength(100)]
    public string? ManufacturerSku { get; set; }

    /// <summary>The visible material color, when useful for shop-floor picking.</summary>
    [StringLength(80)]
    public string? Color { get; set; }

    /// <summary>The grade or specification for this exact piece of material.</summary>
    [StringLength(120)]
    public string? MaterialGrade { get; set; }

    /// <summary>The material length in millimeters for stock pieces.</summary>
    [Range(0, double.MaxValue)]
    public decimal? LengthMm { get; set; }

    /// <summary>The material width in millimeters for stock pieces.</summary>
    [Range(0, double.MaxValue)]
    public decimal? WidthMm { get; set; }

    /// <summary>The material height in millimeters for stock pieces.</summary>
    [Range(0, double.MaxValue)]
    public decimal? HeightMm { get; set; }

    /// <summary>The material diameter in millimeters for round stock or filament.</summary>
    [Range(0, double.MaxValue)]
    public decimal? DiameterMm { get; set; }

    /// <summary>The material thickness in millimeters for sheets.</summary>
    [Range(0, double.MaxValue)]
    public decimal? ThicknessMm { get; set; }

    /// <summary>The employee or integration that received this item.</summary>
    [StringLength(120)]
    public string? ReceivedBy { get; set; }
}

/// <summary>
/// Request model for consuming material from one exact QR-tracked inventory item.
/// </summary>
public sealed record ConsumeInventoryItemRequest
{
    /// <summary>The production job that consumed this item, when available.</summary>
    public Guid? JobId { get; set; }

    /// <summary>The order item that consumed this item, when available.</summary>
    public Guid? OrderItemId { get; set; }

    /// <summary>The operator who scanned or consumed this item.</summary>
    [StringLength(120)]
    public string? OperatorId { get; set; }

    /// <summary>The machine that consumed this item.</summary>
    [StringLength(120)]
    public string? MachineId { get; set; }

    /// <summary>The quantity consumed from this item, in the item quantity unit.</summary>
    [Range(0.01, double.MaxValue)]
    public decimal QuantityConsumed { get; set; }

    /// <summary>Optional notes about the consumption event.</summary>
    [StringLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// Response DTO for one QR-tracked physical material item.
/// </summary>
public sealed record InventoryItemDto
{
    /// <summary>The unique inventory item identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>The material identifier this item belongs to.</summary>
    public Guid MaterialId { get; init; }

    /// <summary>The short shop-floor tracking code encoded into the QR label.</summary>
    public string TrackingCode { get; init; } = string.Empty;

    /// <summary>The QR payload used by scanners and printed labels.</summary>
    public string QrPayload { get; init; } = string.Empty;

    /// <summary>The original received quantity for this item.</summary>
    public decimal InitialQuantity { get; init; }

    /// <summary>The remaining quantity for this item.</summary>
    public decimal RemainingQuantity { get; init; }

    /// <summary>The item quantity unit.</summary>
    public string QuantityUnit { get; init; } = "g";

    /// <summary>The original weight in grams for weight-managed materials.</summary>
    public decimal InitialWeightGrams { get; init; }

    /// <summary>The remaining weight in grams for weight-managed materials.</summary>
    public decimal RemainingWeightGrams { get; init; }

    /// <summary>The current item lifecycle status.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>The physical storage location for this item.</summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>The item form factor, such as spool, block, sheet, bar, bottle, or bag.</summary>
    public string FormFactor { get; init; } = "Spool";

    /// <summary>The optional low-stock threshold in the item quantity unit.</summary>
    public decimal? LowStockThresholdQuantity { get; init; }

    /// <summary>The supplier that provided this item, when known.</summary>
    public Guid? SupplierId { get; init; }

    /// <summary>The purchase order linked to this item, when known.</summary>
    public Guid? PurchaseOrderId { get; init; }

    /// <summary>The supplier or manufacturer lot number.</summary>
    public string? LotNumber { get; init; }

    /// <summary>The manufacturer SKU or catalog code.</summary>
    public string? ManufacturerSku { get; init; }

    /// <summary>The visible material color, when useful for shop-floor picking.</summary>
    public string? Color { get; init; }

    /// <summary>The grade or specification for this exact piece of material.</summary>
    public string? MaterialGrade { get; init; }

    /// <summary>The material length in millimeters for stock pieces.</summary>
    public decimal? LengthMm { get; init; }

    /// <summary>The material width in millimeters for stock pieces.</summary>
    public decimal? WidthMm { get; init; }

    /// <summary>The material height in millimeters for stock pieces.</summary>
    public decimal? HeightMm { get; init; }

    /// <summary>The material diameter in millimeters for round stock or filament.</summary>
    public decimal? DiameterMm { get; init; }

    /// <summary>The material thickness in millimeters for sheets.</summary>
    public decimal? ThicknessMm { get; init; }

    /// <summary>The employee or integration that received this item.</summary>
    public string? ReceivedBy { get; init; }

    /// <summary>The timestamp when this item was received.</summary>
    public DateTimeOffset ReceivedAt { get; init; }
}

/// <summary>
/// Summary of active inventory batches for one material.
/// </summary>
public sealed record MaterialInventoryStatusDto
{
    /// <summary>The material identifier being summarized.</summary>
    public Guid MaterialId { get; init; }

    /// <summary>The number of active batches that still have stock.</summary>
    public int ActiveBatches { get; init; }

    /// <summary>The total remaining active stock in grams.</summary>
    public decimal TotalRemainingGrams { get; init; }

    /// <summary>The remaining grams of the lowest active batch.</summary>
    public decimal LowestBatchGrams { get; init; }

    /// <summary>Whether any active batch has crossed its low-stock threshold.</summary>
    public bool HasLowStockAlert { get; init; }
}

/// <summary>
/// Summary DTO for a material supplier, used in lists and selection.
/// </summary>
public class SupplierSummaryDto
{
    /// <summary>The unique identifier for the supplier record.</summary>
    public Guid Id { get; set; }
    /// <summary>The legal display name of the supplier.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The supplier tax identification number.</summary>
    public string? TaxId { get; set; }
    /// <summary>The city where the supplier is located.</summary>
    public string? City { get; set; }
    /// <summary>The country where the supplier is based.</summary>
    public string? Country { get; set; }
    /// <summary>The primary contact email address for orders.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>The current business status of the supplier (e.g., Active, Restricted).</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>The calculated rating or performance score of the supplier (0.0 to 5.0).</summary>
    public decimal Rating { get; set; }
    /// <summary>The supplier capability names that matched this material search.</summary>
    public List<string> Capabilities { get; set; } = [];
    /// <summary>The reason this supplier is shown for the current material.</summary>
    public string? MatchReason { get; set; }
}

/// <summary>
/// Detailed supplier information for procurement and relationship management.
/// </summary>
public class SupplierDetailDto
{
    /// <summary>The unique identifier for the supplier record.</summary>
    public Guid Id { get; set; }
    /// <summary>The legal display name of the supplier.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The supplier tax identification number.</summary>
    public string? TaxId { get; set; }
    /// <summary>The primary contact email address.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>The primary contact phone number.</summary>
    public string? Phone { get; set; }
    /// <summary>The country where the supplier is based.</summary>
    public string Country { get; set; } = string.Empty;
    /// <summary>The full physical address of the supplier.</summary>
    public string? Address { get; set; }
    /// <summary>The city where the supplier is located.</summary>
    public string? City { get; set; }
    /// <summary>The postal code for the supplier address.</summary>
    public string? PostalCode { get; set; }
    /// <summary>The current business status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>The current performance rating of the supplier.</summary>
    public decimal Rating { get; set; }
    /// <summary>The name of the primary contact person at the supplier company.</summary>
    public string? ContactPerson { get; set; }
    /// <summary>The official website URL of the supplier.</summary>
    public string? Website { get; set; }
    /// <summary>The supplier capabilities or supported service categories.</summary>
    public List<string> Capabilities { get; set; } = [];
    /// <summary>The row version used for optimistic concurrency control.</summary>
    public string RowVersion { get; set; } = string.Empty;
    /// <summary>The current onboarding stage for supplier readiness.</summary>
    public string OnboardingStage { get; set; } = string.Empty;
    /// <summary>The supplier compliance and lifecycle documents.</summary>
    public List<SupplierDocumentDto> Documents { get; set; } = [];
    /// <summary>The date and time when the supplier record was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Supplier document or certification metadata managed by SupplierService.
/// </summary>
public class SupplierDocumentDto
{
    /// <summary>The unique identifier for the supplier document metadata.</summary>
    public Guid Id { get; set; }
    /// <summary>The document classification from SupplierService.</summary>
    public string DocumentType { get; set; } = string.Empty;
    /// <summary>The human-readable document name.</summary>
    public string DocumentName { get; set; } = string.Empty;
    /// <summary>The document issue date, when known.</summary>
    public DateOnly? IssueDate { get; set; }
    /// <summary>The optional expiration date for lifecycle tracking.</summary>
    public DateOnly? ExpirationDate { get; set; }
    /// <summary>The upload service file reference or storage path.</summary>
    public string? ExternalFileRef { get; set; }
    /// <summary>Whether the document has expired.</summary>
    public bool IsExpired { get; set; }
    /// <summary>Whether the document is within the expiring-soon threshold.</summary>
    public bool IsExpiringSoon { get; set; }
    /// <summary>The date and time when the metadata was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request model for creating a new material record.
/// </summary>
public sealed record CreateMaterialRequest
{
    /// <summary>The display name for the new material.</summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>The unique Stock Keeping Unit identifier for the material.</summary>
    [Required]
    public string SKU { get; set; } = string.Empty;

    /// <summary>The classification category for the material.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>A descriptive text for the material.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>The initial unit price for the material.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>The initial quantity available on hand.</summary>
    public int QuantityOnHand { get; set; }

    /// <summary>The default unit of measurement (e.g., "kg").</summary>
    public string Unit { get; set; } = "pcs";
}

/// <summary>
/// Request model for updating specific fields of an existing material record.
/// </summary>
public sealed record UpdateMaterialRequest
{
    /// <summary>The updated material SKU or code.</summary>
    public string? SKU { get; set; }
    /// <summary>The updated display name.</summary>
    public string? Name { get; set; }
    /// <summary>The updated description.</summary>
    public string? Description { get; set; }
    /// <summary>The updated category.</summary>
    public string? Category { get; set; }
    /// <summary>The updated unit price.</summary>
    public decimal? UnitPrice { get; set; }
    /// <summary>An optional adjustment to the physical quantity on hand.</summary>
    public int? QuantityOnHand { get; set; }
    /// <summary>The new availability status.</summary>
    public string? Status { get; set; }
    /// <summary>The updated unit of measurement.</summary>
    public string? Unit { get; set; }
    /// <summary>The selected color identifiers for this material; null preserves the existing color links.</summary>
    public List<Guid>? ColorIds { get; set; }
}

/// <summary>
/// Represents a single entry in a material's historical usage log.
/// </summary>
public class MaterialUsageHistoryDto
{
    /// <summary>The date and time when the material was consumed.</summary>
    public DateTime Date { get; set; }

    /// <summary>The production or work order identifier that triggered the usage.</summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>The decimal quantity of material consumed.</summary>
    public decimal QuantityUsed { get; set; }

    /// <summary>The name or identifier of the person who reported the usage.</summary>
    public string UsedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request model for onboarding a new material supplier.
/// </summary>
public sealed record CreateSupplierRequest
{
    /// <summary>The legal name of the new supplier.</summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>The supplier tax identification number.</summary>
    [Required]
    public string TaxId { get; set; } = string.Empty;

    /// <summary>The primary contact email address for orders.</summary>
    [Required]
    public string Email { get; set; } = string.Empty;

    /// <summary>The contact phone number.</summary>
    public string? Phone { get; set; }
    /// <summary>The country where the supplier is headquartered.</summary>
    public string Country { get; set; } = string.Empty;
    /// <summary>The primary business address.</summary>
    public string? Address { get; set; }
    /// <summary>The city where the supplier is located.</summary>
    [Required]
    public string City { get; set; } = string.Empty;
    /// <summary>The postal code for the supplier address.</summary>
    public string? PostalCode { get; set; }
    /// <summary>The name of the primary contact person.</summary>
    public string? ContactPerson { get; set; }
    /// <summary>The official website URL.</summary>
    public string? Website { get; set; }
    /// <summary>The supplier capabilities or supported service categories.</summary>
    public List<string> Capabilities { get; set; } = [];
}

/// <summary>
/// Request model for adding a managed document to an existing supplier.
/// </summary>
public sealed record CreateSupplierDocumentRequest
{
    /// <summary>The SupplierService document classification.</summary>
    [Required]
    public string DocumentType { get; set; } = "BusinessLicense";

    /// <summary>The document display name.</summary>
    [Required]
    public string DocumentName { get; set; } = string.Empty;

    /// <summary>The document issue date.</summary>
    public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>The optional expiration date.</summary>
    public DateOnly? ExpirationDate { get; set; }

    /// <summary>The external file reference or storage path returned by UploadService.</summary>
    public string? ExternalFileRef { get; set; }

    /// <summary>Optional lifecycle notes.</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Request model for updating an existing supplier's details.
/// </summary>
public sealed record UpdateSupplierRequest
{
    /// <summary>The updated legal name.</summary>
    public string? Name { get; set; }
    /// <summary>The updated contact email address.</summary>
    public string? Email { get; set; }
    /// <summary>The updated phone number.</summary>
    public string? Phone { get; set; }
    /// <summary>The updated country.</summary>
    public string? Country { get; set; }
    /// <summary>The updated physical address.</summary>
    public string? Address { get; set; }
    /// <summary>The updated contact person name.</summary>
    public string? ContactPerson { get; set; }
    /// <summary>The updated website URL.</summary>
    public string? Website { get; set; }
    /// <summary>The updated city where the supplier is located.</summary>
    public string? City { get; set; }
    /// <summary>The updated postal code for the supplier address.</summary>
    public string? PostalCode { get; set; }
    /// <summary>The updated supplier capabilities or supported service categories.</summary>
    public List<string>? Capabilities { get; set; }
    /// <summary>The row version required by SupplierService for optimistic concurrency control.</summary>
    public string? RowVersion { get; set; }
}

/// <summary>
/// Response model containing supplier data extracted by AI analysis.
/// </summary>
public class ExtractedSupplierDataResponse
{
    /// <summary>The extracted supplier company name.</summary>
    [JsonPropertyName("supplier_name")]
    public string? SupplierName { get; set; }

    /// <summary>The extracted supplier tax identification number.</summary>
    [JsonPropertyName("tax_id")]
    public string? TaxId { get; set; }

    /// <summary>The extracted supplier email address.</summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>The extracted supplier phone number.</summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>The extracted primary contact person.</summary>
    [JsonPropertyName("contact_person")]
    public string? ContactPerson { get; set; }

    /// <summary>The extracted country.</summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>The extracted primary address.</summary>
    [JsonPropertyName("address")]
    public ExtractedSupplierAddress? Address { get; set; }

    /// <summary>The extracted supplier capabilities.</summary>
    [JsonPropertyName("capabilities")]
    public List<string> Capabilities { get; set; } = [];

    /// <summary>The document types inferred from supplied text or files.</summary>
    [JsonPropertyName("document_types")]
    public List<string> DocumentTypes { get; set; } = [];

    /// <summary>Overall AI confidence score for the extraction.</summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

/// <summary>
/// Supplier address extracted by AI and optionally corrected with registry data.
/// </summary>
public class ExtractedSupplierAddress
{
    /// <summary>The street-level address or full address line.</summary>
    [JsonPropertyName("address_line_1")]
    public string? AddressLine1 { get; set; }

    /// <summary>The sub-district or tambon, when available.</summary>
    [JsonPropertyName("district")]
    public string? District { get; set; }

    /// <summary>The district/city value.</summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>The province or state value.</summary>
    [JsonPropertyName("state_province")]
    public string? StateProvince { get; set; }

    /// <summary>The postal code.</summary>
    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; set; }

    /// <summary>The matched Thai registry location, if one was resolved.</summary>
    [JsonPropertyName("location")]
    public RegistryThaiLocation? Location { get; set; }
}
