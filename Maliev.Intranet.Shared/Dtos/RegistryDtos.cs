using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Request to create a new company in the registry.
/// </summary>
public class CreateCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? VatNumber { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string Segment { get; set; } = "Retail";
    public string Tier { get; set; } = "Bronze";

    // BDEX fields
    public string? FullNameTh { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public string? CompanyStatus { get; set; }
    public string? CompanyStatusNameTh { get; set; }
    public string? CompanyTypeCode { get; set; }
    public string? BusinessObjectives { get; set; }
    public bool IsVerifiedFromBdex { get; set; }
    public string? StockSymbol { get; set; }
}

/// <summary>
/// DTO representing a country.
/// </summary>
public class CountryDto
{
    public Guid Id { get; set; }

    [JsonPropertyName("iso2")]
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Represents a location in Thailand registry.
/// </summary>
public class RegistryThaiLocation
{
    public Guid Id { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string SubDistrictTh { get; set; } = string.Empty;
    public string DistrictTh { get; set; } = string.Empty;
    public string ProvinceTh { get; set; } = string.Empty;
    public string SubDistrictEn { get; set; } = string.Empty;
    public string DistrictEn { get; set; } = string.Empty;
    public string ProvinceEn { get; set; } = string.Empty;
}

/// <summary>
/// Company profile from Thailand registry.
/// </summary>
public class RegistryCompanyProfile
{
    public string StatusCode { get; set; } = string.Empty;
    public string StatusNameTh { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string CompanyNameTh { get; set; } = string.Empty;
    public string BusinessObjectives { get; set; } = string.Empty;
    public string CompanyTypeCode { get; set; } = string.Empty;
    public string? StockName { get; set; }
    public string FullNameTh { get; set; } = string.Empty;
}

/// <summary>
/// Generic response from registry API.
/// </summary>
public class RegistryApiResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }
}
