using System.Reflection;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class ReferenceDataControllerTests
{
    private readonly Mock<IReferenceDataService> _serviceMock;
    private readonly Mock<ILogger<ReferenceDataController>> _loggerMock;
    private readonly ReferenceDataController _controller;

    public ReferenceDataControllerTests()
    {
        _serviceMock = new Mock<IReferenceDataService>();
        _loggerMock = new Mock<ILogger<ReferenceDataController>>();
        var httpClient = new HttpClient(new MockHttpMessageHandler()) { BaseAddress = new Uri("http://test") };
        var currencyClient = new CurrencyServiceClient(httpClient);
        _controller = new ReferenceDataController(_serviceMock.Object, currencyClient, _loggerMock.Object);
    }

    [Fact]
    public async Task GetCountries_ShouldReturnOk_WhenSuccessful()
    {
        var countries = new List<CountryDto> { new() { Id = Guid.NewGuid(), Name = "Thailand" } };
        _serviceMock.Setup(x => x.GetCountriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(countries);

        var result = await _controller.GetCountries(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(countries, okResult.Value);
    }

    [Fact]
    public async Task GetCountries_ShouldReturn500_WhenExceptionOccurs()
    {
        _serviceMock.Setup(x => x.GetCountriesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        var result = await _controller.GetCountries(CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Theory]
    [InlineData(nameof(ReferenceDataController.GetCurrencies), MalievPermissions.Currency.CurrenciesRead)]
    [InlineData(nameof(ReferenceDataController.GetPrimaryCurrency), MalievPermissions.Currency.CurrenciesRead)]
    [InlineData(nameof(ReferenceDataController.GetExchangeRate), MalievPermissions.Currency.RatesRead)]
    public void CurrencyEndpoints_RequireCurrencyPermissions(string actionName, string expectedPermission)
    {
        var method = typeof(ReferenceDataController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == actionName);

        var attribute = Assert.Single(method.GetCustomAttributes<RequirePermissionAttribute>());

        Assert.Contains(expectedPermission, attribute.Policy, StringComparison.Ordinal);
        Assert.Equal("Bearer,Cookies", attribute.AuthenticationSchemes);
    }
}
