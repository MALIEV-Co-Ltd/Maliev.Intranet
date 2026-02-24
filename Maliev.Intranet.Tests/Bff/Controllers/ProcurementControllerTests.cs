using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class ProcurementControllerTests
{
    [Fact]
    public async Task Get_ReturnsOk()
    {
        var mock = new Mock<IPurchaseOrderServiceClient>();
        mock.Setup(x => x.GetPurchaseOrdersAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<PurchaseOrderDto>());
        var controller = new ProcurementController(mock.Object);
        var result = await controller.Get();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var mock = new Mock<IPurchaseOrderServiceClient>();
        mock.Setup(x => x.GetPurchaseOrderByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PurchaseOrderDto());
        var controller = new ProcurementController(mock.Object);
        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var mock = new Mock<IPurchaseOrderServiceClient>();
        mock.Setup(x => x.GetPurchaseOrderByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrderDto?)null);
        var controller = new ProcurementController(mock.Object);
        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_WhenSucceeds_ReturnsCreated()
    {
        var id = Guid.NewGuid();
        var mock = new Mock<IPurchaseOrderServiceClient>();
        mock.Setup(x => x.CreatePurchaseOrderAsync(It.IsAny<CreatePurchaseOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PurchaseOrderDto { Id = id });
        var controller = new ProcurementController(mock.Object);
        var result = await controller.Create(new CreatePurchaseOrderRequest(), CancellationToken.None);
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }
}
