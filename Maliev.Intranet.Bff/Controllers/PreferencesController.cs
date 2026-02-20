using Asp.Versioning;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for managing user preferences.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/preferences")]
public class PreferencesController(EmployeeServiceClient employeeClient) : ControllerBase
{
    /// <summary>
    /// Gets preferences for a scope.
    /// </summary>
    [HttpGet("{scope}")]
    public async Task<ActionResult<UserPreferenceDto>> GetPreference(string scope, CancellationToken ct)
    {
        var result = await employeeClient.GetPreferenceAsync(scope, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Updates preferences for a scope.
    /// </summary>
    [HttpPut("{scope}")]
    public async Task<ActionResult<UserPreferenceDto>> UpsertPreference(string scope, [FromBody] UpsertPreferenceRequest request, CancellationToken ct)
    {
        var result = await employeeClient.UpsertPreferenceAsync(scope, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Deletes preferences for a scope.
    /// </summary>
    [HttpDelete("{scope}")]
    public async Task<IActionResult> DeletePreference(string scope, CancellationToken ct)
    {
        await employeeClient.DeletePreferenceAsync(scope, ct);
        return NoContent();
    }
}
