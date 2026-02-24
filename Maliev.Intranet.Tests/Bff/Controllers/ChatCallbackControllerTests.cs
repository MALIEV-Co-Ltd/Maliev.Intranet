using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

/// <summary>Tests for the chat callback controller.</summary>
public class ChatCallbackControllerTests
{
    private readonly Mock<IHubContext<Maliev.Intranet.Bff.Hubs.ChatHub>> _hubContextMock;
    private readonly Mock<IHubClients> _clientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly ChatHubService _chatHubService;
    private readonly Mock<ILogger<ChatCallbackController>> _loggerMock;
    private readonly ChatCallbackController _controller;

    /// <summary>Initializes a new instance of the <see cref="ChatCallbackControllerTests"/> class.</summary>
    public ChatCallbackControllerTests()
    {
        _hubContextMock = new Mock<IHubContext<Maliev.Intranet.Bff.Hubs.ChatHub>>();
        _clientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();

        _hubContextMock.Setup(x => x.Clients).Returns(_clientsMock.Object);
        _clientsMock.Setup(x => x.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);

        _chatHubService = new ChatHubService(_hubContextMock.Object);
        _loggerMock = new Mock<ILogger<ChatCallbackController>>();
        _controller = new ChatCallbackController(_chatHubService, _loggerMock.Object);
    }

    /// <summary>Verifies that ReceiveThinkingStep calls the underlying service.</summary>
    [Fact]
    public async Task ReceiveThinkingStep_ShouldCallService()
    {
        var step = new ThinkingStepDto { StepNumber = 1, Title = "Thinking" };
        var result = await _controller.ReceiveThinkingStep("session-1", step);

        _clientsMock.Verify(x => x.Group("session-1"), Times.Once);
        Assert.IsType<OkResult>(result);
    }

    /// <summary>Verifies that ReceiveComplete calls the underlying service.</summary>
    [Fact]
    public async Task ReceiveComplete_ShouldCallService()
    {
        var response = new BffChatMessageResponse { Content = "Hello" };
        var result = await _controller.ReceiveComplete("session-1", response);

        _clientsMock.Verify(x => x.Group("session-1"), Times.Once);
        Assert.IsType<OkResult>(result);
    }
}
