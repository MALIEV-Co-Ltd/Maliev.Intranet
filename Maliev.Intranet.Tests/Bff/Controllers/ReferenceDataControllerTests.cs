using System.Reflection;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Services;
using Microsoft.AspNetCore.Hosting;
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
        var environmentMock = new Mock<IWebHostEnvironment>();
        environmentMock.SetupGet(environment => environment.EnvironmentName).Returns("Testing");
        _controller = new ReferenceDataController(_serviceMock.Object, currencyClient, environmentMock.Object, _loggerMock.Object);
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
        var problemDetails = Assert.IsType<ProblemDetails>(statusResult.Value);
        Assert.Equal("Country reference data unavailable", problemDetails.Title);
        Assert.Equal("Test error", problemDetails.Detail);
    }

    [Theory]
    [InlineData(nameof(ReferenceDataController.GetCountryPage), MalievPermissions.Country.CountriesList)]
    [InlineData(nameof(ReferenceDataController.CreateCountry), MalievPermissions.Country.CountriesCreate)]
    [InlineData(nameof(ReferenceDataController.UpdateCountry), MalievPermissions.Country.CountriesUpdate)]
    [InlineData(nameof(ReferenceDataController.DeleteCountry), MalievPermissions.Country.CountriesDelete)]
    [InlineData(nameof(ReferenceDataController.RestoreCountry), MalievPermissions.Country.CountriesRestore)]
    [InlineData(nameof(ReferenceDataController.GetCurrencies), MalievPermissions.Currency.CurrenciesRead)]
    [InlineData(nameof(ReferenceDataController.GetCurrencyPage), MalievPermissions.Currency.CurrenciesRead)]
    [InlineData(nameof(ReferenceDataController.CreateCurrency), MalievPermissions.Currency.CurrenciesCreate)]
    [InlineData(nameof(ReferenceDataController.UpdateCurrency), MalievPermissions.Currency.CurrenciesUpdate)]
    [InlineData(nameof(ReferenceDataController.DeleteCurrency), MalievPermissions.Currency.CurrenciesDelete)]
    [InlineData(nameof(ReferenceDataController.GetPrimaryCurrency), MalievPermissions.Currency.CurrenciesRead)]
    [InlineData(nameof(ReferenceDataController.GetExchangeRate), MalievPermissions.Currency.RatesRead)]
    [InlineData(nameof(ReferenceDataController.GetRegistryLocations), MalievPermissions.Registry.LocationsRead)]
    [InlineData(nameof(ReferenceDataController.CreateRegistryLocation), MalievPermissions.Registry.LocationsCreate)]
    [InlineData(nameof(ReferenceDataController.UpdateRegistryLocation), MalievPermissions.Registry.LocationsUpdate)]
    [InlineData(nameof(ReferenceDataController.DeleteRegistryLocation), MalievPermissions.Registry.LocationsDelete)]
    public void ReferenceDataManagementEndpoints_RequireExpectedPermissions(string actionName, string expectedPermission)
    {
        var method = typeof(ReferenceDataController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == actionName);

        var attribute = Assert.Single(method.GetCustomAttributes<RequirePermissionAttribute>());

        Assert.Contains(expectedPermission, attribute.Policy, StringComparison.Ordinal);
        Assert.Equal("Bearer,Cookies", attribute.AuthenticationSchemes);
    }

    [Fact]
    public async Task GetRegistryLocations_ShouldReturnPagedLocations_WhenSuccessful()
    {
        var page = new ReferenceDataPage<RegistryThaiLocation>
        {
            Items =
            [
                new()
                {
                    Id = Guid.NewGuid(),
                    PostalCode = "11120",
                    SubDistrictTh = "คลองข่อย",
                    DistrictTh = "ปากเกร็ด",
                    ProvinceTh = "นนทบุรี",
                    SubDistrictEn = "Khlong Khoi",
                    DistrictEn = "Pak Kret",
                    ProvinceEn = "Nonthaburi"
                }
            ],
            PageNumber = 1,
            PageSize = 25,
            TotalCount = 1,
            TotalPages = 1
        };

        _serviceMock.Setup(service => service.GetRegistryLocationPageAsync("11120", 1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await _controller.GetRegistryLocations("11120", 1, 25, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(page, okResult.Value);
    }

    [Fact]
    public async Task CreateCurrency_ShouldReturnCreatedCurrency_WhenSuccessful()
    {
        var request = new CurrencyDto
        {
            Code = "USD",
            Name = "US dollar",
            Symbol = "$",
            DecimalPlaces = 2,
            IsActive = true
        };
        var created = new CurrencyDto
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            Symbol = request.Symbol,
            DecimalPlaces = request.DecimalPlaces,
            IsActive = true
        };

        _serviceMock.Setup(service => service.CreateCurrencyAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var result = await _controller.CreateCurrency(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(ReferenceDataController.GetCurrencyPage), createdResult.ActionName);
        Assert.Same(created, createdResult.Value);
    }
}
