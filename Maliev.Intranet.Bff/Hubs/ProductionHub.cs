using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Hubs;

/// <summary>
/// SignalR hub for real-time production queue updates.
/// </summary>
public class ProductionHub : Hub
{
    /// <summary>
    /// Adds the connection to the production floor group.
    /// </summary>
    public async Task JoinProductionFloor()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "ProductionFloor");
    }

    /// <summary>
    /// Removes the connection from the production floor group.
    /// </summary>
    public async Task LeaveProductionFloor()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "ProductionFloor");
    }

    /// <summary>
    /// Broadcasts a job status change to all production floor clients.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="previousStatus">The previous status.</param>
    /// <param name="newStatus">The new status.</param>
    /// <returns>A task representing the broadcast operation.</returns>
    public async Task NotifyJobStatusChanged(Guid jobId, string previousStatus, string newStatus)
    {
        await Clients.Group("ProductionFloor").SendAsync("JobStatusChanged", jobId, previousStatus, newStatus);
    }

    /// <summary>
    /// Broadcasts a new job added notification.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <returns>A task representing the broadcast operation.</returns>
    public async Task NotifyJobAdded(Guid jobId)
    {
        await Clients.Group("ProductionFloor").SendAsync("JobAdded", jobId);
    }
}
