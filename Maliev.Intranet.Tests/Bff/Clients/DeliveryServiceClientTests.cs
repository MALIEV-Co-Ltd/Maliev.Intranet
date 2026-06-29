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
        var signatureFileId = Guid.Parse("d34091ed-62c2-49f0-bc39-7f8ef680c2d8");
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
                signatureFileId,
                items = Array.Empty<object>(),
                createdAt = DateTime.UtcNow,
                createdBy = "tester"
            });
        });

        var result = await client.UpdateDeliveryStatusAsync("DN-2026-000003", new UpdateDeliveryStatusRequest
        {
            NewStatus = "Delivered",
            ReceivedByName = "Somchai Receiver",
            ActualDeliveryTime = new DateTime(2026, 5, 22, 10, 30, 0, DateTimeKind.Utc),
            SignatureFileId = signatureFileId
        });

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Patch, capturedRequest.Method);
        Assert.Equal("/delivery/v1/delivery-notes/DN-2026-000003/status", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(payload);
        Assert.Equal("Delivered", payload.RootElement.GetProperty("newStatus").GetString());
        Assert.Equal("Somchai Receiver", payload.RootElement.GetProperty("receivedByName").GetString());
        Assert.Equal(signatureFileId, payload.RootElement.GetProperty("signatureFileId").GetGuid());
        Assert.NotNull(result);
        Assert.Equal("Delivered", result.Status);
        Assert.Equal("Somchai Receiver", result.ReceivedByName);
        Assert.Equal(signatureFileId, result.SignatureFileId);
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

    [Fact]
    public async Task GetFilesAsync_MapsDeliveryServiceFiles()
    {
        HttpRequestMessage? capturedRequest = null;
        var fileId = Guid.Parse("c3cfb238-4d49-4b46-88d2-c804841c3ebe");
        var client = MakeClient(request =>
        {
            capturedRequest = request;
            return JsonContent.Create(new[]
            {
                new
                {
                    fileId,
                    deliveryNoteId = "DN-2026-000004",
                    fileType = "DeliveryNotePdf",
                    originalFileName = "delivery-note-DN-2026-000004.pdf",
                    storageUrl = "https://storage.example/delivery-note-DN-2026-000004.pdf",
                    fileSizeBytes = 12450,
                    description = "Generated delivery note PDF",
                    uploadedAt = new DateTime(2026, 6, 18, 8, 15, 0, DateTimeKind.Utc),
                    uploadedBy = "PdfService",
                    version = 3U
                }
            });
        });

        var result = await client.GetFilesAsync("DN-2026-000004");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal("/delivery/v1/delivery-notes/DN-2026-000004/files", capturedRequest.RequestUri!.PathAndQuery);
        var file = Assert.Single(result);
        Assert.Equal(fileId, file.FileId);
        Assert.Equal("DN-2026-000004", file.DeliveryNoteId);
        Assert.Equal("DeliveryNotePdf", file.FileType);
        Assert.Equal("delivery-note-DN-2026-000004.pdf", file.OriginalFileName);
        Assert.Equal("https://storage.example/delivery-note-DN-2026-000004.pdf", file.StorageUrl);
        Assert.Equal("PdfService", file.UploadedBy);
    }

    [Fact]
    public async Task DownloadFileAsync_CallsDeliveryServiceDownloadRoute()
    {
        HttpRequestMessage? capturedRequest = null;
        var fileId = Guid.Parse("c3cfb238-4d49-4b46-88d2-c804841c3ebe");
        var expectedBytes = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 77, 83 };
        var client = MakeClient(request =>
        {
            capturedRequest = request;
            var content = new ByteArrayContent(expectedBytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = "proof.png"
            };
            return content;
        });

        using var response = await client.DownloadFileAsync("DN-2026-000004", fileId);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal($"/delivery/v1/delivery-notes/DN-2026-000004/files/{fileId:D}/download", capturedRequest.RequestUri!.PathAndQuery);
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(expectedBytes, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task UploadFileAsync_PostsDeliveryServiceMultipartFileContract()
    {
        HttpRequestMessage? capturedRequest = null;
        string? multipartBody = null;
        var fileId = Guid.Parse("c263a794-1c98-45b6-858e-d861482a7b1d");
        var client = MakeClient(async (request, ct) =>
        {
            capturedRequest = request;
            multipartBody = await request.Content!.ReadAsStringAsync(ct);
            return JsonContent.Create(new
            {
                fileId,
                deliveryNoteId = "DN-2026-000005",
                fileType = "Signature",
                originalFileName = "receiver-signature.png",
                storageUrl = "memory://delivery/signature.png",
                fileSizeBytes = 18,
                description = "Signed at customer dock",
                uploadedAt = new DateTime(2026, 6, 19, 9, 0, 0, DateTimeKind.Utc),
                uploadedBy = "tester",
                version = 1U
            });
        });

        await using var stream = new MemoryStream("signature-proof"u8.ToArray());
        var result = await client.UploadFileAsync(
            "DN-2026-000005",
            stream,
            "receiver-signature.png",
            "image/png",
            "Signature",
            "Signed at customer dock");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("/delivery/v1/delivery-notes/DN-2026-000005/files", capturedRequest.RequestUri!.PathAndQuery);
        Assert.StartsWith("multipart/form-data", capturedRequest.Content!.Headers.ContentType!.MediaType, StringComparison.Ordinal);
        Assert.Contains("name=file", multipartBody, StringComparison.Ordinal);
        Assert.Contains("filename=receiver-signature.png", multipartBody, StringComparison.Ordinal);
        Assert.Contains("name=fileType", multipartBody, StringComparison.Ordinal);
        Assert.Contains("Signature", multipartBody, StringComparison.Ordinal);
        Assert.Contains("name=description", multipartBody, StringComparison.Ordinal);
        Assert.NotNull(result);
        Assert.Equal(fileId, result.FileId);
        Assert.Equal("Signature", result.FileType);
        Assert.Equal("receiver-signature.png", result.OriginalFileName);
        Assert.Equal("memory://delivery/signature.png", result.StorageUrl);
    }

    [Fact]
    public async Task GetShippingCouriersAsync_TargetsDeliveryServiceShippingCouriersRoute()
    {
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(request =>
        {
            capturedRequest = request;
            return JsonContent.Create(new[]
            {
                new
                {
                    courierCode = "thaipost",
                    courierName = "Thailand Post",
                    note = "Domestic parcel",
                    logoUrl = "https://cdn.example/thailand-post.svg",
                    scope = "domestic",
                    provider = "Shippop"
                }
            });
        });

        var result = await client.GetShippingCouriersAsync();

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal("/delivery/v1/shipping/couriers", capturedRequest.RequestUri!.PathAndQuery);
        var courier = Assert.Single(result);
        Assert.Equal("thaipost", courier.CourierCode);
        Assert.Equal("Thailand Post", courier.CourierName);
        Assert.Equal("https://cdn.example/thailand-post.svg", courier.LogoUrl);
        Assert.Equal("domestic", courier.Scope);
        Assert.Equal("Shippop", courier.Provider);
    }

    [Fact]
    public async Task GetShippingRatesAsync_PostsDeliveryServiceShippingRateContract()
    {
        JsonDocument? payload = null;
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(async (request, ct) =>
        {
            capturedRequest = request;
            payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            return JsonContent.Create(new[]
            {
                new
                {
                    courierCode = "flash",
                    courierName = "Flash Express",
                    price = 72.5m,
                    currency = "THB",
                    serviceLevel = "standard",
                    estimatedDelivery = "2026-06-22",
                    courierLogoUrl = "https://cdn.example/flash.svg",
                    packageCount = 2,
                    totalWeight = 1640m,
                    packages = new[]
                    {
                        new
                        {
                            packageNumber = 1,
                            name = "MALIEV box 1",
                            weight = 820m,
                            width = 18m,
                            length = 28m,
                            height = 12m,
                            price = 36.25m,
                            currency = "THB",
                            estimatedDelivery = "2026-06-22",
                            items = new[]
                            {
                                new { name = "Machined bracket", quantity = 5, unitWidth = 12m, unitLength = 20m, unitHeight = 8m, unitWeight = 150m }
                            }
                        },
                        new
                        {
                            packageNumber = 2,
                            name = "MALIEV box 2",
                            weight = 820m,
                            width = 18m,
                            length = 28m,
                            height = 12m,
                            price = 36.25m,
                            currency = "THB",
                            estimatedDelivery = "2026-06-22",
                            items = new[]
                            {
                                new { name = "Machined bracket", quantity = 5, unitWidth = 12m, unitLength = 20m, unitHeight = 8m, unitWeight = 150m }
                            }
                        }
                    },
                    provider = "GoShip"
                }
            });
        });

        var result = await client.GetShippingRatesAsync(new ShippingRateRequestDto
        {
            From = new ShippingAddressDto
            {
                Name = "MALIEV",
                Address = "MALIEV",
                District = "Pathum Wan",
                State = "Pathum Wan",
                Province = "Bangkok",
                Postcode = "10400",
                Tel = "020000000"
            },
            To = new ShippingAddressDto
            {
                Name = "Nexus Manufacturing",
                Address = "88 Logistics Road",
                District = "Mueang",
                State = "Mueang",
                Province = "Chonburi",
                Postcode = "20000",
                CountryCode = "AU",
                Tel = "0800000000"
            },
            Parcel = new ShippingParcelDto
            {
                Name = "Machined bracket",
                Weight = 750,
                Length = 20,
                Width = 12,
                Height = 8
            },
            CourierCodes = ["flash"],
            Parts =
            [
                new ShippingPackagePartDto
                {
                    Name = "Machined bracket",
                    Quantity = 10,
                    Weight = 150,
                    Length = 20,
                    Width = 12,
                    Height = 8
                }
            ]
        });

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("/delivery/v1/shipping/rates", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(payload);
        Assert.Equal("10400", payload.RootElement.GetProperty("from").GetProperty("postcode").GetString());
        Assert.Equal("20000", payload.RootElement.GetProperty("to").GetProperty("postcode").GetString());
        Assert.Equal("AU", payload.RootElement.GetProperty("to").GetProperty("countryCode").GetString());
        Assert.Equal(750m, payload.RootElement.GetProperty("parcel").GetProperty("weight").GetDecimal());
        Assert.Equal("flash", payload.RootElement.GetProperty("courierCodes")[0].GetString());
        Assert.Equal("Machined bracket", payload.RootElement.GetProperty("parts")[0].GetProperty("name").GetString());
        Assert.Equal(10, payload.RootElement.GetProperty("parts")[0].GetProperty("quantity").GetInt32());
        Assert.Equal(20m, payload.RootElement.GetProperty("parts")[0].GetProperty("length").GetDecimal());
        var rate = Assert.Single(result.Rates);
        Assert.Equal("flash", rate.CourierCode);
        Assert.Equal("Flash Express", rate.ProductName);
        Assert.Equal(72.5m, rate.TotalPrice);
        Assert.Equal("THB", rate.CurrencyCode);
        Assert.Equal("https://cdn.example/flash.svg", rate.CourierLogoUrl);
        Assert.Equal(2, rate.PackageCount);
        Assert.Equal(1640m, rate.TotalWeight);
        Assert.Equal(2, rate.Packages.Count);
        Assert.Equal("Machined bracket", rate.Packages[0].Items[0].Name);
        Assert.Equal("GoShip", rate.Provider);
    }

    [Fact]
    public async Task GetShippingTrackingAsync_TargetsDeliveryServiceTrackingRoute()
    {
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(request =>
        {
            capturedRequest = request;
            return JsonContent.Create(new
            {
                trackingCode = "TH-E2E-001",
                courierCode = "flash",
                courierName = "Flash Express",
                status = "in_transit",
                description = "Parcel is in transit",
                provider = "Shippop"
            });
        });

        var result = await client.GetShippingTrackingAsync("TH-E2E-001");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal("/delivery/v1/shipping/tracking/TH-E2E-001", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(result);
        Assert.Equal("TH-E2E-001", result.TrackingCode);
        Assert.Equal("flash", result.CourierCode);
        Assert.Equal("in_transit", result.Status);
        Assert.Equal("Shippop", result.Provider);
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
