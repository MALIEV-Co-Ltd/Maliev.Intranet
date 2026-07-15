using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for managing permissions and roles via IAM service.
/// </summary>
/// <param name="client">The IAM service client.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PermissionsController(IAMServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves all available permissions.
    /// </summary>
    /// <returns>A list of permissions.</returns>
    [HttpGet("available")]
    [RequirePermission(MalievPermissions.IAM.Permissions.List,
        AuthenticationSchemes = "Bearer,Cookies", RequireLiveCheck = true)]
    public async Task<ActionResult<List<PermissionDto>>> GetAvailablePermissions()
    {
        var permissions = await client.GetPermissionsAsync();
        return Ok(permissions);
    }

    /// <summary>
    /// Retrieves all available roles.
    /// </summary>
    /// <returns>A list of roles.</returns>
    [HttpGet("roles")]
    [RequirePermission(MalievPermissions.IAM.Roles.List,
        AuthenticationSchemes = "Bearer,Cookies", RequireLiveCheck = true)]
    public async Task<ActionResult<List<RoleDto>>> GetAvailableRoles()
    {
        var roles = await client.GetRolesAsync();
        return Ok(roles);
    }

    /// <summary>
    /// Retrieves the assignments for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The user's assignments.</returns>
    [HttpGet("users/{userId}")]
    [RequirePermission(MalievPermissions.IAM.Bindings.List,
        AuthenticationSchemes = "Bearer,Cookies", RequireLiveCheck = true)]
    public async Task<ActionResult<UserContextDto>> GetUserAssignments(string userId)
    {
        var assignments = await client.GetUserAssignmentsAsync(userId);
        return assignments != null ? Ok(assignments) : NotFound();
    }

}
