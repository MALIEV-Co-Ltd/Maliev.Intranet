using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Material summary DTO.
/// </summary>
public class MaterialSummaryDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the material name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the unique code identifier.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the quantity currently in stock.</summary>
    public int StockLevel { get; set; }
    /// <summary>Gets or sets the price per unit.</summary>
    public decimal PricePerUnit { get; set; }
    /// <summary>Gets or sets a value indicating whether the material is active.</summary>
    public bool Active { get; set; }
}

/// <summary>
/// Detailed material information.
/// </summary>
public class MaterialDetailDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the material name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the unique code identifier.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the detailed description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the price per unit.</summary>
    public decimal PricePerUnit { get; set; }
    /// <summary>Gets or sets the current stock level.</summary>
    public int StockLevel { get; set; }
    /// <summary>Gets or sets the material density in g/cm³.</summary>
    public decimal Density { get; set; }
    /// <summary>Gets or sets the cost per kilogram in THB.</summary>
    public decimal CostPerKg { get; set; }
    /// <summary>Gets or sets the technology-specific process parameters.</summary>
    public Dictionary<string, string> ProcessParameters { get; set; } = [];
    /// <summary>Gets or sets the supplier ID if associated.</summary>
    public Guid? SupplierId { get; set; }
    /// <summary>Gets or sets the supplier name if associated.</summary>
    public string? SupplierName { get; set; }
    /// <summary>Gets or sets the creator identifier.</summary>
    public string CreatedBy { get; set; } = string.Empty;
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
    /// <summary>Gets or sets a value indicating whether the material is active.</summary>
    public bool Active { get; set; }
    /// <summary>Gets or sets the version number for concurrency control.</summary>
    public byte[] Version { get; set; } = [];
    /// <summary>Gets or sets the list of material properties.</summary>
    public List<MaterialPropertyDto> Properties { get; set; } = [];
    /// <summary>Gets or sets the list of associated suppliers.</summary>
    public List<SupplierSummaryDto> Suppliers { get; set; } = [];
    /// <summary>Gets or sets the list of recent stock transactions.</summary>
    public List<StockTransactionDto> RecentTransactions { get; set; } = [];
}

/// <summary>
/// Data transfer object for a material property.
/// </summary>
public class MaterialPropertyDto
{
    /// <summary>Gets or sets the property key.</summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>Gets or sets the property value.</summary>
    public string Value { get; set; } = string.Empty;
    /// <summary>Gets or sets the property unit of measure.</summary>
    public string? Unit { get; set; }
}

/// <summary>
/// Data transfer object for a stock transaction.
/// </summary>
public class StockTransactionDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the transaction type (e.g., Receipt, Issue).</summary>
    public string Type { get; set; } = string.Empty; // Receipt, Issue, Adjustment, Transfer
    /// <summary>Gets or sets the transaction quantity.</summary>
    public int Quantity { get; set; }
    /// <summary>Gets or sets the transaction reference identifier.</summary>
    public string Reference { get; set; } = string.Empty;
    /// <summary>Gets or sets the name of the person who performed the transaction.</summary>
    public string PerformedBy { get; set; } = string.Empty;
    /// <summary>Gets or sets the transaction timestamp.</summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Supplier summary DTO.
/// </summary>
public class SupplierSummaryDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the legal name of the supplier company.</summary>
    public string CompanyName { get; set; } = string.Empty;
    /// <summary>Gets or sets the tax identification number of the supplier.</summary>
    public string TaxId { get; set; } = string.Empty;
    /// <summary>Gets or sets the current status of the supplier.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the country where the supplier is located.</summary>
    public string Country { get; set; } = string.Empty;
}

/// <summary>
/// Detailed supplier information.
/// </summary>
public class SupplierDetailDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the legal name of the supplier company.</summary>
    public string CompanyName { get; set; } = string.Empty;
    /// <summary>Gets or sets the tax identification number of the supplier.</summary>
    public string TaxId { get; set; } = string.Empty;
    /// <summary>Gets or sets the street address of the supplier.</summary>
    public string Address { get; set; } = string.Empty;
    /// <summary>Gets or sets the city where the supplier is located.</summary>
    public string City { get; set; } = string.Empty;
    /// <summary>Gets or sets the country where the supplier is located.</summary>
    public string Country { get; set; } = string.Empty;
    /// <summary>Gets or sets the postal code for the supplier's address.</summary>
    public string? PostalCode { get; set; }
    /// <summary>Gets or sets the current status of the supplier.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the current stage of the supplier's onboarding process.</summary>
    public string OnboardingStage { get; set; } = string.Empty;
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTime UpdatedAt { get; set; }
    /// <summary>Gets or sets the row version for optimistic concurrency control.</summary>
    public string RowVersion { get; set; } = string.Empty;
    /// <summary>Gets or sets the list of contacts for the supplier.</summary>
    public List<SupplierContactDto> Contacts { get; set; } = [];
    /// <summary>Gets or sets the performance summary for the supplier.</summary>
    public SupplierPerformanceSummaryDto? PerformanceSummary { get; set; }
}

/// <summary>
/// DTO representing a contact person for a supplier.
/// </summary>
public class SupplierContactDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the full name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the job role.</summary>
    public string? Role { get; set; }
    /// <summary>Gets or sets the email address.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Gets or sets the phone number.</summary>
    public string? Phone { get; set; }
    /// <summary>Gets or sets whether this is the primary contact.</summary>
    public bool IsPrimary { get; set; }
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO summarizing a supplier's performance ratings.
/// </summary>
public class SupplierPerformanceSummaryDto
{
    /// <summary>Gets or sets the overall average rating.</summary>
    public decimal? OverallRating { get; set; }
    /// <summary>Gets or sets the average quality rating.</summary>
    public decimal? QualityRating { get; set; }
    /// <summary>Gets or sets the average delivery rating.</summary>
    public decimal? DeliveryRating { get; set; }
    /// <summary>Gets or sets the average communication rating.</summary>
    public decimal? CommunicationRating { get; set; }
    /// <summary>Gets or sets the average pricing rating.</summary>
    public decimal? PricingRating { get; set; }
    /// <summary>Gets or sets the total number of evaluations.</summary>
    public int TotalEvaluations { get; set; }
    /// <summary>Gets or sets the date of the most recent evaluation.</summary>
    public DateTime? LastEvaluationDate { get; set; }
}

/// <summary>
/// Request model for creating a new material.
/// </summary>
public sealed record CreateMaterialRequest
{
    /// <summary>Gets or sets the name.</summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the code identifier.</summary>
    [Required]
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the category.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the price per unit.</summary>
    public decimal PricePerUnit { get; set; }

    /// <summary>Gets or sets the current stock level.</summary>
    public int StockLevel { get; set; }

    /// <summary>Gets or sets the material density in g/cm³.</summary>
    public decimal Density { get; set; }

    /// <summary>Gets or sets the cost per kilogram in THB.</summary>
    public decimal CostPerKg { get; set; }

    /// <summary>Gets or sets the technology-specific process parameters.</summary>
    public Dictionary<string, string> ProcessParameters { get; set; } = [];

    /// <summary>Gets or sets the optional supplier ID.</summary>
    public Guid? SupplierId { get; set; }

    /// <summary>Gets or sets the list of manufacturing process IDs.</summary>
    public List<Guid> ManufacturingProcessIds { get; set; } = [];

    /// <summary>Gets or sets the list of color IDs.</summary>
    public List<Guid> ColorIds { get; set; } = [];

    /// <summary>Gets or sets the list of post-processing method IDs.</summary>
    public List<Guid> PostProcessingMethodIds { get; set; } = [];
}

/// <summary>
/// Request model for updating an existing material.
/// </summary>
public sealed record UpdateMaterialRequest
{
    /// <summary>Gets or sets the name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the category.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the price per unit.</summary>
    public decimal? PricePerUnit { get; set; }

    /// <summary>Gets or sets the current stock level.</summary>
    public int? StockLevel { get; set; }

    /// <summary>Gets or sets the status.</summary>
    public string? Status { get; set; }

    /// <summary>Gets or sets the unit of measure.</summary>
    public string? Unit { get; set; }

    /// <summary>Gets or sets the material density.</summary>
    public decimal? Density { get; set; }

    /// <summary>Gets or sets the cost per kilogram.</summary>
    public decimal? CostPerKg { get; set; }

    /// <summary>Gets or sets the technology-specific process parameters.</summary>
    public Dictionary<string, string>? ProcessParameters { get; set; }
}

/// <summary>
/// Request model for creating a new supplier.
/// </summary>
public sealed record CreateSupplierRequest
{
    /// <summary>Gets or sets the legal name of the supplier company.</summary>
    [Required]
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the tax identification number of the supplier.</summary>
    [Required]
    public string TaxId { get; set; } = string.Empty;

    /// <summary>Gets or sets the street address of the supplier.</summary>
    [Required]
    public string Address { get; set; } = string.Empty;

    /// <summary>Gets or sets the city where the supplier is located.</summary>
    [Required]
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the country where the supplier is located.</summary>
    [Required]
    public string Country { get; set; } = string.Empty;

    /// <summary>Gets or sets the postal code for the supplier's address.</summary>
    public string? PostalCode { get; set; }

    /// <summary>Gets or sets the collection of IDs for the material categories.</summary>
    public List<Guid>? MaterialCategoryIds { get; set; }

    /// <summary>Gets or sets the list of the supplier's capabilities.</summary>
    public List<string>? Capabilities { get; set; }

    /// <summary>Gets or sets the primary contact person for the supplier.</summary>
    public CreateSupplierContactRequest? PrimaryContact { get; set; }
}

/// <summary>
/// Request model for updating an existing supplier.
/// </summary>
public sealed record UpdateSupplierRequest
{
    /// <summary>Gets or sets the legal name of the supplier company.</summary>
    public string? CompanyName { get; set; }

    /// <summary>Gets or sets the tax identification number of the supplier.</summary>
    public string? TaxId { get; set; }

    /// <summary>Gets or sets the street address of the supplier.</summary>
    public string? Address { get; set; }

    /// <summary>Gets or sets the city where the supplier is located.</summary>
    public string? City { get; set; }

    /// <summary>Gets or sets the country where the supplier is located.</summary>
    public string? Country { get; set; }

    /// <summary>Gets or sets the postal code for the supplier's address.</summary>
    public string? PostalCode { get; set; }

    /// <summary>Gets or sets the current status of the supplier.</summary>
    public string? Status { get; set; }

    /// <summary>Gets or sets the version for concurrency.</summary>
    public string? RowVersion { get; set; }
}

/// <summary>
/// DTO for material status summary from inventory service.
/// </summary>
public class MaterialStatusSummary
{
    /// <summary>Gets or sets the material ID.</summary>
    public Guid MaterialId { get; set; }
    /// <summary>Gets or sets the count of active batches.</summary>
    public int ActiveBatches { get; set; }
    /// <summary>Gets or sets the total remaining grams.</summary>
    public decimal TotalRemainingGrams { get; set; }
    /// <summary>Gets or sets the remaining grams in the lowest batch.</summary>
    public decimal LowestBatchGrams { get; set; }
    /// <summary>Gets or sets a value indicating whether a low stock alert is active.</summary>
    public bool HasLowStockAlert { get; set; }
}

/// <summary>
/// Represents a request to create a new contact person for a supplier.
/// </summary>
public sealed record CreateSupplierContactRequest
{
    /// <summary>Gets or sets the full name of the contact person.</summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the email address of the contact person.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the role or job title of the contact person.</summary>
    public string? Role { get; set; }

    /// <summary>Gets or sets the phone number of the contact person.</summary>
    public string? Phone { get; set; }

    /// <summary>Gets or sets a value indicating whether this is the primary contact.</summary>
    public bool IsPrimary { get; set; }
}
