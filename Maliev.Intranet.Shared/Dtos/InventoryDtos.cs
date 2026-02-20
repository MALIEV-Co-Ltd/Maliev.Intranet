using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Material summary DTO.
/// </summary>
public class MaterialSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public decimal UnitPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Unit { get; set; } = "pcs";
}

/// <summary>
/// Detailed material information.
/// </summary>
public class MaterialDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public string Unit { get; set; } = "pcs";
    public string Status { get; set; } = string.Empty;
    public string? MaterialType { get; set; }
    public string? Color { get; set; }
    public string? Brand { get; set; }
    public decimal? Weight { get; set; }
    public List<MaterialPropertyDto> Properties { get; set; } = [];
    public List<SupplierSummaryDto> Suppliers { get; set; } = [];
    public List<StockTransactionDto> RecentTransactions { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class MaterialPropertyDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Unit { get; set; }
}

public class StockTransactionDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty; // Receipt, Issue, Adjustment, Transfer
    public int Quantity { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Supplier summary DTO.
/// </summary>
public class SupplierSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Rating { get; set; }
}

/// <summary>
/// Detailed supplier information.
/// </summary>
public class SupplierDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public string? ContactPerson { get; set; }
    public string? Website { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed record CreateMaterialRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string SKU { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public string Unit { get; set; } = "pcs";
}

public sealed record UpdateMaterialRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal? UnitPrice { get; set; }
    public int? QuantityOnHand { get; set; }
    public string? Status { get; set; }
    public string? Unit { get; set; }
}

/// <summary>
/// Represents a single entry in a material's usage history.
/// </summary>
public class MaterialUsageHistoryDto
{
    /// <summary>Gets or sets the date the material was used.</summary>
    public DateTime Date { get; set; }

    /// <summary>Gets or sets the production or work order ID this usage belongs to.</summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>Gets or sets the quantity consumed.</summary>
    public decimal QuantityUsed { get; set; }

    /// <summary>Gets or sets the name of the person who consumed the material.</summary>
    public string UsedBy { get; set; } = string.Empty;
}

public sealed record CreateSupplierRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactPerson { get; set; }
    public string? Website { get; set; }
}

public sealed record UpdateSupplierRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Country { get; set; }
    public string? Address { get; set; }
    public string? ContactPerson { get; set; }
    public string? Website { get; set; }
}
