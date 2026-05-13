using System.Net;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Security;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class ChatControllerTests
{
    private readonly Mock<ChatbotServiceClient> _chatbotClientMock;
    private readonly Mock<IChatContextResolver> _contextResolverMock;
    private readonly Mock<IHubClients> _hubClientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<ChatHubService> _chatHubServiceMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IChatCallbackTokenService> _callbackTokenServiceMock;
    private readonly ChatController _controller;

    public ChatControllerTests()
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler());
        _chatbotClientMock = new Mock<ChatbotServiceClient>(httpClient, new Mock<ILogger<ChatbotServiceClient>>().Object);
        _contextResolverMock = new Mock<IChatContextResolver>();

        var hubContextMock = new Mock<IHubContext<Maliev.Intranet.Bff.Hubs.ChatHub>>();
        _hubClientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();
        hubContextMock.Setup(context => context.Clients).Returns(_hubClientsMock.Object);
        _hubClientsMock.Setup(clients => clients.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _chatHubServiceMock = new Mock<ChatHubService>(hubContextMock.Object);

        _configMock = new Mock<IConfiguration>();
        _callbackTokenServiceMock = new Mock<IChatCallbackTokenService>();
        _callbackTokenServiceMock
            .Setup(service => service.CreateToken(It.IsAny<Guid>()))
            .Returns("callback-token");

        _controller = new ChatController(
            _chatbotClientMock.Object,
            _contextResolverMock.Object,
            _chatHubServiceMock.Object,
            _configMock.Object,
            _callbackTokenServiceMock.Object);
    }

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

    [Fact]
    public async Task SendMessageStream_AddsSessionCallbackTokenToCallbackUrl()
    {
        var sessionId = Guid.NewGuid();
        var request = new BffChatMessageRequest { SessionId = sessionId, Content = "hi" };
        var aiResponse = new ChatbotMessageResponse { Content = "Hello from AI" };

        _configMock
            .Setup(configuration => configuration["Services:IntranetBff:CallbackBaseUrl"])
            .Returns("https://intranet.maliev.com");

        _contextResolverMock
            .Setup(resolver => resolver.ResolveContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _chatbotClientMock
            .Setup(client => client.SendMessageStreamAsync(
                sessionId,
                It.IsAny<string>(),
                It.Is<string>(url =>
                    url.StartsWith($"https://intranet.maliev.com/api/v1/chat/callback/{sessionId}/thinking?", StringComparison.Ordinal) &&
                    url.Contains("token=callback-token", StringComparison.Ordinal)),
                It.IsAny<List<ChatbotAttachment>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiResponse);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await _controller.SendMessageStream(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        _callbackTokenServiceMock.Verify(service => service.CreateToken(sessionId), Times.Once);
    }
}
