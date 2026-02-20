using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class PerformanceControllerTests
{
    [Fact]
    public async Task GetReviews_ReturnsOk()
    {
        var mock = new Mock<IPerformanceServiceClient>();
        mock.Setup(x => x.GetReviewsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PerformanceReviewDto>());
        var controller = new PerformanceController(mock.Object);
        var result = await controller.GetReviews(CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetGoals_ReturnsOk()
    {
        var mock = new Mock<IPerformanceServiceClient>();
        mock.Setup(x => x.GetGoalsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GoalDto>());
        var controller = new PerformanceController(mock.Object);
        var result = await controller.GetGoals(CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }
}
