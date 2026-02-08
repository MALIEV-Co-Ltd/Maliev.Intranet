namespace Maliev.Intranet.Client.Services;

public interface ISignalRCustomerService : IAsyncDisposable
{
    event Func<Task> OnCustomerChanged;
    Task StartAsync();
    Task StopAsync();
    bool IsConnected { get; }
}
