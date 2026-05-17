using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Data transfer object representing a user's stored preferences within a specific scope.
/// </summary>
public sealed record UserPreferenceDto
{
    /// <summary>The identifier of the user (principal) who owns these preferences.</summary>
    public Guid PrincipalId { get; set; }

    /// <summary>The application or module scope for these preferences (e.g., "dashboard", "ui-theme").</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>The collection of key-value pairs representing the preference data.</summary>
    public Dictionary<string, object> PreferenceData { get; set; } = [];

    /// <summary>The timestamp when these preferences were last updated.</summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request payload for creating or updating a user's preferences.
/// </summary>
public sealed record UpsertPreferenceRequest
{
    /// <summary>The scope defining the context of the preference update.</summary>
    [Required]
    public string Scope { get; set; } = string.Empty;

    /// <summary>The dictionary of preference keys and their corresponding values to store.</summary>
    [Required]
    public Dictionary<string, object> PreferenceData { get; set; } = [];
}
