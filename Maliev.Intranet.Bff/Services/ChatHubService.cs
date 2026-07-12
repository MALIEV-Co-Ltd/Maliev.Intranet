using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Service for pushing chat events to SignalR clients.
/// </summary>
public class ChatHubService
{
    private readonly IHubContext<ChatHub> _hubContext;

    /// <summary>Initializes a new instance of the ChatHubService class.</summary>
    /// <param name="hubContext">SignalR hub context.</param>
    public ChatHubService(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <summary>
    /// Sends a thinking step to all clients in a session group.
    /// </summary>
    public async Task SendThinkingStepAsync(string sessionId, ThinkingStepDto step)
    {
        await _hubContext.Clients.Group(ChatHub.SessionGroup(Guid.Parse(sessionId))).SendAsync("ReceiveThinkingStep", step);
    }

    /// <summary>
    /// Sends the final assistant message to all clients in a session group.
    /// </summary>
    public async Task SendMessageAsync(string sessionId, BffChatMessageResponse response)
    {
        await _hubContext.Clients.Group(ChatHub.SessionGroup(Guid.Parse(sessionId))).SendAsync("ReceiveMessage", response);
    }

    /// <summary>
    /// Sends an error to all clients in a session group.
    /// </summary>
    public async Task SendErrorAsync(string sessionId, string error)
    {
        await _hubContext.Clients.Group(ChatHub.SessionGroup(Guid.Parse(sessionId))).SendAsync("ReceiveError", error);
    }
}
