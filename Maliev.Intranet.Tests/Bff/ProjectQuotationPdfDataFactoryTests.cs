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
                    Finish = "AS_MACHINED",
                    Color = "Black",
                    Tolerance = "ISO 2768-m",
                    RoughnessCode = "RA_3_2",
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
        Assert.Contains("Bounding box: 11.7 x 11.7 x 7.2 mm", data.Items[0].DetailLines);
        Assert.Contains("Surface finish: As-machined", data.Items[0].DetailLines);
        Assert.Contains("Tolerance: Medium (ISO 2768-m)", data.Items[0].DetailLines);
        Assert.Contains("Surface roughness: Ra 3.2 um", data.Items[0].DetailLines);
        Assert.Contains("Color: Black", data.Items[0].DetailLines);
        Assert.Contains("Inspection: Standard", data.Items[0].DetailLines);
        Assert.Contains("Drawing: drawing.png", data.Items[0].DetailLines);
        Assert.True(
            data.Items[0].DetailLines.IndexOf("Inspection: Standard") <
            data.Items[0].DetailLines.IndexOf("Drawing: drawing.png"));
        Assert.Equal("hello test 123", data.Items[0].Notes);
        Assert.DoesNotContain("Drawing:", data.Items[0].Notes, StringComparison.Ordinal);
        Assert.Equal(5000m, data.TotalDiscount);
        Assert.Equal(5000m, data.ManualDiscountAmount);
        Assert.Contains(data.Discounts, discount => discount.Conditions == "Automatic bulk-order savings");
        Assert.Contains(data.Discounts, discount => discount.Conditions == "Manual discount" && discount.DiscountValue == 3000m);
    }

    /// <summary>
    /// Verifies submitted ProjectNew PDF payloads keep their line data while receiving formal quotation metadata.
    /// </summary>
    [Fact]
    public void ApplyFormalQuotationMetadata_PreservesSubmittedPayloadAndStampsQuotationNumber()
    {
        var submitted = new QuotationPdfData
        {
            QuotationNumber = "DRAFT-111111111111111111",
            CustomerName = "Submitted Customer",
            Currency = "USD",
            DeliveryExpectations = "Standard: 5 business days after order confirmation",
            SpecialTerms = "50% deposit before production.",
            Items =
            [
                new QuotationPdfItem
                {
                    Index = 1,
                    PartName = "submitted-part.step",
                    MaterialName = "PLA",
                    Quantity = 2,
                    UnitPrice = 125m,
                    LineTotal = 250m
                }
            ],
            SubtotalBeforeDiscount = 250m,
            Subtotal = 250m,
            TaxAmount = 17.5m,
            TotalAmount = 267.5m
        };
        var project = new ProjectDetailDto
        {
            ProjectNumber = "PRJ-2026-0001",
            CustomerName = "Project Customer",
            CreatedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            ValidUntil = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
            Currency = "THB"
        };
        var quotation = new QuotationDetailDto
        {
            QuotationNumber = "Q-FORMAL",
            CurrentVersionNumber = 2,
            CurrencyCode = "THB",
            CreatedAt = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc),
            ValidityPeriodStart = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc),
            ValidityPeriodEnd = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            Versions =
            [
                new QuotationVersionDto
                {
                    VersionNumber = 2,
                    CurrencyCode = "THB",
                    ChangeSummary = "Formal version"
                }
            ]
        };

        var data = ProjectQuotationPdfDataFactory.ApplyFormalQuotationMetadata(submitted, project, quotation);

        Assert.Equal("Q-FORMAL", data.QuotationNumber);
        Assert.Equal(2, data.VersionNumber);
        Assert.Equal("Submitted Customer", data.CustomerName);
        Assert.Equal("USD", data.Currency);
        Assert.Equal("submitted-part.step", data.Items[0].PartName);
        Assert.Equal(125m, data.Items[0].UnitPrice);
        Assert.Equal(267.5m, data.TotalAmount);
        Assert.Equal("Formal version", data.ChangeSummary);
        Assert.Equal(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), data.ValidityEnd);
    }

    /// <summary>
    /// Verifies the automatic PDF uses the same part order as the generated quotation line items.
    /// </summary>
    [Fact]
    public void Build_WithProjectPartsOutOfOrder_UsesQuotationLineItemOrder()
    {
        var project = new ProjectDetailDto
        {
            CustomerName = "Nat Buyer",
            CreatedAt = new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc),
            Currency = "THB",
            Parts =
            [
                new ProjectPartDto
                {
                    FileName = "bravo.step",
                    ProcessType = "CNC_MILLING",
                    MaterialName = "Aluminum 6061-T6",
                    Quantity = 3,
                    ConfirmedUnitPrice = 200m,
                    Tolerance = "Iso2768 C",
                },
                new ProjectPartDto
                {
                    FileName = "alpha.step",
                    ProcessType = "CNC_MILLING",
                    MaterialName = "Aluminum 6061-T6",
                    Quantity = 2,
                    ConfirmedUnitPrice = 100m,
                    Tolerance = "Iso2768 C",
                },
            ],
        };
        var quotation = new QuotationDetailDto
        {
            QuotationNumber = "Q-ORDER",
            CustomerName = "Nat Buyer",
            CurrentVersionNumber = 1,
            CreatedAt = project.CreatedAt,
            CurrencyCode = "THB",
            Versions =
            [
                new QuotationVersionDto
                {
                    VersionNumber = 1,
                    LineItems =
                    [
                        new QuotationItemDto
                        {
                            Description = "alpha.step - CNC_MILLING (Aluminum 6061-T6)",
                            Quantity = 2,
                            UnitPrice = 100m,
                        },
                        new QuotationItemDto
                        {
                            Description = "bravo.step - CNC_MILLING (Aluminum 6061-T6)",
                            Quantity = 3,
                            UnitPrice = 200m,
                        },
                    ],
                },
            ],
        };

        var data = ProjectQuotationPdfDataFactory.Build(project, quotation);

        Assert.Equal("alpha.step", data.Items[0].PartName);
        Assert.Equal("bravo.step", data.Items[1].PartName);
        Assert.Equal(1, data.Items[0].Index);
        Assert.Equal(2, data.Items[1].Index);
        Assert.Contains("Tolerance: ISO 2768-c", data.Items[0].DetailLines);
        Assert.Contains("Tolerance: ISO 2768-c", data.Items[1].DetailLines);
    }
}
