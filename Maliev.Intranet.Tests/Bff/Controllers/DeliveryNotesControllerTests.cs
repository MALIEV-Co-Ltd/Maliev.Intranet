using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

/// <summary>Tests for the delivery notes controller.</summary>
public class DeliveryNotesControllerTests
{
    /// <summary>Verifies that Get returns OK.</summary>
    [Fact]
    public async Task Get_ReturnsOk()
    {
        var mock = new Mock<IDeliveryServiceClient>();
        mock.Setup(x => x.GetDeliveryNotesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<DeliveryNoteSummaryDto>());
        var controller = new DeliveryNotesController(mock.Object);
        var result = await controller.Get();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    /// <summary>Verifies that GetById returns OK when the delivery note is found.</summary>
    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var mock = new Mock<IDeliveryServiceClient>();
        mock.Setup(x => x.GetDeliveryNoteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryNoteDetailDto());
        var controller = new DeliveryNotesController(mock.Object);
        var result = await controller.GetById(Guid.NewGuid());
        Assert.IsType<OkObjectResult>(result.Result);
    }

    /// <summary>Verifies that GetById returns NotFound when the delivery note does not exist.</summary>
    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var mock = new Mock<IDeliveryServiceClient>();
        mock.Setup(x => x.GetDeliveryNoteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeliveryNoteDetailDto?)null);
        var controller = new DeliveryNotesController(mock.Object);
        var result = await controller.GetById(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result.Result);
    }

    /// <summary>Verifies that Create returns a Created result when the delivery note is created successfully.</summary>
    [Fact]
    public async Task Create_WhenSucceeds_ReturnsCreated()
    {
        var id = Guid.NewGuid();
        var mock = new Mock<IDeliveryServiceClient>();
        mock.Setup(x => x.CreateDeliveryNoteAsync(It.IsAny<CreateDeliveryNoteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryNoteDetailDto { Id = id });
        var controller = new DeliveryNotesController(mock.Object);
        var result = await controller.Create(new CreateDeliveryNoteRequest(), CancellationToken.None);
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    /// <summary>Verifies that UpdateStatus returns OK when the status update succeeds.</summary>
    [Fact]
    public async Task UpdateStatus_WhenSucceeds_ReturnsOk()
    {
        var mock = new Mock<IDeliveryServiceClient>();
        mock.Setup(x => x.UpdateDeliveryStatusAsync(It.IsAny<Guid>(), It.IsAny<UpdateDeliveryStatusRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryNoteDetailDto());
        var controller = new DeliveryNotesController(mock.Object);
        var result = await controller.UpdateStatus(Guid.NewGuid(), new UpdateDeliveryStatusRequest(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    /// <summary>Verifies that GeneratePdf returns OK when the PDF is generated successfully.</summary>
    [Fact]
    public async Task GeneratePdf_WhenSucceeds_ReturnsOk()
    {
        var mock = new Mock<IDeliveryServiceClient>();
        mock.Setup(x => x.GeneratePdfAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://pdf-url");
        var controller = new DeliveryNotesController(mock.Object);
        var result = await controller.GeneratePdf(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    /// <summary>Verifies that Delete returns NoContent when the delivery note is deleted successfully.</summary>
    [Fact]
    public async Task Delete_WhenSucceeds_ReturnsNoContent()
    {
        var mock = new Mock<IDeliveryServiceClient>();
        mock.Setup(x => x.DeleteDeliveryNoteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = new DeliveryNotesController(mock.Object);
        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NoContentResult>(result);
    }
}
