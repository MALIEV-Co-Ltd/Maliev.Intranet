using System.Text.Json.Serialization;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Represents a single activity event for a customer
/// </summary>
public record CustomerActivityResponse
{
    [JsonPropertyName("action")]
    public string Action { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("actorId")]
    public string ActorId { get; init; } = string.Empty;

    [JsonPropertyName("actorName")]
    public string? ActorName { get; init; }

    [JsonPropertyName("actorEmail")]
    public string? ActorEmail { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }

    [JsonPropertyName("details")]
    public string? Details { get; init; }
}
