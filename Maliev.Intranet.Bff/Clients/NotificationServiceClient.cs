using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.MessagingContracts.Contracts.Shared;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Notification microservice.
/// </summary>
public class NotificationServiceClient(HttpClient httpClient) : INotificationServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

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
    /// Dispatches a notification event through NotificationService.
    /// </summary>
    public async Task DispatchEventAsync(NotificationEvent notificationEvent, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/notification/v1/events", notificationEvent, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
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

        var response = await httpClient.GetFromJsonAsync<DownstreamPagedResponse<DownstreamTemplate>>(url, JsonOptions, ct);
        return response?.ToPagedResponse(page, pageSize, template => template.ToDto());
    }

    /// <summary>
    /// Gets a notification template by ID.
    /// </summary>
    public async Task<NotificationTemplateDto?> GetTemplateByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<DownstreamTemplate>($"/notification/v1/templates/{id}", JsonOptions, ct);
        return response?.ToDto();
    }

    /// <summary>
    /// Creates a new notification template.
    /// </summary>
    public async Task<NotificationTemplateDto?> CreateTemplateAsync(CreateNotificationTemplateRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/notification/v1/templates", new
        {
            templateKey = request.TemplateKey,
            version = request.Version,
            language = request.Language,
            channelType = ChannelTypeValue(request.ChannelType),
            contentTemplate = request.BodyTemplate,
            parameters = ExtractParameters(request.BodyTemplate)
        }, JsonOptions, ct);
        if (response.IsSuccessStatusCode)
        {
            var template = await response.Content.ReadFromJsonAsync<DownstreamTemplate>(JsonOptions, ct);
            return template?.ToDto();
        }
        return null;
    }

    /// <summary>
    /// Updates an existing notification template.
    /// </summary>
    public async Task<NotificationTemplateDto?> UpdateTemplateAsync(Guid id, UpdateNotificationTemplateRequest request, CancellationToken ct = default)
    {
        var bodyTemplate = request.BodyTemplate ?? string.Empty;
        var response = await httpClient.PutAsJsonAsync($"/notification/v1/templates/{id}", new
        {
            contentTemplate = bodyTemplate,
            parameters = ExtractParameters(bodyTemplate)
        }, JsonOptions, ct);
        if (response.IsSuccessStatusCode)
        {
            var template = await response.Content.ReadFromJsonAsync<DownstreamTemplate>(JsonOptions, ct);
            return template?.ToDto();
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
        var response = await httpClient.GetFromJsonAsync<DownstreamPagedResponse<DownstreamDeliveryLog>>(url, JsonOptions, ct);
        return response?.ToPagedResponse(page, pageSize, log => log.ToDto());
    }

    /// <summary>
    /// Gets a notification delivery log by ID.
    /// </summary>
    public async Task<NotificationDeliveryLogDto?> GetDeliveryLogByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<NotificationDeliveryLogDto>($"/notification/v1/delivery-logs/{id}", ct);
    }

    private static int ChannelTypeValue(string? channelType)
    {
        return channelType?.Trim().ToLowerInvariant() switch
        {
            "line" => 1,
            "whatsapp" => 2,
            "sms" => 3,
            "slack" => 4,
            "facebook" => 5,
            "instagram" => 6,
            _ => 0
        };
    }

    private static string ChannelTypeName(JsonElement channelType)
    {
        if (channelType.ValueKind == JsonValueKind.String)
        {
            return channelType.GetString() ?? string.Empty;
        }

        if (channelType.ValueKind == JsonValueKind.Number && channelType.TryGetInt32(out var value))
        {
            return value switch
            {
                1 => "line",
                2 => "whatsapp",
                3 => "sms",
                4 => "slack",
                5 => "facebook",
                6 => "instagram",
                _ => "email"
            };
        }

        return "email";
    }

    private static string[] ExtractParameters(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return [];
        }

        return Regex.Matches(template, @"\{\{(\w+)\}\}")
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private sealed class DownstreamPagedResponse<T>
    {
        public List<T> Items { get; set; } = [];

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public PagedResponse<TDto> ToPagedResponse<TDto>(int fallbackPage, int fallbackPageSize, Func<T, TDto> map) => new()
        {
            Data = Items.Select(map).ToList(),
            Meta = new PaginationMeta
            {
                CurrentPage = Page == 0 ? fallbackPage : Page,
                PageSize = PageSize == 0 ? fallbackPageSize : PageSize,
                TotalCount = TotalCount,
                TotalItems = TotalCount,
                TotalPages = TotalPages
            }
        };
    }

    private sealed class DownstreamTemplate
    {
        public Guid Id { get; set; }

        public string TemplateKey { get; set; } = string.Empty;

        public int Version { get; set; }

        public string Language { get; set; } = string.Empty;

        public JsonElement ChannelType { get; set; }

        public string ContentTemplate { get; set; } = string.Empty;

        public NotificationTemplateDto ToDto() => new()
        {
            Id = Id,
            Name = TemplateKey,
            TemplateKey = TemplateKey,
            SubjectTemplate = string.Empty,
            BodyTemplate = ContentTemplate,
            ChannelType = ChannelTypeName(ChannelType),
            Language = Language,
            Version = Version,
            IsActive = true
        };
    }

    private sealed class DownstreamDeliveryLog
    {
        public Guid Id { get; set; }

        public string EventId { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public string ChannelType { get; set; } = string.Empty;

        public string RecipientIdentifier { get; set; } = string.Empty;

        public string? MessageContent { get; set; }

        public string? ProviderResponse { get; set; }

        public string? ProviderMessageId { get; set; }

        public int AttemptNumber { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTimeOffset? DeliveredAt { get; set; }

        public NotificationDeliveryLogDto ToDto() => new()
        {
            Id = Id,
            EventId = EventId,
            UserId = UserId,
            ChannelType = ChannelType,
            Recipient = RecipientIdentifier,
            Subject = MessageContent,
            Status = Status,
            Error = ProviderResponse,
            ProviderMessageId = ProviderMessageId,
            RetryCount = Math.Max(0, AttemptNumber - 1),
            CreatedAt = CreatedAt.UtcDateTime,
            DeliveredAt = DeliveredAt?.UtcDateTime
        };
    }
}
