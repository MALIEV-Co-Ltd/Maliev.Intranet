using System.Text.Json.Serialization;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Represents a single activity event for a customer, such as an order placement or profile update.
/// </summary>
public record CustomerActivityResponse
{
    /// <summary>
    /// The name of the action or event performed (e.g., "OrderCreated", "NDA_Signed").
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; init; } = string.Empty;

    /// <summary>
    /// A human-readable description of the activity.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// The unique identifier of the user or system that performed the action.
    /// </summary>
    [JsonPropertyName("actorId")]
    public string ActorId { get; init; } = string.Empty;

    /// <summary>
    /// The display name of the actor who performed the action.
    /// </summary>
    [JsonPropertyName("actorName")]
    public string? ActorName { get; init; }

    /// <summary>
    /// The email address of the actor who performed the action.
    /// </summary>
    [JsonPropertyName("actorEmail")]
    public string? ActorEmail { get; init; }

    /// <summary>
    /// The timestamp indicating when the activity occurred.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Optional JSON-formatted string containing additional technical details about the activity.
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details { get; init; }
}
