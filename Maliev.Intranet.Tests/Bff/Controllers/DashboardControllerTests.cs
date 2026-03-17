using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class DashboardControllerTests
{
    private static DashboardController CreateController(HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = JsonContent.Create(new { count = 0, TodayTotal = 0m })
            }));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://test") };

        var orderClient = new OrderServiceClient(httpClient);
        var quotationClient = new QuotationServiceClient(httpClient);
        var paymentClient = new PaymentServiceClient(httpClient);
        var employeeClient = new EmployeeServiceClient(httpClient);
        var invoiceClient = new InvoiceServiceClient(httpClient);
        var leaveClient = new LeaveServiceClient(httpClient);
        var projectClient = new ProjectServiceClient(httpClient);

        return new DashboardController(orderClient, quotationClient, paymentClient, employeeClient, invoiceClient, leaveClient, projectClient);
    }

    [Fact]
    public async Task Get_WithNoWidgets_ShouldReturnOkWithDefaultWidgets()
    {
        var controller = CreateController();

        var result = await controller.Get(null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var model = Assert.IsType<DashboardViewModel>(okResult.Value);
        Assert.NotNull(model);
        Assert.Equal(4, model.Widgets.Count);
    }

    [Fact]
    public async Task Get_WithRevenueWidget_ShouldReturnOkWithRevenueWidget()
    {
        var controller = CreateController();

        var result = await controller.Get("Revenue", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var model = Assert.IsType<DashboardViewModel>(okResult.Value);
        Assert.NotNull(model);
        Assert.Single(model.Widgets);
        Assert.Equal("Total Revenue (Today)", model.Widgets[0].Title);
    }

    [Fact]
    public async Task Get_WithActiveOrdersWidget_ShouldReturnOkWithActiveOrdersWidget()
    {
        var controller = CreateController();

        var result = await controller.Get("ActiveOrders", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var model = Assert.IsType<DashboardViewModel>(okResult.Value);
        Assert.NotNull(model);
        Assert.Single(model.Widgets);
        Assert.Equal("Active Orders", model.Widgets[0].Title);
    }

    [Fact]
    public async Task Get_WithMultipleWidgets_ShouldReturnAllRequestedWidgets()
    {
        var controller = CreateController();

        var result = await controller.Get("Revenue,ActiveOrders", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var model = Assert.IsType<DashboardViewModel>(okResult.Value);
        Assert.NotNull(model);
        Assert.Equal(2, model.Widgets.Count);
    }

    [Fact]
    public async Task Get_WhenDownstreamFails_ShouldStillReturnOk()
    {
        var controller = CreateController(HttpStatusCode.ServiceUnavailable);

        var result = await controller.Get("ActiveOrders,PendingQuotes", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var model = Assert.IsType<DashboardViewModel>(okResult.Value);
        Assert.NotNull(model);
    }
}
