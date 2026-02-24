using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Request to create a new company in the registry.
/// </summary>
public class CreateCompanyRequest
{
    /// <summary>Gets or sets the company name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the VAT number.</summary>
    public string? VatNumber { get; set; }
    /// <summary>Gets or sets the registration number.</summary>
    public string? RegistrationNumber { get; set; }
    /// <summary>Gets or sets the contact email.</summary>
    public string? ContactEmail { get; set; }
    /// <summary>Gets or sets the contact phone.</summary>
    public string? ContactPhone { get; set; }
    /// <summary>Gets or sets the company website URL.</summary>
    public string? WebsiteUrl { get; set; }
    /// <summary>Gets or sets the company segment (e.g., Retail, Corporate).</summary>
    public string Segment { get; set; } = "Retail";
    /// <summary>Gets or sets the company tier (e.g., Bronze, Silver, Gold).</summary>
    public string Tier { get; set; } = "Bronze";

    // BDEX fields
    /// <summary>Gets or sets the full name in Thai from BDEX.</summary>
    public string? FullNameTh { get; set; }
    /// <summary>Gets or sets the registration date.</summary>
    public DateTime? RegistrationDate { get; set; }
    /// <summary>Gets or sets the company status code.</summary>
    public string? CompanyStatus { get; set; }
    /// <summary>Gets or sets the company status name in Thai.</summary>
    public string? CompanyStatusNameTh { get; set; }
    /// <summary>Gets or sets the company type code.</summary>
    public string? CompanyTypeCode { get; set; }
    /// <summary>Gets or sets the business objectives.</summary>
    public string? BusinessObjectives { get; set; }
    /// <summary>Gets or sets a value indicating whether the company is verified from BDEX.</summary>
    public bool IsVerifiedFromBdex { get; set; }
    /// <summary>Gets or sets the stock symbol, if applicable.</summary>
    public string? StockSymbol { get; set; }
}

/// <summary>
/// DTO representing a country.
/// </summary>
public class CountryDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the ISO 3166-1 alpha-2 country code.</summary>
    [JsonPropertyName("iso2")]
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the country name.</summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Represents a location in Thailand registry.
/// </summary>
public class RegistryThaiLocation
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the postal code.</summary>
    public string PostalCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the sub-district name in Thai.</summary>
    public string SubDistrictTh { get; set; } = string.Empty;
    /// <summary>Gets or sets the district name in Thai.</summary>
    public string DistrictTh { get; set; } = string.Empty;
    /// <summary>Gets or sets the province name in Thai.</summary>
    public string ProvinceTh { get; set; } = string.Empty;
    /// <summary>Gets or sets the sub-district name in English.</summary>
    public string SubDistrictEn { get; set; } = string.Empty;
    /// <summary>Gets or sets the district name in English.</summary>
    public string DistrictEn { get; set; } = string.Empty;
    /// <summary>Gets or sets the province name in English.</summary>
    public string ProvinceEn { get; set; } = string.Empty;
}

/// <summary>
/// Company profile from Thailand registry.
/// </summary>
public class RegistryCompanyProfile
{
    /// <summary>Gets or sets the status code.</summary>
    public string StatusCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the status name in Thai.</summary>
    public string StatusNameTh { get; set; } = string.Empty;
    /// <summary>Gets or sets the tax identifier.</summary>
    public string TaxId { get; set; } = string.Empty;
    /// <summary>Gets or sets the company name in Thai.</summary>
    public string CompanyNameTh { get; set; } = string.Empty;
    /// <summary>Gets or sets the business objectives.</summary>
    public string BusinessObjectives { get; set; } = string.Empty;
    /// <summary>Gets or sets the company type code.</summary>
    public string CompanyTypeCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the stock exchange symbol name.</summary>
    public string? StockName { get; set; }
    /// <summary>Gets or sets the full name in Thai.</summary>
    public string FullNameTh { get; set; } = string.Empty;
}

/// <summary>
/// Generic response from registry API.
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
public class RegistryApiResponse<T>
{
    /// <summary>Gets or sets the response data.</summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>Gets or sets a value indicating whether the request was successful.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Gets or sets the error message, if any.</summary>
    [JsonPropertyName("message")]
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the collection of error messages.</summary>
    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }
}
