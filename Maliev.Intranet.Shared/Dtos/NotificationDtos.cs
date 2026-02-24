using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// DTO for a user's notification preferences.
/// </summary>
public sealed record UserNotificationPreferenceDto
{
    /// <summary>Gets or sets the unique user identifier.</summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>Gets or sets the primary notification channel (e.g., Email, SMS).</summary>
    public string PrimaryChannelType { get; set; } = string.Empty;
    /// <summary>Gets or sets the list of fallback channels in order of preference.</summary>
    public List<string> FallbackChannelTypes { get; set; } = new();
    /// <summary>Gets or sets the categories the user has opted out of.</summary>
    public List<string> OptOutCategories { get; set; } = new();
    /// <summary>Gets or sets the collection of active channel bindings.</summary>
    public List<ChannelBindingDto> Bindings { get; set; } = new();
}

/// <summary>
/// DTO for a notification channel binding.
/// </summary>
public sealed record ChannelBindingDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the channel type.</summary>
    public string ChannelType { get; set; } = string.Empty;
    /// <summary>Gets or sets the channel-specific identifier (e.g., email address, phone number).</summary>
    public string ChannelIdentifier { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether the binding is currently valid.</summary>
    public bool IsValid { get; set; }
    /// <summary>Gets or sets the timestamp when the binding was invalidated.</summary>
    public DateTimeOffset? InvalidatedAt { get; set; }
    /// <summary>Gets or sets the reason for invalidation.</summary>
    public string? InvalidatedReason { get; set; }
}

/// <summary>
/// Request to update notification preferences.
/// </summary>
public sealed record UpdateNotificationPreferenceRequest
{
    /// <summary>Gets or sets the primary channel type.</summary>
    public string PrimaryChannelType { get; set; } = string.Empty;
    /// <summary>Gets or sets the fallback channel types.</summary>
    public List<string> FallbackChannelTypes { get; set; } = new();
    /// <summary>Gets or sets the opt-out categories.</summary>
    public List<string> OptOutCategories { get; set; } = new();
}

/// <summary>
/// DTO for a notification template.
/// </summary>
public sealed record NotificationTemplateDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the unique template key identifier.</summary>
    public string TemplateKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the template content with {{parameter}} placeholders.</summary>
    public string ContentTemplate { get; set; } = string.Empty;
    /// <summary>Gets or sets the list of required parameter names.</summary>
    public string[] Parameters { get; set; } = [];
    /// <summary>Gets or sets the target channel type.</summary>
    public string ChannelType { get; set; } = string.Empty;
    /// <summary>Gets or sets the template language.</summary>
    public string Language { get; set; } = "en";
    /// <summary>Gets or sets the template version number.</summary>
    public int Version { get; set; } = 1;
    /// <summary>Gets or sets when the template was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Gets or sets when the template was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Request to create a notification template.
/// </summary>
public sealed record CreateNotificationTemplateRequest
{
    /// <summary>Gets or sets the unique template key.</summary>
    [Required]
    public string TemplateKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the template content with {{parameter}} placeholders.</summary>
    [Required]
    public string ContentTemplate { get; set; } = string.Empty;

    /// <summary>Gets or sets the list of required parameter names.</summary>
    [Required]
    public string[] Parameters { get; set; } = [];

    /// <summary>Gets or sets the channel type.</summary>
    [Required]
    public string ChannelType { get; set; } = string.Empty;

    /// <summary>Gets or sets the language.</summary>
    [Required]
    public string Language { get; set; } = "en";

    /// <summary>Gets or sets the version number.</summary>
    public int Version { get; set; } = 1;
}

/// <summary>
/// DTO for a notification delivery log.
/// </summary>
public sealed record NotificationDeliveryLogDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the target user identifier.</summary>
    public string? UserId { get; set; }
    /// <summary>Gets or sets the identifier of the event that triggered the notification.</summary>
    public string? EventId { get; set; }
    /// <summary>Gets or sets the channel type used for delivery.</summary>
    public string ChannelType { get; set; } = string.Empty;
    /// <summary>Gets or sets the recipient identifier.</summary>
    public string Recipient { get; set; } = string.Empty;
    /// <summary>Gets or sets the subject line sent.</summary>
    public string? Subject { get; set; }
    /// <summary>Gets or sets the delivery status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets any error message during delivery.</summary>
    public string? Error { get; set; }
    /// <summary>Gets or sets the number of retry attempts.</summary>
    public int RetryCount { get; set; }
    /// <summary>Gets or sets the log creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the actual delivery timestamp.</summary>
    public DateTime? DeliveredAt { get; set; }
}

/// <summary>
/// Request to update a notification template.
/// </summary>
public sealed record UpdateNotificationTemplateRequest
{
    /// <summary>Gets or sets the unique template key identifier.</summary>
    public string? TemplateKey { get; set; }
    /// <summary>Gets or sets the template content with {{parameter}} placeholders.</summary>
    public string? ContentTemplate { get; set; }
    /// <summary>Gets or sets the list of required parameter names.</summary>
    public string[]? Parameters { get; set; }
    /// <summary>Gets or sets the target channel type.</summary>
    public string? ChannelType { get; set; }
    /// <summary>Gets or sets the template language.</summary>
    public string? Language { get; set; }
    /// <summary>Gets or sets the template version number.</summary>
    public int? Version { get; set; }
}
