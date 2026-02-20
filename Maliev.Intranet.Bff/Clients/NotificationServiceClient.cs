using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Notification microservice.
/// </summary>
public class NotificationServiceClient(HttpClient httpClient) : INotificationServiceClient
{
    /// <summary>
    /// Gets preferences for a user.
    /// </summary>
    public async Task<UserNotificationPreferenceDto?> GetPreferencesAsync(string userId, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<UserNotificationPreferenceDto>($"/notification/v1/preferences/{userId}", ct);
    }

    /// <summary>
    /// Updates preferences for a user.
    /// </summary>
    public async Task<UserNotificationPreferenceDto?> UpdatePreferencesAsync(string userId, UpdateNotificationPreferenceRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/notification/v1/preferences/{userId}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserNotificationPreferenceDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Lists notification templates.
    /// </summary>
    public async Task<PagedResponse<NotificationTemplateDto>?> GetTemplatesAsync(int page = 1, int pageSize = 20, string? filter = null, CancellationToken ct = default)
    {
        var url = $"/notification/v1/templates?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(filter))
        {
            url += $"&filter={Uri.EscapeDataString(filter)}";
        }
        return await httpClient.GetFromJsonAsync<PagedResponse<NotificationTemplateDto>>(url, ct);
    }

    /// <summary>
    /// Gets a notification template by ID.
    /// </summary>
    public async Task<NotificationTemplateDto?> GetTemplateByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<NotificationTemplateDto>($"/notification/v1/templates/{id}", ct);
    }

    /// <summary>
    /// Creates a new notification template.
    /// </summary>
    public async Task<NotificationTemplateDto?> CreateTemplateAsync(CreateNotificationTemplateRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/notification/v1/templates", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<NotificationTemplateDto>(cancellationToken: ct);
        }
        return null;
    }

    /// <summary>
    /// Updates an existing notification template.
    /// </summary>
    public async Task<NotificationTemplateDto?> UpdateTemplateAsync(Guid id, UpdateNotificationTemplateRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/notification/v1/templates/{id}", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<NotificationTemplateDto>(cancellationToken: ct);
        }
        return null;
    }

    /// <summary>
    /// Deletes a notification template.
    /// </summary>
    public async Task DeleteTemplateAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/notification/v1/templates/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Lists notification delivery logs.
    /// </summary>
    public async Task<PagedResponse<NotificationDeliveryLogDto>?> GetDeliveryLogsAsync(int page = 1, int pageSize = 20, string? filter = null, CancellationToken ct = default)
    {
        var url = $"/notification/v1/delivery-logs?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(filter))
        {
            url += $"&status={Uri.EscapeDataString(filter)}"; // Assuming filter maps to status or search
        }
        return await httpClient.GetFromJsonAsync<PagedResponse<NotificationDeliveryLogDto>>(url, ct);
    }

    /// <summary>
    /// Gets a notification delivery log by ID.
    /// </summary>
    public async Task<NotificationDeliveryLogDto?> GetDeliveryLogByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<NotificationDeliveryLogDto>($"/notification/v1/delivery-logs/{id}", ct);
    }
}
