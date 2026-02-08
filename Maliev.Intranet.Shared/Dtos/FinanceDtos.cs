namespace Maliev.Intranet.Shared;

/// <summary>
/// Invoice summary DTO.
/// </summary>
public class InvoiceSummaryDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Detailed invoice information.
/// </summary>
public class InvoiceDetailDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Payment summary DTO.
/// </summary>
public class PaymentSummaryDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime PaymentDate { get; set; }
}

/// <summary>
/// Detailed payment information.
/// </summary>
public class PaymentDetailDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Statistics for payments.
/// </summary>
public class PaymentStatsDto
{
    public decimal TodayTotal { get; set; }
    public double PercentageChangeFromYesterday { get; set; }
    public decimal PendingTotal { get; set; }
    public int PendingCount { get; set; }
    public decimal MonthTotal { get; set; }
    public decimal FailedTotal { get; set; }
    public int FailedCount { get; set; }
}
