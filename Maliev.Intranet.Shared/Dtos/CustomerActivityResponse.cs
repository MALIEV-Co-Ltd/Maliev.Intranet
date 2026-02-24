using System.Text.Json.Serialization;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Represents a single activity event for a customer
/// </summary>
public record CustomerActivityResponse
{
    /// <summary>Gets or sets the action performed (e.g., Created, Updated).</summary>
    [JsonPropertyName("action")]
    public string Action { get; init; } = string.Empty;

    /// <summary>Gets or sets the detailed description of the activity.</summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets or sets the identifier of the actor who performed the action.</summary>
    [JsonPropertyName("actorId")]
    public string ActorId { get; init; } = string.Empty;

    /// <summary>Gets or sets the name of the actor.</summary>
    [JsonPropertyName("actorName")]
    public string? ActorName { get; init; }

    /// <summary>Gets or sets the email of the actor.</summary>
    [JsonPropertyName("actorEmail")]
    public string? ActorEmail { get; init; }

    /// <summary>Gets or sets the timestamp of the activity.</summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }

    /// <summary>Gets or sets additional technical details or metadata about the activity.</summary>
    [JsonPropertyName("details")]
    public string? Details { get; init; }
}
