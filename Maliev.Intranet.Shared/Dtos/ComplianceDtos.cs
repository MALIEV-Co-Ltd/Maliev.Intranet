using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

public sealed record ComplianceRecordDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed record CreateComplianceRecordRequest
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; }

    public DateTime? ExpiryDate { get; set; }
}
