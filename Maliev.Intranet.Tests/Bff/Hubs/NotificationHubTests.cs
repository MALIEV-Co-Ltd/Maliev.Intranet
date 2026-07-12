using Maliev.Intranet.Bff.Hubs;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Hubs;

public class NotificationHubTests
{
    [Fact]
    public void NotificationHub_RequiresProjectReadPermission()
    {
        var attribute = Assert.Single(
            typeof(NotificationHub).GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: true)
                .Cast<RequirePermissionAttribute>());

        Assert.Equal(MalievPermissions.Project.Read, attribute.Permission);
        Assert.Equal("Bearer,Cookies", attribute.AuthenticationSchemes);
    }

    [Fact]
    public async Task JoinFileGroup_WhenDownstreamConfirmsCurrentUserCanReadFile_AddsConnectionToOpaqueFileGroup()
    {
        var fileId = Guid.NewGuid();
        var groupsMock = new Mock<IGroupManager>();
        var contextMock = new Mock<HubCallerContext>();
        var uploadClient = CreateUploadClient();
        contextMock.Setup(x => x.ConnectionId).Returns("conn-1");
        uploadClient
            .Setup(client => client.CanReadFileAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var hub = new NotificationHub(uploadClient.Object)
        {
            Groups = groupsMock.Object,
            Context = contextMock.Object
        };

        await hub.JoinFileGroup(fileId);

        groupsMock.Verify(
            x => x.AddToGroupAsync("conn-1", NotificationHub.FileGroup(fileId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinFileGroup_WhenDownstreamCannotConfirmCurrentUserCanReadFile_DoesNotAddGroupMembership()
    {
        var fileId = Guid.NewGuid();
        var groupsMock = new Mock<IGroupManager>();
        var contextMock = new Mock<HubCallerContext>();
        var uploadClient = CreateUploadClient();
        contextMock.Setup(x => x.ConnectionId).Returns("conn-1");
        uploadClient
            .Setup(client => client.CanReadFileAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var hub = new NotificationHub(uploadClient.Object)
        {
            Groups = groupsMock.Object,
            Context = contextMock.Object
        };

        await Assert.ThrowsAsync<HubException>(() => hub.JoinFileGroup(fileId));

        groupsMock.Verify(
            x => x.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LeaveFileGroup_WhenDownstreamConfirmsCurrentUserCanReadFile_RemovesOnlyOpaqueFileGroup()
    {
        var fileId = Guid.NewGuid();
        var groupsMock = new Mock<IGroupManager>();
        var contextMock = new Mock<HubCallerContext>();
        var uploadClient = CreateUploadClient();
        contextMock.Setup(x => x.ConnectionId).Returns("conn-1");
        uploadClient
            .Setup(client => client.CanReadFileAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var hub = new NotificationHub(uploadClient.Object)
        {
            Groups = groupsMock.Object,
            Context = contextMock.Object
        };

        await hub.LeaveFileGroup(fileId);

        groupsMock.Verify(
            x => x.RemoveFromGroupAsync("conn-1", NotificationHub.FileGroup(fileId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void NotificationHub_DoesNotExposeClientCallableBroadcastMethods()
    {
        var browserMethods = typeof(NotificationHub)
            .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.DeclaredOnly)
            .Select(method => method.Name);

        Assert.DoesNotContain("SendNotification", browserMethods);
        Assert.DoesNotContain("NotifyCustomerChanged", browserMethods);
    }

    private static Mock<UploadServiceClient> CreateUploadClient() => new(new HttpClient());
}
