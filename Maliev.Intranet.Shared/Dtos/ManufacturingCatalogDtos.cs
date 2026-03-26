namespace Maliev.Intranet.Shared.Dtos;

/// <summary>Manufacturing process summary.</summary>
public record ProcessDto(Guid Id, string Code, string Name, string? Description, int SortOrder);

/// <summary>Material in the manufacturing catalog.</summary>
public record CatalogMaterialDto(Guid Id, string Name, string Code, string Category, decimal? DensityGCm3, string? Description, int SortOrder);

/// <summary>Surface finish option.</summary>
public record CatalogSurfaceFinishDto(Guid Id, string Name, string Code, decimal? RaValueUm, decimal AdditionalCostPercent, string? Description, int SortOrder);

/// <summary>Tolerance class option.</summary>
public record CatalogToleranceDto(Guid Id, string Name, string Code, string IsoStandard, string Grade, string? ToleranceRange, decimal AdditionalCostPercent, int SortOrder);

/// <summary>Dynamic process configuration option.</summary>
public record ProcessConfigOptionDto(Guid Id, string ConfigKey, string Label, string ConfigType, string? DefaultValue, string? OptionsJson, string? Unit, string? HelpText, bool IsRequired, int SortOrder);

/// <summary>Lead time option with pricing multiplier.</summary>
public record LeadTimeOptionDto(string Code, string Name, int MinDays, int MaxDays, decimal PriceMultiplier, bool IsDefault);

/// <summary>Volume discount tier.</summary>
public record VolumeDiscountTierDto(int MinQuantity, int? MaxQuantity, decimal DiscountPercent);

/// <summary>Single entry in a bulk pricing response.</summary>
public record BulkPriceTierDto(int Quantity, decimal UnitPrice, decimal Total, decimal DiscountPercent);

/// <summary>Request body for bulk pricing calculation.</summary>
public class BulkPricingRequestDto
{
    /// <summary>Base unit price (before lead time and volume discounts).</summary>
    public decimal BaseUnitPrice { get; set; }
    /// <summary>Quantities to calculate pricing for.</summary>
    public int[] Quantities { get; set; } = [1, 2, 5, 10, 25, 50, 100];
    /// <summary>Optional lead time code (e.g. "STANDARD").</summary>
    public string? LeadTimeCode { get; set; }
}
