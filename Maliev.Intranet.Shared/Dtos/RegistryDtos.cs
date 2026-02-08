using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Summary information for a company.
/// </summary>
public class CompanySummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? VatNumber { get; set; }
    public string Segment { get; set; } = string.Empty;
}

/// <summary>
/// Source of company information.
/// </summary>
public enum CompanySource
{
    Internal,
    Registry
}

/// <summary>
/// Summary address information.
/// </summary>
public sealed record AddressSummaryDto
{
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public string? AddressLine3 { get; init; }
    public string? District { get; init; }
    public string City { get; init; } = string.Empty;
    public string StateProvince { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string? CountryName { get; init; }
}

/// <summary>
/// Result from a company search.
/// </summary>
public sealed record CompanySearchResultDto
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? VatNumber { get; init; }
    public string? Segment { get; init; }
    public CompanySource Source { get; init; }
    public AddressSummaryDto? BillingAddress { get; init; }
    public string? RegistrationNumber { get; init; }
    public string? BusinessType { get; init; }
}

/// <summary>
/// Request to create a new company.
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
}

/// <summary>
/// Request to create a new address.
/// </summary>
public class CreateAddressRequest
{
    [Required]
    public string Type { get; set; } = "Billing";
    
    public bool IsDefault { get; set; } = true;
    
    [Required]
    public string AddressLine1 { get; set; } = string.Empty;
    
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? District { get; set; }
    
    [Required]
    public string City { get; set; } = string.Empty;
    
    [Required]
    public string StateProvince { get; set; } = string.Empty;
    
    [Required]
    public string PostalCode { get; set; } = string.Empty;
    
    [Required]
    public Guid CountryId { get; set; }

    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
}

/// <summary>
/// Request to create an NDA record.
/// </summary>
public class CreateNDARequest
{
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? FileReference { get; set; }
    public string? FileName { get; set; }
}

/// <summary>
/// DTO representing a country.
/// </summary>
public class CountryDto
{
    public Guid Id { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("iso2")]
    public string Code { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Represents a location in Thailand registry.
/// </summary>
public class RegistryThaiLocation
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("postalCode")]
    public string PostalCode { get; set; } = string.Empty;

    [JsonPropertyName("subDistrictTh")]
    public string SubDistrictTh { get; set; } = string.Empty;

    [JsonPropertyName("districtTh")]
    public string DistrictTh { get; set; } = string.Empty;

    [JsonPropertyName("provinceTh")]
    public string ProvinceTh { get; set; } = string.Empty;

    [JsonPropertyName("subDistrictEn")]
    public string SubDistrictEn { get; set; } = string.Empty;

    [JsonPropertyName("districtEn")]
    public string DistrictEn { get; set; } = string.Empty;

    [JsonPropertyName("provinceEn")]
    public string ProvinceEn { get; set; } = string.Empty;
}

/// <summary>
/// Company profile from Thailand registry.
/// </summary>
public class RegistryCompanyProfile
{
    [JsonPropertyName("statusCode")]
    public string StatusCode { get; set; } = string.Empty;

    [JsonPropertyName("statusNameTh")]
    public string StatusNameTh { get; set; } = string.Empty;

    [JsonPropertyName("taxId")]
    public string TaxId { get; set; } = string.Empty;

    [JsonPropertyName("companyNameTh")]
    public string CompanyNameTh { get; set; } = string.Empty;

    [JsonPropertyName("businessObjectives")]
    public string BusinessObjectives { get; set; } = string.Empty;

    [JsonPropertyName("companyTypeCode")]
    public string CompanyTypeCode { get; set; } = string.Empty;

    [JsonPropertyName("stockName")]
    public string? StockName { get; set; }

    [JsonPropertyName("fullNameTh")]
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
