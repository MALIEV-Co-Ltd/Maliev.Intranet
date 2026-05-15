using System.ComponentModel.DataAnnotations;

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
/// Summary DTO for a material supplier, used in lists and selection.
/// </summary>
public class SupplierSummaryDto
{
    /// <summary>The unique identifier for the supplier record.</summary>
    public Guid Id { get; set; }
    /// <summary>The legal display name of the supplier.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The primary contact email address for orders.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>The current business status of the supplier (e.g., Active, Restricted).</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>The calculated rating or performance score of the supplier (0.0 to 5.0).</summary>
    public decimal Rating { get; set; }
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
    /// <summary>The primary contact email address.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>The primary contact phone number.</summary>
    public string? Phone { get; set; }
    /// <summary>The country where the supplier is based.</summary>
    public string Country { get; set; } = string.Empty;
    /// <summary>The full physical address of the supplier.</summary>
    public string? Address { get; set; }
    /// <summary>The current business status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>The current performance rating of the supplier.</summary>
    public decimal Rating { get; set; }
    /// <summary>The name of the primary contact person at the supplier company.</summary>
    public string? ContactPerson { get; set; }
    /// <summary>The official website URL of the supplier.</summary>
    public string? Website { get; set; }
    /// <summary>The date and time when the supplier record was created.</summary>
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
}
