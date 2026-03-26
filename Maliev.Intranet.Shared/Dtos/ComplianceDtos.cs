using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Data transfer object representing a compliance or regulatory record for an employee.
/// </summary>
public sealed record ComplianceRecordDto
{
    /// <summary>
    /// Unique identifier for the compliance record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The unique identifier of the employee associated with this compliance record.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// The category or type of compliance record (e.g., SafetyTraining, Certification).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The date the compliance requirement was met or the record was created.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// The optional expiration date for the compliance record or certification.
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// The current status of the compliance record (e.g., Active, Expired, Pending).
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Request payload for creating a new compliance record for an employee.
/// </summary>
public sealed record CreateComplianceRecordRequest
{
    /// <summary>
    /// The unique identifier of the employee for whom the compliance record is being created.
    /// </summary>
    [Required]
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// The category or type of the new compliance record.
    /// </summary>
    [Required]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The date when the compliance activity occurred or the certification was issued.
    /// </summary>
    [Required]
    public DateTime Date { get; set; }

    /// <summary>
    /// The optional expiration date for the new compliance record.
    /// </summary>
    public DateTime? ExpiryDate { get; set; }
}
