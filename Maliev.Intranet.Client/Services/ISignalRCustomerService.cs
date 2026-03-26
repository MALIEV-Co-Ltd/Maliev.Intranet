namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Defines a SignalR client service for receiving real-time customer data updates from the CustomerService.
/// </summary>
public interface ISignalRCustomerService : IAsyncDisposable
{
    /// <summary>
    /// Raised when a customer record has been created, updated, or deleted.
    /// </summary>
    event Func<Task> OnCustomerChanged;

    /// <summary>
    /// Starts the SignalR hub connection to begin receiving customer updates.
    /// </summary>
    /// <returns>A task representing the asynchronous start operation.</returns>
    Task StartAsync();

    /// <summary>
    /// Stops the SignalR hub connection.
    /// </summary>
    /// <returns>A task representing the asynchronous stop operation.</returns>
    Task StopAsync();

    /// <summary>
    /// Gets a value indicating whether the service is currently connected to the customer hub.
    /// </summary>
    bool IsConnected { get; }
}
