using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Security;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class ChatCallbackControllerTests
{
    private readonly Mock<IHubContext<Maliev.Intranet.Bff.Hubs.ChatHub>> _hubContextMock;
    private readonly Mock<IHubClients> _clientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<IChatCallbackTokenService> _callbackTokenServiceMock;
    private readonly ChatHubService _chatHubService;
    private readonly Mock<ILogger<ChatCallbackController>> _loggerMock;
    private readonly ChatCallbackController _controller;

    public ChatCallbackControllerTests()
    {
        _hubContextMock = new Mock<IHubContext<Maliev.Intranet.Bff.Hubs.ChatHub>>();
        _clientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();

        _hubContextMock.Setup(x => x.Clients).Returns(_clientsMock.Object);
        _clientsMock.Setup(x => x.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);

        _chatHubService = new ChatHubService(_hubContextMock.Object);
        _callbackTokenServiceMock = new Mock<IChatCallbackTokenService>();
        _callbackTokenServiceMock
            .Setup(service => service.IsValid(It.IsAny<Guid>(), "valid-token"))
            .Returns(true);
        _loggerMock = new Mock<ILogger<ChatCallbackController>>();
        _controller = new ChatCallbackController(_chatHubService, _callbackTokenServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ReceiveThinkingStep_ShouldCallService()
    {
        var sessionId = Guid.NewGuid();
        var step = new ThinkingStepDto { StepNumber = 1, Title = "Thinking" };
        var result = await _controller.ReceiveThinkingStep(sessionId, "valid-token", step);

        _clientsMock.Verify(x => x.Group(Maliev.Intranet.Bff.Hubs.ChatHub.SessionGroup(sessionId)), Times.Once);
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task ReceiveComplete_ShouldCallService()
    {
        var sessionId = Guid.NewGuid();
        var response = new BffChatMessageResponse { Content = "Hello" };
        var result = await _controller.ReceiveComplete(sessionId, "valid-token", response);

        _clientsMock.Verify(x => x.Group(Maliev.Intranet.Bff.Hubs.ChatHub.SessionGroup(sessionId)), Times.Once);
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task ReceiveThinkingStep_WithInvalidToken_ReturnsUnauthorized()
    {
        var step = new ThinkingStepDto { StepNumber = 1, Title = "Thinking" };

        var result = await _controller.ReceiveThinkingStep(Guid.NewGuid(), "invalid-token", step);

        Assert.IsType<UnauthorizedResult>(result);
        _clientsMock.Verify(x => x.Group(It.IsAny<string>()), Times.Never);
    }
}
