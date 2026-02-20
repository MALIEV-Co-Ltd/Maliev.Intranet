using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Interface for interacting with the Notification microservice.
/// </summary>
public interface INotificationServiceClient
{
    /// <summary>
    /// Gets notification preferences for a specific user.
    /// </summary>
    Task<UserNotificationPreferenceDto?> GetPreferencesAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Updates notification preferences for a specific user.
    /// </summary>
    Task<UserNotificationPreferenceDto?> UpdatePreferencesAsync(string userId, UpdateNotificationPreferenceRequest request, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a paged list of notification templates.
    /// </summary>
    Task<PagedResponse<NotificationTemplateDto>?> GetTemplatesAsync(int page = 1, int pageSize = 20, string? filter = null, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a single notification template by ID.
    /// </summary>
    Task<NotificationTemplateDto?> GetTemplateByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new notification template.
    /// </summary>
    Task<NotificationTemplateDto?> CreateTemplateAsync(CreateNotificationTemplateRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing notification template.
    /// </summary>
    Task<NotificationTemplateDto?> UpdateTemplateAsync(Guid id, UpdateNotificationTemplateRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a notification template.
    /// </summary>
    Task DeleteTemplateAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a paged list of notification delivery logs.
    /// </summary>
    Task<PagedResponse<NotificationDeliveryLogDto>?> GetDeliveryLogsAsync(int page = 1, int pageSize = 20, string? filter = null, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a single notification delivery log by ID.
    /// </summary>
    Task<NotificationDeliveryLogDto?> GetDeliveryLogByIdAsync(Guid id, CancellationToken ct = default);
}
