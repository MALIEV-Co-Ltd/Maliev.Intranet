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

    /// <summary>Shipping gateway that served this rate option.</summary>
    public string Provider { get; init; } = string.Empty;
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
