namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Defines the supported PDF document types.
/// </summary>
public enum PdfDocumentType
{
    /// <summary>PDF representing a customer quotation.</summary>
    Quotation,
    /// <summary>PDF representing a formal invoice.</summary>
    Invoice,
    /// <summary>PDF representing a payment receipt.</summary>
    Receipt,
    /// <summary>PDF representing an internal or external report.</summary>
    Report,
    /// <summary>PDF representing a logistics delivery note.</summary>
    DeliveryNote
}

/// <summary>
/// Response from PDF generation request.
/// </summary>
public class PdfGenerationResponse
{
    /// <summary>The unique identifier of the asynchronous PDF generation request.</summary>
    public Guid RequestId { get; set; }
    /// <summary>The direct storage URL of the generated PDF file.</summary>
    public string StorageUrl { get; set; } = "";
}

/// <summary>
/// Data contract for Quotation PDF generation.
/// </summary>
public class QuotationPdfData
{
    /// <summary>The unique number assigned to the quotation.</summary>
    public string QuotationNumber { get; set; } = "";
    /// <summary>The name of the customer the quotation is addressed to.</summary>
    public string CustomerName { get; set; } = "";
    /// <summary>The formal date of the quotation.</summary>
    public DateTime QuotationDate { get; set; }
    /// <summary>The list of individual line items included in the quotation.</summary>
    public List<QuotationPdfItem> Items { get; set; } = [];
    /// <summary>The total calculated amount for the entire quotation.</summary>
    public double TotalAmount { get; set; }
    /// <summary>The ISO currency code for the quotation amounts (e.g., THB, USD).</summary>
    public string Currency { get; set; } = string.Empty;
}

/// <summary>
/// Data contract for individual items in a Quotation PDF.
/// </summary>
public class QuotationPdfItem
{
    /// <summary>The sequential line index of the item.</summary>
    public int Index { get; set; }
    /// <summary>A descriptive name or summary of the item.</summary>
    public string Description { get; set; } = "";
    /// <summary>The quantity of the item quoted.</summary>
    public double Quantity { get; set; }
    /// <summary>The unit price per item.</summary>
    public double UnitPrice { get; set; }
    /// <summary>The total price for this line item (quantity * unit price).</summary>
    public double TotalPrice { get; set; }
}

/// <summary>
/// Data contract for Invoice PDF generation.
/// </summary>
public class InvoicePdfData
{
    /// <summary>The unique number assigned to the invoice.</summary>
    public string InvoiceNumber { get; set; } = "";
    /// <summary>The list of individual line items included in the invoice.</summary>
    public List<InvoicePdfItem> Items { get; set; } = [];
}

/// <summary>
/// Data contract for individual items in an Invoice PDF.
/// </summary>
public class InvoicePdfItem
{
    /// <summary>The sequential line index of the item.</summary>
    public int Index { get; set; }
    /// <summary>A descriptive name or summary of the item.</summary>
    public string Description { get; set; } = "";
    /// <summary>The quantity of the item invoiced.</summary>
    public double Quantity { get; set; }
    /// <summary>The total price for this line item.</summary>
    public double TotalPrice { get; set; }
}
