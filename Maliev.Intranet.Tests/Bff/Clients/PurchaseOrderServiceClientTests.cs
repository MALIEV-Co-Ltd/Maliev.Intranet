using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Clients;

public class PurchaseOrderServiceClientTests
{
    [Fact]
    public async Task GetPurchaseOrdersAsync_TargetsPurchaseOrdersEndpoint()
    {
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(request =>
        {
            capturedRequest = request;
            return JsonContent.Create(new
            {
                items = new[]
                {
                    new
                    {
                        id = 1001,
                        orderNumber = "PO-1001",
                        orderType = "External",
                        status = "Pending",
                        supplierID = 42,
                        supplierName = "Thai Metals",
                        orderID = 5001,
                        currencyCode = "THB",
                        totalAmount = 1200m,
                        expectedDeliveryDate = DateTime.UtcNow.Date.AddDays(7),
                        createdAt = DateTime.UtcNow.Date
                    }
                },
                totalCount = 31,
                page = 2,
                pageSize = 25
            });
        });

        var result = await client.GetPurchaseOrdersAsync(page: 2, pageSize: 25);

        Assert.NotNull(capturedRequest);
        Assert.Equal("/purchase-order/v1/purchase-orders?Page=2&PageSize=25&SortBy=createdAt&SortDirection=desc", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal(31, result.Meta.TotalItems);
        Assert.Equal(2, result.Meta.TotalPages);
        Assert.Equal(1001, result.Data.Single().Id);
        Assert.Equal("PO-1001", result.Data.Single().PoNumber);
        Assert.Equal(42, result.Data.Single().SupplierId);
    }

    [Fact]
    public async Task GetPurchaseOrdersAsync_ForwardsFiltersToPurchaseOrderService()
    {
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(request =>
        {
            capturedRequest = request;
            return JsonContent.Create(new { items = Array.Empty<object>(), totalCount = 0, page = 2, pageSize = 10 });
        });

        await client.GetPurchaseOrdersAsync(
            status: "Pending",
            orderType: "External",
            supplierId: 7,
            orderId: 42,
            page: 2,
            pageSize: 10);

        Assert.NotNull(capturedRequest);
        Assert.Equal(
            "/purchase-order/v1/purchase-orders?Page=2&PageSize=10&Status=Pending&OrderType=External&SupplierId=7&OrderId=42&SortBy=createdAt&SortDirection=desc",
            capturedRequest.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task GetPurchaseOrderByIdAsync_TargetsPurchaseOrdersDetailEndpoint()
    {
        const int purchaseOrderId = 1001;
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(request =>
        {
            capturedRequest = request;
            return JsonContent.Create(new
            {
                id = 1001,
                orderNumber = "PO-1001",
                orderType = "Internal",
                status = "Approved",
                supplierID = 77,
                supplierName = "Apex Supply",
                orderID = 2001,
                currencyID = 1,
                currencyCode = "THB",
                totalAmount = 2500m,
                subtotalAmount = 2500m,
                expectedDeliveryDate = DateTime.UtcNow.Date.AddDays(10),
                createdAt = DateTime.UtcNow.Date,
                orderDate = DateTime.UtcNow.Date,
                items = new[]
                {
                    new
                    {
                        id = 1,
                        externalOrderItemId = 501,
                        productCode = "AL6061",
                        productName = "Aluminium 6061",
                        quantity = 2m,
                        unitOfMeasure = "pcs",
                        unitPrice = 1250m,
                        totalPrice = 2500m,
                        currency = "THB",
                        cachedAt = DateTime.UtcNow,
                        externallyModified = false
                    }
                },
                files = Array.Empty<object>(),
                rowVersion = "AAAA"
            });
        });

        var result = await client.GetPurchaseOrderByIdAsync(purchaseOrderId);

        Assert.NotNull(capturedRequest);
        Assert.Equal($"/purchase-order/v1/purchase-orders/{purchaseOrderId}", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(result);
        Assert.Equal(1001, result.Id);
        Assert.Equal("PO-1001", result.PoNumber);
        Assert.Equal(77, result.SupplierId);
        Assert.Single(result.Items);
        Assert.Equal(501, result.Items[0].ExternalOrderItemId);
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_UsesRealCreateDtoJsonNames()
    {
        string? payload = null;
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(async request =>
        {
            capturedRequest = request;
            payload = await request.Content!.ReadAsStringAsync();
            return JsonContent.Create(new
            {
                id = 1002,
                orderNumber = "PO-1002",
                orderType = "External",
                status = "Pending",
                supplierID = 12,
                supplierName = "Supplier",
                orderID = 44,
                currencyID = 1,
                currencyCode = "THB",
                totalAmount = 0m,
                createdAt = DateTime.UtcNow,
                items = Array.Empty<object>(),
                files = Array.Empty<object>(),
                rowVersion = "AAAA"
            });
        });

        var result = await client.CreatePurchaseOrderAsync(new CreatePurchaseOrderRequest
        {
            OrderType = 1,
            SupplierId = 12,
            OrderId = 44,
            CustomerPo = "CPO-44",
            CurrencyId = 1,
            WhtRate = 3,
            ExpectedDeliveryDate = new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc),
            Notes = "Urgent",
            Items = [new PurchaseOrderLineItemDto { ExternalOrderItemId = 9001, Quantity = 2 }]
        });

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("/purchase-order/v1/purchase-orders", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(result);
        Assert.Equal(1002, result.Id);
        Assert.NotNull(payload);

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("orderType").GetInt32());
        Assert.Equal(12, root.GetProperty("supplierID").GetInt32());
        Assert.Equal(44, root.GetProperty("orderID").GetInt32());
        Assert.Equal("CPO-44", root.GetProperty("customerPO").GetString());
        Assert.Equal(1, root.GetProperty("currencyID").GetInt32());
        Assert.Equal(3m, root.GetProperty("whtRate").GetDecimal());
        Assert.Equal(9001, root.GetProperty("items")[0].GetProperty("externalOrderItemId").GetInt32());
        Assert.Equal(2m, root.GetProperty("items")[0].GetProperty("quantity").GetDecimal());
    }

    [Theory]
    [InlineData("approve", "/purchase-order/v1/purchase-orders/1001/approve")]
    [InlineData("send", "/purchase-order/v1/purchase-orders/1001/send-to-supplier")]
    [InlineData("receive", "/purchase-order/v1/purchase-orders/1001/receive?isPartialReceipt=false")]
    public async Task LifecycleMethods_TargetRealPurchaseOrderRoutes(string action, string expectedPath)
    {
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(request =>
        {
            capturedRequest = request;
            return JsonContent.Create(new
            {
                id = 1001,
                orderNumber = "PO-1001",
                orderType = "Internal",
                status = "Approved",
                supplierID = 1,
                supplierName = "Supplier",
                orderID = 2,
                currencyID = 1,
                currencyCode = "THB",
                totalAmount = 0m,
                createdAt = DateTime.UtcNow,
                items = Array.Empty<object>(),
                files = Array.Empty<object>(),
                rowVersion = "AAAA"
            });
        });

        _ = action switch
        {
            "approve" => await client.ApprovePurchaseOrderAsync(1001),
            "send" => await client.SendToSupplierAsync(1001),
            _ => await client.ReceivePurchaseOrderAsync(1001, isPartialReceipt: false)
        };

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal(expectedPath, capturedRequest.RequestUri!.PathAndQuery);
    }

    private static PurchaseOrderServiceClient MakeClient(Func<HttpRequestMessage, HttpContent> contentFactory)
    {
        var handler = new MockHttpMessageHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = contentFactory(request)
            }));

        return new PurchaseOrderServiceClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://test")
        });
    }

    private static PurchaseOrderServiceClient MakeClient(Func<HttpRequestMessage, Task<HttpContent>> contentFactory)
    {
        var handler = new MockHttpMessageHandler(async (request, ct) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = await contentFactory(request)
            });

        return new PurchaseOrderServiceClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://test")
        });
    }
}
