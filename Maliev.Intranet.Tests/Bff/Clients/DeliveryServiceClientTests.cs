using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Clients;

public sealed class DeliveryServiceClientTests
{
    [Fact]
    public async Task GetDeliveryNotesAsync_MapsDeliveryServicePagination()
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
                        deliveryNoteId = "DN-2026-000001",
                        orderId = "ORD-456",
                        customerId = Guid.Parse("15ae7e17-9413-4b92-9391-e74114f4ee23"),
                        customerName = "Apex Robotics",
                        deliveryDate = new DateTime(2026, 5, 20),
                        status = "Pending",
                        itemCount = 1,
                        carrierName = "Kerry Express",
                        trackingNumber = "TH-E2E-001",
                        createdAt = new DateTime(2026, 5, 16, 8, 0, 0, DateTimeKind.Utc)
                    }
                },
                totalCount = 21,
                page = 2,
                pageSize = 10
            });
        });

        var result = await client.GetDeliveryNotesAsync(page: 2, pageSize: 10);

        Assert.NotNull(capturedRequest);
        Assert.Equal("/delivery/v1/delivery-notes?page=2&pageSize=10&sortBy=delivery_date&sortOrder=desc", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(result);
        Assert.Equal(2, result.Meta.CurrentPage);
        Assert.Equal(10, result.Meta.PageSize);
        Assert.Equal(21, result.Meta.TotalCount);
        Assert.Equal(3, result.Meta.TotalPages);
        var note = Assert.Single(result.Data);
        Assert.Equal("DN-2026-000001", note.Id);
        Assert.Equal("DN-2026-000001", note.DeliveryNoteId);
        Assert.Equal("DN-2026-000001", note.DeliveryNoteNumber);
        Assert.Equal("ORD-456", note.OrderNumber);
        Assert.Equal("Apex Robotics", note.CustomerName);
    }

    [Fact]
    public async Task CreateDeliveryNoteAsync_PostsDeliveryServiceContract()
    {
        JsonDocument? payload = null;
        HttpRequestMessage? capturedRequest = null;
        var customerId = Guid.Parse("1de67545-7a62-4c3e-b8c7-08af97ac01c0");
        var client = MakeClient(async (request, ct) =>
        {
            capturedRequest = request;
            payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            return JsonContent.Create(new
            {
                deliveryNoteId = "DN-2026-000002",
                orderId = "ORD-789",
                customerId,
                customerName = "Nexus Manufacturing",
                deliveryDate = new DateTime(2026, 5, 21),
                status = "Pending",
                trackingNumber = "TH-E2E-002",
                carrierName = "Flash Express",
                shippingAddressLine1 = "88 Logistics Road",
                shippingCity = "Bangkok",
                shippingProvince = "Bangkok",
                shippingPostalCode = "10500",
                shippingCountry = "Thailand",
                items = new[]
                {
                    new
                    {
                        id = 1,
                        orderId = "ORD-789",
                        productCode = "MLV-001",
                        productName = "Machined bracket",
                        quantityOrdered = 2m,
                        quantityManufactured = 2m,
                        quantityDelivered = 2m,
                        unitOfMeasure = "pcs"
                    }
                },
                createdAt = new DateTime(2026, 5, 16, 9, 0, 0, DateTimeKind.Utc),
                createdBy = "tester"
            });
        });

        var result = await client.CreateDeliveryNoteAsync(new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-789",
            CustomerId = customerId,
            CustomerName = "Nexus Manufacturing",
            DeliveryDate = new DateTime(2026, 5, 21),
            CarrierName = "Flash Express",
            TrackingNumber = "TH-E2E-002",
            ShippingAddressLine1 = "88 Logistics Road",
            ShippingCity = "Bangkok",
            ShippingProvince = "Bangkok",
            ShippingPostalCode = "10500",
            ShippingCountry = "Thailand",
            Items =
            [
                new CreateDeliveryNoteItemRequest
                {
                    ProductCode = "MLV-001",
                    ProductName = "Machined bracket",
                    QuantityOrdered = 2,
                    QuantityManufactured = 2,
                    QuantityDelivered = 2,
                    UnitOfMeasure = "pcs"
                }
            ]
        });

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("/delivery/v1/delivery-notes", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(result);
        Assert.Equal("DN-2026-000002", result.DeliveryNoteId);
        Assert.Equal("DN-2026-000002", result.Id);
        Assert.Single(result.Items);
        Assert.Equal("88 Logistics Road, Bangkok, Bangkok, 10500, Thailand", result.CustomerAddress);
        Assert.NotNull(payload);
        var root = payload.RootElement;
        Assert.Equal("ORD-789", root.GetProperty("orderId").GetString());
        Assert.Equal(customerId, root.GetProperty("customerId").GetGuid());
        Assert.Equal("Flash Express", root.GetProperty("carrierName").GetString());
        Assert.Equal("MLV-001", root.GetProperty("items")[0].GetProperty("productCode").GetString());
        Assert.Equal(2m, root.GetProperty("items")[0].GetProperty("quantityDelivered").GetDecimal());
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_PostsDeliveryStatusContract()
    {
        JsonDocument? payload = null;
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(async (request, ct) =>
        {
            capturedRequest = request;
            payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            return JsonContent.Create(new
            {
                deliveryNoteId = "DN-2026-000003",
                customerId = Guid.NewGuid(),
                customerName = "Delivered Customer",
                deliveryDate = DateTime.UtcNow.Date,
                status = "Delivered",
                receivedByName = "Somchai Receiver",
                items = Array.Empty<object>(),
                createdAt = DateTime.UtcNow,
                createdBy = "tester"
            });
        });

        var result = await client.UpdateDeliveryStatusAsync("DN-2026-000003", new UpdateDeliveryStatusRequest
        {
            NewStatus = "Delivered",
            ReceivedByName = "Somchai Receiver",
            ActualDeliveryTime = new DateTime(2026, 5, 22, 10, 30, 0, DateTimeKind.Utc)
        });

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Patch, capturedRequest.Method);
        Assert.Equal("/delivery/v1/delivery-notes/DN-2026-000003/status", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(payload);
        Assert.Equal("Delivered", payload.RootElement.GetProperty("newStatus").GetString());
        Assert.Equal("Somchai Receiver", payload.RootElement.GetProperty("receivedByName").GetString());
        Assert.NotNull(result);
        Assert.Equal("Delivered", result.Status);
        Assert.Equal("Somchai Receiver", result.ReceivedByName);
    }

    [Fact]
    public async Task GeneratePdfAsync_TargetsDeliveryServiceGenerateRoute()
    {
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(HttpStatusCode.Accepted, request =>
        {
            capturedRequest = request;
            return JsonContent.Create(new
            {
                deliveryNoteId = "DN-2026-000004",
                status = "Requested",
                message = "PDF generation request has been queued."
            });
        });

        var result = await client.GeneratePdfAsync("DN-2026-000004");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("/delivery/v1/delivery-notes/DN-2026-000004/generate-pdf", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(result);
        Assert.Equal("DN-2026-000004", result.DeliveryNoteId);
        Assert.Equal("Requested", result.Status);
    }

    private static DeliveryServiceClient MakeClient(Func<HttpRequestMessage, HttpContent> contentFactory)
    {
        var handler = new MockHttpMessageHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = contentFactory(request)
            }));

        return new DeliveryServiceClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://test")
        });
    }

    private static DeliveryServiceClient MakeClient(Func<HttpRequestMessage, CancellationToken, Task<HttpContent>> contentFactory)
    {
        var handler = new MockHttpMessageHandler(async (request, ct) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = await contentFactory(request, ct)
            });

        return new DeliveryServiceClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://test")
        });
    }

    private static DeliveryServiceClient MakeClient(HttpStatusCode statusCode, Func<HttpRequestMessage, HttpContent> contentFactory)
    {
        var handler = new MockHttpMessageHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = contentFactory(request)
            }));

        return new DeliveryServiceClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://test")
        });
    }
}
