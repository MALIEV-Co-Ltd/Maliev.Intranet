using System.Text.Json.Serialization;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Browser-safe Google Maps configuration for address picking.
/// </summary>
public sealed class GoogleAddressConfigResponse
{
    /// <summary>Domain-restricted Google Maps browser API key.</summary>
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Optional Google Maps map ID used for advanced markers.</summary>
    [JsonPropertyName("mapId")]
    public string? MapId { get; set; }

    /// <summary>Default map latitude.</summary>
    [JsonPropertyName("defaultLatitude")]
    public double DefaultLatitude { get; set; } = 13.7563;

    /// <summary>Default map longitude.</summary>
    [JsonPropertyName("defaultLongitude")]
    public double DefaultLongitude { get; set; } = 100.5018;

    /// <summary>Default zoom level for the manual pin map.</summary>
    [JsonPropertyName("defaultZoom")]
    public int DefaultZoom { get; set; } = 12;

    /// <summary>Region codes allowed in Google Places suggestions.</summary>
    [JsonPropertyName("includedRegionCodes")]
    public string[] IncludedRegionCodes { get; set; } = ["th"];
}

/// <summary>
/// Structured address selection returned by Google Places or reverse geocoding.
/// </summary>
public sealed class GoogleAddressSelection
{
    /// <summary>Address source: GooglePlace or GoogleMapPin.</summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = "GooglePlace";

    /// <summary>Google Places identifier when available.</summary>
    [JsonPropertyName("placeId")]
    public string? PlaceId { get; set; }

    /// <summary>Formatted address returned by Google.</summary>
    [JsonPropertyName("formattedAddress")]
    public string? FormattedAddress { get; set; }

    /// <summary>Address number, moo, soi, and road line.</summary>
    [JsonPropertyName("addressLine1")]
    public string? AddressLine1 { get; set; }

    /// <summary>Sub district.</summary>
    [JsonPropertyName("district")]
    public string? District { get; set; }

    /// <summary>District.</summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>Province.</summary>
    [JsonPropertyName("stateProvince")]
    public string? StateProvince { get; set; }

    /// <summary>Postal code.</summary>
    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    /// <summary>Latitude.</summary>
    [JsonPropertyName("latitude")]
    public decimal? Latitude { get; set; }

    /// <summary>Longitude.</summary>
    [JsonPropertyName("longitude")]
    public decimal? Longitude { get; set; }
}
