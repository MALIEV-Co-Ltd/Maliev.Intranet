using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Hubs;

/// <summary>
/// SignalR hub for broadcasting real-time production queue updates to connected Intranet clients.
/// Clients subscribe to this hub to receive live job status changes and stats refreshes
/// without polling the BFF API.
/// </summary>
[RequirePermission(MalievPermissions.Job.Read, AuthenticationSchemes = "Bearer,Cookies")]
public class ProductionHub : Hub
{
}
