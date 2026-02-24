using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Data transfer object for a salary record.
/// </summary>
public sealed record SalaryRecordDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the employee ID.</summary>
    public Guid EmployeeId { get; set; }
    /// <summary>Gets or sets the salary amount.</summary>
    public decimal Amount { get; set; }
    /// <summary>Gets or sets the currency (e.g., THB, USD).</summary>
    public string Currency { get; set; } = string.Empty;
    /// <summary>Gets or sets the effective date of the salary.</summary>
    public DateTime EffectiveDate { get; set; }
}

/// <summary>
/// Data transfer object for benefit enrollment.
/// </summary>
public sealed record BenefitEnrollmentDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the employee ID.</summary>
    public Guid EmployeeId { get; set; }
    /// <summary>Gets or sets the type of benefit.</summary>
    public string BenefitType { get; set; } = string.Empty;
    /// <summary>Gets or sets the date of enrollment.</summary>
    public DateTime EnrollmentDate { get; set; }
}

/// <summary>
/// Data transfer object for a payroll record.
/// </summary>
public sealed record PayrollRecordDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the employee ID.</summary>
    public Guid EmployeeId { get; set; }
    /// <summary>Gets or sets the start date of the payroll period.</summary>
    public DateTime PeriodStart { get; set; }
    /// <summary>Gets or sets the end date of the payroll period.</summary>
    public DateTime PeriodEnd { get; set; }
    /// <summary>Gets or sets the net pay amount.</summary>
    public decimal NetPay { get; set; }
}
