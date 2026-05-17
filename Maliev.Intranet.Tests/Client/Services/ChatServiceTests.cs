using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Client.Helpers;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Client.Services;

public class ChatServiceTests
{
    [Fact]
    public async Task LoadAndSelectConversationAsync_PreservesHistoryWhenStartingNewConversation()
    {
        var sessionId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Get &&
                request.RequestUri?.PathAndQuery.StartsWith("/api/v1/chat/conversations?", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new BffChatConversationListResponse
                    {
                        Data =
                        [
                            new BffChatConversationSummary
                            {
                                SessionId = sessionId,
                                Preview = "Can you create customer Acme?",
                                Channel = "intranet",
                                LastActivityAt = DateTimeOffset.Parse("2026-05-17T10:30:00Z"),
                                MessageCount = 2,
                                Status = "active"
                            }
                        ],
                        Meta = new BffPaginationMeta
                        {
                            Page = 1,
                            PageSize = 20,
                            TotalCount = 1
                        }
                    })
                });
            }

            if (request.Method == HttpMethod.Get &&
                request.RequestUri?.PathAndQuery.Equals($"/api/v1/chat/conversations/{sessionId}/messages", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new BffChatConversationMessagesResponse
                    {
                        SessionId = sessionId,
                        Channel = "intranet",
                        Messages =
                        [
                            new BffChatConversationMessage
                            {
                                MessageId = Guid.NewGuid(),
                                Role = "user",
                                Content = "Can you create customer Acme?",
                                CreatedAt = DateTimeOffset.Parse("2026-05-17T10:30:00Z")
                            },
                            new BffChatConversationMessage
                            {
                                MessageId = Guid.NewGuid(),
                                Role = "assistant",
                                Content = "I can help with that.",
                                CreatedAt = DateTimeOffset.Parse("2026-05-17T10:31:00Z")
                            }
                        ]
                    })
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var service = new ChatService(
            new HttpClient(handler) { BaseAddress = new Uri("http://test/") },
            null!,
            new CookieProvider());

        await service.LoadConversationsAsync();
        await service.SelectConversationAsync(sessionId);

        Assert.Single(service.Conversations);
        Assert.Equal(sessionId, service.SessionId);
        Assert.Equal(2, service.Messages.Count);
        Assert.True(service.Messages[0].IsUser);
        Assert.False(service.Messages[1].IsUser);

        service.StartNewConversation();

        Assert.Null(service.SessionId);
        Assert.Empty(service.Messages);
        Assert.Single(service.Conversations);
    }
}
