using System.Globalization;

using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Clients;

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

    /// <summary>
    /// Retrieves the current roles and permissions for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The user's assignment details.</returns>
    public virtual async Task<UserContextDto?> GetUserAssignmentsAsync(string userId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(userId, out var principalId))
        {
            return null;
        }

        // This endpoint might be different in the new IAM service
        var response = await httpClient.PostAsJsonAsync("/iam/v1/auth/resolve-permissions", new { principalId }, ct);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<UserContextDto>(cancellationToken: ct);
            return result;
        }
        return null;
    }
}
