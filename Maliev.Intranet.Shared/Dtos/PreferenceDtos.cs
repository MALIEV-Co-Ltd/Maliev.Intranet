using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Data transfer object for user preferences.
/// </summary>
public sealed record UserPreferenceDto
{
    /// <summary>Gets or sets the target principal ID.</summary>
    public Guid PrincipalId { get; set; }
    /// <summary>Gets or sets the preference scope (e.g., UI, Notifications).</summary>
    public string Scope { get; set; } = string.Empty;
    /// <summary>Gets or sets the structured preference data.</summary>
    public Dictionary<string, object> PreferenceData { get; set; } = [];
}

/// <summary>
/// Request model for creating or updating user preferences.
/// </summary>
public sealed record UpsertPreferenceRequest
{
    /// <summary>Gets or sets the preference scope.</summary>
    [Required]
    public string Scope { get; set; } = string.Empty;

    /// <summary>Gets or sets the structured preference data.</summary>
    [Required]
    public Dictionary<string, object> PreferenceData { get; set; } = [];
}
