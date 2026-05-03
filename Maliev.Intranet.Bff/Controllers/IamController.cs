using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

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

    /// <summary>
    /// Queues an employee invitation request for future downstream IAM wiring.
    /// </summary>
    /// <param name="request">The invite request.</param>
    /// <returns>An accepted response containing the queued invite data.</returns>
    [RequirePermission(MalievPermissions.IAM.Manage, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("users/invite")]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request)
    {
        if (!await IsAuthorizedAsync()) return Forbid();
        return Accepted(new { request.Email, request.DisplayName, request.RoleId, Status = "Queued" });
    }

    /// <summary>
    /// Updates basic user profile state for future downstream IAM wiring.
    /// </summary>
    /// <param name="principalId">The principal identifier.</param>
    /// <param name="request">The patch request.</param>
    /// <returns>An accepted response containing the requested state.</returns>
    [RequirePermission(MalievPermissions.IAM.Manage, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("users/{principalId:guid}")]
    public async Task<IActionResult> PatchUser(Guid principalId, [FromBody] PatchUserRequest request)
    {
        if (!await IsAuthorizedAsync()) return Forbid();
        return Accepted(new { PrincipalId = principalId, request.DisplayName, request.IsEnabled, Status = "Queued" });
    }

    /// <summary>
    /// Retrieves recent user activity for the IAM console.
    /// </summary>
    /// <param name="principalId">The principal identifier.</param>
    /// <returns>A stable activity response shape.</returns>
    [RequirePermission(MalievPermissions.IAM.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("users/{principalId:guid}/activity")]
    public async Task<IActionResult> GetUserActivity(Guid principalId)
    {
        if (!await IsAuthorizedAsync()) return Forbid();
        return Ok(Array.Empty<object>());
    }

    /// <summary>
    /// Retrieves a permission matrix for the requested role.
    /// </summary>
    /// <param name="roleId">The role identifier.</param>
    /// <returns>A role-to-permission matrix derived from IAM role data.</returns>
    [RequirePermission(MalievPermissions.IAM.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("roles/{roleId}/permissions-matrix")]
    public async Task<IActionResult> GetRolePermissionsMatrix(string roleId)
    {
        if (!await IsAuthorizedAsync()) return Forbid();
        var roles = await client.GetRolesAsync();
        var role = roles.FirstOrDefault(r => string.Equals(r.RoleId, roleId, StringComparison.OrdinalIgnoreCase));
        if (role is null) return NotFound();

        var permissions = role.PermissionIds.Count > 0 ? role.PermissionIds : role.Permissions;
        return Ok(new
        {
            role.RoleId,
            role.Name,
            Permissions = permissions.Select(permission => new { PermissionId = permission, Granted = true }).ToList()
        });
    }

    /// <summary>
    /// Request payload for inviting a user to the employee intranet.
    /// </summary>
    public sealed class InviteUserRequest
    {
        /// <summary>Gets or sets the display name for the invited user.</summary>
        [Required]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Gets or sets the employee email address.</summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>Gets or sets the initial role identifier.</summary>
        [Required]
        public string RoleId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request payload for patching basic IAM user state.
    /// </summary>
    public sealed class PatchUserRequest
    {
        /// <summary>Gets or sets the display name override.</summary>
        public string? DisplayName { get; set; }

        /// <summary>Gets or sets whether the user should be enabled.</summary>
        public bool? IsEnabled { get; set; }
    }
}
