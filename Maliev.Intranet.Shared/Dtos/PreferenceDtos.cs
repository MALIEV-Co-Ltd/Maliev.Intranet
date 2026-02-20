using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

public sealed record UserPreferenceDto
{
    public Guid PrincipalId { get; set; }
    public string Scope { get; set; } = string.Empty;
    // Requirement says "Dictionary<string, object> or JsonElement"
    public Dictionary<string, object> PreferenceData { get; set; } = [];
}

public sealed record UpsertPreferenceRequest
{
    [Required]
    public string Scope { get; set; } = string.Empty;

    [Required]
    public Dictionary<string, object> PreferenceData { get; set; } = [];
}
