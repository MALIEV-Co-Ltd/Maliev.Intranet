namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Defines the supported PDF document types.
/// </summary>
public enum PdfDocumentType
{
    Quotation,
    Invoice,
    Receipt,
    Report,
    DeliveryNote
}

/// <summary>
/// Response from PDF generation request.
/// </summary>
public class PdfGenerationResponse
{
    public Guid RequestId { get; set; }
    public string StorageUrl { get; set; } = "";
}

/// <summary>
/// Data contract for Quotation PDF generation.
/// </summary>
public class QuotationPdfData
{
    public string QuotationNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public DateTime QuotationDate { get; set; }
    public List<QuotationPdfItem> Items { get; set; } = [];
    public double TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
}

/// <summary>
/// Data contract for individual items in a Quotation PDF.
/// </summary>
public class QuotationPdfItem
{
    public int Index { get; set; }
    public string Description { get; set; } = "";
    public double Quantity { get; set; }
    public double UnitPrice { get; set; }
    public double TotalPrice { get; set; }
}

/// <summary>
/// Data contract for Invoice PDF generation.
/// </summary>
public class InvoicePdfData
{
    public string InvoiceNumber { get; set; } = "";
    public List<InvoicePdfItem> Items { get; set; } = [];
}

/// <summary>
/// Data contract for individual items in an Invoice PDF.
/// </summary>
public class InvoicePdfItem
{
    public int Index { get; set; }
    public string Description { get; set; } = "";
    public double Quantity { get; set; }
    public double TotalPrice { get; set; }
}
