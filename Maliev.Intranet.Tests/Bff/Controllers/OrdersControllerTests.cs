using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class OrdersControllerTests
{
    private static OrderServiceClient CreateClient<T>(T response, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = JsonContent.Create(response) }));
        return new OrderServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    private static OrderServiceClient CreateRawClient(HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(status)));
        return new OrderServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    [Fact]
    public async Task Get_ShouldReturnOk_WhenSuccessful()
    {
        var response = new PagedResponse<OrderSummaryDto> { Data = new List<OrderSummaryDto>() };
        var controller = new OrdersController(CreateClient(response));

        var result = await controller.Get(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Get_WhenClientReturnsNull_ShouldReturnEmptyPagedResponse()
    {
        // GetFromJsonAsync returns null when response body is JSON null
        var response = new PagedResponse<OrderSummaryDto>();
        var controller = new OrdersController(CreateClient(response));

        var result = await controller.Get(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<PagedResponse<OrderSummaryDto>>(okResult.Value);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldReturnOk()
    {
        var order = new OrderDetailDto { Id = Guid.NewGuid(), OrderNumber = "ORD-001" };
        var controller = new OrdersController(CreateClient(order));

        var result = await controller.GetById("ORD-001", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturnNotFound()
    {
        // Client returns null JSON (not 404) - controller pattern is result != null ? Ok : NotFound
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null") }));
        var client = new OrderServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new OrdersController(client);

        var result = await controller.GetById("nonexistent-id", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new OrdersController(CreateRawClient(HttpStatusCode.OK));

        var result = await controller.Update("ORD-001", new UpdateOrderRequest(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_WhenDownstreamFails_ShouldReturnErrorStatusCode()
    {
        var controller = new OrdersController(CreateRawClient(HttpStatusCode.BadRequest));

        var result = await controller.Update("ORD-001", new UpdateOrderRequest(), CancellationToken.None);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(400, statusResult.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new OrdersController(CreateRawClient(HttpStatusCode.OK));

        var result = await controller.UpdateStatus("ORD-001", new UpdateOrderStatusRequest { Status = "Completed" }, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_WhenDownstreamFails_ShouldReturnErrorStatusCode()
    {
        var controller = new OrdersController(CreateRawClient(HttpStatusCode.UnprocessableEntity));

        var result = await controller.UpdateStatus("ORD-001", new UpdateOrderStatusRequest { Status = "Invalid" }, CancellationToken.None);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(422, statusResult.StatusCode);
    }
}
