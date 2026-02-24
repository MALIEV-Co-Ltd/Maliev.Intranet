using Maliev.Intranet.Bff.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Hubs;

public class ChatHubTests
{
    [Fact]
    public async Task JoinSession_ShouldAddToGroup()
    {
        var groupsMock = new Mock<IGroupManager>();
        var contextMock = new Mock<HubCallerContext>();
        contextMock.Setup(x => x.ConnectionId).Returns("conn-1");

        var hub = new ChatHub
        {
            Groups = groupsMock.Object,
            Context = contextMock.Object
        };

        await hub.JoinSession("session-1");

        groupsMock.Verify(x => x.AddToGroupAsync("conn-1", "session-1", default), Times.Once);
    }

    [Fact]
    public async Task LeaveSession_ShouldRemoveFromGroup()
    {
        var groupsMock = new Mock<IGroupManager>();
        var contextMock = new Mock<HubCallerContext>();
        contextMock.Setup(x => x.ConnectionId).Returns("conn-1");

        var hub = new ChatHub
        {
            Groups = groupsMock.Object,
            Context = contextMock.Object
        };

        await hub.LeaveSession("session-1");

        groupsMock.Verify(x => x.RemoveFromGroupAsync("conn-1", "session-1", default), Times.Once);
    }
}
