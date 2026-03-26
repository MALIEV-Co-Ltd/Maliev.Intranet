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
    /// The display name of the account.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The category or type of the account (e.g., Asset, Liability, Equity, Income, Expense).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The current balance of the account.
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Indicates whether the account is currently active.
    /// </summary>
    public bool IsActive { get; set; }

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
    /// A description explaining the nature of the transaction.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// An optional reference to an external document or identifier.
    /// </summary>
    public string? Reference { get; set; }

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
    /// The unique identifier of the affected account.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// The debit amount for this line.
    /// </summary>
    public decimal Debit { get; set; }

    /// <summary>
    /// The credit amount for this line.
    /// </summary>
    public decimal Credit { get; set; }

    /// <summary>
    /// A description for this specific line item.
    /// </summary>
    public string Description { get; set; } = string.Empty;
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
