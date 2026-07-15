using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Security;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for the IAM Management Console.
/// Provides a unified interface for managing users, roles, and permissions.
/// </summary>
/// <param name="client">The IAM service client.</param>
/// <param name="authorizationService">The authorization service for compound permission checks.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class IamController(
    IAMServiceClient client,
    IAuthorizationService authorizationService) : ControllerBase
{
    /// <summary>
    /// Retrieves all principals (users and service accounts) for the IAM console.
    /// </summary>
    /// <returns>A list of principals.</returns>
    [RequirePermission(MalievPermissions.IAM.Principals.List,
        AuthenticationSchemes = "Bearer,Cookies", RequireLiveCheck = true)]
    [HttpGet("users")]
    public async Task<ActionResult<PagedResponse<PrincipalSummaryDto>>> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
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

        if (!string.IsNullOrWhiteSpace(status))
        {
            principals = principals
                .Where(principal => status.Equals("active", StringComparison.OrdinalIgnoreCase)
                    ? principal.IsActive && principal.IsEnabled
                    : status.Equals("inactive", StringComparison.OrdinalIgnoreCase) && (!principal.IsActive || !principal.IsEnabled))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            principals = principals
                .Where(principal => principal.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return Ok(ToPagedResponse(principals.OrderBy(principal => principal.DisplayName), page, pageSize));
    }

    /// <summary>
    /// Retrieves a single IAM principal for the IAM user detail page.
    /// </summary>
    /// <param name="principalId">The principal identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching principal, or 404 when it does not exist.</returns>
    [RequirePermission(MalievPermissions.IAM.Principals.Read,
        AuthenticationSchemes = "Bearer,Cookies", RequireLiveCheck = true)]
    [HttpGet("users/{principalId:guid}")]
    public async Task<ActionResult<PrincipalSummaryDto>> GetUser(Guid principalId, CancellationToken ct)
    {
        var principal = await client.GetPrincipalAsync(principalId, ct);
        return principal is null ? NotFound() : Ok(principal);
    }

    /// <summary>
    /// Retrieves all roles for the IAM console.
    /// </summary>
    /// <returns>A list of roles.</returns>
    [RequirePermission(MalievPermissions.IAM.Roles.List,
        AuthenticationSchemes = "Bearer,Cookies", RequireLiveCheck = true)]
    [HttpGet("roles")]
    public async Task<ActionResult<List<RoleDto>>> GetRoles()
    {
        var roles = await client.GetRolesAsync();
        return Ok(roles);
    }

    /// <summary>
    /// Retrieves paged roles for the IAM console.
    /// </summary>
    /// <param name="search">Optional search text.</param>
    /// <param name="service">Optional service/domain filter.</param>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A paged list of roles.</returns>
    [RequirePermission(MalievPermissions.IAM.Roles.List,
        AuthenticationSchemes = "Bearer,Cookies", RequireLiveCheck = true)]
    [HttpGet("roles/paged")]
    public async Task<ActionResult<PagedResponse<RoleDto>>> GetRolesPaged(
        [FromQuery] string? search = null,
        [FromQuery] string? service = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var roles = await client.GetRolesAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            roles = roles
                .Where(role =>
                    role.RoleId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    role.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    role.Description.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(service))
        {
            roles = roles
                .Where(role => role.ServiceName.Equals(service, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return Ok(ToPagedResponse(roles.OrderBy(role => role.ServiceName).ThenBy(role => role.Name), page, pageSize));
    }

    /// <summary>
    /// Retrieves all permissions for the IAM console.
    /// </summary>
    /// <returns>A list of permissions.</returns>
    [RequirePermission(MalievPermissions.IAM.Permissions.List,
        AuthenticationSchemes = "Bearer,Cookies", RequireLiveCheck = true)]
    [HttpGet("permissions")]
    public async Task<ActionResult<List<PermissionDto>>> GetPermissions()
    {
        var permissions = await client.GetPermissionsAsync();
        return Ok(permissions);
    }

    /// <summary>
    /// Retrieves paged permissions for the IAM console.
    /// </summary>
    /// <param name="search">Optional search text.</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A paged list of permissions.</returns>
    [RequirePermission(MalievPermissions.IAM.Permissions.List,
        AuthenticationSchemes = "Bearer,Cookies", RequireLiveCheck = true)]
    [HttpGet("permissions/paged")]
    public async Task<ActionResult<PagedResponse<PermissionDto>>> GetPermissionsPaged(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var permissions = await client.GetPermissionsAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            permissions = permissions
                .Where(permission =>
                    permission.PermissionId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    permission.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    permission.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    permission.Category.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            permissions = permissions
                .Where(permission => permission.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return Ok(ToPagedResponse(permissions.OrderBy(permission => permission.Category).ThenBy(permission => permission.PermissionId), page, pageSize));
    }

    /// <summary>
    /// Retrieves role bindings for a specific principal.
    /// </summary>
    /// <param name="principalId">The principal ID.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The list of role bindings.</returns>
    [RequirePermission(MalievPermissions.IAM.Bindings.List,
        AuthenticationSchemes = "Bearer,Cookies", RequireLiveCheck = true)]
    [HttpGet("users/{principalId}/roles")]
    public async Task<ActionResult<List<RoleBindingDto>>> GetUserRoles(
        Guid principalId,
        CancellationToken cancellationToken)
    {
        var roles = await client.GetPrincipalRolesAsync(principalId, cancellationToken);
        return Ok(roles);
    }

    /// <summary>
    /// Grants a role to a principal.
    /// </summary>
    /// <param name="principalId">The principal ID.</param>
    /// <param name="request">The grant request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>Success status.</returns>
    [RequirePermission(MalievPermissions.IAM.Bindings.Create,
        AuthenticationSchemes = "Bearer,Cookies", RequireLiveCheck = true)]
    [HttpPost("users/{principalId}/roles")]
    public async Task<IActionResult> GrantRole(
        Guid principalId,
        [FromBody] GrantRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var binding = await client.GrantRoleAsync(principalId, request, cancellationToken);
            return Ok(binding);
        }
        catch (HttpRequestException ex)
        {
            return MapIamBindingFailure(ex.StatusCode);
        }
    }

    /// <summary>
    /// Revokes a role from a principal.
    /// </summary>
    /// <param name="principalId">The principal ID.</param>
    /// <param name="bindingId">The unique binding ID.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>Success status.</returns>
    [RequirePermission(MalievPermissions.IAM.Bindings.Delete,
        AuthenticationSchemes = "Bearer,Cookies", RequireLiveCheck = true)]
    [HttpDelete("users/{principalId}/roles/{bindingId}")]
    public async Task<IActionResult> RevokeRole(
        Guid principalId,
        Guid bindingId,
        CancellationToken cancellationToken)
    {
        try
        {
            await client.RevokeRoleAsync(principalId, bindingId, cancellationToken);
            return Ok();
        }
        catch (HttpRequestException ex)
        {
            return MapIamBindingFailure(ex.StatusCode);
        }
    }

    /// <summary>
    /// Queues an employee invitation request for future downstream IAM wiring.
    /// </summary>
    /// <param name="request">The invite request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>An accepted response containing the queued invite data.</returns>
    [RequirePermission(MalievPermissions.IAM.Principals.Create,
        AuthenticationSchemes = "Bearer,Cookies", RequireLiveCheck = true)]
    [BffEnforcedPermission(MalievPermissions.IAM.Bindings.Create, "global", requireLiveCheck: true)]
    [HttpPost("users/invite")]
    public async Task<IActionResult> InviteUser(
        [FromBody] InviteUserRequest request,
        CancellationToken cancellationToken)
    {
        var bindingAuthorization = await authorizationService.AuthorizeAsync(
            User,
            resource: null,
            new PermissionRequirement(
                MalievPermissions.IAM.Bindings.Create,
                "global",
                requireLiveCheck: true));
        if (!bindingAuthorization.Succeeded)
        {
            return Forbid();
        }

        var principal = await client.CreatePrincipalAsync(
            request.Email,
            request.DisplayName,
            cancellationToken);
        if (principal is null)
        {
            return BadRequest("Failed to create IAM principal.");
        }

        if (!string.IsNullOrWhiteSpace(request.RoleId))
        {
            try
            {
                await client.GrantRoleAsync(
                    principal.PrincipalId,
                    new GrantRoleRequestDto { RoleId = request.RoleId },
                    cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                return MapIamBindingFailure(ex.StatusCode);
            }
        }

        return CreatedAtAction(nameof(GetUserRoles), new { principalId = principal.PrincipalId }, principal);
    }

    private IActionResult MapIamBindingFailure(System.Net.HttpStatusCode? downstreamStatus) => downstreamStatus switch
    {
        System.Net.HttpStatusCode.Unauthorized => Unauthorized(),
        System.Net.HttpStatusCode.Forbidden => Forbid(),
        System.Net.HttpStatusCode.NotFound => NotFound("IAM role binding was not found."),
        System.Net.HttpStatusCode.BadRequest => BadRequest("IAM rejected the role-binding request."),
        System.Net.HttpStatusCode.Conflict => Conflict("IAM rejected the role-binding request due to a conflict."),
        _ => StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            "IAM role-binding service is temporarily unavailable.")
    };

    /// <summary>
    /// Updates basic user profile state for future downstream IAM wiring.
    /// </summary>
    /// <param name="principalId">The principal identifier.</param>
    /// <param name="request">The patch request.</param>
    /// <returns>An accepted response containing the requested state.</returns>
    [RequirePermission(MalievPermissions.IAM.Principals.Update,
        AuthenticationSchemes = "Bearer,Cookies", RequireLiveCheck = true)]
    [HttpPatch("users/{principalId:guid}")]
    public async Task<IActionResult> PatchUser(Guid principalId, [FromBody] PatchUserRequest request)
    {
        return Accepted(new { PrincipalId = principalId, request.DisplayName, request.IsEnabled, Status = "Queued" });
    }

    /// <summary>
    /// Retrieves recent user activity for the IAM console.
    /// </summary>
    /// <param name="principalId">The principal identifier.</param>
    /// <returns>A stable activity response shape.</returns>
    [RequirePermission(MalievPermissions.IAM.Audit.List,
        AuthenticationSchemes = "Bearer,Cookies", RequireLiveCheck = true)]
    [HttpGet("users/{principalId:guid}/activity")]
    public async Task<IActionResult> GetUserActivity(Guid principalId)
    {
        return Ok(Array.Empty<object>());
    }

    /// <summary>
    /// Retrieves a permission matrix for the requested role.
    /// </summary>
    /// <param name="roleId">The role identifier.</param>
    /// <returns>A role-to-permission matrix derived from IAM role data.</returns>
    [RequirePermission(MalievPermissions.IAM.Roles.Read,
        AuthenticationSchemes = "Bearer,Cookies", RequireLiveCheck = true)]
    [HttpGet("roles/{roleId}/permissions-matrix")]
    public async Task<IActionResult> GetRolePermissionsMatrix(string roleId)
    {
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

    private static PagedResponse<T> ToPagedResponse<T>(IEnumerable<T> items, int page, int pageSize)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var materialized = items.ToList();
        var totalCount = materialized.Count;
        var data = materialized
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();

        return new PagedResponse<T>
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
        };
    }
}
