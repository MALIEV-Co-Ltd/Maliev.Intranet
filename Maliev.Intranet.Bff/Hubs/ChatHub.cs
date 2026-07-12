using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Hubs;

/// <summary>
/// SignalR hub for real-time AI chat with thinking chain support.
/// </summary>
/// <remarks>
/// Conversation identifiers are locators only. Every browser subscription is revalidated through
/// ChatbotService, which applies the current employee's forwarded identity and returns no session
/// for a conversation the caller cannot read.
/// </remarks>
[RequirePermission(MalievPermissions.Chat.SessionsRead, AuthenticationSchemes = "Bearer,Cookies")]
public class ChatHub(ChatbotServiceClient chatbotClient) : Hub
{
    /// <summary>
    /// Joins the user to a session-specific group for targeted messages.
    /// </summary>
    public async Task JoinSession(Guid sessionId)
    {
        var group = await AuthorizeSessionGroupAsync(sessionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted);
    }

    /// <summary>
    /// Removes the user from a session group.
    /// </summary>
    public async Task LeaveSession(Guid sessionId)
    {
        var group = await AuthorizeSessionGroupAsync(sessionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted);
    }

    /// <summary>Builds the server-derived SignalR group for an authorized chat session.</summary>
    public static string SessionGroup(Guid sessionId) => $"chat-session:{sessionId:N}";

    private async Task<string> AuthorizeSessionGroupAsync(Guid sessionId)
    {
        if (sessionId == Guid.Empty)
        {
            throw new HubException("Chat session is unavailable.");
        }

        var session = await chatbotClient.GetConversationMessagesAsync(sessionId, Context.ConnectionAborted);
        if (session?.SessionId != sessionId)
        {
            throw new HubException("Chat session is unavailable.");
        }

        return SessionGroup(sessionId);
    }
}
