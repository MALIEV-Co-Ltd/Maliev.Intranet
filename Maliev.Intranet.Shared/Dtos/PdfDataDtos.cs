using System.Text.Json.Serialization;

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

    /// <summary>The revision number of the quotation.</summary>
    public int VersionNumber { get; set; } = 1;

    /// <summary>The name of the customer the quotation is addressed to.</summary>
    public string CustomerName { get; set; } = "";

    /// <summary>The customer type used by the quotation layout.</summary>
    public string CustomerType { get; set; } = "Corporate";

    /// <summary>The customer branch name for corporate customers.</summary>
    public string? CustomerBranch { get; set; }

    /// <summary>The customer tax identifier, if available.</summary>
    public string? CustomerTaxId { get; set; }

    /// <summary>The customer billing or registered address, if available.</summary>
    public string? CustomerAddress { get; set; }

    /// <summary>The customer's contact person, if available.</summary>
    public string? ContactPerson { get; set; }

    /// <summary>The formal date of the quotation.</summary>
    public DateTime QuotationDate { get; set; } = DateTime.UtcNow;

    /// <summary>The date the quotation validity starts.</summary>
    public DateTime ValidityStart { get; set; } = DateTime.UtcNow;

    /// <summary>The date the quotation validity ends.</summary>
    public DateTime ValidityEnd { get; set; } = DateTime.UtcNow.AddDays(30);

    /// <summary>The list of individual line items included in the quotation.</summary>
    public List<QuotationPdfItem> Items { get; set; } = [];

    /// <summary>Total price before discounts.</summary>
    public decimal SubtotalBeforeDiscount { get; set; }

    /// <summary>Total discount amount applied to the quotation.</summary>
    public decimal TotalDiscount { get; set; }

    /// <summary>Subtotal after discounts and before tax.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>VAT or tax amount.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>The total calculated amount for the entire quotation.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>The ISO currency code for the quotation amounts (e.g., THB, USD).</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Expected delivery timing or lead time shown on the quotation.</summary>
    public string? DeliveryExpectations { get; set; }

    /// <summary>Special terms and conditions shown on the quotation.</summary>
    public string? SpecialTerms { get; set; }

    /// <summary>Summary of changes for a revised quotation.</summary>
    public string? ChangeSummary { get; set; }

    /// <summary>Discounts applied to the quotation.</summary>
    public List<QuotationPdfDiscount> Discounts { get; set; } = [];
}

/// <summary>
/// Data contract for individual items in a Quotation PDF.
/// </summary>
public class QuotationPdfItem
{
    /// <summary>The sequential line index of the item.</summary>
    public int Index { get; set; }

    /// <summary>Material name, service name, or product identifier.</summary>
    public string MaterialName { get; set; } = "";

    /// <summary>Manufacturing process such as FDM, SLA, or CNC.</summary>
    public string? ManufacturingProcess { get; set; }

    /// <summary>The quantity of the item quoted.</summary>
    public decimal Quantity { get; set; }

    /// <summary>The unit of measure for the quoted quantity.</summary>
    public string QuantityUnit { get; set; } = "pcs";

    /// <summary>The unit price per item.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>The total price for this line item (quantity * unit price).</summary>
    public decimal LineTotal { get; set; }

    /// <summary>Additional notes shown below the material or service name.</summary>
    public string? Notes { get; set; }

    /// <summary>Backward-compatible alias for older callers.</summary>
    [JsonPropertyName("description")]
    public string Description
    {
        get => MaterialName;
        set => MaterialName = value;
    }

    /// <summary>Backward-compatible alias for older callers.</summary>
    [JsonPropertyName("totalPrice")]
    public decimal TotalPrice
    {
        get => LineTotal;
        set => LineTotal = value;
    }
}

/// <summary>
/// Data contract for a discount applied to a Quotation PDF.
/// </summary>
public class QuotationPdfDiscount
{
    /// <summary>The discount type: Percentage, FixedAmount, or VolumeBased.</summary>
    public string DiscountType { get; set; } = "Percentage";

    /// <summary>The discount value.</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>Discount conditions or description.</summary>
    public string? Conditions { get; set; }
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
