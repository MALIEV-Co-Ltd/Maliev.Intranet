using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Services;

namespace Maliev.Intranet.Tests.Bff.Services;

public class SearchResultMapperTests
{
    [Fact]
    public void ResolveHref_ForCustomerResult_MapsToCustomerDetail()
    {
        var customerId = Guid.NewGuid();
        var result = Result("CustomerService", "customer", customerId.ToString(), "Acme");

        var href = SearchResultMapper.ResolveHref(result);

        Assert.Equal($"/customers/{customerId}", href);
    }

    [Fact]
    public void ResolveHref_ForProjectResult_MapsToProjectDetail()
    {
        var projectId = Guid.NewGuid();
        var result = Result("ProjectService", "project", projectId.ToString(), "Fixture");

        var href = SearchResultMapper.ResolveHref(result);

        Assert.Equal($"/sales/projects/{projectId}", href);
    }

    [Fact]
    public void ResolveHref_ForProjectPartResult_MapsToProjectPartsTab()
    {
        var projectId = Guid.NewGuid();
        var partId = Guid.NewGuid();
        var result = Result("ProjectService", "project-part", $"{projectId}:{partId}", "d15-16.stp");

        var href = SearchResultMapper.ResolveHref(result);

        Assert.Equal($"/sales/projects/{projectId}?tab=parts&partId={partId}", href);
    }

    [Fact]
    public void ResolveHref_ForPurchaseOrderResult_MapsToPurchasingDetail()
    {
        const string purchaseOrderId = "1001";
        var result = Result("PurchaseOrderService", "purchase-order", purchaseOrderId, "PO-1001");

        var href = SearchResultMapper.ResolveHref(result);

        Assert.Equal("/purchasing/1001", href);
    }

    [Fact]
    public void ResolveHref_ForMaterialResult_MapsToMaterialDetail()
    {
        var materialId = Guid.NewGuid();
        var result = Result("MaterialService", "material", materialId.ToString(), "Aluminum 6061");

        var href = SearchResultMapper.ResolveHref(result);

        Assert.Equal($"/mfg/materials/{materialId}", href);
    }

    [Fact]
    public void ToGlobalSearchResponse_UsesKnownApplicationArea()
    {
        var response = new SearchServiceResponseDto(
            "invoice",
            1,
            [Result("InvoiceService", "invoice", Guid.NewGuid().ToString(), "INV-1001")]);

        var mapped = SearchResultMapper.ToGlobalSearchResponse(response, "invoice");

        var row = Assert.Single(mapped.Results);
        Assert.Equal("Finance", row.Area);
        Assert.Equal("invoice", row.ResourceType);
    }

    [Fact]
    public void ToGlobalSearchResponse_ForProjectGeneratedStatus_UsesCompactStatusText()
    {
        var response = new SearchServiceResponseDto(
            "project",
            1,
            [Result("ProjectService", "project", Guid.NewGuid().ToString(), "PRJ-2026-0001", status: "QuotationGenerated")]);

        var mapped = SearchResultMapper.ToGlobalSearchResponse(response, "project");

        var row = Assert.Single(mapped.Results);
        Assert.Equal("Generated", row.Status);
    }

    [Fact]
    public void ToGlobalSearchResponse_ForCustomerResult_AddsAvatarText()
    {
        var response = new SearchServiceResponseDto(
            "pim",
            1,
            [Result("CustomerService", "customer", Guid.NewGuid().ToString(), "Pimchanok Garcia")]);

        var mapped = SearchResultMapper.ToGlobalSearchResponse(response, "pim");

        var row = Assert.Single(mapped.Results);
        Assert.Equal("PG", row.AvatarText);
    }

    private static SearchServiceResultDto Result(
        string sourceService,
        string resourceType,
        string resourceId,
        string title,
        string? status = null)
    {
        return new SearchServiceResultDto(
            sourceService,
            resourceType,
            resourceId,
            title,
            null,
            null,
            status,
            "search.documents.read",
            1.0d,
            DateTimeOffset.UtcNow);
    }
}
