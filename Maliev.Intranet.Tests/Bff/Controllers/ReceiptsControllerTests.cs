using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class ReceiptsControllerTests
{
    [Fact]
    public async Task Get_ReturnsOk()
    {
        var mock = new Mock<IReceiptServiceClient>();
        mock.Setup(x => x.GetReceiptsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<ReceiptDto>());
        var controller = new ReceiptsController(mock.Object);
        var result = await controller.Get();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var mock = new Mock<IReceiptServiceClient>();
        mock.Setup(x => x.GetReceiptByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiptDto());
        var controller = new ReceiptsController(mock.Object);
        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var mock = new Mock<IReceiptServiceClient>();
        mock.Setup(x => x.GetReceiptByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReceiptDto?)null);
        var controller = new ReceiptsController(mock.Object);
        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_WhenSucceeds_ReturnsOk()
    {
        var mock = new Mock<IReceiptServiceClient>();
        mock.Setup(x => x.CreateReceiptAsync(It.IsAny<CreateReceiptRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiptDto());
        var controller = new ReceiptsController(mock.Object);
        var result = await controller.Create(new CreateReceiptRequest(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Void_WhenSucceeds_ReturnsNoContent()
    {
        var mock = new Mock<IReceiptServiceClient>();
        mock.Setup(x => x.VoidReceiptAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = new ReceiptsController(mock.Object);
        var result = await controller.Void(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Void_WhenFails_ReturnsBadRequest()
    {
        var mock = new Mock<IReceiptServiceClient>();
        mock.Setup(x => x.VoidReceiptAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = new ReceiptsController(mock.Object);
        var result = await controller.Void(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<BadRequestResult>(result);
    }
}
