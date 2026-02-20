using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class QuickControllerTests
{
    private static HttpClient CreateClient(object response)
    {
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) }));
        return new HttpClient(handler) { BaseAddress = new Uri("http://test") };
    }

    [Fact]
    public async Task Orders_Get_ReturnsOk()
    {
        var controller = new OrdersController(new OrderServiceClient(CreateClient(new PagedResponse<OrderSummaryDto>())));
        var result = await controller.Get();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Quotations_Get_ReturnsOk()
    {
        var controller = new QuotationsController(new QuotationServiceClient(CreateClient(new PagedResponse<QuotationSummaryDto>())));
        var result = await controller.Get();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Dashboard_Get_ReturnsOk()
    {
        var client = CreateClient(new { count = 10 });
        var controller = new DashboardController(new OrderServiceClient(client), new QuotationServiceClient(client), new PaymentServiceClient(client), new EmployeeServiceClient(client));
        var result = await controller.Get();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Diagnostics_GetMe_ReturnsOk()
    {
        var controller = new DiagnosticsController(new IAMServiceClient(CreateClient(new UserContextDto())));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var result = await controller.GetMe();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Iam_GetPermissions_ReturnsOk()
    {
        var authMock = new Mock<IAuthorizationService>();
        authMock.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>())).ReturnsAsync(AuthorizationResult.Success());
        var controller = new IamController(new IAMServiceClient(CreateClient(new List<PermissionDto>())), authMock.Object, new Mock<IWebHostEnvironment>().Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "u")], "Test")) } };
        var result = await controller.GetPermissions();
        Assert.IsType<OkObjectResult>(result.Result);
    }
}
