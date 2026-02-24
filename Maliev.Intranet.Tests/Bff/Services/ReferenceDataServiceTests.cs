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
    private readonly RegistryServiceClient _registryClient;
    private readonly ReferenceDataService _service;

    public ReferenceDataServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<ReferenceDataService>>();

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://test")
        };

        _httpClientFactoryMock.Setup(x => x.CreateClient("CountryServiceAccount"))
            .Returns(httpClient);

        _registryClient = new RegistryServiceClient(httpClient);
        _service = new ReferenceDataService(_httpClientFactoryMock.Object, _registryClient, _loggerMock.Object);
    }

    [Fact]
    public async Task GetCountriesAsync_ShouldReturnCountries_WhenSuccessful()
    {
        var countryId = Guid.NewGuid();
        var countries = new List<CountryDto> { new() { Id = countryId, Name = "Thailand" } };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { data = countries })
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
    }

    [Fact]
    public async Task GetCountriesAsync_ShouldReturnEmpty_WhenError()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await _service.GetCountriesAsync();

        Assert.Empty(result);
    }
}
