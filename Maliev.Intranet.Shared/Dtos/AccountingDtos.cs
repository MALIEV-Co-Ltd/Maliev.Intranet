using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

public sealed record ChartOfAccountDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
    public List<ChartOfAccountDto> Children { get; set; } = [];
}

public sealed record JournalEntryDto
{
    public Guid Id { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<JournalEntryLineDto> Lines { get; set; } = [];
}

public sealed record JournalEntryLineDto
{
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed record CreateJournalEntryRequest
{
    [Required]
    public DateTime Date { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? Reference { get; set; }

    [Required]
    public List<JournalEntryLineDto> Lines { get; set; } = [];
}

public sealed record LedgerEntryDto
{
    public Guid AccountId { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}

public sealed record FinancialReportDto
{
    public string ReportName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public List<ReportSectionDto> Sections { get; set; } = [];
}

public sealed record ReportSectionDto
{
    public string Title { get; set; } = string.Empty;
    public List<ReportRowDto> Rows { get; set; } = [];
    public decimal Total { get; set; }
}

public sealed record ReportRowDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
