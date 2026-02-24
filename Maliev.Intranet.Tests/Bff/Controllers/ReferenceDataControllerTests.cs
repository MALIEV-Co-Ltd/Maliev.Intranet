using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

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
        _controller = new ReferenceDataController(_serviceMock.Object, _loggerMock.Object);
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
}
