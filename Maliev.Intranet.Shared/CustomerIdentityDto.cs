namespace Maliev.Intranet.Shared;

/// <summary>
/// Customer identity information for billing identity selection
/// </summary>
public class CustomerIdentityDto
{
    /// <summary>
    /// Customer unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Customer first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Customer last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Masked Thai National ID (e.g., "***-****-**34")
    /// Null if customer doesn't have a Thai National ID
    /// </summary>
    public string? ThaiNationalIdMasked { get; set; }

    /// <summary>
    /// Linked company identifier (null if no company)
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// Linked company name (null if no company)
    /// </summary>
    public string? CompanyName { get; set; }

    /// <summary>
    /// Linked company tax ID / VAT number (null if no company)
    /// </summary>
    public string? CompanyTaxId { get; set; }
}
