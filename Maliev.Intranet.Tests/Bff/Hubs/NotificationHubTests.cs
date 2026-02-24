using Maliev.Intranet.Bff.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Hubs;

public class NotificationHubTests
{
    [Fact]
    public async Task SendNotification_ShouldSendToAll()
    {
        var clientsMock = new Mock<IHubCallerClients>();
        var clientProxyMock = new Mock<IClientProxy>();
        clientsMock.Setup(x => x.All).Returns(clientProxyMock.Object);

        var hub = new NotificationHub
        {
            Clients = clientsMock.Object
        };

        await hub.SendNotification("test message");

        clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveNotification", It.Is<object[]>(o => o[0].ToString() == "test message"), default), Times.Once);
    }

    [Fact]
    public async Task NotifyCustomerChanged_ShouldSendToAll()
    {
        var clientsMock = new Mock<IHubCallerClients>();
        var clientProxyMock = new Mock<IClientProxy>();
        clientsMock.Setup(x => x.All).Returns(clientProxyMock.Object);

        var hub = new NotificationHub
        {
            Clients = clientsMock.Object
        };

        await hub.NotifyCustomerChanged();

        clientProxyMock.Verify(x => x.SendCoreAsync("CustomerChanged", It.Is<object[]>(o => o.Length == 0), default), Times.Once);
    }
}
