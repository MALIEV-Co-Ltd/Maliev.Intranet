using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Hubs;

/// <summary>
/// SignalR hub for broadcasting real-time production queue updates to connected Intranet clients.
/// Clients subscribe to this hub to receive live job status changes and stats refreshes
/// without polling the BFF API.
/// </summary>
public class ProductionHub : Hub
{
    /// <summary>
    /// Broadcasts a job status change to all connected clients.
    /// Invoked by <see cref="Maliev.Intranet.Bff.Controllers.JobsController"/> after a successful
    /// downstream status update.
    /// </summary>
    /// <param name="jobId">The job GUID that changed.</param>
    /// <param name="newStatus">The new status string.</param>
    /// <returns>A task representing the broadcast operation.</returns>
    public async Task NotifyJobStatusChanged(Guid jobId, string newStatus) =>
        await Clients.All.SendAsync("JobStatusChanged", new { JobId = jobId, Status = newStatus });

    /// <summary>
    /// Broadcasts a machine assignment change to all connected clients.
    /// Invoked by <see cref="Maliev.Intranet.Bff.Controllers.JobsController"/> after a successful
    /// machine assignment.
    /// </summary>
    /// <param name="jobId">The job GUID that was assigned.</param>
    /// <param name="machineId">The machine GUID that was assigned.</param>
    /// <returns>A task representing the broadcast operation.</returns>
    public async Task NotifyJobAssigned(Guid jobId, Guid machineId) =>
        await Clients.All.SendAsync("JobAssigned", new { JobId = jobId, MachineId = machineId });

    /// <summary>
    /// Broadcasts updated production statistics to all connected clients.
    /// Can be called from a background service after any stat-affecting event.
    /// </summary>
    /// <param name="stats">Serializable stats payload.</param>
    /// <returns>A task representing the broadcast operation.</returns>
    public async Task NotifyStatsUpdated(object stats) =>
        await Clients.All.SendAsync("ProductionStatsUpdated", stats);

    /// <summary>
    /// Broadcasts a schedule change notification to all connected clients.
    /// Triggered after a job is reordered or rescheduled on a machine.
    /// </summary>
    /// <param name="machineId">The machine whose schedule changed.</param>
    /// <returns>A task representing the broadcast operation.</returns>
    public async Task NotifyScheduleChanged(string machineId) =>
        await Clients.All.SendAsync("ScheduleChanged", new { MachineId = machineId });
}
