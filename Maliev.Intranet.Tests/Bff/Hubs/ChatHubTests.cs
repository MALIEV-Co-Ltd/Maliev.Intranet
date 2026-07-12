using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Hubs;

public class ChatHubTests
{
    [Fact]
    public void ChatHub_RequiresConversationReadPermission()
    {
        var attribute = Assert.Single(
            typeof(ChatHub).GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: true)
                .Cast<RequirePermissionAttribute>());

        Assert.Equal(MalievPermissions.Chat.SessionsRead, attribute.Permission);
        Assert.Equal("Bearer,Cookies", attribute.AuthenticationSchemes);
    }

    [Fact]
    public async Task JoinSession_WhenDownstreamConfirmsCurrentUserOwnsSession_AddsConnectionToServerDerivedGroup()
    {
        var sessionId = Guid.NewGuid();
        var groupsMock = new Mock<IGroupManager>();
        var contextMock = new Mock<HubCallerContext>();
        var chatbotClient = CreateChatbotClient();
        contextMock.Setup(x => x.ConnectionId).Returns("conn-1");
        chatbotClient
            .Setup(client => client.GetConversationMessagesAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatbotConversationMessagesResponse { SessionId = sessionId });

        var hub = new ChatHub(chatbotClient.Object)
        {
            Groups = groupsMock.Object,
            Context = contextMock.Object
        };

        await hub.JoinSession(sessionId);

        groupsMock.Verify(
            x => x.AddToGroupAsync("conn-1", ChatHub.SessionGroup(sessionId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinSession_WhenDownstreamCannotConfirmCurrentUserOwnership_DoesNotAddGroupMembership()
    {
        var sessionId = Guid.NewGuid();
        var groupsMock = new Mock<IGroupManager>();
        var contextMock = new Mock<HubCallerContext>();
        var chatbotClient = CreateChatbotClient();
        contextMock.Setup(x => x.ConnectionId).Returns("conn-1");
        chatbotClient
            .Setup(client => client.GetConversationMessagesAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatbotConversationMessagesResponse?)null);

        var hub = new ChatHub(chatbotClient.Object)
        {
            Groups = groupsMock.Object,
            Context = contextMock.Object
        };

        await Assert.ThrowsAsync<HubException>(() => hub.JoinSession(sessionId));

        groupsMock.Verify(
            x => x.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LeaveSession_WhenDownstreamConfirmsCurrentUserOwnsSession_RemovesOnlyServerDerivedGroup()
    {
        var sessionId = Guid.NewGuid();
        var groupsMock = new Mock<IGroupManager>();
        var contextMock = new Mock<HubCallerContext>();
        var chatbotClient = CreateChatbotClient();
        contextMock.Setup(x => x.ConnectionId).Returns("conn-1");
        chatbotClient
            .Setup(client => client.GetConversationMessagesAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatbotConversationMessagesResponse { SessionId = sessionId });

        var hub = new ChatHub(chatbotClient.Object)
        {
            Groups = groupsMock.Object,
            Context = contextMock.Object
        };

        await hub.LeaveSession(sessionId);

        groupsMock.Verify(
            x => x.RemoveFromGroupAsync("conn-1", ChatHub.SessionGroup(sessionId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Mock<ChatbotServiceClient> CreateChatbotClient()
    {
        return new Mock<ChatbotServiceClient>(
            new HttpClient(),
            new Mock<ILogger<ChatbotServiceClient>>().Object);
    }
}
