using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Implements the <see cref="ISignalRCustomerService"/> to provide real-time customer data updates using SignalR.
/// </summary>
public class SignalRCustomerService : ISignalRCustomerService
{
    private HubConnection? _hubConnection;
    private readonly NavigationManager _navigationManager;

    /// <inheritdoc />
    public event Func<Task>? OnCustomerChanged;

    /// <inheritdoc />
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRCustomerService"/> class.
    /// </summary>
    /// <param name="navigationManager">The navigation manager used to resolve the SignalR hub URL.</param>
    public SignalRCustomerService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    /// <inheritdoc />
    public async Task StartAsync()
    {
        // Only connect to SignalR when running in the browser (client-side)
        // Skip during server-side prerendering
        if (!OperatingSystem.IsBrowser())
        {
            return;
        }

        if (_hubConnection != null) return;

        var hubUrl = _navigationManager.BaseUri.TrimEnd('/') + "/hubs/notifications";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        // Subscribe to customer change events
        _hubConnection.On("CustomerChanged", async () =>
        {
            if (OnCustomerChanged != null)
                await OnCustomerChanged.Invoke();
        });

        await _hubConnection.StartAsync();
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
