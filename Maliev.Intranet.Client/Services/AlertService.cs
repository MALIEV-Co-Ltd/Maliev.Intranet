using Maliev.Intranet.Shared.Dtos;
using System.Net.Http.Json;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Scoped singleton that holds the current employee's unread alerts in memory.
/// Loaded once from the BFF on startup via <see cref="InitializeAsync"/>.
/// Real-time updates arrive via SignalR calls to <see cref="AddAlert"/>.
/// </summary>
public class AlertService(HttpClient http, ILogger<AlertService> logger)
{
    private readonly List<AlertSummaryDto> _alerts = [];

    /// <summary>Read-only snapshot of the current in-memory unread alerts list.</summary>
    public IReadOnlyList<AlertSummaryDto> Alerts => _alerts.AsReadOnly();

    /// <summary>Number of unread alerts currently in memory.</summary>
    public int UnreadCount => _alerts.Count;

    /// <summary>
    /// Fired whenever the alerts list changes (add, mark-read, mark-all-read).
    /// Subscribers call <c>StateHasChanged()</c> in response.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Loads unread alerts from the BFF. Called once from MainLayout after the
    /// SignalR hub connection starts. Safe to call multiple times — each call
    /// replaces the in-memory list.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (!OperatingSystem.IsBrowser()) return;

        try
        {
            using var response = await http.GetAsync("api/v1/alerts");
            if (response.IsSuccessStatusCode)
            {
                var alerts = await response.Content.ReadFromJsonAsync<List<AlertSummaryDto>>();
                _alerts.Clear();
                if (alerts is not null)
                    _alerts.AddRange(alerts);
                Changed?.Invoke(this, EventArgs.Empty);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                logger.LogDebug("AlertService: user not authenticated — alerts will load after login");
            }
            else
            {
                logger.LogWarning("AlertService: BFF returned {StatusCode} — showing empty panel", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AlertService: failed to load alerts from BFF — showing empty panel");
        }
    }

    /// <summary>
    /// Prepends a real-time alert received from the SignalR <c>ReceiveAlert</c> handler.
    /// </summary>
    public void AddAlert(AlertSummaryDto alert)
    {
        _alerts.Insert(0, alert);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Marks a single alert as read: calls the BFF and removes it from the in-memory list.
    /// If the HTTP call fails the alert stays in the list (safe failure, no data loss).
    /// </summary>
    public async Task MarkReadAsync(Guid id)
    {
        try
        {
            var response = await http.PostAsync($"api/v1/alerts/{id}/read", null);
            if (response.IsSuccessStatusCode)
            {
                _alerts.RemoveAll(a => a.Id == id);
                Changed?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                logger.LogWarning("AlertService.MarkReadAsync: BFF returned {StatusCode} for alert {Id}", response.StatusCode, id);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AlertService.MarkReadAsync: HTTP call failed for alert {Id} — keeping alert unread", id);
        }
    }

    /// <summary>
    /// Marks all current alerts as read: calls the BFF and clears the in-memory list.
    /// If the HTTP call fails the list is preserved (safe failure).
    /// </summary>
    public async Task MarkAllReadAsync()
    {
        try
        {
            var response = await http.PostAsync("api/v1/alerts/read-all", null);
            if (response.IsSuccessStatusCode)
            {
                _alerts.Clear();
                Changed?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                logger.LogWarning("AlertService.MarkAllReadAsync: BFF returned {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AlertService.MarkAllReadAsync: HTTP call failed — keeping alerts unread");
        }
    }
}
