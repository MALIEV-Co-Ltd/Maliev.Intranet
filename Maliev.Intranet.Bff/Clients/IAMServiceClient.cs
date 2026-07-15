using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Clients;

internal sealed record IamResolvePermissionsResult(
    Guid PrincipalId,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> Roles,
    string? ResourcePath,
    DateTime? CacheUntil,
    bool FromCache);

/// <summary>
/// Client for interacting with the IAM microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class IAMServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Retrieves all available permissions from the IAM service.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of permissions.</returns>
    public virtual async Task<List<PermissionDto>> GetPermissionsAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<List<PermissionDto>>("/iam/v1/permissions", ct);
        return response ?? new();
    }

    /// <summary>
    /// Retrieves all available roles from the IAM service.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of roles.</returns>
    public virtual async Task<List<RoleDto>> GetRolesAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<List<IamRoleResponse>>("/iam/v1/roles", ct);
        return response?.Select(MapRole).ToList() ?? new();
    }

    /// <summary>
    /// Retrieves all principals from the IAM service.
    /// </summary>
    public virtual async Task<List<PrincipalSummaryDto>> GetPrincipalsAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<List<IamPrincipalResponse>>("/iam/v1/principals", ct);
        return response?.Select(MapPrincipal).ToList() ?? new();
    }

    /// <summary>
    /// Retrieves a single principal by identifier.
    /// </summary>
    public virtual async Task<PrincipalSummaryDto?> GetPrincipalAsync(Guid principalId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/iam/v1/principals/{principalId}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var principal = await response.Content.ReadFromJsonAsync<IamPrincipalResponse>(cancellationToken: ct);
        return principal is null ? null : MapPrincipal(principal);
    }

    /// <summary>
    /// Creates a new human IAM principal.
    /// </summary>
    public virtual async Task<PrincipalSummaryDto?> CreatePrincipalAsync(string email, string displayName, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/iam/v1/principals", new
        {
            principalType = "user",
            email,
            displayName
        }, ct);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var created = await response.Content.ReadFromJsonAsync<CreatePrincipalResponse>(cancellationToken: ct);
        if (created is null)
        {
            return null;
        }

        return new PrincipalSummaryDto
        {
            Id = created.PrincipalId,
            PrincipalId = created.PrincipalId,
            Type = "user",
            Identifier = email,
            Email = email,
            DisplayName = displayName,
            IsActive = true,
            IsEnabled = true,
            CreatedAt = created.CreatedAt
        };
    }

    /// <summary>
    /// Gets the total count of principals for bootstrapping purposes.
    /// </summary>
    public virtual async Task<int> GetPrincipalCountAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<BootstrapStatusDto>("/iam/v1/principals/bootstrap/status", ct);
        return response?.Count ?? 0;
    }

    /// <summary>
    /// Promotes the current user to IAM Admin if it's the first user.
    /// </summary>
    public virtual async Task<bool> PromoteCallerToAdminAsync(CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync("/iam/v1/principals/bootstrap/promote", null, ct);
        return response.IsSuccessStatusCode;
    }

    private record BootstrapStatusDto(int Count);

    private sealed record CreatePrincipalResponse(Guid PrincipalId, DateTime CreatedAt);

    private sealed record IamPrincipalResponse(
        Guid PrincipalId,
        string PrincipalType,
        string? Email,
        string? DisplayName,
        string? LinkedService,
        Guid? LinkedEntityId,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private sealed record IamRoleResponse(
        string RoleId,
        string? ServiceName,
        string? Name,
        string? RoleName,
        string? Description,
        List<string>? Permissions,
        List<string>? PermissionIds);

    private sealed record IamResolvePermissionsRequest
    {
        [JsonPropertyName("principalId")]
        public required string PrincipalId { get; init; }

        [JsonPropertyName("resourcePath")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ResourcePath { get; init; }

        [JsonPropertyName("requestTime")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? RequestTime { get; init; }

        [JsonPropertyName("requestIp")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RequestIp { get; init; }
    }

    private sealed record IamResolvePermissionsResponse
    {
        [JsonPropertyName("principalId")]
        public required Guid PrincipalId { get; init; }

        [JsonPropertyName("permissions")]
        public required List<string> Permissions { get; init; }

        [JsonPropertyName("roles")]
        public required List<string> Roles { get; init; }

        [JsonPropertyName("resourcePath")]
        public string? ResourcePath { get; init; }

        [JsonPropertyName("cacheUntil")]
        public DateTime? CacheUntil { get; init; }

        [JsonPropertyName("fromCache")]
        public required bool FromCache { get; init; }
    }

    private static PrincipalSummaryDto MapPrincipal(IamPrincipalResponse principal) => new()
    {
        Id = principal.PrincipalId,
        PrincipalId = principal.PrincipalId,
        Type = principal.PrincipalType,
        Identifier = principal.Email ?? principal.LinkedService ?? principal.PrincipalId.ToString(),
        DisplayName = string.IsNullOrWhiteSpace(principal.DisplayName) ? principal.Email ?? principal.PrincipalId.ToString() : principal.DisplayName,
        Email = principal.Email ?? string.Empty,
        IsActive = principal.IsActive,
        IsEnabled = principal.IsActive,
        CreatedAt = principal.CreatedAt
    };

    private static RoleDto MapRole(IamRoleResponse role) => new()
    {
        RoleId = role.RoleId,
        ServiceName = FirstNonBlank(role.ServiceName, ExtractServiceName(role.RoleId)),
        Name = FirstNonBlank(role.Name, role.RoleName, HumanizeRoleId(role.RoleId)),
        Description = role.Description ?? string.Empty,
        Permissions = role.Permissions ?? [],
        PermissionIds = role.PermissionIds ?? role.Permissions ?? []
    };

    private static string FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    /// <summary>
    /// Converts canonical IAM role identifiers such as roles.contact.viewer into display labels.
    /// </summary>
    public static string HumanizeRoleId(string roleId)
    {
        var normalized = roleId.StartsWith("roles.", StringComparison.OrdinalIgnoreCase)
            ? roleId["roles.".Length..]
            : roleId;

        var words = normalized
            .Replace('-', '.')
            .Replace('_', '.')
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(word => word.Equals("iam", StringComparison.OrdinalIgnoreCase)
                ? "IAM"
                : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word.ToLowerInvariant()));

        var label = string.Join(" ", words);
        return string.IsNullOrWhiteSpace(label) ? roleId : label;
    }

    private static string ExtractServiceName(string roleId)
    {
        var normalized = roleId.StartsWith("roles.", StringComparison.OrdinalIgnoreCase)
            ? roleId["roles.".Length..]
            : roleId;

        var separatorIndex = normalized.IndexOfAny(['.', '-', '_']);
        return separatorIndex > 0 ? normalized[..separatorIndex] : string.Empty;
    }

    /// <summary>
    /// Retrieves role bindings for a principal.
    /// </summary>
    public virtual async Task<List<RoleBindingDto>> GetPrincipalRolesAsync(Guid principalId, CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<List<RoleBindingDto>>($"/iam/v1/principals/{principalId}/roles", ct);
        return response?
            .Select(binding =>
            {
                binding.RoleName = FirstNonBlank(binding.RoleName, HumanizeRoleId(binding.RoleId));
                return binding;
            })
            .ToList() ?? new();
    }

    /// <summary>
    /// Grants a role to a principal.
    /// </summary>
    public virtual async Task<bool> GrantRoleAsync(Guid principalId, GrantRoleRequestDto request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/iam/v1/principals/{principalId}/roles", request, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Revokes a role from a principal.
    /// </summary>
    public virtual async Task<bool> RevokeRoleAsync(Guid principalId, Guid bindingId, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/iam/v1/principals/{principalId}/roles/{bindingId}", ct);
        return response.IsSuccessStatusCode;
    }

    internal virtual async Task<IamResolvePermissionsResult?> ResolvePermissionsAsync(
        string principalId,
        CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "/iam/v1/auth/resolve-permissions",
            new IamResolvePermissionsRequest { PrincipalId = principalId },
            ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<IamResolvePermissionsResponse>(cancellationToken: ct);
        if (result is null)
        {
            throw new JsonException("IAM returned a null permission-resolution payload.");
        }

        if (result.Permissions is null)
        {
            throw new JsonException("IAM permission-resolution field 'permissions' cannot be null.");
        }

        if (result.Roles is null)
        {
            throw new JsonException("IAM permission-resolution field 'roles' cannot be null.");
        }

        return new IamResolvePermissionsResult(
            result.PrincipalId,
            result.Permissions,
            result.Roles,
            result.ResourcePath,
            result.CacheUntil,
            result.FromCache);
    }
}
