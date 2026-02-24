namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Service for receiving real-time customer data updates via SignalR.
/// </summary>
public interface ISignalRCustomerService : IAsyncDisposable
{
    /// <summary>Event raised when a customer record has changed.</summary>
    event Func<Task> OnCustomerChanged;
    /// <summary>Starts the SignalR connection.</summary>
    Task StartAsync();
    /// <summary>Stops the SignalR connection.</summary>
    Task StopAsync();
    /// <summary>Gets a value indicating whether the service is connected.</summary>
    bool IsConnected { get; }
}
