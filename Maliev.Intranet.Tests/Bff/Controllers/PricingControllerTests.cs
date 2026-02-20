using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class PricingControllerTests
{
    private readonly Mock<IPricingServiceClient> _clientMock;
    private readonly PricingController _controller;

    public PricingControllerTests()
    {
        _clientMock = new Mock<IPricingServiceClient>();
        _controller = new PricingController(_clientMock.Object);
    }

    [Fact]
    public async Task CalculatePrice_ShouldReturnOk_WhenSuccessful()
    {
        var response = new PricingResultDto
        {
            Strategy = PricingStrategy.RuleBased,
            MaterialCost = 10,
            SupportMaterialCost = 2,
            MachineTimeCost = 5,
            SetupCost = 20,
            ComplexitySurcharge = 0,
            SubtotalBeforeMargin = 37,
            MarginAmount = 11.1m,
            TotalUnitPrice = 48.1m,
            TotalPrice = 48.1m,
            ConfidenceLevel = 1.0m,
            ValidUntil = DateTime.UtcNow.AddDays(30),
            CalculationDuration = TimeSpan.FromSeconds(1)
        };

        _clientMock.Setup(c => c.CalculatePriceAsync(It.IsAny<PricingRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.CalculatePrice(new PricingRequestDto
        {
            FileId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            MaterialId = Guid.NewGuid(),
            MaterialCode = "PLA",
            ManufacturingProcessId = Guid.NewGuid(),
            ManufacturingProcessName = "FDM",
            Geometry = new GeometryMetricsDto
            {
                VolumeCm3 = 10,
                SupportVolumeCm3 = 1,
                SurfaceAreaCm2 = 50,
                BoundingBoxX = 50,
                BoundingBoxY = 50,
                BoundingBoxZ = 50,
                IsManifold = true,
                TriangleCount = 1000
            }
        }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actual = Assert.IsType<PricingResultDto>(okResult.Value);
        Assert.Equal(response.TotalPrice, actual.TotalPrice);
    }

    [Fact]
    public async Task CreateSnapshot_ShouldReturnOk()
    {
        var response = new PricingSnapshotDto { Id = Guid.NewGuid() };
        _clientMock.Setup(c => c.CreateSnapshotAsync(It.IsAny<CreatePricingSnapshotRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.CreateSnapshot(new CreatePricingSnapshotRequest(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actual = Assert.IsType<PricingSnapshotDto>(okResult.Value);
        Assert.Equal(response.Id, actual.Id);
    }
}
