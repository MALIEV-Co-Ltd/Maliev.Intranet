using System.Net;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

/// <summary>Tests for the chat controller.</summary>
public class ChatControllerTests
{
    private readonly Mock<ChatbotServiceClient> _chatbotClientMock;
    private readonly Mock<IChatContextResolver> _contextResolverMock;
    private readonly Mock<ChatHubService> _chatHubServiceMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly ChatController _controller;

    /// <summary>Initializes a new instance of the <see cref="ChatControllerTests"/> class.</summary>
    public ChatControllerTests()
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler());
        _chatbotClientMock = new Mock<ChatbotServiceClient>(httpClient, new Mock<ILogger<ChatbotServiceClient>>().Object);
        _contextResolverMock = new Mock<IChatContextResolver>();

        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<Maliev.Intranet.Bff.Hubs.ChatHub>>();
        _chatHubServiceMock = new Mock<ChatHubService>(hubContextMock.Object);

        _configMock = new Mock<IConfiguration>();
        _controller = new ChatController(_chatbotClientMock.Object, _contextResolverMock.Object, _chatHubServiceMock.Object, _configMock.Object);
    }

    /// <summary>Verifies that initiating a session returns OK.</summary>
    [Fact]
    public async Task InitiateSession_ShouldReturnOk()
    {
        var response = new ChatbotSessionResponse { SessionId = Guid.NewGuid(), WelcomeMessage = "Hi" };
        _chatbotClientMock.Setup(x => x.InitiateSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.InitiateSession(new BffChatSessionRequest(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bffResponse = Assert.IsType<BffChatSessionResponse>(okResult.Value);
        Assert.Equal(response.SessionId, bffResponse.SessionId);
    }

    /// <summary>Verifies that sending a message returns OK.</summary>
    [Fact]
    public async Task SendMessage_ShouldReturnOk()
    {
        var request = new BffChatMessageRequest { SessionId = Guid.NewGuid(), Content = "hi" };
        var aiResponse = new ChatbotMessageResponse { Content = "Hello from AI" };

        _contextResolverMock.Setup(x => x.ResolveContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _chatbotClientMock.Setup(x => x.SendMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<List<ChatbotAttachment>>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiResponse);

        var result = await _controller.SendMessage(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bffResponse = Assert.IsType<BffChatMessageResponse>(okResult.Value);
        Assert.Equal("Hello from AI", bffResponse.Content);
    }
}
