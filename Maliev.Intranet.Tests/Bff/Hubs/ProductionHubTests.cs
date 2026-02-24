using Maliev.Intranet.Bff.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System;

namespace Maliev.Intranet.Tests.Bff.Hubs;

public class ProductionHubTests
{
    [Fact]
    public async Task JoinProductionFloor_ShouldAddToGroup()
    {
        var mockClients = new Mock<IHubClients>();
        var mockGroupManager = new Mock<IGroupManager>();
        var mockCaller = new Mock<ISingleClientProxy>();
        var mockContext = new Mock<HubCallerContext>();
        
        mockContext.SetupGet(c => c.ConnectionId).Returns("test-connection-id");
        mockGroupManager.Setup(g => g.AddToGroupAsync("test-connection-id", "ProductionFloor", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hub = new ProductionHub
        {
            Context = mockContext.Object,
            Groups = mockGroupManager.Object
        };

        await hub.JoinProductionFloor();

        mockGroupManager.Verify(g => g.AddToGroupAsync("test-connection-id", "ProductionFloor", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LeaveProductionFloor_ShouldRemoveFromGroup()
    {
        var mockGroupManager = new Mock<IGroupManager>();
        var mockContext = new Mock<HubCallerContext>();
        
        mockContext.SetupGet(c => c.ConnectionId).Returns("test-connection-id");
        mockGroupManager.Setup(g => g.RemoveFromGroupAsync("test-connection-id", "ProductionFloor", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hub = new ProductionHub
        {
            Context = mockContext.Object,
            Groups = mockGroupManager.Object
        };

        await hub.LeaveProductionFloor();

        mockGroupManager.Verify(g => g.RemoveFromGroupAsync("test-connection-id", "ProductionFloor", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyJobStatusChanged_ShouldBroadcastToGroup()
    {
        var mockClients = new Mock<IHubCallerClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        var mockGroupManager = new Mock<IGroupManager>();
        var mockContext = new Mock<HubCallerContext>();
        
        var jobId = Guid.NewGuid();
        mockClients.Setup(c => c.Group("ProductionFloor")).Returns(mockClientProxy.Object);
        mockClientProxy.Setup(p => p.SendCoreAsync("JobStatusChanged", It.Is<object[]>(args => 
            args.Length == 3 && 
            args[0] is Guid && 
            args[1] is string && 
            args[2] is string), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hub = new ProductionHub
        {
            Context = mockContext.Object,
            Clients = mockClients.Object,
            Groups = mockGroupManager.Object
        };

        await hub.NotifyJobStatusChanged(jobId, "Pending", "Queued");

        mockClientProxy.Verify(p => p.SendCoreAsync("JobStatusChanged", 
            It.Is<object[]>(args => 
                args.Length == 3 && 
                (Guid)args[0] == jobId && 
                (string)args[1] == "Pending" && 
                (string)args[2] == "Queued"), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyJobAdded_ShouldBroadcastToGroup()
    {
        var mockClients = new Mock<IHubCallerClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        var mockGroupManager = new Mock<IGroupManager>();
        var mockContext = new Mock<HubCallerContext>();
        
        var jobId = Guid.NewGuid();
        mockClients.Setup(c => c.Group("ProductionFloor")).Returns(mockClientProxy.Object);
        mockClientProxy.Setup(p => p.SendCoreAsync("JobAdded", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hub = new ProductionHub
        {
            Context = mockContext.Object,
            Clients = mockClients.Object,
            Groups = mockGroupManager.Object
        };

        await hub.NotifyJobAdded(jobId);

        mockClientProxy.Verify(p => p.SendCoreAsync("JobAdded", 
            It.Is<object[]>(args => args.Length == 1 && (Guid)args[0] == jobId), 
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
