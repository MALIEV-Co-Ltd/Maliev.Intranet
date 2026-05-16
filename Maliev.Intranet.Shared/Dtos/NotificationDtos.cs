using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// DTO for a user's notification preferences.
/// </summary>
public sealed record UserNotificationPreferenceDto
{
    /// <summary>
    /// The unique identifier of the user these preferences belong to.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The preferred primary channel for sending notifications (e.g., Email, SMS, Push).
    /// </summary>
    public string PrimaryChannelType { get; set; } = string.Empty;

    /// <summary>
    /// A list of secondary channels to use if the primary channel delivery fails.
    /// </summary>
    public List<string> FallbackChannelTypes { get; set; } = new();

    /// <summary>
    /// A list of notification categories that the user has opted out of.
    /// </summary>
    public List<string> OptOutCategories { get; set; } = new();

    /// <summary>
    /// The configured channel bindings (e.g., email addresses, phone numbers) for this user.
    /// </summary>
    public List<ChannelBindingDto> Bindings { get; set; } = new();
}

/// <summary>
/// DTO for a notification channel binding.
/// </summary>
public sealed record ChannelBindingDto
{
    /// <summary>
    /// The unique identifier of this channel binding.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The type of notification channel (e.g., Email, SMS).
    /// </summary>
    public string ChannelType { get; set; } = string.Empty;

    /// <summary>
    /// The channel-specific identifier, such as an email address or mobile phone number.
    /// </summary>
    public string ChannelIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this channel binding is currently valid and usable for notification delivery.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// The date and time when this channel was invalidated, if applicable.
    /// </summary>
    public DateTimeOffset? InvalidatedAt { get; set; }

    /// <summary>
    /// The reason why this channel was invalidated (e.g., hard bounce, user request).
    /// </summary>
    public string? InvalidatedReason { get; set; }
}

/// <summary>
/// Request to update notification preferences.
/// </summary>
public sealed record UpdateNotificationPreferenceRequest
{
    /// <summary>
    /// The new primary channel for sending notifications.
    /// </summary>
    public string PrimaryChannelType { get; set; } = string.Empty;

    /// <summary>
    /// The updated list of fallback channels to use on delivery failure.
    /// </summary>
    public List<string> FallbackChannelTypes { get; set; } = new();

    /// <summary>
    /// The updated list of categories to opt out from.
    /// </summary>
    public List<string> OptOutCategories { get; set; } = new();
}

/// <summary>
/// DTO for a notification template.
/// </summary>
public sealed record NotificationTemplateDto
{
    /// <summary>
    /// The unique identifier of this template.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The display name of the notification template.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The unique system key used to identify this template programmatically.
    /// </summary>
    public string TemplateKey { get; set; } = string.Empty;

    /// <summary>
    /// The template string for the notification subject or title.
    /// </summary>
    public string SubjectTemplate { get; set; } = string.Empty;

    /// <summary>
    /// The template string for the notification body content.
    /// </summary>
    public string BodyTemplate { get; set; } = string.Empty;

    /// <summary>
    /// The channel type this template is designed for.
    /// </summary>
    public string ChannelType { get; set; } = string.Empty;

    /// <summary>
    /// The ISO 639-1 language code for this template.
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// The version number of the template.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Indicates whether this template version is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for a notification delivery log.
/// </summary>
public sealed record NotificationDeliveryLogDto
{
    /// <summary>
    /// The unique identifier of the delivery log entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The unique identifier of the recipient user, if known.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// The unique identifier of the event that triggered this notification.
    /// </summary>
    public string? EventId { get; set; }

    /// <summary>
    /// The channel type through which the notification was sent.
    /// </summary>
    public string ChannelType { get; set; } = string.Empty;

    /// <summary>
    /// The actual recipient address or identifier (e.g., email address, phone number).
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// The subject line of the notification as it was sent.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// The current status of the notification delivery (e.g., Pending, Sent, Failed).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Error message detailing why delivery failed, if applicable.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Provider-specific message identifier or local simulated provider identifier.
    /// </summary>
    public string? ProviderMessageId { get; set; }

    /// <summary>
    /// The number of delivery retry attempts performed.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// The date and time when the delivery request was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The date and time when the notification was successfully delivered.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }
}

/// <summary>
/// Request to create a notification template.
/// </summary>
public sealed record CreateNotificationTemplateRequest
{
    /// <summary>
    /// The display name for the new template.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The unique system key for the template.
    /// </summary>
    [Required]
    public string TemplateKey { get; set; } = string.Empty;

    /// <summary>
    /// The template content for the notification subject.
    /// </summary>
    [Required]
    public string SubjectTemplate { get; set; } = string.Empty;

    /// <summary>
    /// The template content for the notification body.
    /// </summary>
    [Required]
    public string BodyTemplate { get; set; } = string.Empty;

    /// <summary>
    /// The target delivery channel for this template.
    /// </summary>
    [Required]
    public string ChannelType { get; set; } = string.Empty;

    /// <summary>
    /// The language for the template. Defaults to English.
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// The initial version number for the template.
    /// </summary>
    public int Version { get; set; } = 1;
}

/// <summary>
/// Request to update a notification template.
/// </summary>
public sealed record UpdateNotificationTemplateRequest
{
    /// <summary>
    /// The updated display name for the template.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The updated subject template content.
    /// </summary>
    public string? SubjectTemplate { get; set; }

    /// <summary>
    /// The updated body template content.
    /// </summary>
    public string? BodyTemplate { get; set; }

    /// <summary>
    /// Whether to activate or deactivate this template version.
    /// </summary>
    public bool? IsActive { get; set; }
}
