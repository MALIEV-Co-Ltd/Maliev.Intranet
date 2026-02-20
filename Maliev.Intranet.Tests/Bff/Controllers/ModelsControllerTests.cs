using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.Protected;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class ModelsControllerTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly UploadServiceClient _client;
    private readonly ModelsController _controller;

    public ModelsControllerTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://test")
        };
        _client = new UploadServiceClient(httpClient);
        _controller = new ModelsController(_client);
    }

    [Fact]
    public async Task GetModels_ShouldReturnOk_WhenSuccessful()
    {
        var response = new PagedResponse<Model3DDto> { Data = new List<Model3DDto>() };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response)
            });

        var result = await _controller.GetModels(1, 20, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actual = Assert.IsType<PagedResponse<Model3DDto>>(okResult.Value);
        Assert.NotNull(actual);
    }

    [Fact]
    public async Task GetModel_ShouldReturnNotFound_WhenClientReturns404()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await _controller.GetModel(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UploadModel_ShouldReturnOk_WhenSuccessful()
    {
        var response = new BffUploadResponse { UploadId = "test-id", FileName = "test.stl" };
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response)
            });

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(100);
        fileMock.Setup(f => f.FileName).Returns("test.stl");
        fileMock.Setup(f => f.ContentType).Returns("application/octet-stream");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

        var result = await _controller.UploadModel(fileMock.Object, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actual = Assert.IsType<BffUploadResponse>(okResult.Value);
        Assert.Equal("test-id", actual.UploadId);
    }
}
