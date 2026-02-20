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
        var response = await httpClient.GetFromJsonAsync<List<RoleDto>>("/iam/v1/roles", ct);
        return response ?? new();
    }

    /// <summary>
    /// Retrieves all principals from the IAM service.
    /// </summary>
    public virtual async Task<List<PrincipalSummaryDto>> GetPrincipalsAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<List<PrincipalSummaryDto>>("/iam/v1/principals", ct);
        return response ?? new();
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


    /// <summary>
    /// Retrieves role bindings for a principal.
    /// </summary>
    public virtual async Task<List<RoleBindingDto>> GetPrincipalRolesAsync(Guid principalId, CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<List<RoleBindingDto>>($"/iam/v1/principals/{principalId}/roles", ct);
        return response ?? new();
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
    /// Assigns roles and permissions to a user.
    /// </summary>
    /// <param name="request">The assignment request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public virtual async Task<bool> AssignToUserAsync(UserAssignmentRequest request, CancellationToken ct = default)
    {
        if (!Guid.TryParse(request.UserId, out var principalId))
        {
            return false;
        }

        // This is a legacy method, we should prefer GrantRoleAsync
        foreach (var role in request.Roles)
        {
            await GrantRoleAsync(principalId, new GrantRoleRequestDto { RoleId = role }, ct);
        }
        return true;
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
