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
}
