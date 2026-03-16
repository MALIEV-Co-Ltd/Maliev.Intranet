using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class MaterialsControllerTests
{
    private static MaterialServiceClient CreateClient<T>(T response, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = JsonContent.Create(response) }));
        return new MaterialServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    private static MaterialServiceClient CreateRawClient(HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(status)));
        return new MaterialServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    [Fact]
    public async Task Get_ShouldReturnOk_WhenSuccessful()
    {
        var response = new PagedResponse<MaterialSummaryDto> { Data = new List<MaterialSummaryDto>() };
        var controller = new MaterialsController(CreateClient(response));

        var result = await controller.Get();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Get_WhenClientReturnsNull_ShouldReturnEmptyPagedResponse()
    {
        var response = new PagedResponse<MaterialSummaryDto>();
        var controller = new MaterialsController(CreateClient(response));

        var result = await controller.Get();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<PagedResponse<MaterialSummaryDto>>(okResult.Value);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldReturnOk()
    {
        var material = new MaterialDetailDto { Id = Guid.NewGuid(), Name = "PLA Filament" };
        var controller = new MaterialsController(CreateClient(material));

        var result = await controller.GetById(material.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturnNotFound()
    {
        // Client returns null JSON -> controller returns NotFound
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null") }));
        var client = new MaterialServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new MaterialsController(client);

        var result = await controller.GetById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ShouldReturnCreatedAtAction()
    {
        var created = new MaterialSummaryDto { Id = Guid.NewGuid(), Name = "ABS Filament" };
        var controller = new MaterialsController(CreateClient(created));

        var result = await controller.Create(new CreateMaterialRequest { Name = "ABS Filament", SKU = "ABS-001" }, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task Create_WhenClientFails_ShouldReturnBadRequest()
    {
        var controller = new MaterialsController(CreateRawClient(HttpStatusCode.BadRequest));

        var result = await controller.Create(new CreateMaterialRequest(), CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ShouldReturnOk()
    {
        var updated = new MaterialDetailDto { Id = Guid.NewGuid(), Name = "Updated Material" };
        var controller = new MaterialsController(CreateClient(updated));

        var result = await controller.Update(updated.Id, new UpdateMaterialRequest { Name = "Updated Material" }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_WhenNotFound_ShouldReturnNotFound()
    {
        var controller = new MaterialsController(CreateRawClient(HttpStatusCode.NotFound));

        var result = await controller.Update(Guid.NewGuid(), new UpdateMaterialRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new MaterialsController(CreateRawClient(HttpStatusCode.OK));

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ShouldReturnNotFound()
    {
        var controller = new MaterialsController(CreateRawClient(HttpStatusCode.NotFound));

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
