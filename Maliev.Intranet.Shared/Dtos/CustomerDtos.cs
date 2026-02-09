using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Represents a summary of a customer record.
/// </summary>
public class CustomerSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Extension { get; set; }
    public string? Landline { get; set; }
    public string? CompanyPhone { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? NdaStatus { get; set; }
    public string Segment { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public decimal TotalSpent { get; set; }
    public decimal OutstandingBalance { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Detailed customer information for view/edit.
/// </summary>
public class CustomerDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Extension { get; set; }
    public string? Landline { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "en";
    public string Timezone { get; set; } = "UTC";
    public decimal TotalSpent { get; set; }
    public int ActiveOrdersCount { get; set; }
    public int OpenQuotationsCount { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyPhone { get; set; }
    public List<AddressResponse> Addresses { get; set; } = [];
    public NDAResponse? Nda { get; set; }
    public List<InternalNoteResponse> Notes { get; set; } = [];
    public Dictionary<string, bool> CommunicationPreferences { get; set; } = [];
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Response model for address data.
/// </summary>
public class AddressResponse
{
    public Guid Id { get; set; }
    public string OwnerType { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? District { get; set; }
    public string City { get; set; } = string.Empty;
    public string StateProvince { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public Guid CountryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Response model for NDA data.
/// </summary>
public class NDAResponse
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? DocumentReferenceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SignedBy { get; set; }
    public DateTime? SignedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Response model for internal note data.
/// </summary>
public class InternalNoteResponse
{
    public Guid Id { get; set; }
    public string OwnerType { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public string NoteText { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Request model for creating a document.
/// </summary>
public class CreateDocumentRequest
{
    [Required]
    [StringLength(100)]
    public string DocumentCategory { get; set; } = string.Empty;

    [StringLength(100)]
    public string? DocumentSubType { get; set; }

    [Required]
    [StringLength(500)]
    public string FileReference { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public long FileSize { get; set; }

    [Required]
    [StringLength(100)]
    public string MimeType { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public int? DisplayOrder { get; set; }
}

/// <summary>
/// Response model for document data.
/// </summary>
public class DocumentResponse
{
    public Guid Id { get; set; }
    public string OwnerType { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public string DocumentCategory { get; set; } = string.Empty;
    public string? DocumentSubType { get; set; }
    public string FileReference { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Request model for creating a new customer.
/// </summary>
public class CreateCustomerRequest
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(20)]
    public string? Mobile { get; set; }

    [StringLength(10)]
    public string? Extension { get; set; }

    [Phone]
    [StringLength(20)]
    public string? Landline { get; set; }

    [Required]
    public string Segment { get; set; } = "Retail";

    [Required]
    public string Tier { get; set; } = "Bronze";

    [Required]
    [StringLength(2)]
    public string PreferredLanguage { get; set; } = "en";

    [Required]
    [StringLength(50)]
    public string Timezone { get; set; } = "UTC";

    public Guid? CompanyId { get; set; }

    public bool UsesCompanyBillingAddress { get; set; } = true;

    public Dictionary<string, bool> CommunicationPreferences { get; set; } = new()
    {
        { "email_opt_in", true },
        { "sms_opt_in", false },
        { "marketing_opt_in", false }
    };
}

/// <summary>
/// Request model for updating an existing customer.
/// </summary>
public class UpdateCustomerRequest
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(20)]
    public string? Mobile { get; set; }

    [StringLength(10)]
    public string? Extension { get; set; }

    [Phone]
    [StringLength(20)]
    public string? Landline { get; set; }

    [Required]
    public string Segment { get; set; } = "Retail";

    [Required]
    public string Tier { get; set; } = "Bronze";

    [Required]
    [StringLength(2)]
    public string PreferredLanguage { get; set; } = "en";

    [Required]
    [StringLength(50)]
    public string Timezone { get; set; } = "UTC";

    public Dictionary<string, bool> CommunicationPreferences { get; set; } = new()
    {
        { "email_opt_in", true },
        { "sms_opt_in", false },
        { "marketing_opt_in", false }
    };

    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Response model for customer data.
/// </summary>
public class CustomerResponse
{
    public Guid Id { get; set; }
    public Guid? PrincipalId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Extension { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyPhone { get; set; }
    public string? NdaStatus { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Composite request for customer onboarding.
/// </summary>
public class CustomerOnboardingRequest
{
    [Required]
    public CreateCustomerRequest Customer { get; set; } = new();

    public CreateCompanyRequest? NewCompany { get; set; }

    public List<CreateAddressRequest> Addresses { get; set; } = new();

    public CreateNDARequest? Nda { get; set; }

    public string? InternalNote { get; set; }

    /// <summary>
    /// All documents (both NDA and customer documents).
    /// NDA documents have DocumentCategory = "NDA".
    /// </summary>
    public List<CreateDocumentRequest> Documents { get; set; } = [];
}

/// <summary>
/// Request model for extracting customer data from files or text.
/// </summary>
public class ExtractCustomerDataRequest
{
    public List<string> FilePaths { get; set; } = new();
    public string? RawText { get; set; }
}

/// <summary>
/// AI-extracted customer data with confidence score.
/// </summary>
public class ExtractedCustomerDataResponse
{
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }
    
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }
    
    [JsonPropertyName("landline")]
    public string? Landline { get; set; }
    
    [JsonPropertyName("extension")]
    public string? Extension { get; set; }
    
    [JsonPropertyName("segment")]
    public string? Segment { get; set; }
    
    [JsonPropertyName("company_name")]
    public string? CompanyName { get; set; }
    
    [JsonPropertyName("company_phone")]
    public string? CompanyPhone { get; set; }
    
    [JsonPropertyName("vat_number")]
    public string? VatNumber { get; set; }
    
    [JsonPropertyName("branch_number")]
    public string? BranchNumber { get; set; }
    
    [JsonPropertyName("addresses")]
    public List<ExtractedAddress>? Addresses { get; set; }
    
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

/// <summary>
/// An address extracted by AI, with structured fields.
/// </summary>
public class ExtractedAddress
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("address_line_1")]
    public string? AddressLine1 { get; set; }
    
    [JsonPropertyName("address_line_2")]
    public string? AddressLine2 { get; set; }
    
    [JsonPropertyName("address_line_3")]
    public string? AddressLine3 { get; set; }
    
    [JsonPropertyName("district")]
    public string? District { get; set; }
    
    [JsonPropertyName("city")]
    public string? City { get; set; }
    
    [JsonPropertyName("state_province")]
    public string? StateProvince { get; set; }
    
    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; set; }
    
    [JsonPropertyName("recipient_name")]
    public string? RecipientName { get; set; }
    
    [JsonPropertyName("recipient_phone")]
    public string? RecipientPhone { get; set; }

    /// <summary>
    /// The matched location object from Registry (if found).
    /// </summary>
    [JsonPropertyName("location")]
    public RegistryThaiLocation? Location { get; set; }
}
