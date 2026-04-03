using Maliev.Intranet.Client.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Client-side service that maintains a SignalR connection to the BFF <c>ProductionHub</c>
/// and exposes events for job status and stats changes consumed by the Production Queue page.
/// Forwards authentication cookies when running server-side (InteractiveServer mode).
/// </summary>
public sealed class ProductionHubService : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly NavigationManager _navigation;
    private readonly CookieProvider _cookieProvider;

    /// <summary>Fired when a job's status changes. Args: jobId, newStatus.</summary>
    public event Action<Guid, string>? JobStatusChanged;

    /// <summary>Fired when a job machine assignment changes. Args: jobId, machineId.</summary>
    public event Action<Guid, Guid>? JobAssigned;

    /// <summary>Fired when production stats are updated — signals callers to refresh stats.</summary>
    public event Action? ProductionStatsUpdated;

    /// <summary>Gets the current hub connection state.</summary>
    public HubConnectionState State => _connection?.State ?? HubConnectionState.Disconnected;

    /// <summary>
    /// Initializes a new instance of <see cref="ProductionHubService"/>.
    /// </summary>
    /// <param name="navigation">The Blazor NavigationManager used to resolve the hub URL.</param>
    /// <param name="cookieProvider">Provides cookies captured during SSR for server-side hub auth.</param>
    public ProductionHubService(NavigationManager navigation, CookieProvider cookieProvider)
    {
        _navigation = navigation;
        _cookieProvider = cookieProvider;
    }

    /// <summary>
    /// Starts the SignalR connection to the production hub and registers event handlers.
    /// Only connects in the browser (guards against SSR prerender). Safe to call multiple times.
    /// </summary>
    /// <returns>A task that completes when the connection is established (or immediately when not in browser).</returns>
    public async Task StartAsync()
    {
        if (_connection is { State: HubConnectionState.Connected or HubConnectionState.Connecting })
            return;

        _connection = new HubConnectionBuilder()
            .WithUrlAndCookies(_navigation.ToAbsoluteUri("/hubs/production").ToString(), _cookieProvider.CookieHeader)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<JobStatusChangedPayload>("JobStatusChanged", payload =>
            JobStatusChanged?.Invoke(payload.JobId, payload.Status));

        _connection.On<JobAssignedPayload>("JobAssigned", payload =>
            JobAssigned?.Invoke(payload.JobId, payload.MachineId));

        _connection.On("ProductionStatsUpdated", () =>
            ProductionStatsUpdated?.Invoke());

        await _connection.StartAsync();
    }

    /// <summary>
    /// Stops the SignalR connection gracefully.
    /// </summary>
    /// <returns>A task that completes when the connection is stopped.</returns>
    public async Task StopAsync()
    {
        if (_connection != null)
            await _connection.StopAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
            await _connection.DisposeAsync();
    }

    // ── Payload records (match server-side anonymous object shapes) ───────────

    private sealed record JobStatusChangedPayload(Guid JobId, string Status);
    private sealed record JobAssignedPayload(Guid JobId, Guid MachineId);
}
