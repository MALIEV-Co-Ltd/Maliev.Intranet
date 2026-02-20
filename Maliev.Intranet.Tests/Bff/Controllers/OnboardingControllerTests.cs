using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class OnboardingControllerTests
{
    [Fact]
    public async Task Get_ReturnsOk()
    {
        var mock = new Mock<ILifecycleServiceClient>();
        mock.Setup(x => x.GetOnboardingsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<OnboardingSummaryDto>());
        var controller = new OnboardingController(mock.Object);
        var result = await controller.Get();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetChecklist_WhenFound_ReturnsOk()
    {
        var mock = new Mock<ILifecycleServiceClient>();
        mock.Setup(x => x.GetChecklistAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OnboardingChecklistDto());
        var controller = new OnboardingController(mock.Object);
        var result = await controller.GetChecklist(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetChecklist_WhenNotFound_ReturnsNotFound()
    {
        var mock = new Mock<ILifecycleServiceClient>();
        mock.Setup(x => x.GetChecklistAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingChecklistDto?)null);
        var controller = new OnboardingController(mock.Object);
        var result = await controller.GetChecklist(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateTask_WhenSucceeds_ReturnsNoContent()
    {
        var mock = new Mock<ILifecycleServiceClient>();
        mock.Setup(x => x.UpdateTaskAsync(It.IsAny<Guid>(), It.IsAny<UpdateOnboardingProgressRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = new OnboardingController(mock.Object);
        var result = await controller.UpdateTask(Guid.NewGuid(), new UpdateOnboardingProgressRequest(), CancellationToken.None);
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateTask_WhenFails_ReturnsBadRequest()
    {
        var mock = new Mock<ILifecycleServiceClient>();
        mock.Setup(x => x.UpdateTaskAsync(It.IsAny<Guid>(), It.IsAny<UpdateOnboardingProgressRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = new OnboardingController(mock.Object);
        var result = await controller.UpdateTask(Guid.NewGuid(), new UpdateOnboardingProgressRequest(), CancellationToken.None);
        Assert.IsType<BadRequestResult>(result);
    }
}
