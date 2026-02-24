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
    private readonly Mock<ILeaveServiceClient> _leaveClientMock = new();
    private readonly Mock<EmployeeServiceClient> _employeeClientMock;
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly Guid _principalId = Guid.NewGuid();

    public TimeOffControllerTests()
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler());
        var clientLogger = new Mock<ILogger<EmployeeServiceClient>>().Object;
        _employeeClientMock = new Mock<EmployeeServiceClient>(httpClient, clientLogger);
        
        _employeeClientMock.Setup(x => x.GetByPrincipalIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmployeeDetailDto { Id = _employeeId });
    }

    private ControllerContext UserContext() =>
        new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", _principalId.ToString())], "Test")) } };

    [Fact]
    public async Task GetBalances_ReturnsOk()
    {
        _leaveClientMock.Setup(x => x.GetMyBalancesAsync(_employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LeaveBalanceDto>());
            
        var controller = new TimeOffController(_leaveClientMock.Object, _employeeClientMock.Object) { ControllerContext = UserContext() };
        var result = await controller.GetBalances(CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetRequests_ReturnsOk()
    {
        _leaveClientMock.Setup(x => x.GetMyRequestsAsync(_employeeId, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LeaveRequestSummaryDto>());
            
        var controller = new TimeOffController(_leaveClientMock.Object, _employeeClientMock.Object) { ControllerContext = UserContext() };
        var result = await controller.GetRequests(CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task SubmitRequest_WhenClientReturnsResult_ReturnsOk()
    {
        _leaveClientMock.Setup(x => x.SubmitRequestAsync(_employeeId, It.IsAny<SubmitLeaveRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LeaveRequestDetailDto());
            
        var controller = new TimeOffController(_leaveClientMock.Object, _employeeClientMock.Object) { ControllerContext = UserContext() };
        var result = await controller.SubmitRequest(new SubmitLeaveRequestDto(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task SubmitRequest_WhenClientReturnsNull_ReturnsBadRequest()
    {
        _leaveClientMock.Setup(x => x.SubmitRequestAsync(_employeeId, It.IsAny<SubmitLeaveRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LeaveRequestDetailDto?)null);
            
        var controller = new TimeOffController(_leaveClientMock.Object, _employeeClientMock.Object) { ControllerContext = UserContext() };
        var result = await controller.SubmitRequest(new SubmitLeaveRequestDto(), CancellationToken.None);
        Assert.IsType<BadRequestResult>(result.Result);
    }
}
