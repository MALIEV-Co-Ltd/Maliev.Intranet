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
    public void ResolveHref_ForPurchaseOrderResult_MapsToPurchasingDetail()
    {
        const string purchaseOrderId = "1001";
        var result = Result("PurchaseOrderService", "purchase-order", purchaseOrderId, "PO-1001");

        var href = SearchResultMapper.ResolveHref(result);

        Assert.Equal("/purchasing/1001", href);
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

    private static SearchServiceResultDto Result(string sourceService, string resourceType, string resourceId, string title)
    {
        return new SearchServiceResultDto(
            sourceService,
            resourceType,
            resourceId,
            title,
            null,
            null,
            null,
            "search.documents.read",
            1.0d,
            DateTimeOffset.UtcNow);
    }
}
