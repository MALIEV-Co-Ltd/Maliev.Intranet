using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class AiProcessingControllerTests
{
    private readonly Mock<ChatbotServiceClient> _chatbotClientMock;
    private readonly AiProcessingController _controller;

    public AiProcessingControllerTests()
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler())
        {
            BaseAddress = new Uri("http://test")
        };

        _chatbotClientMock = new Mock<ChatbotServiceClient>(
            httpClient,
            new Mock<ILogger<ChatbotServiceClient>>().Object);

        _controller = new AiProcessingController(
            _chatbotClientMock.Object,
            new UploadServiceClient(httpClient),
            new RegistryServiceClient(httpClient),
            new CustomerServiceClient(httpClient, new Mock<ILogger<CustomerServiceClient>>().Object),
            new Mock<ILogger<AiProcessingController>>().Object);
    }

    [Fact]
    public async Task Health_WhenChatbotHealthy_DoesNotCreateChatSession()
    {
        _chatbotClientMock
            .Setup(client => client.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Health(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _chatbotClientMock.Verify(
            client => client.InitiateSessionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Health_WhenChatbotUnavailable_ReturnsServiceUnavailable()
    {
        _chatbotClientMock
            .Setup(client => client.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.Health(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetDownloadUrl_WhenReferenceIsUploadId_UsesUploadIdSignedUrlEndpoint()
    {
        HttpRequestMessage? downstreamRequest = null;
        var controller = CreateController((request, _) =>
        {
            downstreamRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { signedUrl = "https://signed.example/customer.pdf" })
            });
        });

        var result = await controller.GetDownloadUrl("upload-123", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("https://signed.example/customer.pdf", ReadUrl(ok.Value));
        Assert.NotNull(downstreamRequest);
        Assert.Equal("/upload/v1/files/upload-123/signed-url", downstreamRequest.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task GetDownloadUrl_WhenReferenceIsStoragePath_UsesByPathSignedUrlEndpoint()
    {
        HttpRequestMessage? downstreamRequest = null;
        var controller = CreateController((request, _) =>
        {
            downstreamRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { signedUrl = "https://signed.example/customer.pdf" })
            });
        });

        var result = await controller.GetDownloadUrl("customers/customer-1/customer.pdf", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("https://signed.example/customer.pdf", ReadUrl(ok.Value));
        Assert.NotNull(downstreamRequest);
        Assert.Equal("/upload/v1/files/by-path/signed-url", downstreamRequest.RequestUri?.AbsolutePath);
        var payload = JsonDocument.Parse(await downstreamRequest.Content!.ReadAsStringAsync()).RootElement;
        Assert.Equal("customers/customer-1/customer.pdf", payload.GetProperty("storagePath").GetString());
    }

    private static AiProcessingController CreateController(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("http://test")
        };

        var chatbotClientMock = new Mock<ChatbotServiceClient>(
            httpClient,
            new Mock<ILogger<ChatbotServiceClient>>().Object);

        return new AiProcessingController(
            chatbotClientMock.Object,
            new UploadServiceClient(httpClient),
            new RegistryServiceClient(httpClient),
            new CustomerServiceClient(httpClient, new Mock<ILogger<CustomerServiceClient>>().Object),
            new Mock<ILogger<AiProcessingController>>().Object);
    }

    private static string? ReadUrl(object? value)
    {
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(value));
        return payload.RootElement.GetProperty("url").GetString();
    }
}
