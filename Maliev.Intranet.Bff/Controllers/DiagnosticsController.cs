using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Maliev.Intranet.Bff.Clients;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Diagnostic controller for inspecting authentication and authorization state.
/// </summary>
/// <param name="iamClient">The IAM service client.</param>
[RequirePermission(MalievPermissions.System.DiagnosticsRead, AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController(IAMServiceClient iamClient) : ControllerBase
{
    /// <summary>
    /// Returns information about the currently authenticated user and their IAM state.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var user = HttpContext.User;
        var claims = user.Claims.Select(c => new { c.Type, c.Value }).ToList();
        
        var principalId = user.FindFirst("user_id")?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var iamData = principalId != null 
            ? await GetIamData(principalId)
            : null;

        return Ok(new
        {
            IsAuthenticated = user.Identity?.IsAuthenticated,
            AuthenticationType = user.Identity?.AuthenticationType,
            PrincipalId = principalId,
            Claims = claims,
            IamResolvedData = iamData
        });
    }

    private async Task<object?> GetIamData(string principalId)
    {
        try
        {
            // Just try to resolve permissions via the client
            return await iamClient.GetUserAssignmentsAsync(principalId);
        }
        catch (Exception ex)
        {
            return new { error = ex.Message };
        }
    }
}
