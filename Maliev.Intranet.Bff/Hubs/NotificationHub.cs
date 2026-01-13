using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Hubs;

/// <summary>
/// SignalR hub for broadcasting real-time system notifications.
/// </summary>
public class NotificationHub : Hub
{
    /// <summary>
    /// Broadcasts a notification message to all connected clients.
    /// </summary>
    /// <param name="message">The notification message content.</param>
    /// <returns>A task representing the broadcast operation.</returns>
    public async Task SendNotification(string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", message);
    }
}