using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;
using Maliev.Intranet.Tests.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class InvoicesControllerTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly InvoiceServiceClient _client;
    private readonly PdfServiceClient _pdfClient;
    private readonly InvoicesController _controller;

    public InvoicesControllerTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://test")
        };
        _client = new InvoiceServiceClient(httpClient);
        
        var pdfHttpClient = new HttpClient(new MockHttpMessageHandler((req, ct) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { storageUrl = "http://test.pdf" }) });
        }))
        {
            BaseAddress = new Uri("http://test")
        };
        _pdfClient = new PdfServiceClient(pdfHttpClient);
        
        _controller = new InvoicesController(_client, _pdfClient);
    }

    [Fact]
    public async Task Get_ShouldReturnOk_WhenSuccessful()
    {
        var response = new PagedResponse<InvoiceSummaryDto> { Data = new List<InvoiceSummaryDto>() };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response)
            });

        var result = await _controller.Get(null, 1, 20, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actual = Assert.IsType<PagedResponse<InvoiceSummaryDto>>(okResult.Value);
        Assert.NotNull(actual);
    }

    [Fact]
    public async Task Get_ShouldReturnOk_WhenSuccessful_WithDefaultPageSize()
    {
        var response = new PagedResponse<InvoiceSummaryDto> { Data = new List<InvoiceSummaryDto>() };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response)
            });

        var result = await _controller.Get(null, 1, 20, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actual = Assert.IsType<PagedResponse<InvoiceSummaryDto>>(okResult.Value);
        Assert.NotNull(actual);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenClientReturns404()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ShouldReturnCreated()
    {
        var response = new InvoiceSummaryDto { Id = Guid.NewGuid() };
        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) });

        var result = await _controller.Create(new CreateInvoiceRequest(), CancellationToken.None);
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task Finalize_ShouldReturnOk()
    {
        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var result = await _controller.Finalize(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Update_ShouldReturnOk()
    {
        var response = new InvoiceDetailDto();
        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) });

        var result = await _controller.Update(Guid.NewGuid(), new UpdateInvoiceRequest(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Cancel_ShouldReturnOk()
    {
        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var result = await _controller.Cancel(Guid.NewGuid(), new CancelInvoiceRequest(), CancellationToken.None);
        Assert.IsType<OkResult>(result);
    }
}
