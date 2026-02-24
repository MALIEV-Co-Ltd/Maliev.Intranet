using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Data transfer object for a compliance record.
/// </summary>
public sealed record ComplianceRecordDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the employee ID.</summary>
    public Guid EmployeeId { get; set; }
    /// <summary>Gets or sets the compliance type.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Gets or sets the date of compliance.</summary>
    public DateTime Date { get; set; }
    /// <summary>Gets or sets the expiration date, if applicable.</summary>
    public DateTime? ExpiryDate { get; set; }
    /// <summary>Gets or sets the current status.</summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Request model for creating a compliance record.
/// </summary>
public sealed record CreateComplianceRecordRequest
{
    /// <summary>Gets or sets the employee ID.</summary>
    [Required]
    public Guid EmployeeId { get; set; }

    /// <summary>Gets or sets the compliance type.</summary>
    [Required]
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the date of compliance.</summary>
    [Required]
    public DateTime Date { get; set; }

    /// <summary>Gets or sets the optional expiration date.</summary>
    public DateTime? ExpiryDate { get; set; }
}
