using Microsoft.AspNetCore.SignalR;
using Maliev.Intranet.Bff.Hubs;

namespace Maliev.Intranet.Bff;

/// <summary>
/// Background service that periodically broadcasts system health alerts via SignalR.
/// </summary>
/// <param name="hubContext">The SignalR hub context for notifications.</param>
/// <param name="lifetime">The application lifetime.</param>
/// <param name="logger">The logger instance.</param>
public class AlertBackgroundService(
    IHubContext<NotificationHub> hubContext,
    IHostApplicationLifetime lifetime,
    ILogger<AlertBackgroundService> logger) : BackgroundService
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
            logger.LogInformation("Broadcasting system health status via SignalR");

            await hubContext.Clients.All.SendAsync("ReceiveNotification",
                $"System Update: All microservices operating within normal parameters at {DateTime.Now:HH:mm:ss}.",
                stoppingToken);

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}