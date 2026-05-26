using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Predefined document categories for customer documents.
/// </summary>
public static class DocumentCategories
{
    /// <summary>General purpose documents.</summary>
    public const string General = "General";
    /// <summary>Non-Disclosure Agreements.</summary>
    public const string NDA = "NDA";
    /// <summary>Legal and business contracts.</summary>
    public const string Contract = "Contract";
    /// <summary>Financial invoice documents.</summary>
    public const string Invoice = "Invoice";
    /// <summary>Tax registration and VAT certificates.</summary>
    public const string TaxRegistration = "Tax Registration";
    /// <summary>Business contact cards.</summary>
    public const string BusinessCard = "Business Card";
    /// <summary>Personal or company identification documents.</summary>
    public const string Identification = "Identification";
    /// <summary>Professional or quality certificates.</summary>
    public const string Certificate = "Certificate";
    /// <summary>Location and site maps.</summary>
    public const string Map = "Map";

    /// <summary>List of all supported document categories.</summary>
    public static readonly string[] All = [General, NDA, Contract, Invoice, TaxRegistration, BusinessCard, Identification, Certificate, Map];
}

/// <summary>
/// Represents a summary of a customer record for list views and selection.
/// </summary>
public class CustomerSummaryDto
{
    /// <summary>The unique identifier for the customer record.</summary>
    public Guid Id { get; set; }
    /// <summary>The display name of the customer.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The unique identifier of the company the customer belongs to, if any.</summary>
    public Guid? CompanyId { get; set; }
    /// <summary>The name of the company the customer belongs to.</summary>
    public string? CompanyName { get; set; }
    /// <summary>The primary email address for communication.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>The customer's profile image URL imported from an external identity provider.</summary>
    public string? ProfileImageUrl { get; set; }
    /// <summary>The mobile phone number of the customer.</summary>
    public string? Mobile { get; set; }
    /// <summary>The internal extension number for landline calls.</summary>
    public string? Extension { get; set; }
    /// <summary>The landline phone number of the customer.</summary>
    public string? Landline { get; set; }
    /// <summary>The general contact phone number of the customer's company.</summary>
    public string? CompanyPhone { get; set; }
    /// <summary>The current lifecycle status of the customer (e.g., Active, Lead).</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>The current status of the Non-Disclosure Agreement for this customer.</summary>
    public string? NdaStatus { get; set; }
    /// <summary>The market segment the customer belongs to (e.g., Retail, Enterprise).</summary>
    public string Segment { get; set; } = string.Empty;
    /// <summary>The loyalty or business tier of the customer (e.g., Bronze, Gold).</summary>
    public string Tier { get; set; } = string.Empty;
    /// <summary>The total amount spent by the customer across all orders.</summary>
    public decimal TotalSpent { get; set; }
    /// <summary>The current unpaid balance across all invoices.</summary>
    public decimal OutstandingBalance { get; set; }
    /// <summary>The date and time when the customer record was first created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Detailed customer information for comprehensive view and editing.
/// </summary>
public class CustomerDetailDto
{
    /// <summary>The unique identifier for the customer record.</summary>
    public Guid Id { get; set; }
    /// <summary>The IAM principal identifier linked to this customer account.</summary>
    public Guid? PrincipalId { get; set; }
    /// <summary>The full display name of the customer.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The first name of the customer.</summary>
    public string FirstName { get; set; } = string.Empty;
    /// <summary>The last name of the customer.</summary>
    public string LastName { get; set; } = string.Empty;
    /// <summary>The primary email address for communication.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>The customer's profile image URL imported from an external identity provider.</summary>
    public string? ProfileImageUrl { get; set; }
    /// <summary>The mobile phone number of the customer.</summary>
    public string? Mobile { get; set; }
    /// <summary>The internal extension number for landline calls.</summary>
    public string? Extension { get; set; }
    /// <summary>The landline phone number of the customer.</summary>
    public string? Landline { get; set; }
    /// <summary>The current lifecycle status of the customer.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>The market segment the customer belongs to.</summary>
    public string Segment { get; set; } = string.Empty;
    /// <summary>The loyalty or business tier of the customer.</summary>
    public string Tier { get; set; } = string.Empty;
    /// <summary>The preferred language for communication (ISO 639-1 code).</summary>
    public string PreferredLanguage { get; set; } = "en";
    /// <summary>The customer's local timezone (IANA format).</summary>
    public string Timezone { get; set; } = "UTC";
    /// <summary>The total amount spent by the customer across all orders.</summary>
    public decimal TotalSpent { get; set; }
    /// <summary>The number of currently active orders for this customer.</summary>
    public int ActiveOrdersCount { get; set; }
    /// <summary>The number of currently open quotations for this customer.</summary>
    public int OpenQuotationsCount { get; set; }
    /// <summary>The date and time when the customer record was first created.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>The unique identifier of the company the customer belongs to.</summary>
    public Guid? CompanyId { get; set; }
    /// <summary>The EmployeeService employee ID assigned as this customer's account manager.</summary>
    public Guid? AccountManagerEmployeeId { get; set; }
    /// <summary>The resolved display name of this customer's account manager, when loaded by the Intranet.</summary>
    public string? AccountManagerName { get; set; }
    /// <summary>The name of the company the customer belongs to.</summary>
    public string? CompanyName { get; set; }
    /// <summary>The general contact phone number of the customer's company.</summary>
    public string? CompanyPhone { get; set; }
    /// <summary>The company's VAT registration number.</summary>
    public string? CompanyVatNumber { get; set; }
    /// <summary>The company's legal registration number.</summary>
    public string? CompanyRegistrationNumber { get; set; }
    /// <summary>The primary contact email for the company.</summary>
    public string? CompanyContactEmail { get; set; }
    /// <summary>The market segment of the company.</summary>
    public string? CompanySegment { get; set; }
    /// <summary>The business tier of the company.</summary>
    public string? CompanyTier { get; set; }
    /// <summary>The identifier of the user who created this record.</summary>
    public string? CreatedBy { get; set; }
    /// <summary>The name of the user who created this record.</summary>
    public string? CreatedByName { get; set; }
    /// <summary>The email address of the user who created this record.</summary>
    public string? CreatedByEmail { get; set; }
    /// <summary>The default billing address of the company.</summary>
    public AddressResponse? CompanyBillingAddress { get; set; }
    /// <summary>List of all addresses associated with this customer.</summary>
    public List<AddressResponse> Addresses { get; set; } = [];
    /// <summary>List of all documents uploaded for this customer.</summary>
    public List<DocumentResponse> Documents { get; set; } = [];
    /// <summary>List of Non-Disclosure Agreements associated with this customer.</summary>
    public List<NDAResponse> Ndas { get; set; } = [];
    /// <summary>The most recent Non-Disclosure Agreement for this customer.</summary>
    public NDAResponse? Nda => Ndas.OrderByDescending(n => n.CreatedAt).FirstOrDefault();
    /// <summary>Internal notes and comments related to this customer.</summary>
    public List<InternalNoteResponse> Notes { get; set; } = [];
    /// <summary>Communication preference settings (e.g., email_opt_in).</summary>
    public Dictionary<string, bool> CommunicationPreferences { get; set; } = [];
    /// <summary>The payment terms configured for this customer.</summary>
    public string PaymentTerms { get; set; } = "Due on receipt";
    /// <summary>Concurrency version token for the customer record.</summary>
    public byte[] Version { get; set; } = [];
    /// <summary>The PostgreSQL xmin concurrency token returned by CustomerService.</summary>
    [JsonPropertyName("xmin")]
    public uint Xmin { get; set; }
}

/// <summary>
/// Request model for sending a customer email from the intranet.
/// </summary>
public class CustomerEmailRequest
{
    /// <summary>The email subject line.</summary>
    [Required]
    public string Subject { get; set; } = string.Empty;

    /// <summary>The email body content.</summary>
    [Required]
    public string Body { get; set; } = string.Empty;
}

/// <summary>
/// Payment term reference data for customer profile selection.
/// </summary>
public class PaymentTermDto
{
    /// <summary>Stable payment term code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Human-readable payment term label.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Payment term category used for grouping and filtering.</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>Description of how the payment term calculates payment timing.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Guidance describing when this term is typically used.</summary>
    public string TypicalUse { get; set; } = string.Empty;
    /// <summary>Number of calendar days until payment is due for day-based terms.</summary>
    public int? DueDays { get; set; }
    /// <summary>Early payment discount percentage, when one is available.</summary>
    public decimal? DiscountPercent { get; set; }
    /// <summary>Number of days the early payment discount is available.</summary>
    public int? DiscountDays { get; set; }
    /// <summary>Whether this payment term is the default for new customers.</summary>
    public bool IsDefault { get; set; }
    /// <summary>Sort order for presenting payment terms.</summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// Response model for address data, representing a physical or logical location.
/// </summary>
public class AddressResponse
{
    /// <summary>The unique identifier for the address record.</summary>
    public Guid Id { get; set; }
    /// <summary>The type of entity that owns this address (e.g., Customer, Company).</summary>
    public string OwnerType { get; set; } = string.Empty;
    /// <summary>The unique identifier of the owner of this address.</summary>
    public Guid OwnerId { get; set; }
    /// <summary>The type of address (e.g., Billing, Shipping, Home).</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Indicates if this is the default address for its type.</summary>
    public bool IsDefault { get; set; }
    /// <summary>User-facing place label such as Home, Work, or Other.</summary>
    [JsonPropertyName("placeLabel")]
    public string? PlaceLabel { get; set; }
    /// <summary>Custom label when <see cref="PlaceLabel"/> is Other.</summary>
    [JsonPropertyName("placeLabelOther")]
    public string? PlaceLabelOther { get; set; }
    /// <summary>The first line of the street address.</summary>
    public string AddressLine1 { get; set; } = string.Empty;
    /// <summary>The second line of the street address (e.g., apartment or suite).</summary>
    public string? AddressLine2 { get; set; }
    /// <summary>The third line of the street address.</summary>
    public string? AddressLine3 { get; set; }
    /// <summary>The district or sub-district name.</summary>
    public string? District { get; set; }
    /// <summary>The city name.</summary>
    public string City { get; set; } = string.Empty;
    /// <summary>The state, province, or region name.</summary>
    public string StateProvince { get; set; } = string.Empty;
    /// <summary>The postal or ZIP code.</summary>
    public string PostalCode { get; set; } = string.Empty;
    /// <summary>The unique identifier of the country record.</summary>
    public Guid CountryId { get; set; }
    /// <summary>The name of the recipient for deliveries to this address.</summary>
    public string? RecipientName { get; set; }
    /// <summary>The contact phone number for the recipient.</summary>
    public string? RecipientPhone { get; set; }
    /// <summary>Optional delivery note for the driver.</summary>
    [JsonPropertyName("driverNote")]
    public string? DriverNote { get; set; }
    /// <summary>Source used to populate the address: Manual, GooglePlace, or GoogleMapPin.</summary>
    [JsonPropertyName("addressSource")]
    public string AddressSource { get; set; } = "Manual";
    /// <summary>Google Places identifier when the address came from Google suggestions.</summary>
    [JsonPropertyName("googlePlaceId")]
    public string? GooglePlaceId { get; set; }
    /// <summary>Formatted address returned by Google.</summary>
    [JsonPropertyName("formattedAddress")]
    public string? FormattedAddress { get; set; }
    /// <summary>Latitude returned by Google address selection.</summary>
    [JsonPropertyName("latitude")]
    public decimal? Latitude { get; set; }
    /// <summary>Longitude returned by Google address selection.</summary>
    [JsonPropertyName("longitude")]
    public decimal? Longitude { get; set; }
    /// <summary>The date and time when the address record was created.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>The date and time when the address record was last updated.</summary>
    public DateTime UpdatedAt { get; set; }
    /// <summary>Concurrency version token for the address record.</summary>
    public byte[] Version { get; set; } = [];
    /// <summary>The PostgreSQL xmin concurrency token returned by CustomerService.</summary>
    [JsonPropertyName("xmin")]
    public uint Xmin { get; set; }
}

/// <summary>
/// Response model for Non-Disclosure Agreement (NDA) data.
/// </summary>
public class NDAResponse
{
    /// <summary>The unique identifier for the NDA record.</summary>
    public Guid Id { get; set; }
    /// <summary>The identifier of the customer this NDA belongs to.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>The identifier of the uploaded document record representing the signed NDA.</summary>
    public Guid? DocumentReferenceId { get; set; }
    /// <summary>The current status of the NDA (e.g., Pending, Signed, Expired).</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>The name or identifier of the person who signed the NDA.</summary>
    public string? SignedBy { get; set; }
    /// <summary>The date and time when the NDA was signed.</summary>
    public DateTime? SignedAt { get; set; }
    /// <summary>The date and time when the NDA was revoked, if applicable.</summary>
    public DateTime? RevokedAt { get; set; }
    /// <summary>The expiration date and time of the NDA.</summary>
    public DateTime? ExpiresAt { get; set; }
    /// <summary>The date and time when the NDA record was created.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>The date and time when the NDA record was last updated.</summary>
    public DateTime UpdatedAt { get; set; }
    /// <summary>Concurrency version token for the NDA record.</summary>
    public byte[] Version { get; set; } = [];
    /// <summary>The PostgreSQL xmin concurrency token returned by CustomerService.</summary>
    [JsonPropertyName("xmin")]
    public uint Xmin { get; set; }
}

/// <summary>
/// Request model for updating the status and details of a Non-Disclosure Agreement.
/// </summary>
public class UpdateNDAStatusRequest
{
    /// <summary>The new status to apply to the NDA.</summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>The name of the person who signed the agreement.</summary>
    public string? SignedBy { get; set; }

    /// <summary>The date the agreement was signed.</summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>The date the agreement was revoked.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>The reason for revoking the agreement.</summary>
    public string? RevokeReason { get; set; }

    /// <summary>The new expiration date for the agreement.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>The identifier of the document associated with this NDA update.</summary>
    public Guid? DocumentReferenceId { get; set; }

    /// <summary>Concurrency version token required for updates.</summary>
    [Required]
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Response model for NDA audit log data, tracking historical changes to an NDA.
/// </summary>
public class NDAAuditLogResponse
{
    /// <summary>The unique identifier for the audit log entry.</summary>
    public Guid Id { get; set; }
    /// <summary>The action performed (e.g., Created, Signed, Revoked).</summary>
    public string Action { get; set; } = string.Empty;
    /// <summary>The identifier of the user who performed the action.</summary>
    public string ActorId { get; set; } = string.Empty;
    /// <summary>The type of user who performed the action (e.g., Staff, Customer).</summary>
    public string ActorType { get; set; } = string.Empty;
    /// <summary>The display name of the user who performed the action.</summary>
    public string? ActorName { get; set; }
    /// <summary>The email address of the user who performed the action.</summary>
    public string? ActorEmail { get; set; }
    /// <summary>The date and time when the action occurred.</summary>
    public DateTime Timestamp { get; set; }
    /// <summary>The NDA status after the action.</summary>
    public string? Status { get; set; }
    /// <summary>The NDA status before the action.</summary>
    public string? PreviousStatus { get; set; }
    /// <summary>The expiration date set during this action.</summary>
    public DateTime? ExpiresAt { get; set; }
    /// <summary>The revocation date set during this action.</summary>
    public DateTime? RevokedAt { get; set; }
    /// <summary>The identifier of the document associated with this log entry.</summary>
    public Guid? DocumentReferenceId { get; set; }
    /// <summary>The name of the document associated with this log entry.</summary>
    public string? DocumentName { get; set; }
    /// <summary>The identifier of the previous document associated with the NDA.</summary>
    public Guid? PreviousDocumentReferenceId { get; set; }
    /// <summary>The name of the previous document associated with the NDA.</summary>
    public string? PreviousDocumentName { get; set; }
}

/// <summary>
/// Response model for internal notes associated with various entities.
/// </summary>
public class InternalNoteResponse
{
    /// <summary>The unique identifier for the internal note.</summary>
    public Guid Id { get; set; }
    /// <summary>The type of entity that owns this note (e.g., Customer).</summary>
    public string OwnerType { get; set; } = string.Empty;
    /// <summary>The unique identifier of the owner of this note.</summary>
    public Guid OwnerId { get; set; }
    /// <summary>The content of the internal note.</summary>
    public string NoteText { get; set; } = string.Empty;
    /// <summary>The identifier of the user who created the note.</summary>
    public string CreatedBy { get; set; } = string.Empty;
    /// <summary>The display name of the user who created the note.</summary>
    public string? CreatedByName { get; set; }
    /// <summary>The email address of the user who created the note.</summary>
    public string? CreatedByEmail { get; set; }
    /// <summary>The date and time when the note was created.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>The date and time when the note was last updated.</summary>
    public DateTime UpdatedAt { get; set; }
    /// <summary>Concurrency version token for the note record.</summary>
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Request model for creating a comment on an existing internal note.
/// </summary>
public class CreateInternalNoteCommentRequest
{
    /// <summary>The content of the comment.</summary>
    [Required]
    [StringLength(2000)]
    public string CommentText { get; set; } = string.Empty;
}

/// <summary>
/// Response model for data representing a comment on an internal note.
/// </summary>
public class InternalNoteCommentResponse
{
    /// <summary>The unique identifier for the comment record.</summary>
    public Guid Id { get; set; }
    /// <summary>The identifier of the parent internal note.</summary>
    public Guid InternalNoteId { get; set; }
    /// <summary>The content of the comment.</summary>
    public string CommentText { get; set; } = string.Empty;
    /// <summary>The identifier of the user who created the comment.</summary>
    public string CreatedBy { get; set; } = string.Empty;
    /// <summary>The display name of the user who created the comment.</summary>
    public string? CreatedByName { get; set; }
    /// <summary>The email address of the user who created the comment.</summary>
    public string? CreatedByEmail { get; set; }
    /// <summary>The date and time when the comment was created.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Concurrency version token for the comment record.</summary>
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Request model for creating a new internal note.
/// </summary>
public class CreateInternalNoteRequest
{
    /// <summary>The type of entity this note belongs to (e.g., Customer).</summary>
    [Required]
    public string OwnerType { get; set; } = string.Empty;

    /// <summary>The unique identifier of the target entity.</summary>
    [Required]
    public Guid OwnerId { get; set; }

    /// <summary>The content of the note.</summary>
    [Required]
    [StringLength(5000)]
    public string NoteText { get; set; } = string.Empty;
}

/// <summary>
/// Request model for updating an existing internal note's content.
/// </summary>
public class UpdateInternalNoteRequest
{
    /// <summary>The new content for the internal note.</summary>
    [Required]
    [StringLength(5000)]
    public string NoteText { get; set; } = string.Empty;

    /// <summary>Concurrency version token required for updates.</summary>
    [Required]
    public byte[] Version { get; set; } = [];
}

/// <summary>
/// Request model for creating a document record linked to a file storage reference.
/// </summary>
public class CreateDocumentRequest
{
    /// <summary>The category of the document (e.g., Contract, NDA).</summary>
    [Required]
    [StringLength(100)]
    public string DocumentCategory { get; set; } = string.Empty;

    /// <summary>A more specific sub-type for the document category.</summary>
    [StringLength(100)]
    public string? DocumentSubType { get; set; }

    /// <summary>The storage provider's reference or path to the file.</summary>
    [Required]
    [StringLength(500)]
    public string FileReference { get; set; } = string.Empty;

    /// <summary>The original name of the uploaded file.</summary>
    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>The size of the file in bytes.</summary>
    [Required]
    public long FileSize { get; set; }

    /// <summary>The standard MIME type of the file.</summary>
    [Required]
    [StringLength(100)]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>An optional descriptive text for the document.</summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>The order in which this document should be displayed in lists.</summary>
    public int? DisplayOrder { get; set; }
}

/// <summary>
/// Response model for document metadata and storage references.
/// </summary>
public class DocumentResponse
{
    /// <summary>The unique identifier for the document record.</summary>
    public Guid Id { get; set; }
    /// <summary>The type of entity that owns this document.</summary>
    public string OwnerType { get; set; } = string.Empty;
    /// <summary>The identifier of the owner entity.</summary>
    public Guid OwnerId { get; set; }
    /// <summary>The category of the document.</summary>
    [JsonPropertyName("documentType")]
    public string DocumentCategory { get; set; } = string.Empty;
    /// <summary>A more specific sub-type for the document category.</summary>
    public string? DocumentSubType { get; set; }
    /// <summary>The storage reference or path to the file.</summary>
    public string FileReference { get; set; } = string.Empty;
    /// <summary>The display name or file name of the document.</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>The size of the file in bytes.</summary>
    public long FileSize { get; set; }
    /// <summary>The MIME type of the file.</summary>
    public string MimeType { get; set; } = string.Empty;
    /// <summary>The identifier of the user who uploaded the document.</summary>
    public string CreatedBy { get; set; } = string.Empty;
    /// <summary>The display name of the user who uploaded the document.</summary>
    public string? CreatedByName { get; set; }
    /// <summary>The email address of the user who uploaded the document.</summary>
    public string? CreatedByEmail { get; set; }
    /// <summary>An optional descriptive text for the document.</summary>
    public string? Description { get; set; }
    /// <summary>The display sort order for the document.</summary>
    public int? DisplayOrder { get; set; }
    /// <summary>Indicates if the document record is currently active.</summary>
    public bool IsActive { get; set; }
    /// <summary>The date and time when the document record was created.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>The date and time when the document record was last updated.</summary>
    public DateTime UpdatedAt { get; set; }
    /// <summary>Version number for concurrency control.</summary>
    public int Version { get; set; }
}

/// <summary>
/// Request model for creating a new address record.
/// </summary>
public class CreateAddressRequest
{
    /// <summary>The optional identifier for the new address. Generated if not provided.</summary>
    public Guid? Id { get; set; }

    /// <summary>The type of address (e.g., Billing, Shipping).</summary>
    [Required]
    public string Type { get; set; } = "Billing";

    /// <summary>Whether this address should be set as the default for its type.</summary>
    public bool IsDefault { get; set; } = true;

    /// <summary>User-facing place label such as Home, Work, or Other.</summary>
    [JsonPropertyName("placeLabel")]
    public string? PlaceLabel { get; set; }

    /// <summary>Custom label when <see cref="PlaceLabel"/> is Other.</summary>
    [JsonPropertyName("placeLabelOther")]
    public string? PlaceLabelOther { get; set; }

    /// <summary>The primary street address line.</summary>
    [Required]
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>The secondary address line (apartment, suite, etc.).</summary>
    public string? AddressLine2 { get; set; }
    /// <summary>Additional address details.</summary>
    public string? AddressLine3 { get; set; }
    /// <summary>The district or sub-district.</summary>
    public string? District { get; set; }

    /// <summary>The city name.</summary>
    [Required]
    public string City { get; set; } = string.Empty;

    /// <summary>The state, province, or region.</summary>
    [Required]
    public string StateProvince { get; set; } = string.Empty;

    /// <summary>The postal or ZIP code.</summary>
    [Required]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>The identifier of the country.</summary>
    [Required]
    public Guid CountryId { get; set; }

    /// <summary>The name of the recipient at this address.</summary>
    public string? RecipientName { get; set; }
    /// <summary>The phone number for the recipient.</summary>
    public string? RecipientPhone { get; set; }

    /// <summary>Optional delivery note for the driver.</summary>
    [JsonPropertyName("driverNote")]
    public string? DriverNote { get; set; }

    /// <summary>Source used to populate the address: Manual, GooglePlace, or GoogleMapPin.</summary>
    [JsonPropertyName("addressSource")]
    public string AddressSource { get; set; } = "Manual";

    /// <summary>Google Places identifier when the address came from Google suggestions.</summary>
    [JsonPropertyName("googlePlaceId")]
    public string? GooglePlaceId { get; set; }

    /// <summary>Formatted address returned by Google.</summary>
    [JsonPropertyName("formattedAddress")]
    public string? FormattedAddress { get; set; }

    /// <summary>Latitude returned by Google address selection.</summary>
    [JsonPropertyName("latitude")]
    public decimal? Latitude { get; set; }

    /// <summary>Longitude returned by Google address selection.</summary>
    [JsonPropertyName("longitude")]
    public decimal? Longitude { get; set; }

    /// <summary>The optional concurrency version token.</summary>
    public byte[]? Version { get; set; }
}

/// <summary>
/// Request model for updating an existing address.
/// </summary>
public class UpdateAddressRequest
{
    /// <summary>The type of address (e.g., Billing, Shipping).</summary>
    public string? Type { get; set; }
    /// <summary>Indicates if this is the default address for its type.</summary>
    public bool? IsDefault { get; set; }
    /// <summary>User-facing place label such as Home, Work, or Other.</summary>
    [JsonPropertyName("placeLabel")]
    public string? PlaceLabel { get; set; }
    /// <summary>Custom label when <see cref="PlaceLabel"/> is Other.</summary>
    [JsonPropertyName("placeLabelOther")]
    public string? PlaceLabelOther { get; set; }
    /// <summary>The primary street address line.</summary>
    public string? AddressLine1 { get; set; }
    /// <summary>The secondary address line.</summary>
    public string? AddressLine2 { get; set; }
    /// <summary>Additional address details.</summary>
    public string? AddressLine3 { get; set; }
    /// <summary>The district or sub-district.</summary>
    public string? District { get; set; }
    /// <summary>The city name.</summary>
    public string? City { get; set; }
    /// <summary>The state, province, or region.</summary>
    public string? StateProvince { get; set; }
    /// <summary>The postal or ZIP code.</summary>
    public string? PostalCode { get; set; }
    /// <summary>The identifier of the country.</summary>
    public Guid? CountryId { get; set; }
    /// <summary>The name of the recipient.</summary>
    public string? RecipientName { get; set; }
    /// <summary>The phone number for the recipient.</summary>
    public string? RecipientPhone { get; set; }
    /// <summary>Optional delivery note for the driver.</summary>
    [JsonPropertyName("driverNote")]
    public string? DriverNote { get; set; }
    /// <summary>Source used to populate the address: Manual, GooglePlace, or GoogleMapPin.</summary>
    [JsonPropertyName("addressSource")]
    public string? AddressSource { get; set; }
    /// <summary>Google Places identifier when the address came from Google suggestions.</summary>
    [JsonPropertyName("googlePlaceId")]
    public string? GooglePlaceId { get; set; }
    /// <summary>Formatted address returned by Google.</summary>
    [JsonPropertyName("formattedAddress")]
    public string? FormattedAddress { get; set; }
    /// <summary>Latitude returned by Google address selection.</summary>
    [JsonPropertyName("latitude")]
    public decimal? Latitude { get; set; }
    /// <summary>Longitude returned by Google address selection.</summary>
    [JsonPropertyName("longitude")]
    public decimal? Longitude { get; set; }

    /// <summary>Concurrency version token required for updates.</summary>
    [Required]
    public byte[] Version { get; set; } = [];
    /// <summary>The PostgreSQL xmin concurrency token required by CustomerService.</summary>
    [JsonPropertyName("xmin")]
    public uint Xmin { get; set; }
}

/// <summary>
/// Request model for creating a new customer account.
/// </summary>
public class CreateCustomerRequest
{
    /// <summary>The first name of the customer.</summary>
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>The last name of the customer.</summary>
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>The primary email address for the customer.</summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>The mobile phone number of the customer.</summary>
    [RegularExpression(@"^$|^[+]?[0-9\s-]{7,20}$", ErrorMessage = "Invalid phone format")]
    [StringLength(20)]
    public string? Mobile { get; set; }

    /// <summary>The landline extension number.</summary>
    [StringLength(10)]
    public string? Extension { get; set; }

    /// <summary>The landline phone number.</summary>
    [RegularExpression(@"^$|^[+]?[0-9\s-]{7,20}$", ErrorMessage = "Invalid phone format")]
    [StringLength(20)]
    public string? Landline { get; set; }

    /// <summary>The initial market segment for the customer (e.g., Retail).</summary>
    [Required]
    public string Segment { get; set; } = "Retail";

    /// <summary>The initial loyalty tier for the customer (e.g., Bronze).</summary>
    [Required]
    public string Tier { get; set; } = "Bronze";

    /// <summary>The preferred language for communication (ISO 639-1 code).</summary>
    [Required]
    [StringLength(2)]
    public string PreferredLanguage { get; set; } = "en";

    /// <summary>The customer's local timezone (IANA format).</summary>
    [Required]
    [StringLength(50)]
    public string Timezone { get; set; } = "UTC";

    /// <summary>The identifier of the company this customer belongs to, if applicable.</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>The EmployeeService employee ID assigned as this customer's account manager.</summary>
    public Guid? AccountManagerEmployeeId { get; set; }

    /// <summary>Whether to automatically use the company's billing address for this customer.</summary>
    public bool UsesCompanyBillingAddress { get; set; } = true;

    /// <summary>Opt-in settings for various communication channels.</summary>
    public Dictionary<string, bool> CommunicationPreferences { get; set; } = new()
    {
        { "email_opt_in", true },
        { "sms_opt_in", false },
        { "marketing_opt_in", false }
    };

    /// <summary>The initial payment terms for the customer.</summary>
    [Required]
    [StringLength(100)]
    public string PaymentTerms { get; set; } = "Due on receipt";
}

/// <summary>
/// Request model for updating an existing customer's profile.
/// </summary>
public class UpdateCustomerRequest
{
    /// <summary>The first name of the customer.</summary>
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>The last name of the customer.</summary>
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>The primary email address for the customer.</summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>The mobile phone number of the customer.</summary>
    [RegularExpression(@"^$|^[+]?[0-9\s-]{7,20}$", ErrorMessage = "Invalid phone format")]
    [StringLength(20)]
    public string? Mobile { get; set; }

    /// <summary>The landline extension number.</summary>
    [StringLength(10)]
    public string? Extension { get; set; }

    /// <summary>The landline phone number.</summary>
    [RegularExpression(@"^$|^[+]?[0-9\s-]{7,20}$", ErrorMessage = "Invalid phone format")]
    [StringLength(20)]
    public string? Landline { get; set; }

    /// <summary>The market segment the customer belongs to.</summary>
    [Required]
    public string Segment { get; set; } = "Retail";

    /// <summary>The loyalty or business tier of the customer.</summary>
    [Required]
    public string Tier { get; set; } = "Bronze";

    /// <summary>The preferred language for communication.</summary>
    [Required]
    [StringLength(2)]
    public string PreferredLanguage { get; set; } = "en";

    /// <summary>The customer's local timezone.</summary>
    [Required]
    [StringLength(50)]
    public string Timezone { get; set; } = "UTC";

    /// <summary>The identifier of the company this customer belongs to, if applicable.</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>The EmployeeService employee ID assigned as this customer's account manager.</summary>
    public Guid? AccountManagerEmployeeId { get; set; }

    /// <summary>Clears the current account manager assignment when true.</summary>
    public bool ClearAccountManager { get; set; }

    /// <summary>Updated communication preference settings.</summary>
    public Dictionary<string, bool> CommunicationPreferences { get; set; } = new()
    {
        { "email_opt_in", true },
        { "sms_opt_in", false },
        { "marketing_opt_in", false }
    };

    /// <summary>The updated payment terms for the customer.</summary>
    [Required]
    [StringLength(100)]
    public string PaymentTerms { get; set; } = "Due on receipt";

    /// <summary>Concurrency version token required for updates.</summary>
    public byte[] Version { get; set; } = [];
    /// <summary>The PostgreSQL xmin concurrency token required by CustomerService.</summary>
    [JsonPropertyName("xmin")]
    public uint Xmin { get; set; }
}

/// <summary>
/// Response model for customer profile data.
/// </summary>
public class CustomerResponse
{
    /// <summary>The unique identifier for the customer record.</summary>
    public Guid Id { get; set; }
    /// <summary>The identifier of the corresponding IAM principal, if linked.</summary>
    public Guid? PrincipalId { get; set; }
    /// <summary>The first name of the customer.</summary>
    public string FirstName { get; set; } = string.Empty;
    /// <summary>The last name of the customer.</summary>
    public string LastName { get; set; } = string.Empty;
    /// <summary>The full display name of the customer.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The primary email address.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>The customer's profile image URL imported from an external identity provider.</summary>
    public string? ProfileImageUrl { get; set; }
    /// <summary>The mobile phone number.</summary>
    public string? Mobile { get; set; }
    /// <summary>The landline extension number.</summary>
    public string? Extension { get; set; }
    /// <summary>The name of the company the customer belongs to.</summary>
    public string? CompanyName { get; set; }
    /// <summary>The contact phone number of the company.</summary>
    public string? CompanyPhone { get; set; }
    /// <summary>The current status of the NDA for this customer.</summary>
    public string? NdaStatus { get; set; }
    /// <summary>The current lifecycle status of the customer record.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>The market segment the customer belongs to.</summary>
    public string Segment { get; set; } = string.Empty;
    /// <summary>The loyalty or business tier of the customer.</summary>
    public string Tier { get; set; } = string.Empty;
    /// <summary>The preferred language for communication.</summary>
    public string PreferredLanguage { get; set; } = string.Empty;
    /// <summary>The customer's local timezone.</summary>
    public string Timezone { get; set; } = string.Empty;
    /// <summary>The payment terms configured for this customer.</summary>
    public string PaymentTerms { get; set; } = "Due on receipt";
    /// <summary>The identifier of the associated company.</summary>
    public Guid? CompanyId { get; set; }
    /// <summary>The EmployeeService employee ID assigned as this customer's account manager.</summary>
    public Guid? AccountManagerEmployeeId { get; set; }
    /// <summary>Indicates if the customer record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }
    /// <summary>The date and time when the record was created.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>The date and time when the record was last updated.</summary>
    public DateTime UpdatedAt { get; set; }
    /// <summary>Concurrency version token for the record.</summary>
    public byte[] Version { get; set; } = [];
    /// <summary>The PostgreSQL xmin concurrency token returned by CustomerService.</summary>
    [JsonPropertyName("xmin")]
    public uint Xmin { get; set; }
}

/// <summary>
/// Request model for creating a new Non-Disclosure Agreement record.
/// </summary>
public class CreateNDARequest
{
    /// <summary>The expiration date and time for the agreement.</summary>
    public DateTime? ExpiresAt { get; set; }
    /// <summary>Whether the agreement is considered active upon creation.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>The storage reference for the signed agreement file.</summary>
    public string? FileReference { get; set; }
    /// <summary>The name of the uploaded agreement file.</summary>
    public string? FileName { get; set; }
}

/// <summary>
/// Composite request model for the full customer onboarding workflow, including company and addresses.
/// </summary>
public class CustomerOnboardingRequest
{
    /// <summary>The core customer profile information to create.</summary>
    [Required]
    public CreateCustomerRequest Customer { get; set; } = new();

    /// <summary>Optional request to create a new company and link the customer to it.</summary>
    public CreateCompanyRequest? NewCompany { get; set; }

    /// <summary>Initial set of addresses to associate with the customer.</summary>
    public List<CreateAddressRequest> Addresses { get; set; } = new();

    /// <summary>Optional billing address to associate with the newly created company.</summary>
    public CreateAddressRequest? CompanyBillingAddress { get; set; }

    /// <summary>Optional request to create an initial NDA record.</summary>
    public CreateNDARequest? Nda { get; set; }

    /// <summary>An initial internal note to add to the customer profile.</summary>
    public string? InternalNote { get; set; }

    /// <summary>
    /// All documents to upload during onboarding (NDA and customer documents).
    /// </summary>
    public List<CreateDocumentRequest> Documents { get; set; } = [];
}

/// <summary>
/// Request model for the Non-Disclosure Agreement step within the onboarding process.
/// </summary>
public class CreateNdaStepRequest
{
    /// <summary>The NDA details to create.</summary>
    [Required]
    public CreateNDARequest Nda { get; set; } = new();

    /// <summary>The list of documents uploaded during this step.</summary>
    public List<DocumentResponse> Documents { get; set; } = [];
}

/// <summary>
/// Response model for checking if an email address is already registered in the system.
/// </summary>
public class EmailExistsResponse
{
    /// <summary>Indicates if the email address was found in the database.</summary>
    public bool Exists { get; set; }
    /// <summary>The normalized email address that was checked.</summary>
    public string? Email { get; set; }
}

/// <summary>
/// Request model for triggering AI extraction of customer data from provided files or raw text.
/// </summary>
public class ExtractCustomerDataRequest
{
    /// <summary>List of storage paths for files to be analyzed.</summary>
    public List<string> FilePaths { get; set; } = new();
    /// <summary>Raw text content to be analyzed for customer information.</summary>
    public string? RawText { get; set; }
}

/// <summary>
/// Response model containing customer data extracted by AI analysis, with a confidence score.
/// </summary>
public class ExtractedCustomerDataResponse
{
    /// <summary>The extracted first name.</summary>
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    /// <summary>The extracted last name.</summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    /// <summary>The extracted email address.</summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>The extracted mobile phone number.</summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>The extracted landline phone number.</summary>
    [JsonPropertyName("landline")]
    public string? Landline { get; set; }

    /// <summary>The extracted phone extension.</summary>
    [JsonPropertyName("extension")]
    public string? Extension { get; set; }

    /// <summary>The detected market segment.</summary>
    [JsonPropertyName("segment")]
    public string? Segment { get; set; }

    /// <summary>The extracted company name.</summary>
    [JsonPropertyName("company_name")]
    public string? CompanyName { get; set; }

    /// <summary>The extracted company contact phone number.</summary>
    [JsonPropertyName("company_phone")]
    public string? CompanyPhone { get; set; }

    /// <summary>The extracted company VAT identification number.</summary>
    [JsonPropertyName("vat_number")]
    public string? VatNumber { get; set; }

    /// <summary>The extracted company branch number.</summary>
    [JsonPropertyName("branch_number")]
    public string? BranchNumber { get; set; }

    /// <summary>List of addresses extracted from the source material.</summary>
    [JsonPropertyName("addresses")]
    public List<ExtractedAddress>? Addresses { get; set; }

    /// <summary>Overall AI confidence score for the extraction (0.0 to 1.0).</summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

/// <summary>
/// Represents a physical or logical address extracted by AI analysis.
/// </summary>
public class ExtractedAddress
{
    /// <summary>The type of address (e.g., Office, Factory).</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>The first line of the address.</summary>
    [JsonPropertyName("address_line_1")]
    public string? AddressLine1 { get; set; }

    /// <summary>The second line of the address.</summary>
    [JsonPropertyName("address_line_2")]
    public string? AddressLine2 { get; set; }

    /// <summary>The third line of the address.</summary>
    [JsonPropertyName("address_line_3")]
    public string? AddressLine3 { get; set; }

    /// <summary>The district name.</summary>
    [JsonPropertyName("district")]
    public string? District { get; set; }

    /// <summary>The city name.</summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>The state or province name.</summary>
    [JsonPropertyName("state_province")]
    public string? StateProvince { get; set; }

    /// <summary>The postal or ZIP code.</summary>
    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; set; }

    /// <summary>The recipient's name for this address.</summary>
    [JsonPropertyName("recipient_name")]
    public string? RecipientName { get; set; }

    /// <summary>The recipient's contact phone for this address.</summary>
    [JsonPropertyName("recipient_phone")]
    public string? RecipientPhone { get; set; }

    /// <summary>
    /// The matched location object from the official registry, if found.
    /// </summary>
    [JsonPropertyName("location")]
    public RegistryThaiLocation? Location { get; set; }
}

/// <summary>
/// Represents a summary of a company found via search.
/// </summary>
public class CompanySearchResultDto
{
    /// <summary>The unique identifier for the company.</summary>
    public Guid? Id { get; set; }
    /// <summary>The full legal name of the company.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The company's VAT identification number.</summary>
    public string? VatNumber { get; set; }
    /// <summary>The company's official registration number.</summary>
    public string? RegistrationNumber { get; set; }
    /// <summary>The primary contact email for the company.</summary>
    public string? ContactEmail { get; set; }
    /// <summary>The primary contact phone number for the company.</summary>
    public string? ContactPhone { get; set; }
    /// <summary>The market segment the company belongs to.</summary>
    public string Segment { get; set; } = string.Empty;
    /// <summary>The business tier assigned to the company.</summary>
    public string Tier { get; set; } = string.Empty;
    /// <summary>The company's default billing address details.</summary>
    public AddressResponse? DefaultBillingAddress { get; set; }
    /// <summary>The origin of the result, such as Internal or Registry.</summary>
    public string? Source { get; set; }
    /// <summary>The business type returned by registry-backed search, if available.</summary>
    public string? BusinessType { get; set; }
}

/// <summary>
/// Response model for comprehensive company profile data.
/// </summary>
public class CompanyResponse
{
    /// <summary>The unique identifier for the company record.</summary>
    public Guid Id { get; set; }
    /// <summary>The full legal name of the company.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The company's VAT identification number.</summary>
    public string? VatNumber { get; set; }
    /// <summary>The company's official registration number.</summary>
    public string? RegistrationNumber { get; set; }
    /// <summary>The primary contact email for the company.</summary>
    public string? ContactEmail { get; set; }
    /// <summary>The primary contact phone number for the company.</summary>
    public string? ContactPhone { get; set; }
    /// <summary>The market segment the company belongs to.</summary>
    public string Segment { get; set; } = string.Empty;
    /// <summary>The business tier assigned to the company.</summary>
    public string Tier { get; set; } = string.Empty;
    /// <summary>The date and time when the company record was created.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>The date and time when the company record was last updated.</summary>
    public DateTime UpdatedAt { get; set; }
    /// <summary>Concurrency version token for the company record.</summary>
    public byte[] Version { get; set; } = [];
    /// <summary>The xmin system column value for database-level concurrency control.</summary>
    public uint xmin { get; set; }
    /// <summary>Brief information about the company's primary contact person.</summary>
    public CompanyPrimaryContactDto? PrimaryContact { get; set; }
}

/// <summary>
/// Lightweight DTO for representing the primary contact person for a company.
/// </summary>
public class CompanyPrimaryContactDto
{
    /// <summary>The unique identifier for the contact person.</summary>
    public Guid Id { get; set; }
    /// <summary>The full name of the contact person.</summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>The contact's primary email address.</summary>
    public string? Email { get; set; }
    /// <summary>The contact's mobile phone number.</summary>
    public string? Mobile { get; set; }
    /// <summary>Indicates if this person is designated as the primary point of contact.</summary>
    public bool IsPrimaryContact { get; set; }
}

/// <summary>
/// Represents a basic summary of a company record.
/// </summary>
public class CompanySummaryDto
{
    /// <summary>The unique identifier for the company.</summary>
    public Guid Id { get; set; }
    /// <summary>The legal name of the company.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The company's VAT registration number.</summary>
    public string? VatNumber { get; set; }
    /// <summary>The company's registration number.</summary>
    public string? RegistrationNumber { get; set; }
    /// <summary>The primary contact email.</summary>
    public string? ContactEmail { get; set; }
    /// <summary>The primary contact phone.</summary>
    public string? ContactPhone { get; set; }
    /// <summary>The company's market segment.</summary>
    public string Segment { get; set; } = string.Empty;
    /// <summary>The company's business tier.</summary>
    public string Tier { get; set; } = string.Empty;
}

/// <summary>
/// Request model for updating company profile details.
/// </summary>
public class UpdateCompanyRequest
{
    /// <summary>The new legal name for the company.</summary>
    public string? Name { get; set; }
    /// <summary>The new VAT identification number.</summary>
    public string? VatNumber { get; set; }
    /// <summary>The new official registration number.</summary>
    public string? RegistrationNumber { get; set; }
    /// <summary>The new primary contact email.</summary>
    public string? ContactEmail { get; set; }
    /// <summary>The new primary contact phone number.</summary>
    public string? ContactPhone { get; set; }
    /// <summary>The new market segment for the company.</summary>
    public string? Segment { get; set; }
    /// <summary>The new business tier for the company.</summary>
    public string? Tier { get; set; }
    /// <summary>The company's full name in Thai language, if applicable.</summary>
    public string? FullNameTh { get; set; }
    /// <summary>The stated business objectives of the company.</summary>
    public string? BusinessObjectives { get; set; }
    /// <summary>The xmin system column value for optimistic concurrency control.</summary>
    public uint xmin { get; set; }
}
