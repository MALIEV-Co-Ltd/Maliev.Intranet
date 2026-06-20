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

        var jobClient = new JobServiceClient(httpClient);

        return new DashboardController(orderClient, quotationClient, paymentClient, employeeClient, invoiceClient, leaveClient, projectClient, jobClient);
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

    [Fact]
    public async Task Get_WhenOneStatSourceReturnsMalformedJson_ShouldStillReturnOtherWidgets()
    {
        var requestedPaths = new List<string>();
        var handler = new MockHttpMessageHandler((req, ct) =>
        {
            requestedPaths.Add(req.RequestUri?.PathAndQuery ?? string.Empty);
            var path = req.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.Equals("/payment/v1/metrics/stats", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{not-json")
                });
            }

            if (path.Equals("/order/v1/metrics/active-count", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { count = 3 })
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { count = 0, TodayTotal = 0m })
            });
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://test") };
        var controller = new DashboardController(
            new OrderServiceClient(httpClient),
            new QuotationServiceClient(httpClient),
            new PaymentServiceClient(httpClient),
            new EmployeeServiceClient(httpClient),
            new InvoiceServiceClient(httpClient),
            new LeaveServiceClient(httpClient),
            new ProjectServiceClient(httpClient),
            new JobServiceClient(httpClient));

        var result = await controller.Get("Revenue,ActiveOrders", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var model = Assert.IsType<DashboardViewModel>(okResult.Value);
        Assert.Equal(2, model.Widgets.Count);
        Assert.Equal("THB 0", model.Widgets.Single(widget => widget.Title == "Total Revenue (Today)").Data.GetString());
        Assert.Equal("3", model.Widgets.Single(widget => widget.Title == "Active Orders").Data.GetString());
        Assert.Contains("/payment/v1/metrics/stats", requestedPaths);
        Assert.Contains("/order/v1/metrics/active-count", requestedPaths);
    }

    [Fact]
    public async Task GetActionItems_WithOverdueProductionJobs_ShouldReturnProductionActionItem()
    {
        var requestedPaths = new List<string>();
        var overdueStartedAt = DateTime.UtcNow.AddHours(-4);
        var handler = new MockHttpMessageHandler((req, ct) =>
        {
            requestedPaths.Add(req.RequestUri?.PathAndQuery ?? string.Empty);
            var path = req.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.Equals("/job/v1/jobs/kanban", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new
                    {
                        pending = Array.Empty<object>(),
                        queued = Array.Empty<object>(),
                        inProgress = new[]
                        {
                            new
                            {
                                jobId = Guid.NewGuid(),
                                orderId = Guid.NewGuid(),
                                technology = "CNC",
                                materialId = Guid.NewGuid(),
                                assignedMachineId = Guid.NewGuid().ToString(),
                                priority = 3,
                                estimatedPrintTimeMinutes = 60,
                                startedAt = overdueStartedAt
                            }
                        },
                        finishing = Array.Empty<object>(),
                        completed = Array.Empty<object>(),
                        cancelled = Array.Empty<object>()
                    })
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { count = 0, TodayTotal = 0m })
            });
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://test") };
        var controller = new DashboardController(
            new OrderServiceClient(httpClient),
            new QuotationServiceClient(httpClient),
            new PaymentServiceClient(httpClient),
            new EmployeeServiceClient(httpClient),
            new InvoiceServiceClient(httpClient),
            new LeaveServiceClient(httpClient),
            new ProjectServiceClient(httpClient),
            new JobServiceClient(httpClient));

        var result = await controller.GetActionItems(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var model = Assert.IsType<DashboardActionItemsDto>(okResult.Value);
        var productionItem = Assert.Single(model.Categories, item => item.NavigateTo == "/mfg/production-schedule");
        Assert.Equal("1 production job overdue", productionItem.Label);
        Assert.Equal("Error", productionItem.Severity);
        Assert.Contains("/job/v1/jobs/kanban", requestedPaths);
    }

    [Fact]
    public async Task GetActionItems_WithCustomerReviewProjects_ShouldReturnProjectReviewQueueActionItem()
    {
        var requestedPaths = new List<string>();
        var handler = new MockHttpMessageHandler((req, ct) =>
        {
            requestedPaths.Add(req.RequestUri?.PathAndQuery ?? string.Empty);
            var path = req.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.Equals("/project/v1/projects/stats", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new
                    {
                        activeCount = 5,
                        configuringCount = 0,
                        customerReviewCount = 2,
                        quotedCount = 0,
                        inProductionCount = 0
                    })
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { count = 0, TodayTotal = 0m })
            });
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://test") };
        var controller = new DashboardController(
            new OrderServiceClient(httpClient),
            new QuotationServiceClient(httpClient),
            new PaymentServiceClient(httpClient),
            new EmployeeServiceClient(httpClient),
            new InvoiceServiceClient(httpClient),
            new LeaveServiceClient(httpClient),
            new ProjectServiceClient(httpClient),
            new JobServiceClient(httpClient));

        var result = await controller.GetActionItems(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var model = Assert.IsType<DashboardActionItemsDto>(okResult.Value);
        var reviewItem = Assert.Single(model.Categories, item => item.NavigateTo == "/sales/projects?status=CustomerReview");
        Assert.Equal("2 projects waiting for employee review", reviewItem.Label);
        Assert.Equal("Warning", reviewItem.Severity);
        Assert.Contains("/project/v1/projects/stats", requestedPaths);
    }

    [Fact]
    public async Task GetActionItems_WhenRequestIsCanceled_ShouldPropagateCancellation()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            ct.IsCancellationRequested
                ? Task.FromCanceled<HttpResponseMessage>(ct)
                : Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { count = 0, TodayTotal = 0m })
                }));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://test") };
        var controller = new DashboardController(
            new OrderServiceClient(httpClient),
            new QuotationServiceClient(httpClient),
            new PaymentServiceClient(httpClient),
            new EmployeeServiceClient(httpClient),
            new InvoiceServiceClient(httpClient),
            new LeaveServiceClient(httpClient),
            new ProjectServiceClient(httpClient),
            new JobServiceClient(httpClient));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => controller.GetActionItems(cts.Token));
    }
}
