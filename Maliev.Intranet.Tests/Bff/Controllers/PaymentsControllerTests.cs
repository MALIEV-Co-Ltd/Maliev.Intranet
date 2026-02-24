using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class PaymentsControllerTests
{
    private static PaymentServiceClient CreateClient<T>(T response, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = JsonContent.Create(response) }));
        return new PaymentServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    private static PaymentServiceClient CreateRawClient(HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(status)));
        return new PaymentServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    [Fact]
    public async Task GetStats_ReturnsOk()
    {
        var controller = new PaymentsController(CreateClient(new PaymentStatsDto()));
        var result = await controller.GetStats();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Get_ReturnsOk()
    {
        var controller = new PaymentsController(CreateClient(new PagedResponse<PaymentSummaryDto>()));
        var result = await controller.Get();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var controller = new PaymentsController(CreateClient(new PaymentDetailDto()));
        var result = await controller.GetById(Guid.NewGuid());
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var controller = new PaymentsController(CreateRawClient(HttpStatusCode.NotFound));
        var result = await controller.GetById(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_WhenSucceeds_Returns201()
    {
        var controller = new PaymentsController(CreateRawClient(HttpStatusCode.Created));
        var result = await controller.Create(new CreatePaymentRequest(), CancellationToken.None);
        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(201, statusResult.StatusCode);
    }

    [Fact]
    public async Task Allocate_WhenSucceeds_ReturnsNoContent()
    {
        var controller = new PaymentsController(CreateRawClient(HttpStatusCode.OK));
        var result = await controller.Allocate(Guid.NewGuid(), new AllocatePaymentRequest(), CancellationToken.None);
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Void_WhenSucceeds_ReturnsNoContent()
    {
        var controller = new PaymentsController(CreateRawClient(HttpStatusCode.OK));
        var result = await controller.Void(Guid.NewGuid(), new VoidPaymentRequest(), CancellationToken.None);
        Assert.IsType<NoContentResult>(result);
    }
}
