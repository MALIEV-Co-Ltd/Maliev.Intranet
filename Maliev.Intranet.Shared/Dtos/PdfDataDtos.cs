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
    DeliveryNote,
    /// <summary>PDF representing a Commerce product bill of materials.</summary>
    CommerceBom
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
/// Data contract for financial report PDF generation.
/// </summary>
public class FinancialReportPdfData
{
    /// <summary>The title shown on the financial report.</summary>
    public string ReportTitle { get; set; } = "";

    /// <summary>The stable report number used for audit and reference.</summary>
    public string ReportNumber { get; set; } = "";

    /// <summary>The date the report was generated.</summary>
    public DateTime ReportDate { get; set; }

    /// <summary>The first date included in the report period.</summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>The final date included in the report period.</summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>The company name shown on the report header.</summary>
    public string CompanyName { get; set; } = "";

    /// <summary>The optional company address shown on the report header.</summary>
    public string? CompanyAddress { get; set; }

    /// <summary>The ordered report sections included in the PDF.</summary>
    public List<FinancialReportPdfSection> Sections { get; set; } = [];

    /// <summary>The revenue-side total used by the report summary.</summary>
    public double TotalRevenue { get; set; }

    /// <summary>The expense-side total used by the report summary.</summary>
    public double TotalExpenses { get; set; }

    /// <summary>The net result used by the report summary.</summary>
    public double NetProfit { get; set; }

    /// <summary>The ISO currency code for the report amounts.</summary>
    public string Currency { get; set; } = "THB";
}

/// <summary>
/// Section data for a financial report PDF.
/// </summary>
public class FinancialReportPdfSection
{
    /// <summary>The report section heading.</summary>
    public string SectionTitle { get; set; } = "";

    /// <summary>The line items contained in this report section.</summary>
    public List<FinancialReportPdfLineItem> LineItems { get; set; } = [];

    /// <summary>The calculated total for the report section.</summary>
    public double SectionTotal { get; set; }
}

/// <summary>
/// Line item data for a financial report PDF section.
/// </summary>
public class FinancialReportPdfLineItem
{
    /// <summary>The account or row description shown in the report.</summary>
    public string Description { get; set; } = "";

    /// <summary>The monetary amount shown for this row.</summary>
    public double Amount { get; set; }

    /// <summary>Whether the line should receive highlight styling in the PDF.</summary>
    public bool IsHighlight { get; set; }
}

/// <summary>
/// Data contract for Commerce product BOM PDF generation.
/// </summary>
public class CommerceBomPdfData
{
    /// <summary>Gets or sets the product title.</summary>
    public string ProductTitle { get; set; } = "";

    /// <summary>Gets or sets the product handle.</summary>
    public string ProductHandle { get; set; } = "";

    /// <summary>Gets or sets the product brand.</summary>
    public string? Brand { get; set; }

    /// <summary>Gets or sets the product type.</summary>
    public string ProductType { get; set; } = "";

    /// <summary>Gets or sets the product publication status.</summary>
    public string Status { get; set; } = "";

    /// <summary>Gets or sets the date the BOM was generated.</summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the default ISO currency code.</summary>
    public string Currency { get; set; } = "THB";

    /// <summary>Gets or sets the BOM line items.</summary>
    public List<CommerceBomPdfItem> Items { get; set; } = [];

    /// <summary>Gets or sets the total expected cost.</summary>
    public decimal TotalCost { get; set; }

    /// <summary>Gets or sets the longest expected sourcing time in days across BOM items.</summary>
    public int SourcingTimeDays { get; set; }
}

/// <summary>
/// Data contract for one Commerce product BOM line in PDF generation.
/// </summary>
public class CommerceBomPdfItem
{
    /// <summary>Gets or sets the one-based line index.</summary>
    public int Index { get; set; }

    /// <summary>Gets or sets the material, component, or consumable name.</summary>
    public string ItemName { get; set; } = "";

    /// <summary>Gets or sets the supplier or internal part number.</summary>
    public string? PartNumber { get; set; }

    /// <summary>Gets or sets the parent assembly name.</summary>
    public string? AssemblyName { get; set; }

    /// <summary>Gets or sets the nested subassembly name.</summary>
    public string? SubassemblyName { get; set; }

    /// <summary>Gets or sets the component image URL.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Gets or sets the drawing or technical file URL.</summary>
    public string? DrawingUrl { get; set; }

    /// <summary>Gets or sets the preferred supplier name.</summary>
    public string? SupplierName { get; set; }

    /// <summary>Gets or sets the supplier product or sourcing URL.</summary>
    public string? SupplierUrl { get; set; }

    /// <summary>Gets or sets the item specification, grade, color, size, or supplier reference.</summary>
    public string? Specification { get; set; }

    /// <summary>Gets or sets the quantity used by one sellable product unit.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Gets or sets the unit of measure.</summary>
    public string Unit { get; set; } = "pcs";

    /// <summary>Gets or sets the expected unit cost.</summary>
    public decimal UnitCost { get; set; }

    /// <summary>Gets or sets the ISO currency code.</summary>
    public string Currency { get; set; } = "THB";

    /// <summary>Gets or sets the expected line total.</summary>
    public decimal LineTotal { get; set; }

    /// <summary>Gets or sets the supplier or manufacturing lead time in days.</summary>
    public int? LeadTimeDays { get; set; }

    /// <summary>Gets or sets the internal sourcing preparation time in days.</summary>
    public int? SourcingTimeDays { get; set; }

    /// <summary>Gets or sets internal notes about the item.</summary>
    public string? Notes { get; set; }
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

    /// <summary>The customer phone number shown on the quotation.</summary>
    public string? CustomerPhone { get; set; }

    /// <summary>Preformatted customer identity lines shown in the QUOTE TO section.</summary>
    public List<string> CustomerDisplayLines { get; set; } = [];

    /// <summary>The customer billing or registered address, if available.</summary>
    public string? CustomerAddress { get; set; }

    /// <summary>The customer billing address shown on the quotation.</summary>
    public string? BillingAddress { get; set; }

    /// <summary>Preformatted billing address lines shown on the quotation.</summary>
    public List<string> BillingAddressLines { get; set; } = [];

    /// <summary>The customer shipping address shown on the quotation.</summary>
    public string? ShippingAddress { get; set; }

    /// <summary>Preformatted shipping address lines shown on the quotation.</summary>
    public List<string> ShippingAddressLines { get; set; } = [];

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

    /// <summary>Manual discount amount entered for this quotation.</summary>
    public decimal ManualDiscountAmount { get; set; }

    /// <summary>Shipping or delivery cost applied to the quotation.</summary>
    public decimal ShippingCost { get; set; }

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

    /// <summary>The employee name automatically recorded as the person who generated this quotation.</summary>
    public string? QuotedByName { get; set; }

    /// <summary>The employee email automatically recorded as the person who generated this quotation.</summary>
    public string? QuotedByEmail { get; set; }

    /// <summary>The employee phone number automatically recorded as the person who generated this quotation.</summary>
    public string? QuotedByPhone { get; set; }

    /// <summary>The UTC timestamp when this quotation PDF was generated or issued.</summary>
    public DateTime? QuotedAt { get; set; }

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

    /// <summary>The part file name or customer-facing part name.</summary>
    public string? PartName { get; set; }

    /// <summary>Manufacturing process such as FDM, SLA, or CNC.</summary>
    public string? ManufacturingProcess { get; set; }

    /// <summary>Individual part information lines shown under the part name.</summary>
    public List<string> DetailLines { get; set; } = [];

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

    /// <summary>Optional thumbnail image URL for the quoted part.</summary>
    public string? ThumbnailUrl { get; set; }

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
