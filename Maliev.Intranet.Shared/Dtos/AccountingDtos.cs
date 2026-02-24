using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Data transfer object for a chart of account.
/// </summary>
public sealed record ChartOfAccountDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the account number.</summary>
    public string AccountNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the account name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the account type (e.g., Asset, Liability).</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Gets or sets the current balance.</summary>
    public decimal Balance { get; set; }
    /// <summary>Gets or sets a value indicating whether the account is active.</summary>
    public bool IsActive { get; set; }
    /// <summary>Gets or sets the child accounts.</summary>
    public List<ChartOfAccountDto> Children { get; set; } = [];
}

/// <summary>
/// Data transfer object for a journal entry.
/// </summary>
public sealed record JournalEntryDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the entry number.</summary>
    public string EntryNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the entry date.</summary>
    public DateTime EntryDate { get; set; }
    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the reference identifier.</summary>
    public string? Reference { get; set; }
    /// <summary>Gets or sets the total debit amount.</summary>
    public decimal TotalDebit { get; set; }
    /// <summary>Gets or sets the total credit amount.</summary>
    public decimal TotalCredit { get; set; }
    /// <summary>Gets or sets the current status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the journal entry lines.</summary>
    public List<JournalEntryLineDto> Lines { get; set; } = [];
}

/// <summary>
/// Data transfer object for a journal entry line.
/// </summary>
public sealed record JournalEntryLineDto
{
    /// <summary>Gets or sets the associated account ID.</summary>
    public Guid AccountId { get; set; }
    /// <summary>Gets or sets the debit amount.</summary>
    public decimal DebitAmount { get; set; }
    /// <summary>Gets or sets the credit amount.</summary>
    public decimal CreditAmount { get; set; }
    /// <summary>Gets or sets the line description.</summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Request model for creating a journal entry.
/// </summary>
public sealed record CreateJournalEntryRequest
{
    /// <summary>Gets or sets the entry date.</summary>
    [Required]
    public DateTime EntryDate { get; set; }

    /// <summary>Gets or sets the entry description.</summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional reference identifier.</summary>
    public string? Reference { get; set; }

    /// <summary>Gets or sets the collection of journal entry lines.</summary>
    [Required]
    public List<JournalEntryLineDto> Lines { get; set; } = [];
}

/// <summary>
/// Data transfer object for a ledger entry.
/// </summary>
public sealed record LedgerEntryDto
{
    /// <summary>Gets or sets the associated account ID.</summary>
    public Guid AccountId { get; set; }
    /// <summary>Gets or sets the entry date.</summary>
    public DateTime Date { get; set; }
    /// <summary>Gets or sets the entry description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the debit amount.</summary>
    public decimal Debit { get; set; }
    /// <summary>Gets or sets the credit amount.</summary>
    public decimal Credit { get; set; }
    /// <summary>Gets or sets the balance after this entry.</summary>
    public decimal Balance { get; set; }
}

/// <summary>
/// Data transfer object for a financial report.
/// </summary>
public sealed record FinancialReportDto
{
    /// <summary>Gets or sets the name of the report.</summary>
    public string ReportName { get; set; } = string.Empty;
    /// <summary>Gets or sets the timestamp when the report was generated.</summary>
    public DateTime GeneratedAt { get; set; }
    /// <summary>Gets or sets the collection of report sections.</summary>
    public List<ReportSectionDto> Sections { get; set; } = [];
}

/// <summary>
/// Data transfer object for a section within a financial report.
/// </summary>
public sealed record ReportSectionDto
{
    /// <summary>Gets or sets the section title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the collection of rows in the section.</summary>
    public List<ReportRowDto> Rows { get; set; } = [];
    /// <summary>Gets or sets the total amount for the section.</summary>
    public decimal Total { get; set; }
}

/// <summary>
/// Data transfer object for a single row in a financial report.
/// </summary>
public sealed record ReportRowDto
{
    /// <summary>Gets or sets the row label.</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>Gets or sets the row amount.</summary>
    public decimal Amount { get; set; }
}
