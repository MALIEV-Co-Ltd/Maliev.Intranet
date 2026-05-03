using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Data transfer object representing an account in the chart of accounts.
/// </summary>
public sealed record ChartOfAccountDto
{
    /// <summary>
    /// Unique identifier for the account.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The alphanumeric code assigned to the account.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// The service-native account number.
    /// </summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the account.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The category or type of the account (e.g., Asset, Liability, Equity, Income, Expense).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The optional account category.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// The current balance of the account.
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Indicates whether the account is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The parent account identifier, when this account is nested.
    /// </summary>
    public Guid? ParentAccountId { get; set; }

    /// <summary>
    /// The list of child accounts for hierarchical representations.
    /// </summary>
    public List<ChartOfAccountDto> Children { get; set; } = [];
}

/// <summary>
/// Data transfer object for a double-entry accounting journal entry.
/// </summary>
public sealed record JournalEntryDto
{
    /// <summary>
    /// Unique identifier for the journal entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The reference number for identifying the journal entry.
    /// </summary>
    public string EntryNumber { get; set; } = string.Empty;

    /// <summary>
    /// The date the transaction was recorded.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// The service-native journal entry date.
    /// </summary>
    public DateTime EntryDate { get; set; }

    /// <summary>
    /// A description explaining the nature of the transaction.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// An optional reference to an external document or identifier.
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>
    /// The accounting period identifier.
    /// </summary>
    public Guid? PeriodId { get; set; }

    /// <summary>
    /// The accounting period display name.
    /// </summary>
    public string? PeriodName { get; set; }

    /// <summary>
    /// The total debit amount for the entry.
    /// </summary>
    public decimal TotalDebit { get; set; }

    /// <summary>
    /// The total credit amount for the entry.
    /// </summary>
    public decimal TotalCredit { get; set; }

    /// <summary>
    /// The current workflow status of the journal entry (e.g., Draft, Posted).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// The date and time when the journal entry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The date and time when the journal entry was posted.
    /// </summary>
    public DateTime? PostedAt { get; set; }

    /// <summary>
    /// The individual transaction lines associated with this journal entry.
    /// </summary>
    public List<JournalEntryLineDto> Lines { get; set; } = [];
}

/// <summary>
/// Represents a single line (debit or credit) within a journal entry.
/// </summary>
public sealed record JournalEntryLineDto
{
    /// <summary>
    /// The unique identifier of the journal entry line.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The service-native line sequence.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// The unique identifier of the affected account.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// The affected account number.
    /// </summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// The affected account display name.
    /// </summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// The debit amount for this line.
    /// </summary>
    public decimal Debit { get; set; }

    /// <summary>
    /// The service-native debit amount.
    /// </summary>
    public decimal DebitAmount
    {
        get => Debit;
        set => Debit = value;
    }

    /// <summary>
    /// The credit amount for this line.
    /// </summary>
    public decimal Credit { get; set; }

    /// <summary>
    /// The service-native credit amount.
    /// </summary>
    public decimal CreditAmount
    {
        get => Credit;
        set => Credit = value;
    }

    /// <summary>
    /// A description for this specific line item.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// An optional source document, slip, or external reference for this line.
    /// </summary>
    public string? Reference { get; set; }
}

/// <summary>
/// Request payload for creating a new journal entry.
/// </summary>
public sealed record CreateJournalEntryRequest
{
    /// <summary>
    /// The date the transaction occurred.
    /// </summary>
    [Required]
    public DateTime Date { get; set; }

    /// <summary>
    /// The service-native entry date.
    /// </summary>
    public DateTime EntryDate
    {
        get => Date;
        set => Date = value;
    }

    /// <summary>
    /// A summary describing the transaction.
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// An optional reference number for the transaction.
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>
    /// The collection of debit and credit lines comprising the journal entry.
    /// </summary>
    [Required]
    public List<JournalEntryLineDto> Lines { get; set; } = [];
}

/// <summary>
/// Represents an entry in the general ledger for a specific account.
/// </summary>
public sealed record LedgerEntryDto
{
    /// <summary>
    /// The unique identifier of the account.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// The date of the ledger entry.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// The description of the transaction from the original journal entry.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The debit amount recorded in this entry.
    /// </summary>
    public decimal Debit { get; set; }

    /// <summary>
    /// The credit amount recorded in this entry.
    /// </summary>
    public decimal Credit { get; set; }

    /// <summary>
    /// The running balance for the account after this entry.
    /// </summary>
    public decimal Balance { get; set; }
}

/// <summary>
/// Data transfer object for a generated financial report.
/// </summary>
public sealed record FinancialReportDto
{
    /// <summary>
    /// The name or title of the financial report.
    /// </summary>
    public string ReportName { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp indicating when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// The hierarchical sections comprising the report.
    /// </summary>
    public List<ReportSectionDto> Sections { get; set; } = [];
}

/// <summary>
/// Data transfer object for an accounting period.
/// </summary>
public sealed record AccountingPeriodDto
{
    /// <summary>The unique accounting period identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>The display name of the period.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The first date included in the period.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>The last date included in the period.</summary>
    public DateTime EndDate { get; set; }

    /// <summary>The current period status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>The fiscal year name.</summary>
    public string FiscalYear { get; set; } = string.Empty;
}

/// <summary>
/// Result returned from an accounting reconciliation run.
/// </summary>
public sealed record ReconciliationResultDto
{
    /// <summary>The source system being reconciled.</summary>
    public string SourceSystem { get; set; } = string.Empty;

    /// <summary>The period identifier being reconciled.</summary>
    public Guid PeriodId { get; set; }

    /// <summary>The reconciliation status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>The total number of differences found.</summary>
    public int DifferenceCount { get; set; }

    /// <summary>The net difference amount.</summary>
    public decimal DifferenceAmount { get; set; }

    /// <summary>Raw diagnostic details returned by AccountingService.</summary>
    public string? DetailsJson { get; set; }
}

/// <summary>
/// Represents a section within a financial report, such as Current Assets or Operating Expenses.
/// </summary>
public sealed record ReportSectionDto
{
    /// <summary>
    /// The title of the report section.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The individual data rows within this section.
    /// </summary>
    public List<ReportRowDto> Rows { get; set; } = [];

    /// <summary>
    /// The calculated total for all rows in this section.
    /// </summary>
    public decimal Total { get; set; }
}

/// <summary>
/// Represents a single data row within a report section.
/// </summary>
public sealed record ReportRowDto
{
    /// <summary>
    /// The label or name of the row.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The numerical amount associated with this row.
    /// </summary>
    public decimal Amount { get; set; }
}
