using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Client.Services;

public sealed class ShippingServiceTests
{
    [Fact]
    public async Task GetRatesAsync_PostsDeliveryServiceShippingPayload()
    {
        JsonDocument? payload = null;
        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler(async (request, ct) =>
        {
            capturedRequest = request;
            payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new MalievResponse<ShippingRateResponseDto>
                {
                    Success = true,
                    Data = new ShippingRateResponseDto()
                })
            };
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new ShippingService(http);

        await service.GetRatesAsync(new ShippingRateRequestDto
        {
            From = new ShippingAddressDto
            {
                Name = "MALIEV",
                Address = "Factory",
                District = "Pathum Wan",
                State = "Pathum Wan",
                Province = "Bangkok",
                Postcode = "10400",
                Tel = "020000000"
            },
            To = new ShippingAddressDto
            {
                Name = "Customer",
                Address = "Dock 2",
                District = "Bang Rak",
                State = "Bang Rak",
                Province = "Bangkok",
                Postcode = "10500",
                CountryCode = "SG",
                Tel = "0800000000"
            },
            Parcel = new ShippingParcelDto
            {
                Name = "MALIEV shipment",
                Weight = 1250m,
                Length = 10.5m,
                Width = 20.25m,
                Height = 30.75m
            },
            CourierCodes = ["EMST"]
        });

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("/api/v1/shipping/rates", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(payload);
        var root = payload.RootElement;
        Assert.Equal("10400", root.GetProperty("from").GetProperty("postcode").GetString());
        Assert.Equal("10500", root.GetProperty("to").GetProperty("postcode").GetString());
        Assert.Equal("SG", root.GetProperty("to").GetProperty("countryCode").GetString());
        Assert.Equal(1250m, root.GetProperty("parcel").GetProperty("weight").GetDecimal());
        Assert.Equal("EMST", root.GetProperty("courierCodes")[0].GetString());
    }
}
