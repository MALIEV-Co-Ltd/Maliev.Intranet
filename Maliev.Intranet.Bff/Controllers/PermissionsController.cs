using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for managing permissions and roles via IAM service.
/// </summary>
/// <param name="client">The IAM service client.</param>
[RequirePermission(MalievPermissions.Iam.Manage, AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[Route("api/[controller]")]
public class PermissionsController(IAMServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves all available permissions.
    /// </summary>
    /// <returns>A list of permissions.</returns>
    [HttpGet("available")]
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
    public async Task<ActionResult<UserContextDto>> GetUserAssignments(string userId)
    {
        var assignments = await client.GetUserAssignmentsAsync(userId);
        return assignments != null ? Ok(assignments) : NotFound();
    }

    /// <summary>
    /// Updates assignments for a user.
    /// </summary>
    /// <param name="request">The assignment request.</param>
    /// <returns>An OK result if successful.</returns>
    [HttpPost("assignments")]
    public async Task<IActionResult> UpdateAssignments([FromBody] UserAssignmentRequest request)
    {
        var success = await client.AssignToUserAsync(request);
        return success ? Ok() : BadRequest("Failed to update user assignments.");
    }
}
