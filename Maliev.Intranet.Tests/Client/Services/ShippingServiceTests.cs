using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Client.Services;

public sealed class ShippingServiceTests
{
    [Fact]
    public async Task GetRatesAsync_FormatsNumericQueryValuesWithInvariantCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        Uri? requestedUri = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            requestedUri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new MalievResponse<ShippingRateResponseDto>
                {
                    Success = true,
                    Data = new ShippingRateResponseDto()
                })
            });
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new ShippingService(http);

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

            await service.GetRatesAsync(new ShippingRateRequestDto
            {
                OriginCountryCode = "TH",
                DestinationCountryCode = "US",
                DestinationPostalCode = "90210",
                WeightKg = 1.25m,
                LengthCm = 10.5m,
                WidthCm = 20.25m,
                HeightCm = 30.75m
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }

        Assert.NotNull(requestedUri);
        var query = requestedUri.Query;
        Assert.Contains("weightKg=1.25", query, StringComparison.Ordinal);
        Assert.Contains("lengthCm=10.5", query, StringComparison.Ordinal);
        Assert.Contains("widthCm=20.25", query, StringComparison.Ordinal);
        Assert.Contains("heightCm=30.75", query, StringComparison.Ordinal);
        Assert.DoesNotContain("1,25", query, StringComparison.Ordinal);
    }
}
