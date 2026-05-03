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
    private async Task<bool> IsAuthorizedAsync(params string[] permissions)
    {
        const string permissionPrefix = "Permission:";
        foreach (var permission in permissions)
        {
            var authResult = await authorizationService.AuthorizeAsync(User, $"{permissionPrefix}{permission}");
            if (authResult.Succeeded)
            {
                return true;
            }
        }

        if (env.IsDevelopment())
        {
            var promoted = await client.PromoteCallerToAdminAsync();
            if (promoted)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Retrieves all principals (users and service accounts) for the IAM console.
    /// </summary>
    /// <returns>A list of principals.</returns>
    [RequirePermission(MalievPermissions.IAM.Principals.List, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("users")]
    public async Task<ActionResult<PagedResponse<PrincipalSummaryDto>>> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!await IsAuthorizedAsync(MalievPermissions.IAM.Principals.List)) return Forbid();

        var principals = await client.GetPrincipalsAsync();
        if (!string.IsNullOrWhiteSpace(search))
        {
            principals = principals
                .Where(principal =>
                    principal.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    principal.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    principal.Type.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var totalCount = principals.Count;
        var data = principals
            .OrderBy(principal => principal.DisplayName)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();

        return Ok(new PagedResponse<PrincipalSummaryDto>
        {
            Data = data,
            Meta = new PaginationMeta
            {
                CurrentPage = normalizedPage,
                PageSize = normalizedPageSize,
                TotalCount = totalCount,
                TotalItems = totalCount,
                TotalPages = normalizedPageSize > 0 ? (int)Math.Ceiling(totalCount / (double)normalizedPageSize) : 0
            }
        });
    }

    /// <summary>
    /// Retrieves all roles for the IAM console.
    /// </summary>
    /// <returns>A list of roles.</returns>
    [RequirePermission(MalievPermissions.IAM.Roles.List, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("roles")]
    public async Task<ActionResult<List<RoleDto>>> GetRoles()
    {
        if (!await IsAuthorizedAsync(MalievPermissions.IAM.Roles.List)) return Forbid();
        var roles = await client.GetRolesAsync();
        return Ok(roles);
    }

    /// <summary>
    /// Retrieves all permissions for the IAM console.
    /// </summary>
    /// <returns>A list of permissions.</returns>
    [RequirePermission(MalievPermissions.IAM.Permissions.List, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("permissions")]
    public async Task<ActionResult<List<PermissionDto>>> GetPermissions()
    {
        if (!await IsAuthorizedAsync(MalievPermissions.IAM.Permissions.List)) return Forbid();
        var permissions = await client.GetPermissionsAsync();
        return Ok(permissions);
    }

    /// <summary>
    /// Retrieves role bindings for a specific principal.
    /// </summary>
    /// <param name="principalId">The principal ID.</param>
    /// <returns>The list of role bindings.</returns>
    [RequirePermission(MalievPermissions.IAM.Bindings.List, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("users/{principalId}/roles")]
    public async Task<ActionResult<List<RoleBindingDto>>> GetUserRoles(Guid principalId)
    {
        if (!await IsAuthorizedAsync(MalievPermissions.IAM.Bindings.List)) return Forbid();
        var roles = await client.GetPrincipalRolesAsync(principalId);
        return Ok(roles);
    }

    /// <summary>
    /// Grants a role to a principal.
    /// </summary>
    /// <param name="principalId">The principal ID.</param>
    /// <param name="request">The grant request.</param>
    /// <returns>Success status.</returns>
    [RequirePermission(MalievPermissions.IAM.Bindings.Create, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("users/{principalId}/roles")]
    public async Task<IActionResult> GrantRole(Guid principalId, [FromBody] GrantRoleRequestDto request)
    {
        if (!await IsAuthorizedAsync(MalievPermissions.IAM.Bindings.Create)) return Forbid();
        var success = await client.GrantRoleAsync(principalId, request);
        return success ? Ok() : BadRequest("Failed to grant role.");
    }

    /// <summary>
    /// Revokes a role from a principal.
    /// </summary>
    /// <param name="principalId">The principal ID.</param>
    /// <param name="bindingId">The unique binding ID.</param>
    /// <returns>Success status.</returns>
    [RequirePermission(MalievPermissions.IAM.Bindings.Delete, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpDelete("users/{principalId}/roles/{bindingId}")]
    public async Task<IActionResult> RevokeRole(Guid principalId, Guid bindingId)
    {
        if (!await IsAuthorizedAsync(MalievPermissions.IAM.Bindings.Delete)) return Forbid();
        var success = await client.RevokeRoleAsync(principalId, bindingId);
        return success ? Ok() : BadRequest("Failed to revoke role.");
    }

    /// <summary>
    /// Queues an employee invitation request for future downstream IAM wiring.
    /// </summary>
    /// <param name="request">The invite request.</param>
    /// <returns>An accepted response containing the queued invite data.</returns>
    [RequirePermission(MalievPermissions.IAM.Principals.Create, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("users/invite")]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request)
    {
        if (!await IsAuthorizedAsync(MalievPermissions.IAM.Principals.Create)) return Forbid();
        var principal = await client.CreatePrincipalAsync(request.Email, request.DisplayName);
        if (principal is null)
        {
            return BadRequest("Failed to create IAM principal.");
        }

        if (!string.IsNullOrWhiteSpace(request.RoleId))
        {
            var granted = await client.GrantRoleAsync(principal.PrincipalId, new GrantRoleRequestDto { RoleId = request.RoleId });
            if (!granted)
            {
                return BadRequest("Principal was created but the role could not be granted.");
            }
        }

        return CreatedAtAction(nameof(GetUserRoles), new { principalId = principal.PrincipalId }, principal);
    }

    /// <summary>
    /// Updates basic user profile state for future downstream IAM wiring.
    /// </summary>
    /// <param name="principalId">The principal identifier.</param>
    /// <param name="request">The patch request.</param>
    /// <returns>An accepted response containing the requested state.</returns>
    [RequirePermission(MalievPermissions.IAM.Principals.Update, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("users/{principalId:guid}")]
    public async Task<IActionResult> PatchUser(Guid principalId, [FromBody] PatchUserRequest request)
    {
        if (!await IsAuthorizedAsync(MalievPermissions.IAM.Principals.Update)) return Forbid();
        return Accepted(new { PrincipalId = principalId, request.DisplayName, request.IsEnabled, Status = "Queued" });
    }

    /// <summary>
    /// Retrieves recent user activity for the IAM console.
    /// </summary>
    /// <param name="principalId">The principal identifier.</param>
    /// <returns>A stable activity response shape.</returns>
    [RequirePermission(MalievPermissions.IAM.Audit.List, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("users/{principalId:guid}/activity")]
    public async Task<IActionResult> GetUserActivity(Guid principalId)
    {
        if (!await IsAuthorizedAsync(MalievPermissions.IAM.Audit.List, MalievPermissions.IAM.Principals.Read)) return Forbid();
        return Ok(Array.Empty<object>());
    }

    /// <summary>
    /// Retrieves a permission matrix for the requested role.
    /// </summary>
    /// <param name="roleId">The role identifier.</param>
    /// <returns>A role-to-permission matrix derived from IAM role data.</returns>
    [RequirePermission(MalievPermissions.IAM.Roles.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("roles/{roleId}/permissions-matrix")]
    public async Task<IActionResult> GetRolePermissionsMatrix(string roleId)
    {
        if (!await IsAuthorizedAsync(MalievPermissions.IAM.Roles.Read, MalievPermissions.IAM.Roles.List)) return Forbid();
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
