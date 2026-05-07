using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Pages;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Tests.Client.Pages;

/// <summary>
/// Tests ProjectNew quotation PDF payload mapping.
/// </summary>
public class ProjectQuotationPdfMapperTests
{
    /// <summary>
    /// Verifies draft quotation PDFs include Thailand VAT and customer details.
    /// </summary>
    [Fact]
    public void BuildDraftPdfData_IncludesVatAndCompleteCustomerDetails()
    {
        var data = ProjectQuotationPdfMapper.BuildDraftPdfData(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            new CustomerSummaryDto
            {
                Id = Guid.NewGuid(),
                Name = "Jane Buyer",
                CompanyName = "Acme Thailand",
                Email = "summary.buyer@example.com",
                CompanyPhone = "+66 2 123 4567",
            },
            new CustomerDetailDto
            {
                Name = "Jane Buyer",
                Email = "jane.buyer@example.com",
                CompanyName = "Acme Thailand",
                CompanyPhone = "+66 2 765 4321",
                CompanyContactEmail = "sales@acme.example",
                CompanyVatNumber = "0105559999999",
                CompanyBillingAddress = new AddressResponse
                {
                    Type = "Billing",
                    AddressLine1 = "88 Billing Road",
                    City = "Bangkok",
                    StateProvince = "Bangkok",
                    PostalCode = "10110",
                },
                Addresses =
                [
                    new AddressResponse
                    {
                        Type = "Shipping",
                        AddressLine1 = "99 Shipping Road",
                        City = "Nonthaburi",
                        StateProvince = "Nonthaburi",
                        PostalCode = "11120",
                        IsDefault = true,
                    },
                ],
            },
            "THB",
            DateTime.SpecifyKind(new DateTime(2026, 5, 2), DateTimeKind.Utc),
            "7 business days after order confirmation",
            [
                new PartViewModel
                {
                    Name = "bracket.step",
                    Quantity = 2,
                    EstimatedUnitPrice = 100,
                    EstimatedTotalAmount = 200,
                    ProcessCode = "FDM",
                    ProcessId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    MaterialId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    FinishId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    FinishCode = "AS_PRNTED",
                    ToleranceId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                    ToleranceCode = "FDM_STD",
                    RoughnessCode = "RA_1_6",
                    Dimensions = new FileAnalysisDimensionsDto { X = 12.5, Y = 30, Z = 4.25 },
                    HasThreadedHoles = true,
                    ThreadedHoleSpec = "M3",
                    ThreadedHoleCount = 4,
                    HasInserts = true,
                    InsertType = InsertType.HeatSet,
                    InsertCount = 2,
                    InspectionLevel = InspectionLevel.Dimensional,
                    DrawingFiles =
                    [
                        new DraftProjectAttachmentDto { Name = "bracket-drawing.pdf" },
                    ],
                    AvailableMaterials =
                    [
                        new CatalogMaterialDto(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "PLA", "PLA", "Plastic", null, null, 1),
                    ],
                    AvailableFinishes =
                    [
                        new CatalogSurfaceFinishDto(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "As-printed", "AS_PRNTED", null, 0, null, 1),
                    ],
                    AvailableTolerances =
                    [
                        new CatalogToleranceDto(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), "FDM Standard", "FDM_STD", "", "", "+-0.3mm", 0, 1),
                    ],
                    ProcessOptionValues =
                    {
                        ["material_color"] = "Black",
                        ["deburring"] = "true",
                    },
                    PartNotes = "hello test 123",
                },
            ],
            [new ProcessDto(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "FDM", "3D Printing (FDM)", null, 1)]);

        Assert.Equal(200, data.Subtotal);
        Assert.Equal(14, data.TaxAmount);
        Assert.Equal(214, data.TotalAmount);
        Assert.Equal("Jane Buyer", data.ContactPerson);
        Assert.Equal("0105559999999", data.CustomerTaxId);
        Assert.Equal("Head Office", data.CustomerBranch);
        Assert.Equal("+66 2 765 4321", data.CustomerPhone);
        Assert.Contains("Email: sales@acme.example", data.CustomerDisplayLines);
        Assert.Contains("88 Billing Road", data.BillingAddress);
        Assert.Contains("99 Shipping Road", data.ShippingAddress);
        Assert.Equal("bracket.step", data.Items[0].PartName);
        Assert.Equal("PLA", data.Items[0].MaterialName);
        Assert.Equal("3D Printing (FDM)", data.Items[0].ManufacturingProcess);
        Assert.Contains("Bounding box: 12.5 x 30 x 4.25 mm", data.Items[0].DetailLines);
        Assert.Contains("Surface finish: As-printed", data.Items[0].DetailLines);
        Assert.Contains("Tolerance: FDM Standard +-0.3mm", data.Items[0].DetailLines);
        Assert.Contains("Surface roughness: Ra 1.6 um", data.Items[0].DetailLines);
        Assert.Contains("Color: Black", data.Items[0].DetailLines);
        Assert.Contains("Tapped holes: 4 x M3", data.Items[0].DetailLines);
        Assert.Contains("Inserts: 2 x HeatSet", data.Items[0].DetailLines);
        Assert.Contains("Deburring", data.Items[0].DetailLines);
        Assert.Contains("Inspection: Dimensional", data.Items[0].DetailLines);
        Assert.Contains("Drawing: bracket-drawing.pdf", data.Items[0].DetailLines);
        Assert.Equal("hello test 123", data.Items[0].Notes);
        Assert.DoesNotContain("Bounding box:", data.Items[0].Notes, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies ProjectNew quotation notes use only the part note text, not the full configuration details.
    /// </summary>
    [Fact]
    public void BuildLineItemNotes_WithPartNote_ReturnsOnlyPartNote()
    {
        var part = new PartViewModel
        {
            Dimensions = new FileAnalysisDimensionsDto { X = 27.5, Y = 24.5, Z = 18.5 },
            FinishCode = "AS_PRINTED",
            ToleranceCode = "FDM_STD",
            ProcessCode = "FDM",
            RoughnessCode = "RA_3_2",
            InspectionLevel = InspectionLevel.Standard,
            PartNotes = "hello test 123",
            DrawingFiles =
            [
                new DraftProjectAttachmentDto { Name = "Screenshot 2026-05-07 184328.png" },
                new DraftProjectAttachmentDto { Name = "Screenshot 2026-05-07 183642.png" },
            ],
        };

        var notes = ProjectQuotationPdfMapper.BuildLineItemNotes(part);
        var detailLines = ProjectQuotationPdfMapper.BuildLineItemDetailLines(part);

        Assert.Equal("hello test 123", notes);
        Assert.Contains("Bounding box: 27.5 x 24.5 x 18.5 mm", detailLines);
        Assert.Contains("Surface finish: As-printed", detailLines);
        Assert.Contains("Tolerance: FDM Standard +-0.3mm", detailLines);
        Assert.Contains("Surface roughness: Ra 3.2 um", detailLines);
        Assert.Contains("Inspection: Standard", detailLines);
        Assert.Contains("Drawing: Screenshot 2026-05-07 184328.png, Screenshot 2026-05-07 183642.png", detailLines);
        Assert.DoesNotContain("hello test 123", detailLines);
    }

    /// <summary>
    /// Verifies the standard inspection selection is still visible in PDF part details.
    /// </summary>
    [Fact]
    public void BuildLineItemDetailLines_WithStandardInspection_IncludesInspection()
    {
        var part = new PartViewModel
        {
            Name = "bracket.step",
            InspectionLevel = InspectionLevel.Standard,
        };

        var lines = ProjectQuotationPdfMapper.BuildLineItemDetailLines(part);

        Assert.Contains("Inspection: Standard", lines);
    }

    /// <summary>
    /// Verifies tapped-hole placeholders are not shown when details are left to attached drawings.
    /// </summary>
    [Fact]
    public void BuildLineItemDetailLines_WithUnspecifiedTappedHoles_UsesDrawingReference()
    {
        var part = new PartViewModel
        {
            Name = "bracket.step",
            HasThreadedHoles = true,
            DrawingFiles =
            [
                new DraftProjectAttachmentDto { Name = "bracket-drawing.pdf" },
            ],
        };

        var lines = ProjectQuotationPdfMapper.BuildLineItemDetailLines(part);

        Assert.Contains("Tapped holes: Yes (see attached drawings)", lines);
        Assert.DoesNotContain("Tapped holes: TBD x specified thread", lines);
    }

    /// <summary>
    /// Verifies ISO tolerance names are not duplicated with secondary ISO labels.
    /// </summary>
    [Fact]
    public void BuildLineItemDetailLines_WithIsoToleranceName_DoesNotDuplicateIsoStandard()
    {
        var toleranceId = Guid.NewGuid();
        var part = new PartViewModel
        {
            ToleranceId = toleranceId,
            ToleranceCode = "ISO2768_M",
            AvailableTolerances =
            [
                new CatalogToleranceDto(toleranceId, "Medium (ISO 2768-m)", "ISO2768_M", "ISO2768-1", "", "", 0, 1),
            ],
        };

        var lines = ProjectQuotationPdfMapper.BuildLineItemDetailLines(part);

        Assert.Contains("Tolerance: Medium (ISO 2768-m)", lines);
        Assert.DoesNotContain(lines, line => line.Contains("ISO2768-1", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies Thai company quotation data uses Thai branch and multiline address format.
    /// </summary>
    [Fact]
    public void BuildDraftPdfData_WithThaiCompany_FormatsQuoteToAndAddresses()
    {
        var data = ProjectQuotationPdfMapper.BuildDraftPdfData(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            new CustomerSummaryDto
            {
                Id = Guid.NewGuid(),
                Name = "ณฐพล วนาศรีวิไล",
                CompanyName = "บริษัท มาลีฟ จำกัด",
                CompanyPhone = "028816002",
            },
            new CustomerDetailDto
            {
                Name = "ณฐพล วนาศรีวิไล",
                CompanyName = "บริษัท มาลีฟ จำกัด",
                CompanyPhone = "028816002",
                CompanyBillingAddress = new AddressResponse
                {
                    Type = "Billing",
                    AddressLine1 = "36/1 หมู่ 3",
                    District = "คลองข่อย",
                    City = "ปากเกร็ด",
                    StateProvince = "นนทบุรี",
                    PostalCode = "11120",
                },
                Addresses =
                [
                    new AddressResponse
                    {
                        Type = "Shipping",
                        AddressLine1 = "36/2 หมู่ 4",
                        District = "บางจาก",
                        City = "ภาษีเจริญ",
                        StateProvince = "กรุงเทพมหานคร",
                        PostalCode = "10160",
                    },
                ],
            },
            "THB",
            DateTime.SpecifyKind(new DateTime(2026, 5, 2), DateTimeKind.Utc),
            "Standard: 5 - 8 business days after order confirmation",
            [],
            []);

        Assert.Equal("สำนักงานใหญ่", data.CustomerBranch);
        Assert.Equal("บริษัท มาลีฟ จำกัด (สำนักงานใหญ่)", data.CustomerDisplayLines[0]);
        Assert.Equal("Attn: ณฐพล วนาศรีวิไล (028816002)", data.CustomerDisplayLines[1]);
        Assert.Equal(["36/1 หมู่ 3", "ตำบลคลองข่อย", "อำเภอปากเกร็ด", "จังหวัดนนทบุรี 11120"], data.BillingAddressLines);
        Assert.Equal(["36/2 หมู่ 4", "ตำบลบางจาก", "อำเภอภาษีเจริญ", "จังหวัดกรุงเทพมหานคร 10160"], data.ShippingAddressLines);
    }

    /// <summary>
    /// Verifies quotation PDFs default missing shipping address details from billing address details.
    /// </summary>
    [Fact]
    public void BuildDraftPdfData_WithBillingOnly_DefaultsShippingAddressToBillingAddress()
    {
        var data = ProjectQuotationPdfMapper.BuildDraftPdfData(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            new CustomerSummaryDto
            {
                Id = Guid.NewGuid(),
                Name = "Jane Buyer",
                CompanyName = "Acme Thailand",
            },
            new CustomerDetailDto
            {
                Name = "Jane Buyer",
                CompanyName = "Acme Thailand",
                CompanyBillingAddress = new AddressResponse
                {
                    Type = "Billing",
                    AddressLine1 = "88 Billing Road",
                    District = "Khlong Toei",
                    City = "Khlong Toei",
                    StateProvince = "Bangkok",
                    PostalCode = "10110",
                },
            },
            "THB",
            DateTime.SpecifyKind(new DateTime(2026, 5, 2), DateTimeKind.Utc),
            "Standard: 5 - 8 business days after order confirmation",
            [],
            []);

        Assert.Equal(data.BillingAddress, data.ShippingAddress);
        Assert.Equal(data.BillingAddressLines, data.ShippingAddressLines);
    }

    /// <summary>
    /// Verifies selected display currency, shipping, manual discount, and user-entered terms are mapped to the PDF payload.
    /// </summary>
    [Fact]
    public void BuildDraftPdfData_WithAdjustments_UsesSelectedCurrencyAndTerms()
    {
        var data = ProjectQuotationPdfMapper.BuildDraftPdfData(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            null,
            null,
            "USD",
            DateTime.SpecifyKind(new DateTime(2026, 5, 2), DateTimeKind.Utc),
            "Standard: 5 business days after order confirmation",
            [
                new PartViewModel
                {
                    Name = "gear.step",
                    Quantity = 1,
                    EstimatedUnitPrice = 1000,
                    EstimatedTotalAmount = 1000,
                },
            ],
            [],
            "Payment due before production.",
            shippingCost: 20,
            manualDiscountAmount: 5,
            currencyExchangeRate: 0.03m);

        Assert.Equal("USD", data.Currency);
        Assert.Equal(30, data.SubtotalBeforeDiscount);
        Assert.Equal(5, data.ManualDiscountAmount);
        Assert.Equal(20, data.ShippingCost);
        Assert.Equal(45, data.Subtotal);
        Assert.Equal(3.15m, data.TaxAmount);
        Assert.Equal(48.15m, data.TotalAmount);
        Assert.Equal("Payment due before production.", data.SpecialTerms);
        Assert.Contains(data.Discounts, discount => discount.Conditions == "Manual discount" && discount.DiscountValue == 5m);
    }

    /// <summary>
    /// Verifies bulk pricing is shown as an explicit quotation discount instead of hiding it in the line total.
    /// </summary>
    [Fact]
    public void BuildDraftPdfData_WithBulkPricing_ShowsOriginalSubtotalAndBulkDiscount()
    {
        var data = ProjectQuotationPdfMapper.BuildDraftPdfData(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            null,
            null,
            "THB",
            DateTime.SpecifyKind(new DateTime(2026, 5, 2), DateTimeKind.Utc),
            "Standard: 5 business days after order confirmation",
            [
                new PartViewModel
                {
                    Name = "bulk-part.step",
                    Quantity = 10,
                    EstimatedBaseUnitPrice = 100,
                    EstimatedUnitPrice = 85,
                    EstimatedTotalAmount = 850,
                },
            ],
            []);

        Assert.Equal(1000, data.SubtotalBeforeDiscount);
        Assert.Equal(150, data.TotalDiscount);
        Assert.Equal(150, data.ManualDiscountAmount);
        Assert.Equal(850, data.Subtotal);
        Assert.Equal(100, data.Items[0].UnitPrice);
        Assert.Equal(1000, data.Items[0].LineTotal);
        Assert.Contains(data.Discounts, discount => discount.Conditions == "Automatic bulk-order savings" && discount.DiscountValue == 150m);
    }

    /// <summary>
    /// Verifies finish surcharge is not subtracted from the bulk order discount.
    /// </summary>
    [Fact]
    public void BuildDraftPdfData_WithBulkPricingAndFinish_DiscountsOnlyVolumePriceDifference()
    {
        var data = ProjectQuotationPdfMapper.BuildDraftPdfData(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            null,
            null,
            "THB",
            DateTime.SpecifyKind(new DateTime(2026, 5, 2), DateTimeKind.Utc),
            "Standard: 5 business days after order confirmation",
            [
                new PartViewModel
                {
                    Name = "sanded-part.step",
                    Quantity = 28,
                    EstimatedBaseUnitPrice = 355.45m,
                    EstimatedDiscountedUnitPriceBeforeFinish = 291.44m,
                    FinishAdditionalUnitCost = 29.14m,
                    EstimatedUnitPrice = 320.58m,
                    EstimatedTotalAmount = 8976.24m,
                },
            ],
            []);

        Assert.Equal(10768.52m, data.SubtotalBeforeDiscount);
        Assert.Equal(1792.28m, data.TotalDiscount);
        Assert.Equal(8976.24m, data.Subtotal);
        Assert.Equal(384.59m, data.Items[0].UnitPrice);
        Assert.Equal(10768.52m, data.Items[0].LineTotal);
    }

    /// <summary>
    /// Verifies PDF lead time uses the same selected range shown on ProjectNew.
    /// </summary>
    [Fact]
    public void BuildDeliveryExpectation_WithSelectedLeadTime_IncludesSelectedRange()
    {
        var leadTime = new LeadTimeOptionDto("STANDARD", "Standard", 5, 8, 1m, true);

        var expectation = ProjectQuotationPdfMapper.BuildDeliveryExpectation(leadTime);

        Assert.Equal("Standard: 5 - 8 business days after order confirmation", expectation);
    }
}
