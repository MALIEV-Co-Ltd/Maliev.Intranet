using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

public sealed record SalaryRecordDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
}

public sealed record BenefitEnrollmentDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string BenefitType { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
}

public sealed record PayrollRecordDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal NetPay { get; set; }
}
