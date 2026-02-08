using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Maliev.Intranet.Client.Services;

public class SignalRCustomerService : ISignalRCustomerService
{
    private HubConnection? _hubConnection;
    private readonly NavigationManager _navigationManager;

    public event Func<Task>? OnCustomerChanged;
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public SignalRCustomerService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

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

    public async Task StopAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
