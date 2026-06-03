using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Request to fetch shipping rates from a carrier.
/// </summary>
public record ShippingRateRequestDto
{
    /// <summary>Origin country ISO code (e.g. "TH").</summary>
    [Required]
    public string OriginCountryCode { get; init; } = string.Empty;

    /// <summary>Destination country ISO code (e.g. "US").</summary>
    [Required]
    public string DestinationCountryCode { get; init; } = string.Empty;

    /// <summary>Destination postal code for accuracy.</summary>
    public string? DestinationPostalCode { get; init; }

    /// <summary>Total package weight in kilograms.</summary>
    [Range(0.001, double.MaxValue)]
    public decimal WeightKg { get; init; }

    /// <summary>Package length in centimeters.</summary>
    public decimal? LengthCm { get; init; }

    /// <summary>Package width in centimeters.</summary>
    public decimal? WidthCm { get; init; }

    /// <summary>Package height in centimeters.</summary>
    public decimal? HeightCm { get; init; }
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
    /// <summary>Carrier product name (e.g. "Express Worldwide").</summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>Total price in THB.</summary>
    public decimal TotalPrice { get; init; }

    /// <summary>Currency code for the price.</summary>
    public string CurrencyCode { get; init; } = "THB";

    /// <summary>Estimated delivery date in ISO format.</summary>
    public string? EstimatedDeliveryDate { get; init; }
}
