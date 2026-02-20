using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// DTO for a user's notification preferences.
/// </summary>
public sealed record UserNotificationPreferenceDto
{
    public string UserId { get; set; } = string.Empty;
    public string PrimaryChannelType { get; set; } = string.Empty;
    public List<string> FallbackChannelTypes { get; set; } = new();
    public List<string> OptOutCategories { get; set; } = new();
    public List<ChannelBindingDto> Bindings { get; set; } = new();
}

/// <summary>
/// DTO for a notification channel binding.
/// </summary>
public sealed record ChannelBindingDto
{
    public Guid Id { get; set; }
    public string ChannelType { get; set; } = string.Empty;
    public string ChannelIdentifier { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public DateTimeOffset? InvalidatedAt { get; set; }
    public string? InvalidatedReason { get; set; }
}

/// <summary>
/// Request to update notification preferences.
/// </summary>
public sealed record UpdateNotificationPreferenceRequest
{
    public string PrimaryChannelType { get; set; } = string.Empty;
    public List<string> FallbackChannelTypes { get; set; } = new();
    public List<string> OptOutCategories { get; set; } = new();
}

/// <summary>
/// DTO for a notification template.
/// </summary>
public sealed record NotificationTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TemplateKey { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public string ChannelType { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for a notification delivery log.
/// </summary>
public sealed record NotificationDeliveryLogDto
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public string? EventId { get; set; }
    public string ChannelType { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

/// <summary>
/// Request to create a notification template.
/// </summary>
public sealed record CreateNotificationTemplateRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string TemplateKey { get; set; } = string.Empty;

    [Required]
    public string SubjectTemplate { get; set; } = string.Empty;

    [Required]
    public string BodyTemplate { get; set; } = string.Empty;

    [Required]
    public string ChannelType { get; set; } = string.Empty;

    public string Language { get; set; } = "en";
    public int Version { get; set; } = 1;
}

/// <summary>
/// Request to update a notification template.
/// </summary>
public sealed record UpdateNotificationTemplateRequest
{
    public string? Name { get; set; }
    public string? SubjectTemplate { get; set; }
    public string? BodyTemplate { get; set; }
    public bool? IsActive { get; set; }
}
