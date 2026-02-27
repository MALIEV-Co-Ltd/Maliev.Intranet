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

    /// <summary>
    /// Notifies all connected clients that customer data has changed.
    /// </summary>
    /// <returns>A task representing the broadcast operation.</returns>
    public async Task NotifyCustomerChanged()
    {
        await Clients.All.SendAsync("CustomerChanged");
    }
}
