using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class RecruitmentControllerTests
{
    [Fact]
    public async Task GetJobs_ShouldReturnOk()
    {
        var response = new MalievResponse<List<JobPostingSummaryDto>> { Data = new List<JobPostingSummaryDto>() };
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) }));
        var client = new CareerServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new RecruitmentController(client);
        var result = await controller.GetJobs();
        Assert.IsType<OkObjectResult>(result.Result);
    }
}

public class SystemHealthControllerTests
{
    [Fact]
    public async Task GetSystemHealth_ShouldReturnOk()
    {
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var configMock = new Mock<IConfiguration>();
        var controller = new SystemHealthController(httpClientFactoryMock.Object, configMock.Object);
        var result = await controller.GetSystemHealth(CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }
}

public class ComplianceControllerTests
{
    [Fact]
    public async Task GetStats_ShouldReturnOk()
    {
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new ComplianceStatsDto()) }));
        var client = new ComplianceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new ComplianceController(client);
        var result = await controller.GetStats();
        Assert.IsType<OkObjectResult>(result.Result);
    }
}

public class CompensationControllerTests
{
    [Fact]
    public async Task GetSummary_ShouldReturnOk()
    {
        var clientMock = new Mock<ICompensationServiceClient>();
        clientMock.Setup(x => x.GetSummaryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new CompensationSummaryDto());
        var controller = new CompensationController(clientMock.Object);
        var result = await controller.GetSummary(CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }
}

public class BillingNotesControllerTests
{
    [Fact]
    public async Task GetBillingNote_ShouldReturnOk()
    {
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new BillingNoteDto()) }));
        var client = new InvoiceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new BillingNotesController(client);
        var result = await controller.GetBillingNote(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }
}

public class AiProcessingControllerTests
{
    [Fact]
    public async Task ExtractCustomerFromDocument_ShouldReturnBadRequest_WhenInputEmpty()
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler());
        var chatbotClient = new ChatbotServiceClient(httpClient, new Mock<ILogger<ChatbotServiceClient>>().Object);
        var uploadClient = new UploadServiceClient(httpClient);
        var registryClient = new RegistryServiceClient(httpClient);
        var customerClient = new CustomerServiceClient(httpClient, new Mock<ILogger<CustomerServiceClient>>().Object);
        var logger = new Mock<ILogger<AiProcessingController>>().Object;

        var controller = new AiProcessingController(chatbotClient, uploadClient, registryClient, customerClient, logger);

        var files = new FormFileCollection();
        var result = await controller.ExtractCustomerFromDocument(files, null);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
