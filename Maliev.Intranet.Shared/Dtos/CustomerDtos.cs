using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Predefined document categories for customer documents.
/// </summary>
public static class DocumentCategories
{
    /// <summary>General document category.</summary>
    public const string General = "General";
    /// <summary>Non-Disclosure Agreement category.</summary>
    public const string NDA = "NDA";
    /// <summary>Legal contract category.</summary>
    public const string Contract = "Contract";
    /// <summary>Invoice category.</summary>
    public const string Invoice = "Invoice";
    /// <summary>Tax registration document category.</summary>
    public const string TaxRegistration = "Tax Registration";
    /// <summary>Business card image category.</summary>
    public const string BusinessCard = "Business Card";
    /// <summary>Identification document (ID/Passport) category.</summary>
    public const string Identification = "Identification";
    /// <summary>Certification or qualification category.</summary>
    public const string Certificate = "Certificate";
    /// <summary>Map or location document category.</summary>
    public const string Map = "Map";

    /// <summary>List of all document categories.</summary>
    public static readonly string[] All = [General, NDA, Contract, Invoice, TaxRegistration, BusinessCard, Identification, Certificate, Map];
}

/// <summary>
/// Represents a summary of a customer record.
/// </summary>
public class CustomerSummaryDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the customer's full name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the associated company ID.</summary>
    public Guid? CompanyId { get; set; }
    /// <summary>Gets or sets the company name.</summary>
    public string? CompanyName { get; set; }
    /// <summary>Gets or sets the email address.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Gets or sets the mobile phone number.</summary>
    public string? Mobile { get; set; }
    /// <summary>Gets or sets the phone extension.</summary>
    public string? Extension { get; set; }
    /// <summary>Gets or sets the landline phone number.</summary>
    public string? Landline { get; set; }
    /// <summary>Gets or sets the company phone number.</summary>
    public string? CompanyPhone { get; set; }
    /// <summary>Gets or sets a value indicating whether this is the main contact.</summary>
    public bool IsMainContact { get; set; }
    /// <summary>Gets or sets the customer status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the NDA status.</summary>
    public string? NdaStatus { get; set; }
    /// <summary>Gets or sets the customer segment.</summary>
    public string Segment { get; set; } = string.Empty;
    /// <summary>Gets or sets the customer tier.</summary>
    public string Tier { get; set; } = string.Empty;
    /// <summary>Gets or sets the total amount spent by the customer.</summary>
    public decimal TotalSpent { get; set; }
    /// <summary>Gets or sets the current outstanding balance.</summary>
    public decimal OutstandingBalance { get; set; }
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Detailed customer information for view/edit.
/// </summary>
public class CustomerDetailDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the full name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the first name.</summary>
    public string FirstName { get; set; } = string.Empty;
    /// <summary>Gets or sets the last name.</summary>
    public string LastName { get; set; } = string.Empty;
    /// <summary>Gets or sets the email address.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Gets or sets the mobile phone number.</summary>
    public string? Mobile { get; set; }
    /// <summary>Gets or sets the extension.</summary>
    public string? Extension { get; set; }
    /// <summary>Gets or sets the landline.</summary>
    public string? Landline { get; set; }
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the segment.</summary>
    public string Segment { get; set; } = string.Empty;
    /// <summary>Gets or sets the tier.</summary>
    public string Tier { get; set; } = string.Empty;
    /// <summary>Gets or sets the preferred language.</summary>
    public string PreferredLanguage { get; set; } = "en";
    /// <summary>Gets or sets the timezone.</summary>
    public string Timezone { get; set; } = "UTC";
    /// <summary>Gets or sets the total spent.</summary>
    public decimal TotalSpent { get; set; }
    /// <summary>Gets or sets the count of active orders.</summary>
    public int ActiveOrdersCount { get; set; }
    /// <summary>Gets or sets the count of open quotations.</summary>
    public int OpenQuotationsCount { get; set; }
    /// <summary>Gets or sets a value indicating whether this is the main contact.</summary>
    public bool IsMainContact { get; set; }
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the company ID.</summary>
    public Guid? CompanyId { get; set; }
    /// <summary>Gets or sets the company name.</summary>
    public string? CompanyName { get; set; }
    /// <summary>Gets or sets the company phone.</summary>
    public string? CompanyPhone { get; set; }
    /// <summary>Gets or sets the company VAT number.</summary>
    public string? CompanyVatNumber { get; set; }
    /// <summary>Gets or sets the company registration number.</summary>
    public string? CompanyRegistrationNumber { get; set; }
    /// <summary>Gets or sets the company contact email.</summary>
    public string? CompanyContactEmail { get; set; }
    /// <summary>Gets or sets the company segment.</summary>
    public string? CompanySegment { get; set; }
    /// <summary>Gets or sets the company tier.</summary>
    public string? CompanyTier { get; set; }
    /// <summary>Gets or sets the creator ID.</summary>
    public string? CreatedBy { get; set; }
    /// <summary>Gets or sets the creator's name.</summary>
    public string? CreatedByName { get; set; }
    /// <summary>Gets or sets the creator's email.</summary>
    public string? CreatedByEmail { get; set; }
    /// <summary>Gets or sets the company billing address.</summary>
    public AddressResponse? CompanyBillingAddress { get; set; }
    /// <summary>Gets or sets the list of addresses.</summary>
    public List<AddressResponse> Addresses { get; set; } = [];
    /// <summary>Gets or sets the list of documents.</summary>
    public List<DocumentResponse> Documents { get; set; } = [];
    /// <summary>Gets or sets the list of NDAs.</summary>
    public List<NDAResponse> Ndas { get; set; } = [];
    /// <summary>Gets the most recent NDA.</summary>
    public NDAResponse? Nda => Ndas.OrderByDescending(n => n.CreatedAt).FirstOrDefault();
    /// <summary>Gets or sets internal notes.</summary>
    public List<InternalNoteResponse> Notes { get; set; } = [];
    /// <summary>Gets or sets communication preferences.</summary>
    public Dictionary<string, bool> CommunicationPreferences { get; set; } = [];
    /// <summary>Gets or sets the version for concurrency.</summary>
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Response model for address data.
/// </summary>
public class AddressResponse
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the owner type (e.g., Customer, Company).</summary>
    public string OwnerType { get; set; } = string.Empty;
    /// <summary>Gets or sets the owner ID.</summary>
    public Guid OwnerId { get; set; }
    /// <summary>Gets or sets the address type (e.g., Billing, Shipping).</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether this is the default address.</summary>
    public bool IsDefault { get; set; }
    /// <summary>Gets or sets address line 1.</summary>
    public string AddressLine1 { get; set; } = string.Empty;
    /// <summary>Gets or sets address line 2.</summary>
    public string? AddressLine2 { get; set; }
    /// <summary>Gets or sets address line 3.</summary>
    public string? AddressLine3 { get; set; }
    /// <summary>Gets or sets the district.</summary>
    public string? District { get; set; }
    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;
    /// <summary>Gets or sets the state or province.</summary>
    public string StateProvince { get; set; } = string.Empty;
    /// <summary>Gets or sets the postal code.</summary>
    public string PostalCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the country ID.</summary>
    public Guid CountryId { get; set; }
    /// <summary>Gets or sets the recipient's name.</summary>
    public string? RecipientName { get; set; }
    /// <summary>Gets or sets the recipient's phone number.</summary>
    public string? RecipientPhone { get; set; }
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTime UpdatedAt { get; set; }
    /// <summary>Gets or sets the version.</summary>
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Response model for NDA data.
/// </summary>
public class NDAResponse
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the customer ID.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Gets or sets the document reference ID.</summary>
    public Guid? DocumentReferenceId { get; set; }
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets who signed the NDA.</summary>
    public string? SignedBy { get; set; }
    /// <summary>Gets or sets when it was signed.</summary>
    public DateTime? SignedAt { get; set; }
    /// <summary>Gets or sets when it was revoked.</summary>
    public DateTime? RevokedAt { get; set; }
    /// <summary>Gets or sets when it expires.</summary>
    public DateTime? ExpiresAt { get; set; }
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTime UpdatedAt { get; set; }
    /// <summary>Gets or sets the version.</summary>
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Request model for updating NDA status.
/// </summary>
public class UpdateNDAStatusRequest
{
    /// <summary>Gets or sets the new status.</summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets who signed it.</summary>
    public string? SignedBy { get; set; }

    /// <summary>Gets or sets the signing timestamp.</summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>Gets or sets the revocation timestamp.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Gets or sets the reason for revocation.</summary>
    public string? RevokeReason { get; set; }

    /// <summary>Gets or sets the expiration timestamp.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Gets or sets the document reference ID.</summary>
    public Guid? DocumentReferenceId { get; set; }

    /// <summary>Gets or sets the version for concurrency.</summary>
    [Required]
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Response model for NDA audit log data.
/// </summary>
public class NDAAuditLogResponse
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the action performed.</summary>
    public string Action { get; set; } = string.Empty;
    /// <summary>Gets or sets the actor ID.</summary>
    public string ActorId { get; set; } = string.Empty;
    /// <summary>Gets or sets the actor type.</summary>
    public string ActorType { get; set; } = string.Empty;
    /// <summary>Gets or sets the actor name.</summary>
    public string? ActorName { get; set; }
    /// <summary>Gets or sets the actor email.</summary>
    public string? ActorEmail { get; set; }
    /// <summary>Gets or sets the event timestamp.</summary>
    public DateTime Timestamp { get; set; }
    /// <summary>Gets or sets the new status.</summary>
    public string? Status { get; set; }
    /// <summary>Gets or sets the previous status.</summary>
    public string? PreviousStatus { get; set; }
    /// <summary>Gets or sets the expiration date.</summary>
    public DateTime? ExpiresAt { get; set; }
    /// <summary>Gets or sets the revocation date.</summary>
    public DateTime? RevokedAt { get; set; }
    /// <summary>Gets or sets the document reference ID.</summary>
    public Guid? DocumentReferenceId { get; set; }
    /// <summary>Gets or sets the document name.</summary>
    public string? DocumentName { get; set; }
    /// <summary>Gets or sets the previous document reference ID.</summary>
    public Guid? PreviousDocumentReferenceId { get; set; }
    /// <summary>Gets or sets the previous document name.</summary>
    public string? PreviousDocumentName { get; set; }
}

/// <summary>
/// Response model for internal note data.
/// </summary>
public class InternalNoteResponse
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the owner type.</summary>
    public string OwnerType { get; set; } = string.Empty;
    /// <summary>Gets or sets the owner ID.</summary>
    public Guid OwnerId { get; set; }
    /// <summary>Gets or sets the note text.</summary>
    public string NoteText { get; set; } = string.Empty;
    /// <summary>Gets or sets the creator ID.</summary>
    public string CreatedBy { get; set; } = string.Empty;
    /// <summary>Gets or sets the creator's name.</summary>
    public string? CreatedByName { get; set; }
    /// <summary>Gets or sets the creator's email.</summary>
    public string? CreatedByEmail { get; set; }
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTime UpdatedAt { get; set; }
    /// <summary>Gets or sets the version.</summary>
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Request model for creating a comment on an internal note.
/// </summary>
public class CreateInternalNoteCommentRequest
{
    /// <summary>Gets or sets the comment text.</summary>
    [Required]
    [StringLength(2000)]
    public string CommentText { get; set; } = string.Empty;
}

/// <summary>
/// Response model for internal note comment data.
/// </summary>
public class InternalNoteCommentResponse
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the associated internal note ID.</summary>
    public Guid InternalNoteId { get; set; }
    /// <summary>Gets or sets the comment text.</summary>
    public string CommentText { get; set; } = string.Empty;
    /// <summary>Gets or sets the creator ID.</summary>
    public string CreatedBy { get; set; } = string.Empty;
    /// <summary>Gets or sets the creator's name.</summary>
    public string? CreatedByName { get; set; }
    /// <summary>Gets or sets the creator's email.</summary>
    public string? CreatedByEmail { get; set; }
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the version.</summary>
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Request model for creating an internal note.
/// </summary>
public class CreateInternalNoteRequest
{
    /// <summary>Gets or sets the owner type (e.g., Customer, Order).</summary>
    [Required]
    public string OwnerType { get; set; } = string.Empty;

    /// <summary>Gets or sets the owner identifier.</summary>
    [Required]
    public Guid OwnerId { get; set; }

    /// <summary>Gets or sets the content of the note.</summary>
    [Required]
    [StringLength(5000)]
    public string NoteText { get; set; } = string.Empty;
}

/// <summary>
/// Request model for updating an internal note.
/// </summary>
public class UpdateInternalNoteRequest
{
    /// <summary>Gets or sets the updated content of the note.</summary>
    [Required]
    [StringLength(5000)]
    public string NoteText { get; set; } = string.Empty;

    /// <summary>Gets or sets the version for concurrency control.</summary>
    [Required]
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Request model for creating a document.
/// </summary>
public class CreateDocumentRequest
{
    /// <summary>Gets or sets the document category (e.g., NDA, Invoice).</summary>
    [Required]
    [StringLength(100)]
    public string DocumentCategory { get; set; } = string.Empty;

    /// <summary>Gets or sets the document sub-type.</summary>
    [StringLength(100)]
    public string? DocumentSubType { get; set; }

    /// <summary>Gets or sets the cloud storage file reference.</summary>
    [Required]
    [StringLength(500)]
    public string FileReference { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of the file.</summary>
    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the size of the file in bytes.</summary>
    [Required]
    public long FileSize { get; set; }

    /// <summary>Gets or sets the MIME type of the file.</summary>
    [Required]
    [StringLength(100)]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional description.</summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>Gets or sets the order in which to display the document.</summary>
    public int? DisplayOrder { get; set; }
}

/// <summary>
/// Response model for document data.
/// </summary>
public class DocumentResponse
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the type of owner.</summary>
    public string OwnerType { get; set; } = string.Empty;
    /// <summary>Gets or sets the ID of the owner.</summary>
    public Guid OwnerId { get; set; }
    /// <summary>Gets or sets the document category.</summary>
    [JsonPropertyName("documentType")]
    public string DocumentCategory { get; set; } = string.Empty;
    /// <summary>Gets or sets the document sub-type.</summary>
    public string? DocumentSubType { get; set; }
    /// <summary>Gets or sets the file reference.</summary>
    public string FileReference { get; set; } = string.Empty;
    /// <summary>Gets or sets the file name.</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>Gets or sets the file size.</summary>
    public long FileSize { get; set; }
    /// <summary>Gets or sets the MIME type.</summary>
    public string MimeType { get; set; } = string.Empty;
    /// <summary>Gets or sets the creator ID.</summary>
    public string CreatedBy { get; set; } = string.Empty;
    /// <summary>Gets or sets the creator's name.</summary>
    public string? CreatedByName { get; set; }
    /// <summary>Gets or sets the creator's email.</summary>
    public string? CreatedByEmail { get; set; }
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the display order.</summary>
    public int? DisplayOrder { get; set; }
    /// <summary>Gets or sets a value indicating whether the document is active.</summary>
    public bool IsActive { get; set; }
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the update timestamp.</summary>
    public DateTime UpdatedAt { get; set; }
    /// <summary>Gets or sets the version number.</summary>
    public int Version { get; set; }
    /// <summary>Gets or sets the row version byte array.</summary>
    public byte[] RowVersion { get; set; } = [];
}

/// <summary>
/// Request model for creating a new address.
/// </summary>
public class CreateAddressRequest
{
    /// <summary>Gets or sets the optional ID.</summary>
    public Guid? Id { get; set; }

    /// <summary>Gets or sets the address type (e.g., Billing, Shipping).</summary>
    [Required]
    public string Type { get; set; } = "Billing";

    /// <summary>Gets or sets a value indicating whether this is the default address.</summary>
    public bool IsDefault { get; set; } = true;

    /// <summary>Gets or sets address line 1.</summary>
    [Required]
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>Gets or sets address line 2.</summary>
    public string? AddressLine2 { get; set; }
    /// <summary>Gets or sets address line 3.</summary>
    public string? AddressLine3 { get; set; }
    /// <summary>Gets or sets the district.</summary>
    public string? District { get; set; }

    /// <summary>Gets or sets the city.</summary>
    [Required]
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the state or province.</summary>
    [Required]
    public string StateProvince { get; set; } = string.Empty;

    /// <summary>Gets or sets the postal code.</summary>
    [Required]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the country identifier.</summary>
    [Required]
    public Guid CountryId { get; set; }

    /// <summary>Gets or sets the recipient name.</summary>
    public string? RecipientName { get; set; }
    /// <summary>Gets or sets the recipient phone number.</summary>
    public string? RecipientPhone { get; set; }

    /// <summary>Gets or sets the version.</summary>
    public byte[]? Version { get; set; }
}

/// <summary>
/// Request model for updating an existing address.
/// </summary>
public class UpdateAddressRequest
{
    /// <summary>Gets or sets the address type.</summary>
    public string? Type { get; set; }
    /// <summary>Gets or sets whether it is the default address.</summary>
    public bool? IsDefault { get; set; }
    /// <summary>Gets or sets address line 1.</summary>
    public string? AddressLine1 { get; set; }
    /// <summary>Gets or sets address line 2.</summary>
    public string? AddressLine2 { get; set; }
    /// <summary>Gets or sets address line 3.</summary>
    public string? AddressLine3 { get; set; }
    /// <summary>Gets or sets the district.</summary>
    public string? District { get; set; }
    /// <summary>Gets or sets the city.</summary>
    public string? City { get; set; }
    /// <summary>Gets or sets the state or province.</summary>
    public string? StateProvince { get; set; }
    /// <summary>Gets or sets the postal code.</summary>
    public string? PostalCode { get; set; }
    /// <summary>Gets or sets the country ID.</summary>
    public Guid? CountryId { get; set; }
    /// <summary>Gets or sets the recipient name.</summary>
    public string? RecipientName { get; set; }
    /// <summary>Gets or sets the recipient phone.</summary>
    public string? RecipientPhone { get; set; }

    /// <summary>Gets or sets the version for concurrency.</summary>
    [Required]
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Request model for creating a new customer.
/// </summary>
public class CreateCustomerRequest
{
    /// <summary>Gets or sets the first name.</summary>
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets the last name.</summary>
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets the email address.</summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the mobile phone number.</summary>
    [RegularExpression(@"^$|^[+]?[0-9\s-]{7,20}$", ErrorMessage = "Invalid phone format")]
    [StringLength(20)]
    public string? Mobile { get; set; }

    /// <summary>Gets or sets the phone extension.</summary>
    [StringLength(10)]
    public string? Extension { get; set; }

    /// <summary>Gets or sets the landline phone number.</summary>
    [RegularExpression(@"^$|^[+]?[0-9\s-]{7,20}$", ErrorMessage = "Invalid phone format")]
    [StringLength(20)]
    public string? Landline { get; set; }

    /// <summary>Gets or sets the customer segment (e.g., Retail, Corporate).</summary>
    [Required]
    public string Segment { get; set; } = "Retail";

    /// <summary>Gets or sets the customer tier (e.g., Bronze, Silver, Gold).</summary>
    [Required]
    public string Tier { get; set; } = "Bronze";

    /// <summary>Gets or sets the preferred language code.</summary>
    [Required]
    [StringLength(2)]
    public string PreferredLanguage { get; set; } = "en";

    /// <summary>Gets or sets the timezone ID.</summary>
    [Required]
    [StringLength(50)]
    public string Timezone { get; set; } = "UTC";

    /// <summary>Gets or sets the associated company ID.</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>Gets or sets a value indicating whether to use the company's billing address.</summary>
    public bool UsesCompanyBillingAddress { get; set; } = true;

    /// <summary>Gets or sets communication channel preferences.</summary>
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
    /// <summary>Gets or sets the first name.</summary>
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets the last name.</summary>
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets the email address.</summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the mobile phone.</summary>
    [RegularExpression(@"^$|^[+]?[0-9\s-]{7,20}$", ErrorMessage = "Invalid phone format")]
    [StringLength(20)]
    public string? Mobile { get; set; }

    /// <summary>Gets or sets the phone extension.</summary>
    [StringLength(10)]
    public string? Extension { get; set; }

    /// <summary>Gets or sets the landline phone.</summary>
    [RegularExpression(@"^$|^[+]?[0-9\s-]{7,20}$", ErrorMessage = "Invalid phone format")]
    [StringLength(20)]
    public string? Landline { get; set; }

    /// <summary>Gets or sets the segment.</summary>
    [Required]
    public string Segment { get; set; } = "Retail";

    /// <summary>Gets or sets the tier.</summary>
    [Required]
    public string Tier { get; set; } = "Bronze";

    /// <summary>Gets or sets the preferred language.</summary>
    [Required]
    [StringLength(2)]
    public string PreferredLanguage { get; set; } = "en";

    /// <summary>Gets or sets the timezone.</summary>
    [Required]
    [StringLength(50)]
    public string Timezone { get; set; } = "UTC";

    /// <summary>Gets or sets communication preferences.</summary>
    public Dictionary<string, bool> CommunicationPreferences { get; set; } = new()
    {
        { "email_opt_in", true },
        { "sms_opt_in", false },
        { "marketing_opt_in", false }
    };

    /// <summary>Gets or sets the version for concurrency.</summary>
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Response model for customer data.
/// </summary>
public class CustomerResponse
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the principal identifier.</summary>
    public Guid? PrincipalId { get; set; }
    /// <summary>Gets or sets the first name.</summary>
    public string FirstName { get; set; } = string.Empty;
    /// <summary>Gets or sets the last name.</summary>
    public string LastName { get; set; } = string.Empty;
    /// <summary>Gets or sets the full name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the email address.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Gets or sets the mobile phone.</summary>
    public string? Mobile { get; set; }
    /// <summary>Gets or sets the extension.</summary>
    public string? Extension { get; set; }
    /// <summary>Gets or sets the company name.</summary>
    public string? CompanyName { get; set; }
    /// <summary>Gets or sets the company phone.</summary>
    public string? CompanyPhone { get; set; }
    /// <summary>Gets or sets the NDA status.</summary>
    public string? NdaStatus { get; set; }
    /// <summary>Gets or sets whether this is the main contact.</summary>
    public bool IsMainContact { get; set; }
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the segment.</summary>
    public string Segment { get; set; } = string.Empty;
    /// <summary>Gets or sets the tier.</summary>
    public string Tier { get; set; } = string.Empty;
    /// <summary>Gets or sets the preferred language.</summary>
    public string PreferredLanguage { get; set; } = string.Empty;
    /// <summary>Gets or sets the timezone.</summary>
    public string Timezone { get; set; } = string.Empty;
    /// <summary>Gets or sets the company ID.</summary>
    public Guid? CompanyId { get; set; }
    /// <summary>Gets or sets a value indicating whether the customer is deleted.</summary>
    public bool IsDeleted { get; set; }
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTime UpdatedAt { get; set; }
    /// <summary>Gets or sets the version.</summary>
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Request model for creating an NDA record.
/// </summary>
public class CreateNDARequest
{
    /// <summary>Gets or sets the expiration timestamp.</summary>
    public DateTime? ExpiresAt { get; set; }
    /// <summary>Gets or sets a value indicating whether the NDA is active.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>Gets or sets the file reference.</summary>
    public string? FileReference { get; set; }
    /// <summary>Gets or sets the file name.</summary>
    public string? FileName { get; set; }
}

/// <summary>
/// Composite request for customer onboarding.
/// </summary>
public class CustomerOnboardingRequest
{
    /// <summary>Gets or sets the customer creation data.</summary>
    [Required]
    public CreateCustomerRequest Customer { get; set; } = new();

    /// <summary>Gets or sets optional new company data.</summary>
    public CreateCompanyRequest? NewCompany { get; set; }

    /// <summary>Gets or sets the list of addresses to create.</summary>
    public List<CreateAddressRequest> Addresses { get; set; } = new();

    /// <summary>Gets or sets optional initial NDA data.</summary>
    public CreateNDARequest? Nda { get; set; }

    /// <summary>Gets or sets an optional internal note.</summary>
    public string? InternalNote { get; set; }

    /// <summary>
    /// All documents (both NDA and customer documents).
    /// NDA documents have DocumentCategory = "NDA".
    /// </summary>
    public List<CreateDocumentRequest> Documents { get; set; } = [];
}

/// <summary>
/// Request for the NDA creation step during onboarding.
/// </summary>
public class CreateNdaStepRequest
{
    /// <summary>Gets or sets the NDA data.</summary>
    [Required]
    public CreateNDARequest Nda { get; set; } = new();

    /// <summary>Gets or sets the associated documents.</summary>
    public List<DocumentResponse> Documents { get; set; } = [];
}

/// <summary>
/// Response model for email existence check.
/// </summary>
public class EmailExistsResponse
{
    /// <summary>Gets or sets whether the email exists.</summary>
    public bool Exists { get; set; }
    /// <summary>Gets or sets the email address.</summary>
    public string? Email { get; set; }
}

/// <summary>
/// Request model for extracting customer data from files or text.
/// </summary>
public class ExtractCustomerDataRequest
{
    /// <summary>Gets or sets the list of file paths to analyze.</summary>
    public List<string> FilePaths { get; set; } = new();
    /// <summary>Gets or sets the raw text to analyze.</summary>
    public string? RawText { get; set; }
}

/// <summary>
/// AI-extracted customer data with confidence score.
/// </summary>
public class ExtractedCustomerDataResponse
{
    /// <summary>Extracted first name.</summary>
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    /// <summary>Extracted last name.</summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    /// <summary>Extracted email address.</summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>Extracted mobile phone.</summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>Extracted landline phone.</summary>
    [JsonPropertyName("landline")]
    public string? Landline { get; set; }

    /// <summary>Extracted phone extension.</summary>
    [JsonPropertyName("extension")]
    public string? Extension { get; set; }

    /// <summary>Extracted customer segment.</summary>
    [JsonPropertyName("segment")]
    public string? Segment { get; set; }

    /// <summary>Extracted company name.</summary>
    [JsonPropertyName("company_name")]
    public string? CompanyName { get; set; }

    /// <summary>Extracted company phone.</summary>
    [JsonPropertyName("company_phone")]
    public string? CompanyPhone { get; set; }

    /// <summary>Extracted VAT number.</summary>
    [JsonPropertyName("vat_number")]
    public string? VatNumber { get; set; }

    /// <summary>Extracted business branch number.</summary>
    [JsonPropertyName("branch_number")]
    public string? BranchNumber { get; set; }

    /// <summary>Extracted addresses.</summary>
    [JsonPropertyName("addresses")]
    public List<ExtractedAddress>? Addresses { get; set; }

    /// <summary>Confidence score of the extraction (0 to 1).</summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

/// <summary>
/// An address extracted by AI, with structured fields.
/// </summary>
public class ExtractedAddress
{
    /// <summary>Extracted address type.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Extracted address line 1.</summary>
    [JsonPropertyName("address_line_1")]
    public string? AddressLine1 { get; set; }

    /// <summary>Extracted address line 2.</summary>
    [JsonPropertyName("address_line_2")]
    public string? AddressLine2 { get; set; }

    /// <summary>Extracted address line 3.</summary>
    [JsonPropertyName("address_line_3")]
    public string? AddressLine3 { get; set; }

    /// <summary>Extracted district.</summary>
    [JsonPropertyName("district")]
    public string? District { get; set; }

    /// <summary>Extracted city.</summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>Extracted state or province.</summary>
    [JsonPropertyName("state_province")]
    public string? StateProvince { get; set; }

    /// <summary>Extracted postal code.</summary>
    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; set; }

    /// <summary>Extracted recipient name.</summary>
    [JsonPropertyName("recipient_name")]
    public string? RecipientName { get; set; }

    /// <summary>Extracted recipient phone.</summary>
    [JsonPropertyName("recipient_phone")]
    public string? RecipientPhone { get; set; }

    /// <summary>
    /// The matched location object from Registry (if found).
    /// </summary>
    [JsonPropertyName("location")]
    public RegistryThaiLocation? Location { get; set; }
}

/// <summary>
/// Represents a search result for a company.
/// </summary>
public class CompanySearchResultDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
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
    /// <summary>Gets or sets the main contact ID.</summary>
    public Guid? MainContactId { get; set; }
    /// <summary>Gets or sets the main contact name.</summary>
    public string? MainContactName { get; set; }
    /// <summary>Gets or sets the segment.</summary>
    public string Segment { get; set; } = string.Empty;
}

/// <summary>
/// Response model for company data.
/// </summary>
public class CompanyResponse
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
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
    /// <summary>Gets or sets the website URL.</summary>
    public string? WebsiteUrl { get; set; }
    /// <summary>Gets or sets the main contact ID.</summary>
    public Guid? MainContactId { get; set; }
    /// <summary>Gets or sets the main contact name.</summary>
    public string? MainContactName { get; set; }
    /// <summary>Gets or sets the segment.</summary>
    public string Segment { get; set; } = string.Empty;
    /// <summary>Gets or sets the tier.</summary>
    public string Tier { get; set; } = string.Empty;
    /// <summary>Gets or sets whether company is verified from BDEX.</summary>
    public bool IsVerifiedFromBdex { get; set; }
    /// <summary>Gets or sets the current year purchase value (THB).</summary>
    public decimal CurrentYearPurchaseValue { get; set; }
    /// <summary>Gets or sets the current year order count.</summary>
    public int CurrentYearOrderCount { get; set; }
    /// <summary>Gets or sets when tier was last calculated.</summary>
    public DateTime? TierCalculatedAt { get; set; }
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTime UpdatedAt { get; set; }
    /// <summary>Gets or sets the version.</summary>
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Represents a company summary.
/// </summary>
public class CompanySummaryDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
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
    /// <summary>Gets or sets the main contact ID.</summary>
    public Guid? MainContactId { get; set; }
    /// <summary>Gets or sets the main contact name.</summary>
    public string? MainContactName { get; set; }
    /// <summary>Gets or sets the main contact email.</summary>
    public string? MainContactEmail { get; set; }
    /// <summary>Gets or sets the segment.</summary>
    public string Segment { get; set; } = string.Empty;
    /// <summary>Gets or sets the tier.</summary>
    public string Tier { get; set; } = string.Empty;
}
