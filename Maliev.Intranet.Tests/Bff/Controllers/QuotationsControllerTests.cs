using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class QuotationsControllerTests
{
    private static QuotationServiceClient CreateClient<T>(T response, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = JsonContent.Create(response) }));
        return new QuotationServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    private static QuotationServiceClient CreateRawClient(HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(status)));
        return new QuotationServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    [Fact]
    public async Task Get_ShouldReturnOk_WhenSuccessful()
    {
        var response = new PagedResponse<QuotationSummaryDto> { Data = new List<QuotationSummaryDto>() };
        var controller = new QuotationsController(CreateClient(response));

        var result = await controller.Get();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Get_WhenClientReturnsNull_ShouldReturnEmptyPagedResponse()
    {
        var response = new PagedResponse<QuotationSummaryDto>();
        var controller = new QuotationsController(CreateClient(response));

        var result = await controller.Get();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<PagedResponse<QuotationSummaryDto>>(okResult.Value);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldReturnOk()
    {
        var quotation = new QuotationDetailDto { Id = Guid.NewGuid(), QuotationNumber = "QUO-001" };
        var controller = new QuotationsController(CreateClient(quotation));

        var result = await controller.GetById(quotation.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturnNotFound()
    {
        // Client returns JSON null -> controller returns NotFound
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null") }));
        var client = new QuotationServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new QuotationsController(client);

        var result = await controller.GetById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ShouldReturnCreatedAtAction()
    {
        var created = new QuotationSummaryDto { Id = Guid.NewGuid(), QuotationNumber = "QUO-001" };
        var controller = new QuotationsController(CreateClient(created));

        var request = new CreateQuotationRequest
        {
            CustomerId = Guid.NewGuid(),
            ValidityPeriodStart = DateTime.UtcNow,
            ValidityPeriodEnd = DateTime.UtcNow.AddDays(30)
        };

        var result = await controller.Create(request, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task Create_WhenClientFails_ShouldReturnBadRequest()
    {
        var controller = new QuotationsController(CreateRawClient(HttpStatusCode.BadRequest));

        var request = new CreateQuotationRequest
        {
            CustomerId = Guid.NewGuid(),
            ValidityPeriodStart = DateTime.UtcNow,
            ValidityPeriodEnd = DateTime.UtcNow.AddDays(30)
        };

        var result = await controller.Create(request, CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ShouldReturnOk()
    {
        var updated = new QuotationDetailDto { Id = Guid.NewGuid(), QuotationNumber = "QUO-001" };
        var controller = new QuotationsController(CreateClient(updated));

        var result = await controller.Update(updated.Id, new UpdateQuotationRequest(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_WhenNotFound_ShouldReturnNotFound()
    {
        var controller = new QuotationsController(CreateRawClient(HttpStatusCode.NotFound));

        var result = await controller.Update(Guid.NewGuid(), new UpdateQuotationRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new QuotationsController(CreateRawClient(HttpStatusCode.OK));

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ShouldReturnNotFound()
    {
        var controller = new QuotationsController(CreateRawClient(HttpStatusCode.NotFound));

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new QuotationsController(CreateRawClient(HttpStatusCode.OK));

        var result = await controller.UpdateStatus(Guid.NewGuid(), "Approved", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_WhenFails_ShouldReturnBadRequest()
    {
        var controller = new QuotationsController(CreateRawClient(HttpStatusCode.BadRequest));

        var result = await controller.UpdateStatus(Guid.NewGuid(), "InvalidStatus", CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }
}
