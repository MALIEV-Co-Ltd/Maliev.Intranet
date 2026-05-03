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
                CompanyPhone = "+66 2 123 4567",
            },
            new CustomerDetailDto
            {
                Name = "Jane Buyer",
                CompanyName = "Acme Thailand",
                CompanyPhone = "+66 2 765 4321",
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
        Assert.Contains("88 Billing Road", data.BillingAddress);
        Assert.Contains("99 Shipping Road", data.ShippingAddress);
        Assert.Equal("bracket.step", data.Items[0].PartName);
        Assert.Equal("PLA", data.Items[0].MaterialName);
        Assert.Equal("3D Printing (FDM)", data.Items[0].ManufacturingProcess);
        Assert.Contains("As-printed", data.Items[0].DetailLines);
        Assert.Contains("FDM Standard +-0.3mm", data.Items[0].DetailLines);
        Assert.Contains("Ra 1.6 um", data.Items[0].DetailLines);
        Assert.Contains("Black", data.Items[0].DetailLines);
        Assert.Contains("Tapped holes: 4 x M3", data.Items[0].Notes);
        Assert.Contains("Inserts: 2 x HeatSet", data.Items[0].Notes);
        Assert.Contains("Deburring", data.Items[0].DetailLines);
        Assert.Contains("Inspection: Dimensional", data.Items[0].Notes);
        Assert.Contains("Drawing: bracket-drawing.pdf", data.Items[0].Notes);
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
        Assert.Equal("ณฐพล วนาศรีวิไล (028816002)", data.CustomerDisplayLines[0]);
        Assert.Equal("บริษัท มาลีฟ จำกัด (สำนักงานใหญ่)", data.CustomerDisplayLines[1]);
        Assert.Equal(["36/1 หมู่ 3", "ตำบลคลองข่อย", "อำเภอปากเกร็ด", "จังหวัดนนทบุรี 11120"], data.BillingAddressLines);
        Assert.Equal(["36/2 หมู่ 4", "ตำบลบางจาก", "อำเภอภาษีเจริญ", "จังหวัดกรุงเทพมหานคร 10160"], data.ShippingAddressLines);
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
