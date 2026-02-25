using System.Security.Claims;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class TimeOffControllerTests
{
    private static ControllerContext UserContext(string sub = "00000000-0000-0000-0000-000000000001", string? employeeId = "00000000-0000-0000-0000-000000000002")
    {
        var claims = new List<Claim> { new Claim("sub", sub) };
        if (employeeId != null)
        {
            claims.Add(new Claim("employee_id", employeeId));
        }
        return new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) } };
    }

    private static EmployeeServiceClient CreateMockEmployeeClient()
    {
        var mockHttp = new Mock<HttpMessageHandler>();
        var client = new HttpClient(mockHttp.Object) { BaseAddress = new Uri("http://localhost") };
        return new EmployeeServiceClient(client);
    }

    [Fact]
    public async Task GetBalances_ReturnsOk()
    {
        var mock = new Mock<ILeaveServiceClient>();
        mock.Setup(x => x.GetMyBalancesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LeaveBalanceDto>());
        var controller = new TimeOffController(mock.Object, CreateMockEmployeeClient()) { ControllerContext = UserContext() };
        var result = await controller.GetBalances(CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetRequests_ReturnsOk()
    {
        var mock = new Mock<ILeaveServiceClient>();
        mock.Setup(x => x.GetMyRequestsAsync(It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LeaveRequestSummaryDto>());
        var controller = new TimeOffController(mock.Object, CreateMockEmployeeClient()) { ControllerContext = UserContext() };
        var result = await controller.GetRequests(CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task SubmitRequest_WhenClientReturnsResult_ReturnsOk()
    {
        var mock = new Mock<ILeaveServiceClient>();
        mock.Setup(x => x.SubmitRequestAsync(It.IsAny<Guid>(), It.IsAny<SubmitLeaveRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LeaveRequestDetailDto());
        var controller = new TimeOffController(mock.Object, CreateMockEmployeeClient()) { ControllerContext = UserContext() };
        var result = await controller.SubmitRequest(new SubmitLeaveRequestDto(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task SubmitRequest_WhenClientReturnsNull_ReturnsBadRequest()
    {
        var mock = new Mock<ILeaveServiceClient>();
        mock.Setup(x => x.SubmitRequestAsync(It.IsAny<Guid>(), It.IsAny<SubmitLeaveRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LeaveRequestDetailDto?)null);
        var controller = new TimeOffController(mock.Object, CreateMockEmployeeClient()) { ControllerContext = UserContext() };
        var result = await controller.SubmitRequest(new SubmitLeaveRequestDto(), CancellationToken.None);
        Assert.IsType<BadRequestResult>(result.Result);
    }
}
