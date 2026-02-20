using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class SuppliersControllerTests
{
    private static SupplierServiceClient CreateClient<T>(T response)
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) }));
        return new SupplierServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    private static SupplierServiceClient CreateRawClient(HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(status)));
        return new SupplierServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    [Fact]
    public async Task Get_ReturnsOk()
    {
        var controller = new SuppliersController(CreateClient(new PagedResponse<SupplierSummaryDto>()));
        var result = await controller.Get();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var controller = new SuppliersController(CreateClient(new SupplierDetailDto()));
        var result = await controller.GetById(Guid.NewGuid());
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var controller = new SuppliersController(CreateRawClient(HttpStatusCode.NotFound));
        var result = await controller.GetById(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_WhenSucceeds_Returns201()
    {
        var controller = new SuppliersController(CreateRawClient(HttpStatusCode.Created));
        var result = await controller.Create(new CreateSupplierRequest(), CancellationToken.None);
        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(201, statusResult.StatusCode);
    }

    [Fact]
    public async Task Update_WhenSucceeds_ReturnsNoContent()
    {
        var controller = new SuppliersController(CreateRawClient(HttpStatusCode.OK));
        var result = await controller.Update(Guid.NewGuid(), new UpdateSupplierRequest(), CancellationToken.None);
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Deactivate_WhenSucceeds_ReturnsNoContent()
    {
        var controller = new SuppliersController(CreateRawClient(HttpStatusCode.OK));
        var result = await controller.Deactivate(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NoContentResult>(result);
    }
}
