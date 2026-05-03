using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
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

    private static PdfServiceClient CreatePdfClient()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
        {
            var response = new { storageUrl = "http://test.pdf" };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) });
        });
        return new PdfServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    [Fact]
    public async Task Orders_Get_ReturnsOk()
    {
        var controller = new OrdersController(new OrderServiceClient(CreateClient(new PagedResponse<OrderSummaryDto>())));
        var result = await controller.Get(null, 1, null, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Quotations_Get_ReturnsOk()
    {
        var controller = new QuotationsController(new QuotationServiceClient(CreateClient(new PagedResponse<QuotationSummaryDto>())), CreatePdfClient());
        var result = await controller.Get(null, 1, 20);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Quotations_GenerateDraftPdf_StampsQuotedByFromAuthenticatedEmployee()
    {
        JsonDocument? capturedRequest = null;
        var handler = new MockHttpMessageHandler(async (req, ct) =>
        {
            capturedRequest = JsonDocument.Parse(await req.Content!.ReadAsStringAsync(ct));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { storageUrl = "http://test.pdf" })
            };
        });
        var pdfClient = new PdfServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new QuotationsController(new QuotationServiceClient(CreateClient(new PagedResponse<QuotationSummaryDto>())), pdfClient)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.Name, "Alex Kim"),
                        new Claim("email", "alex.kim@maliev.com"),
                    ], "Test"))
                }
            }
        };

        var result = await controller.GenerateDraftPdf(new QuotationPdfData { QuotationNumber = "Q-DRAFT" }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(capturedRequest);
        var data = capturedRequest.RootElement.GetProperty("data");
        Assert.Equal("Alex Kim", data.GetProperty("QuotedByName").GetString());
        Assert.Equal("alex.kim@maliev.com", data.GetProperty("QuotedByEmail").GetString());
        Assert.True(data.TryGetProperty("QuotedAt", out var quotedAt));
        Assert.False(string.IsNullOrWhiteSpace(quotedAt.GetString()));
    }

    [Fact]
    public async Task Dashboard_Get_ReturnsOk()
    {
        var client = CreateClient(new { count = 10 });
        var controller = new DashboardController(
            new OrderServiceClient(client),
            new QuotationServiceClient(client),
            new PaymentServiceClient(client),
            new EmployeeServiceClient(client),
            new InvoiceServiceClient(client),
            new LeaveServiceClient(client),
            new ProjectServiceClient(client));
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
