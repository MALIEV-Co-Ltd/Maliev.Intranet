using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class PricingControllerTests
{
    [Fact]
    public async Task CalculatePrice_WhenFinishHasTenPercent_AddsTenPercentSurcharge()
    {
        var finishId = Guid.NewGuid();
        var pricingClient = new Mock<IPricingServiceClient>();
        pricingClient
            .Setup(client => client.CalculatePriceAsync(It.IsAny<PricingRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePricingResult(100m, 100m));
        var controller = new PricingController(pricingClient.Object, CreateMaterialClient(
        [
            new CatalogSurfaceFinishDto(finishId, "Sanded", "SANDED", null, 10m, null, 20),
        ]));

        var result = await controller.CalculatePrice(CreatePricingRequest(finishId), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var price = Assert.IsType<PricingResultDto>(ok.Value);
        Assert.Equal(110m, price.TotalUnitPrice);
        Assert.Equal(110m, price.TotalPrice);
    }

    [Fact]
    public async Task GetFinishPrices_WhenFinishHasTenPercent_ReturnsTenPercentOfBaseUnitPrice()
    {
        var finishId = Guid.NewGuid();
        var controller = new PricingController(new Mock<IPricingServiceClient>().Object, CreateMaterialClient(
        [
            new CatalogSurfaceFinishDto(finishId, "Sanded", "SANDED", null, 10m, null, 20),
        ]));

        var result = await controller.GetFinishPrices(new FinishPriceRequestDto
        {
            ProcessCode = "FDM",
            MaterialId = Guid.NewGuid(),
            ToleranceId = Guid.NewGuid(),
            BaseUnitPrice = 100m,
            FinishIds = [finishId],
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var prices = Assert.IsType<List<FinishPriceItemDto>>(ok.Value);
        var price = Assert.Single(prices);
        Assert.Equal(finishId, price.FinishId);
        Assert.Equal(10m, price.AdditionalUnitCost);
        Assert.Equal("THB", price.Currency);
    }

    private static MaterialServiceClient CreateMaterialClient(List<CatalogSurfaceFinishDto> finishes)
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(finishes),
            }));

        return new MaterialServiceClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://material.test"),
        });
    }

    private static PricingRequestDto CreatePricingRequest(Guid finishId) => new()
    {
        FileId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        MaterialId = Guid.NewGuid(),
        MaterialCode = "PETG_CF",
        ManufacturingProcessId = Guid.NewGuid(),
        ManufacturingProcessName = "FDM",
        ManufacturingProcessCode = "FDM",
        Quantity = 1,
        FinishId = finishId,
        Geometry = new GeometryMetricsDto
        {
            VolumeCm3 = 1m,
            SupportVolumeCm3 = 0m,
            SurfaceAreaCm2 = 1m,
            BoundingBoxX = 10m,
            BoundingBoxY = 10m,
            BoundingBoxZ = 10m,
            IsManifold = true,
            TriangleCount = 100,
        },
    };

    private static PricingResultDto CreatePricingResult(decimal unitPrice, decimal totalPrice) => new()
    {
        Strategy = PricingStrategy.RuleBased,
        MaterialCost = 0m,
        SupportMaterialCost = 0m,
        MachineTimeCost = 0m,
        SetupCost = 0m,
        ComplexitySurcharge = 0m,
        SubtotalBeforeMargin = unitPrice,
        MarginAmount = 0m,
        TotalUnitPrice = unitPrice,
        TotalPrice = totalPrice,
        ConfidenceLevel = 1m,
        ValidUntil = DateTime.UtcNow.AddHours(24),
        CalculationDuration = TimeSpan.Zero,
        EstimatedLeadTimeDays = 7,
    };
}
