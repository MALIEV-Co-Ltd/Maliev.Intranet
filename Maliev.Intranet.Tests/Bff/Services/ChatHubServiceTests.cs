using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Services;

public class ChatHubServiceTests
{
    private readonly Mock<IHubContext<ChatHub>> _hubContextMock;
    private readonly Mock<IHubClients> _clientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly ChatHubService _service;

    public ChatHubServiceTests()
    {
        _hubContextMock = new Mock<IHubContext<ChatHub>>();
        _clientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();

        _hubContextMock.Setup(x => x.Clients).Returns(_clientsMock.Object);
        _clientsMock.Setup(x => x.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);

        _service = new ChatHubService(_hubContextMock.Object);
    }

    [Fact]
    public async Task SendThinkingStepAsync_ShouldCallGroupSend()
    {
        var sessionId = Guid.NewGuid();
        var step = new ThinkingStepDto { Title = "Thinking" };
        await _service.SendThinkingStepAsync(sessionId.ToString("D"), step);

        _clientsMock.Verify(x => x.Group(ChatHub.SessionGroup(sessionId)), Times.Once);
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveThinkingStep", It.Is<object[]>(o => o[0] == step), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldCallGroupSend()
    {
        var sessionId = Guid.NewGuid();
        var response = new BffChatMessageResponse { Content = "Hello" };
        await _service.SendMessageAsync(sessionId.ToString("D"), response);

        _clientsMock.Verify(x => x.Group(ChatHub.SessionGroup(sessionId)), Times.Once);
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveMessage", It.Is<object[]>(o => o[0] == response), It.IsAny<CancellationToken>()), Times.Once);
    }
}
