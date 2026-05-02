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
                    },
                },
            ],
            [new ProcessDto(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "FDM", "3D Printing (FDM)", null, 1)]);

        Assert.Equal(200, data.Subtotal);
        Assert.Equal(14, data.TaxAmount);
        Assert.Equal(214, data.TotalAmount);
        Assert.Equal("Jane Buyer", data.ContactPerson);
        Assert.Equal("0105559999999", data.CustomerTaxId);
        Assert.Equal("Head Office / สำนักงานใหญ่", data.CustomerBranch);
        Assert.Equal("+66 2 765 4321", data.CustomerPhone);
        Assert.Contains("88 Billing Road", data.BillingAddress);
        Assert.Contains("99 Shipping Road", data.ShippingAddress);
        Assert.Equal("3D Printing (FDM)", data.Items[0].ManufacturingProcess);
        Assert.Contains("Finish: As-printed", data.Items[0].Notes);
        Assert.Contains("Tolerance: FDM Standard +-0.3mm", data.Items[0].Notes);
        Assert.Contains("Color: Black", data.Items[0].Notes);
        Assert.Contains("Tapped holes: 4 x M3", data.Items[0].Notes);
        Assert.Contains("Inserts: 2 x HeatSet", data.Items[0].Notes);
        Assert.Contains("Inspection: Dimensional", data.Items[0].Notes);
        Assert.Contains("Drawing: bracket-drawing.pdf", data.Items[0].Notes);
    }
}
