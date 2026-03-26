using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Data transfer object representing a salary record for an employee.
/// </summary>
public sealed record SalaryRecordDto
{
    /// <summary>
    /// Unique identifier for the salary record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The unique identifier of the employee associated with this salary record.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// The gross salary amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// The currency code (e.g., USD, THB) for the salary amount.
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// The date when this salary amount becomes effective.
    /// </summary>
    public DateTime EffectiveDate { get; set; }
}

/// <summary>
/// Data transfer object representing an employee's enrollment in a benefit program.
/// </summary>
public sealed record BenefitEnrollmentDto
{
    /// <summary>
    /// Unique identifier for the benefit enrollment record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The unique identifier of the employee enrolled in the benefit.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// The type or category of benefit (e.g., HealthInsurance, RetirementPlan).
    /// </summary>
    public string BenefitType { get; set; } = string.Empty;

    /// <summary>
    /// The date the employee was enrolled in the benefit.
    /// </summary>
    public DateTime EnrollmentDate { get; set; }
}

/// <summary>
/// Data transfer object representing a payroll record for a specific period.
/// </summary>
public sealed record PayrollRecordDto
{
    /// <summary>
    /// Unique identifier for the payroll record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The unique identifier of the employee associated with this payroll record.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// The start date of the payroll period.
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// The end date of the payroll period.
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// The final net pay amount for the employee after all deductions and taxes.
    /// </summary>
    public decimal NetPay { get; set; }
}
