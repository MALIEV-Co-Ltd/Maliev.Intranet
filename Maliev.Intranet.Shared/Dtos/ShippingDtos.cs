using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Address payload used for shipping rate requests.
/// </summary>
public record ShippingAddressDto
{
    /// <summary>Contact name.</summary>
    [Required]
    public string Name { get; init; } = string.Empty;

    /// <summary>Street address.</summary>
    [Required]
    public string Address { get; init; } = string.Empty;

    /// <summary>District or subdistrict.</summary>
    [Required]
    public string District { get; init; } = string.Empty;

    /// <summary>State or amphoe.</summary>
    [Required]
    public string State { get; init; } = string.Empty;

    /// <summary>Province.</summary>
    [Required]
    public string Province { get; init; } = string.Empty;

    /// <summary>Postal code.</summary>
    [Required]
    public string Postcode { get; init; } = string.Empty;

    /// <summary>ISO 3166-1 alpha-2 country code.</summary>
    public string CountryCode { get; init; } = "TH";

    /// <summary>Contact phone number.</summary>
    [Required]
    public string Tel { get; init; } = string.Empty;

    /// <summary>Contact email address.</summary>
    public string? Email { get; init; }

    /// <summary>Latitude for on-demand couriers.</summary>
    public string? Lat { get; init; }

    /// <summary>Longitude for on-demand couriers.</summary>
    public string? Lng { get; init; }
}

/// <summary>
/// Parcel dimensions and weight used for live shipping rate requests.
/// </summary>
public record ShippingParcelDto
{
    /// <summary>Parcel display name.</summary>
    [Required]
    public string Name { get; init; } = "MALIEV shipment";

    /// <summary>Parcel weight in grams.</summary>
    [Range(1, double.MaxValue)]
    public decimal Weight { get; init; }

    /// <summary>Parcel width in centimeters.</summary>
    [Range(0.1, double.MaxValue)]
    public decimal Width { get; init; }

    /// <summary>Parcel length in centimeters.</summary>
    [Range(0.1, double.MaxValue)]
    public decimal Length { get; init; }

    /// <summary>Parcel height in centimeters.</summary>
    [Range(0.1, double.MaxValue)]
    public decimal Height { get; init; }
}

/// <summary>
/// A quoted project part used for automatic package planning.
/// </summary>
public record ShippingPackagePartDto
{
    /// <summary>Part display name.</summary>
    [Required]
    public string Name { get; init; } = string.Empty;

    /// <summary>Ordered quantity for this part.</summary>
    [Range(1, 100_000)]
    public int Quantity { get; init; } = 1;

    /// <summary>Single-part weight in grams.</summary>
    [Range(1, double.MaxValue)]
    public decimal Weight { get; init; }

    /// <summary>Part bounding-box width in centimeters.</summary>
    [Range(0.1, double.MaxValue)]
    public decimal Width { get; init; }

    /// <summary>Part bounding-box length in centimeters.</summary>
    [Range(0.1, double.MaxValue)]
    public decimal Length { get; init; }

    /// <summary>Part bounding-box height in centimeters.</summary>
    [Range(0.1, double.MaxValue)]
    public decimal Height { get; init; }

    /// <summary>Optional per-side wrapping margin in centimeters.</summary>
    public decimal? PackagingMargin { get; init; }
}

/// <summary>
/// Package planning constraints and packaging material allowances.
/// </summary>
public record ShippingPackagingOptionsDto
{
    /// <summary>Default per-side wrapping margin around each part in centimeters.</summary>
    public decimal PartMargin { get; init; } = 1.5m;

    /// <summary>Per-side box filler margin in centimeters.</summary>
    public decimal BoxMargin { get; init; } = 3m;

    /// <summary>Default wrapping material weight per part in grams.</summary>
    public decimal PartPackagingWeight { get; init; } = 10m;

    /// <summary>Default carton/filler weight per box in grams.</summary>
    public decimal BoxPackagingWeight { get; init; } = 250m;

    /// <summary>Maximum preferred package weight in grams before splitting.</summary>
    public decimal MaxPackageWeight { get; init; } = 20_000m;

    /// <summary>Maximum preferred package length in centimeters before splitting.</summary>
    public decimal MaxPackageLength { get; init; } = 60m;

    /// <summary>Maximum preferred package width in centimeters before splitting.</summary>
    public decimal MaxPackageWidth { get; init; } = 45m;

    /// <summary>Maximum preferred package height in centimeters before splitting.</summary>
    public decimal MaxPackageHeight { get; init; } = 45m;
}

/// <summary>
/// Request to fetch shipping rates from the DeliveryService shipping gateway.
/// </summary>
public record ShippingRateRequestDto
{
    /// <summary>Origin address.</summary>
    [Required]
    public ShippingAddressDto From { get; init; } = new();

    /// <summary>Destination address.</summary>
    [Required]
    public ShippingAddressDto To { get; init; } = new();

    /// <summary>Parcel dimensions and weight.</summary>
    [Required]
    public ShippingParcelDto Parcel { get; init; } = new();

    /// <summary>Optional courier codes to restrict the rate request.</summary>
    public List<string> CourierCodes { get; init; } = [];

    /// <summary>Optional project parts. When present, DeliveryService calculates packages from bounding boxes.</summary>
    public List<ShippingPackagePartDto> Parts { get; init; } = [];

    /// <summary>Package planning constraints and material margins.</summary>
    public ShippingPackagingOptionsDto Packaging { get; init; } = new();

    /// <summary>Whether to use SHIPPOP public rate data.</summary>
    public bool UsePublicRates { get; init; }
}

/// <summary>
/// Response containing available shipping rate options.
/// </summary>
public record ShippingRateResponseDto
{
    /// <summary>Available shipping rate options from the carrier.</summary>
    public List<ShippingRateOptionDto> Rates { get; init; } = [];
}

/// <summary>
/// A single shipping rate option from a carrier.
/// </summary>
public record ShippingRateOptionDto
{
    /// <summary>Courier code from the shipping gateway.</summary>
    public string CourierCode { get; init; } = string.Empty;

    /// <summary>Carrier product name.</summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>Total price in THB.</summary>
    public decimal TotalPrice { get; init; }

    /// <summary>Currency code for the price.</summary>
    public string CurrencyCode { get; init; } = "THB";

    /// <summary>Estimated delivery date in ISO format.</summary>
    public string? EstimatedDeliveryDate { get; init; }

    /// <summary>Raw service level returned by the gateway.</summary>
    public string? ServiceLevel { get; init; }

    /// <summary>Courier logo URL.</summary>
    public string? CourierLogoUrl { get; init; }

    /// <summary>Number of packages included in this rate.</summary>
    public int PackageCount { get; init; } = 1;

    /// <summary>Total package weight in grams.</summary>
    public decimal TotalWeight { get; init; }

    /// <summary>Package breakdown used for this rate.</summary>
    public List<ShippingPackageQuoteDto> Packages { get; init; } = [];

    /// <summary>Shipping gateway that served this rate option.</summary>
    public string Provider { get; init; } = string.Empty;
}

/// <summary>
/// Package breakdown used by a courier rate quote.
/// </summary>
public record ShippingPackageQuoteDto
{
    /// <summary>One-based package number.</summary>
    public int PackageNumber { get; init; }

    /// <summary>Package display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Package weight in grams.</summary>
    public decimal Weight { get; init; }

    /// <summary>Package width in centimeters.</summary>
    public decimal Width { get; init; }

    /// <summary>Package length in centimeters.</summary>
    public decimal Length { get; init; }

    /// <summary>Package height in centimeters.</summary>
    public decimal Height { get; init; }

    /// <summary>Whether this package exceeds preferred box constraints.</summary>
    public bool IsOversized { get; init; }

    /// <summary>Courier price for this package.</summary>
    public decimal Price { get; init; }

    /// <summary>Package quote currency.</summary>
    public string Currency { get; init; } = "THB";

    /// <summary>Courier lead-time text for this package.</summary>
    public string? EstimatedDelivery { get; init; }

    /// <summary>Items allocated into this package.</summary>
    public List<ShippingPackageItemDto> Items { get; init; } = [];
}

/// <summary>
/// Part quantity allocated into a planned package.
/// </summary>
public record ShippingPackageItemDto
{
    /// <summary>Part name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Quantity of this part in the package.</summary>
    public int Quantity { get; init; }

    /// <summary>Wrapped unit width in centimeters.</summary>
    public decimal UnitWidth { get; init; }

    /// <summary>Wrapped unit length in centimeters.</summary>
    public decimal UnitLength { get; init; }

    /// <summary>Wrapped unit height in centimeters.</summary>
    public decimal UnitHeight { get; init; }

    /// <summary>Wrapped unit weight in grams.</summary>
    public decimal UnitWeight { get; init; }
}

/// <summary>
/// Courier option exposed by DeliveryService.
/// </summary>
public record ShippingCourierDto
{
    /// <summary>Courier code from the shipping gateway.</summary>
    public string CourierCode { get; init; } = string.Empty;

    /// <summary>Courier display name.</summary>
    public string CourierName { get; init; } = string.Empty;

    /// <summary>Optional courier note.</summary>
    public string? Note { get; init; }

    /// <summary>Courier logo URL.</summary>
    public string? LogoUrl { get; init; }

    /// <summary>Shipping scope.</summary>
    public string Scope { get; init; } = "domestic";

    /// <summary>Shipping gateway that served this courier option.</summary>
    public string Provider { get; init; } = string.Empty;
}

/// <summary>
/// Current shipment tracking status.
/// </summary>
public record ShippingTrackingDto
{
    /// <summary>Tracking code.</summary>
    public string TrackingCode { get; init; } = string.Empty;

    /// <summary>Courier code.</summary>
    public string? CourierCode { get; init; }

    /// <summary>Courier name.</summary>
    public string? CourierName { get; init; }

    /// <summary>Latest tracking status.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Latest status description.</summary>
    public string? Description { get; init; }

    /// <summary>Shipping gateway that served this tracking status.</summary>
    public string Provider { get; init; } = string.Empty;
}
