using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for the IAM Management Console.
/// Provides a unified interface for managing users, roles, and permissions.
/// </summary>
/// <param name="client">The IAM service client.</param>
/// <param name="authorizationService">The authorization service for manual checks.</param>
/// <param name="env">The host environment.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class IamController(
    IAMServiceClient client,
    IAuthorizationService authorizationService,
    IWebHostEnvironment env) : ControllerBase
{
    private async Task<bool> IsAuthorizedAsync()
    {
        // 1. Check if user has explicit permission (Standard Path)
        // Use standard policy name format expected by the platform's PermissionAuthorizationPolicyProvider
        const string permissionPrefix = "Permission:";
        var authResult = await authorizationService.AuthorizeAsync(User, $"{permissionPrefix}{MalievPermissions.Iam.Manage}");
        if (authResult.Succeeded) return true;

        // 2. Bootstrap logic (Development Only)
        // Calls promote directly — the IAM endpoint has its own guard (humanUsers.Count <= 1)
        if (env.IsDevelopment())
        {
            var promoted = await client.PromoteCallerToAdminAsync();
            if (promoted) return true;
        }

        return false;
    }

    /// <summary>
    /// Retrieves all principals (users and service accounts) for the IAM console.
    /// </summary>
    /// <returns>A list of principals.</returns>
    [RequirePermission(MalievPermissions.IAM.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("users")]
    public async Task<ActionResult<List<PrincipalSummaryDto>>> GetUsers()
    {
        if (!await IsAuthorizedAsync()) return Forbid();

        var principals = await client.GetPrincipalsAsync();
        return Ok(principals);
    }

    /// <summary>
    /// Retrieves all roles for the IAM console.
    /// </summary>
    /// <returns>A list of roles.</returns>
    [RequirePermission(MalievPermissions.IAM.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("roles")]
    public async Task<ActionResult<List<RoleDto>>> GetRoles()
    {
        if (!await IsAuthorizedAsync()) return Forbid();
        var roles = await client.GetRolesAsync();
        return Ok(roles);
    }

    /// <summary>
    /// Retrieves all permissions for the IAM console.
    /// </summary>
    /// <returns>A list of permissions.</returns>
    [RequirePermission(MalievPermissions.IAM.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("permissions")]
    public async Task<ActionResult<List<PermissionDto>>> GetPermissions()
    {
        if (!await IsAuthorizedAsync()) return Forbid();
        var permissions = await client.GetPermissionsAsync();
        return Ok(permissions);
    }

    /// <summary>
    /// Retrieves role bindings for a specific principal.
    /// </summary>
    /// <param name="principalId">The principal ID.</param>
    /// <returns>The list of role bindings.</returns>
    [RequirePermission(MalievPermissions.IAM.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("users/{principalId}/roles")]
    public async Task<ActionResult<List<RoleBindingDto>>> GetUserRoles(Guid principalId)
    {
        if (!await IsAuthorizedAsync()) return Forbid();
        var roles = await client.GetPrincipalRolesAsync(principalId);
        return Ok(roles);
    }

    /// <summary>
    /// Grants a role to a principal.
    /// </summary>
    /// <param name="principalId">The principal ID.</param>
    /// <param name="request">The grant request.</param>
    /// <returns>Success status.</returns>
    [RequirePermission(MalievPermissions.IAM.Manage, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("users/{principalId}/roles")]
    public async Task<IActionResult> GrantRole(Guid principalId, [FromBody] GrantRoleRequestDto request)
    {
        if (!await IsAuthorizedAsync()) return Forbid();
        var success = await client.GrantRoleAsync(principalId, request);
        return success ? Ok() : BadRequest("Failed to grant role.");
    }

    /// <summary>
    /// Revokes a role from a principal.
    /// </summary>
    /// <param name="principalId">The principal ID.</param>
    /// <param name="bindingId">The unique binding ID.</param>
    /// <returns>Success status.</returns>
    [RequirePermission(MalievPermissions.IAM.Manage, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpDelete("users/{principalId}/roles/{bindingId}")]
    public async Task<IActionResult> RevokeRole(Guid principalId, Guid bindingId)
    {
        if (!await IsAuthorizedAsync()) return Forbid();
        var success = await client.RevokeRoleAsync(principalId, bindingId);
        return success ? Ok() : BadRequest("Failed to revoke role.");
    }
}
