using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Tests.Bff;

/// <summary>
/// Tests automatic quotation PDF payload mapping from generated projects.
/// </summary>
public class ProjectQuotationPdfDataFactoryTests
{
    /// <summary>
    /// Verifies the automatic ProjectNew quotation path keeps the rich project, customer, and discount data needed by the PDF layout.
    /// </summary>
    [Fact]
    public void Build_IncludesRichProjectCustomerAndDiscountDetails()
    {
        var quotationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var project = new ProjectDetailDto
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            CustomerId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            CustomerName = "Nat Buyer",
            CustomerEmail = "nat@example.com",
            CustomerCompanyName = "Eastern Seaboard Tooling",
            CustomerCompanyPhone = "+6621000002",
            BillingAddressLine = "110 Seed Testing Road" + Environment.NewLine + "Chiang Mai 50000",
            ShippingAddressLine = "Warehouse 2" + Environment.NewLine + "Chiang Mai 50000",
            QuotationId = quotationId,
            QuotationNumber = "Q-2AEA6F28",
            CreatedAt = new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc),
            ValidUntil = new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc),
            Currency = "THB",
            Parts =
            [
                new ProjectPartDto
                {
                    FileName = "SYSTEMBOARD_MIDDLE.stl",
                    ThumbnailUrl = "https://uploads.example/signed-thumbnail.png",
                    ProcessType = "CNC_MILLING",
                    MaterialName = "Aluminum 6061-T6",
                    Quantity = 12,
                    ConfirmedUnitPrice = 2500m,
                    Finish = "As Machined",
                    Tolerance = "ISO 2768-m",
                    Dimensions = new ModelDimensionsDto { X = 11.7, Y = 11.7, Z = 7.2 },
                    PartNotes = "hello test 123",
                    DrawingFiles =
                    [
                        new ProjectPartAttachmentDto { FileName = "drawing.png" }
                    ]
                }
            ]
        };
        var quotation = new QuotationDetailDto
        {
            Id = quotationId,
            QuotationNumber = "Q-2AEA6F28",
            CustomerName = "Eastern Seaboard Tooling",
            CurrentVersionNumber = 1,
            CreatedAt = project.CreatedAt,
            ValidityPeriodStart = project.CreatedAt,
            ValidityPeriodEnd = project.ValidUntil!.Value,
            CurrencyCode = "THB",
            DeliveryExpectations = "Standard: 16 - 19 business days after order confirmation",
            Total = 35240m,
            Versions =
            [
                new QuotationVersionDto
                {
                    VersionNumber = 1,
                    TotalPrice = 35240m,
                    ManualDiscountAmount = 3000m,
                    ShippingCost = 6000m,
                    TaxAmount = 2240m,
                    SpecialTerms = "test terms",
                    DiscountStructure = new SalesDiscountStructureDto
                    {
                        DiscountType = SalesDiscountType.FixedAmount,
                        DiscountValue = 2000m,
                        Conditions = "Automatic bulk-order savings"
                    },
                    LineItems =
                    [
                        new QuotationItemDto
                        {
                            Description = "SYSTEMBOARD_MIDDLE.stl - CNC_MILLING (Aluminum 6061-T6)",
                            Quantity = 12,
                            UnitPrice = 2500m
                        }
                    ]
                }
            ]
        };

        var data = ProjectQuotationPdfDataFactory.Build(project, quotation);

        Assert.Equal("Eastern Seaboard Tooling", data.CustomerName);
        Assert.Contains("Attn: Nat Buyer", data.CustomerDisplayLines);
        Assert.Equal(["110 Seed Testing Road", "Chiang Mai 50000"], data.BillingAddressLines);
        Assert.Equal(["Warehouse 2", "Chiang Mai 50000"], data.ShippingAddressLines);
        Assert.Equal("SYSTEMBOARD_MIDDLE.stl", data.Items[0].PartName);
        Assert.Equal("https://uploads.example/signed-thumbnail.png", data.Items[0].ThumbnailUrl);
        Assert.Equal("CNC Milling", data.Items[0].ManufacturingProcess);
        Assert.Equal("Aluminum 6061-T6", data.Items[0].MaterialName);
        Assert.Contains("11.7 x 11.7 x 7.2 mm", data.Items[0].DetailLines);
        Assert.Contains("Drawing: drawing.png", data.Items[0].DetailLines);
        Assert.Equal("hello test 123", data.Items[0].Notes);
        Assert.DoesNotContain("Drawing:", data.Items[0].Notes, StringComparison.Ordinal);
        Assert.Contains(data.Discounts, discount => discount.Conditions == "Automatic bulk-order savings");
        Assert.Contains(data.Discounts, discount => discount.Conditions == "Manual discount" && discount.DiscountValue == 3000m);
    }
}
