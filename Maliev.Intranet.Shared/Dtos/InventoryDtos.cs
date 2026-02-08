namespace Maliev.Intranet.Shared;

/// <summary>
/// Material summary DTO.
/// </summary>
public class MaterialSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
}

/// <summary>
/// Detailed material information.
/// </summary>
public class MaterialDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int QuantityOnHand { get; set; }
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
}
