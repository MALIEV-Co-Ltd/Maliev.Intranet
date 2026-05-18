using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Request to create a new company in the registry.
/// </summary>
public class CreateCompanyRequest
{
    /// <summary>The display name of the company.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The unique VAT registration number of the company.</summary>
    public string? VatNumber { get; set; }

    /// <summary>The official registration number assigned by the commerce authority.</summary>
    public string? RegistrationNumber { get; set; }

    /// <summary>The primary email address for company-wide correspondence.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>The primary phone number for company-wide correspondence.</summary>
    public string? ContactPhone { get; set; }

    /// <summary>The industry or market segment of the company (e.g., "Retail", "Manufacturing").</summary>
    public string Segment { get; set; } = "Retail";

    /// <summary>The service tier assigned to the company for internal ranking (e.g., "Bronze", "Gold").</summary>
    public string Tier { get; set; } = "Bronze";

    // BDEX fields
    /// <summary>The full legal name of the company in Thai language.</summary>
    public string? FullNameTh { get; set; }

    /// <summary>The date when the company was officially registered.</summary>
    public DateTime? RegistrationDate { get; set; }

    /// <summary>The status code representing the current operational state of the company.</summary>
    public string? CompanyStatus { get; set; }

    /// <summary>The display name for the company status in Thai language.</summary>
    public string? CompanyStatusNameTh { get; set; }

    /// <summary>The official code for the company's legal entity type.</summary>
    public string? CompanyTypeCode { get; set; }

    /// <summary>The business objectives or activities the company is authorized to perform.</summary>
    public string? BusinessObjectives { get; set; }

    /// <summary>Indicates if the company profile has been verified against the BDEX registry.</summary>
    public bool IsVerifiedFromBdex { get; set; }

    /// <summary>The stock exchange ticker symbol for the company, if publicly traded.</summary>
    public string? StockSymbol { get; set; }
}

/// <summary>
/// Data representing a country in the global registry.
/// </summary>
public class CountryDto
{
    /// <summary>The unique identifier of the country record.</summary>
    public Guid Id { get; set; }

    /// <summary>The ISO 3166-1 alpha-2 code of the country (e.g., "TH", "US").</summary>
    [JsonPropertyName("iso2")]
    public string Code { get; set; } = string.Empty;

    /// <summary>The ISO 3166-1 alpha-3 code of the country (e.g., "THA", "USA").</summary>
    [JsonPropertyName("iso3")]
    public string? Iso3 { get; set; }

    /// <summary>The full display name of the country.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The official country name, when different from the common name.</summary>
    public string? OfficialName { get; set; }

    /// <summary>The ISO 3166-1 numeric code.</summary>
    public string? NumericCode { get; set; }

    /// <summary>The capital city of the country.</summary>
    public string? Capital { get; set; }

    /// <summary>The geographic region for grouping countries.</summary>
    public string? Region { get; set; }

    /// <summary>The geographic subregion for grouping countries.</summary>
    public string? Subregion { get; set; }

    /// <summary>Indicates whether the country record is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>The optimistic concurrency token returned by CountryService.</summary>
    public string? ETag { get; set; }

    /// <summary>The UTC timestamp when the country record was last changed.</summary>
    public DateTime? LastModifiedUtc { get; set; }
}

/// <summary>
/// Data representing a monetary currency in the global registry.
/// </summary>
public class CurrencyDto
{
    /// <summary>The unique identifier of the currency record.</summary>
    public Guid Id { get; set; }

    /// <summary>The ISO 4217 currency code used for transactions (e.g., "THB", "USD").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>The display symbol of the currency (e.g., "฿", "$").</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>The full display name of the currency.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The number of decimal places used for financial calculations with this currency.</summary>
    public int DecimalPlaces { get; set; }

    /// <summary>Indicates if the currency is currently active and available for use in the system.</summary>
    public bool IsActive { get; set; }

    /// <summary>Indicates if this currency is the primary or base currency for system-wide accounting.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>The optimistic concurrency token returned by CurrencyService.</summary>
    public string? ETag { get; set; }

    /// <summary>The UTC timestamp when the currency was created.</summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>The UTC timestamp when the currency was last updated.</summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Paginated reference data returned by management endpoints.
/// </summary>
/// <typeparam name="T">The type of reference data item.</typeparam>
public class ReferenceDataPage<T>
{
    /// <summary>The items in the current page.</summary>
    public List<T> Items { get; set; } = [];

    /// <summary>The one-based page number.</summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>The number of rows requested per page.</summary>
    public int PageSize { get; set; } = 25;

    /// <summary>The total number of matching rows.</summary>
    public int TotalCount { get; set; }

    /// <summary>The total number of pages.</summary>
    public int TotalPages { get; set; }

    /// <summary>Indicates whether a previous page exists.</summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>Indicates whether a next page exists.</summary>
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Represents a specific administrative location in the Thailand geographic registry.
/// </summary>
public class RegistryThaiLocation
{
    /// <summary>The unique identifier of the location record.</summary>
    public Guid Id { get; set; }

    /// <summary>The five-digit postal code of the location.</summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>The sub-district (Tambon) name in Thai language.</summary>
    public string SubDistrictTh { get; set; } = string.Empty;

    /// <summary>The district (Amphoe) name in Thai language.</summary>
    public string DistrictTh { get; set; } = string.Empty;

    /// <summary>The province (Changwat) name in Thai language.</summary>
    public string ProvinceTh { get; set; } = string.Empty;

    /// <summary>The sub-district (Tambon) name in English language.</summary>
    public string SubDistrictEn { get; set; } = string.Empty;

    /// <summary>The district (Amphoe) name in English language.</summary>
    public string DistrictEn { get; set; } = string.Empty;

    /// <summary>The province (Changwat) name in English language.</summary>
    public string ProvinceEn { get; set; } = string.Empty;
}

/// <summary>
/// Geocoded map position for an address lookup result.
/// </summary>
public class AddressGeocodeResponse
{
    /// <summary>The latitude returned by the geocoding provider.</summary>
    public double Latitude { get; set; }

    /// <summary>The longitude returned by the geocoding provider.</summary>
    public double Longitude { get; set; }

    /// <summary>The display label returned by the geocoding provider.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>The attribution text required for the geocoding and map provider.</summary>
    public string Attribution { get; set; } = "© OpenStreetMap contributors";
}

/// <summary>
/// Detailed company profile information retrieved from the Thailand official registry.
/// </summary>
public class RegistryCompanyProfile
{
    /// <summary>The current status code for the company from the registry.</summary>
    public string StatusCode { get; set; } = string.Empty;

    /// <summary>The display name for the company status in Thai language.</summary>
    public string StatusNameTh { get; set; } = string.Empty;

    /// <summary>The unique tax identification number issued by the revenue department.</summary>
    public string TaxId { get; set; } = string.Empty;

    /// <summary>The official name of the company in Thai language.</summary>
    public string CompanyNameTh { get; set; } = string.Empty;

    /// <summary>The primary business objectives or industrial activities of the company.</summary>
    public string BusinessObjectives { get; set; } = string.Empty;

    /// <summary>The legal entity type code of the company.</summary>
    public string CompanyTypeCode { get; set; } = string.Empty;

    /// <summary>The stock exchange symbol name for the company, if applicable.</summary>
    public string? StockName { get; set; }

    /// <summary>The full legal name of the company in Thai language.</summary>
    public string FullNameTh { get; set; } = string.Empty;
}

/// <summary>
/// Represents a standardized wrapper for responses returned by external registry APIs.
/// </summary>
/// <typeparam name="T">The type of the primary data payload within the response.</typeparam>
public class RegistryApiResponse<T>
{
    /// <summary>The primary data payload of the response, if the request was successful.</summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>Indicates if the API request was completed successfully.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>The primary error or status message returned by the API.</summary>
    [JsonPropertyName("message")]
    public string? ErrorMessage { get; set; }

    /// <summary>A collection of detailed error descriptions if the request failed.</summary>
    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }
}
