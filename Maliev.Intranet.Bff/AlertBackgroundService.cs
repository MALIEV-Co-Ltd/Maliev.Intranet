namespace Maliev.Intranet.Bff;

/// <summary>
/// Background service that periodically broadcasts system health alerts via SignalR.
/// </summary>
/// <param name="lifetime">The application lifetime.</param>
public class AlertBackgroundService(
    IHostApplicationLifetime lifetime) : BackgroundService
{
    /// <summary>
    /// Executes the background task logic.
    /// </summary>
    /// <param name="stoppingToken">Triggered when the host is shutting down.</param>
    /// <returns>A task that represents the background operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait until the application has fully started
        var tcs = new TaskCompletionSource();
        using var registration = lifetime.ApplicationStarted.Register(() => tcs.SetResult());
        await tcs.Task;

        while (!stoppingToken.IsCancellationRequested)
        {
            // System health status broadcast removed to prevent notification spam.
            // Health status is now visible in the Admin -> System Health dashboard.

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
