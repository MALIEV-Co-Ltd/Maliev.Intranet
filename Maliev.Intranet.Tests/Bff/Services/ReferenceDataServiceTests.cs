using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Maliev.Intranet.Tests.Bff.Services;

public class ReferenceDataServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<ReferenceDataService>> _loggerMock;
    private readonly Mock<ILogger<CountryServiceClient>> _countryLoggerMock;
    private readonly CountryServiceClient _countryClient;
    private readonly RegistryServiceClient _registryClient;
    private readonly CurrencyServiceClient _currencyClient;
    private readonly ReferenceDataService _service;

    public ReferenceDataServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<ReferenceDataService>>();
        _countryLoggerMock = new Mock<ILogger<CountryServiceClient>>();

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://test")
        };

        _httpClientFactoryMock.Setup(x => x.CreateClient("CountryServiceAccount"))
            .Returns(httpClient);

        _countryClient = new CountryServiceClient(httpClient, _countryLoggerMock.Object);
        _registryClient = new RegistryServiceClient(httpClient);
        _currencyClient = new CurrencyServiceClient(httpClient);
        _service = new ReferenceDataService(_httpClientFactoryMock.Object, _countryClient, _registryClient, _currencyClient, _loggerMock.Object);
    }

    [Fact]
    public async Task GetCountriesAsync_ShouldReturnCountries_WhenSuccessful()
    {
        var countryId = Guid.NewGuid();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { data = new[] { new { id = countryId, iso2 = "TH", name = "Thailand" } } })
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var result = await _service.GetCountriesAsync();

        Assert.Single(result);
        Assert.Equal(countryId, result[0].Id);
        Assert.Equal("TH", result[0].Code);
        Assert.Equal("Thailand", result[0].Name);
    }

    [Fact]
    public async Task GetCountriesAsync_WhenCountryServiceFails_Throws()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                ReasonPhrase = "Internal Server Error",
                Content = new StringContent("seed failure")
            });

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _service.GetCountriesAsync());

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Contains("seed failure", exception.Message);
    }

    [Fact]
    public async Task GetCountriesAsync_WhenCountryServiceReturnsNoCountries_Throws()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { data = Array.Empty<object>() })
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetCountriesAsync());

        Assert.Contains("returned no countries", exception.Message);
    }

    [Fact]
    public async Task GetRegistryLocationPageAsync_ShouldReturnPagedRegistryLocations()
    {
        var locationId = Guid.NewGuid();
        HttpRequestMessage? capturedRequest = null;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                success = true,
                data = new
                {
                    items = new[]
                    {
                        new
                        {
                            id = locationId,
                            postalCode = "11120",
                            subDistrictTh = "คลองข่อย",
                            districtTh = "ปากเกร็ด",
                            provinceTh = "นนทบุรี",
                            subDistrictEn = "Khlong Khoi",
                            districtEn = "Pak Kret",
                            provinceEn = "Nonthaburi"
                        }
                    },
                    pageNumber = 2,
                    pageSize = 10,
                    totalCount = 21,
                    totalPages = 3
                }
            })
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(response);

        var result = await _service.GetRegistryLocationPageAsync("คลอง", 2, 10);

        Assert.NotNull(capturedRequest);
        Assert.Contains("/registry/v1/thai/addresses", capturedRequest!.RequestUri!.PathAndQuery, StringComparison.Ordinal);
        Assert.Contains("pageNumber=2", capturedRequest.RequestUri.PathAndQuery, StringComparison.Ordinal);
        Assert.Contains("pageSize=10", capturedRequest.RequestUri.PathAndQuery, StringComparison.Ordinal);
        Assert.Contains("query=", capturedRequest.RequestUri.PathAndQuery, StringComparison.Ordinal);
        Assert.Single(result.Items);
        Assert.Equal(locationId, result.Items[0].Id);
        Assert.Equal(21, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }
}
