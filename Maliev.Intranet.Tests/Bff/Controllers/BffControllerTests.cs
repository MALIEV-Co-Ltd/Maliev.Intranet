using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class CompaniesControllerTests
{
    [Fact]
    public async Task Get_ShouldReturnOk()
    {
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new PagedResponse<CompanySummaryDto>()) }));
        var client = new CustomerServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") }, new Mock<Microsoft.Extensions.Logging.ILogger<CustomerServiceClient>>().Object);
        var controller = new CompaniesController(client);
        var result = await controller.Get();
        Assert.IsType<OkObjectResult>(result.Result);
    }
}

public class CreditTermsControllerTests
{
    [Fact]
    public async Task Get_ShouldReturnOk()
    {
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new List<CreditTermDto>()) }));
        var client = new InvoiceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new CreditTermsController(client);
        var result = await controller.GetCreditTerms(CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }
}

public class PreferencesControllerTests
{
    private readonly Mock<EmployeeServiceClient> _clientMock;
    private readonly PreferencesController _controller;

    public PreferencesControllerTests()
    {
        _clientMock = new Mock<EmployeeServiceClient>(new HttpClient());
        _controller = new PreferencesController(_clientMock.Object);
    }

    [Fact]
    public async Task Get_ShouldReturnOk()
    {
        _clientMock.Setup(x => x.GetPreferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new UserPreferenceDto());
        var result = await _controller.GetPreference("UI", CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }
}

public class OrdersControllerExtendedTests
{
    // OrdersController does not have Create in the snippet seen.
}

public class QuotationsControllerExtendedTests
{
    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction()
    {
        var response = new QuotationSummaryDto { Id = Guid.NewGuid() };
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) }));
        var client = new QuotationServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new QuotationsController(client);
        var result = await controller.Create(new CreateQuotationRequest(), CancellationToken.None);
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }
}

public class MaterialsControllerExtendedTests
{
    [Fact]
    public async Task GetById_ShouldReturnOk()
    {
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new MaterialDetailDto()) }));
        var client = new MaterialServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new MaterialsController(client);
        var result = await controller.GetById(Guid.NewGuid());
        Assert.IsType<OkObjectResult>(result.Result);
    }
}

public class SuppliersControllerExtendedTests
{
    [Fact]
    public async Task GetById_ShouldReturnOk()
    {
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new SupplierDetailDto()) }));
        var client = new SupplierServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new SuppliersController(client);
        var result = await controller.GetById(Guid.NewGuid());
        Assert.IsType<OkObjectResult>(result.Result);
    }
}




