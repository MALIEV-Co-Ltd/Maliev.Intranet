using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class CompaniesControllerFullTests
{
    private static CustomerServiceClient CreateClient<T>(T response, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = JsonContent.Create(response) }));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://test") };
        var logger = new Mock<ILogger<CustomerServiceClient>>().Object;
        return new CustomerServiceClient(httpClient, logger);
    }

    private static CustomerServiceClient CreateRawClient(HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(status)));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://test") };
        var logger = new Mock<ILogger<CustomerServiceClient>>().Object;
        return new CustomerServiceClient(httpClient, logger);
    }

    [Fact]
    public async Task Get_ShouldReturnOk_WhenSuccessful()
    {
        var response = new { items = new List<CompanySummaryDto>(), totalCount = 0, page = 1, pageSize = 20, totalPages = 0 };
        var controller = new CompaniesController(CreateClient(response));

        var result = await controller.Get(null, 1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Get_WithSearchQuery_ShouldReturnOk()
    {
        var response = new { items = new List<CompanySummaryDto>(), totalCount = 0, page = 1, pageSize = 20, totalPages = 0 };
        var controller = new CompaniesController(CreateClient(response));

        var result = await controller.Get("Acme", 1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldReturnOk()
    {
        var company = new CompanyResponse { Id = Guid.NewGuid(), Name = "Acme Corp" };
        var controller = new CompaniesController(CreateClient(company));

        var result = await controller.GetById(company.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturnNotFound()
    {
        // Client returns null JSON -> controller returns NotFound
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null") }));
        var client = new CustomerServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") }, Microsoft.Extensions.Logging.Abstractions.NullLogger<CustomerServiceClient>.Instance);
        var controller = new CompaniesController(client);

        var result = await controller.GetById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ShouldReturnOk()
    {
        var company = new CompanyResponse { Id = Guid.NewGuid(), Name = "Updated Corp" };
        var controller = new CompaniesController(CreateClient(company));

        var result = await controller.Update(company.Id, new UpdateCompanyRequest { Name = "Updated Corp" }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_WhenNotFound_ShouldReturnNotFound()
    {
        var controller = new CompaniesController(CreateRawClient(HttpStatusCode.NotFound));

        var result = await controller.Update(Guid.NewGuid(), new UpdateCompanyRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task PromotePrimaryContact_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new CompaniesController(CreateRawClient(HttpStatusCode.OK));

        var result = await controller.PromotePrimaryContact(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task PromotePrimaryContact_WhenNotFound_ShouldReturnNotFound()
    {
        var controller = new CompaniesController(CreateRawClient(HttpStatusCode.NotFound));

        var result = await controller.PromotePrimaryContact(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
