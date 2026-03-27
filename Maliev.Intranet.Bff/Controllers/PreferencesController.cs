using Asp.Versioning;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for managing user preferences.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/preferences")]
public class PreferencesController(EmployeeServiceClient employeeClient) : ControllerBase
{
    /// <summary>
    /// Gets preferences for a scope. Returns a default empty preference if none exists,
    /// preventing 404 errors for new users who have never saved preferences.
    /// </summary>
    [HttpGet("{scope}")]
    public async Task<ActionResult<UserPreferenceDto>> GetPreference(string scope, CancellationToken ct)
    {
        var result = await employeeClient.GetPreferenceAsync(scope, ct);
        if (result != null) return Ok(result);

        // No preference exists for this scope — return a default one rather than 404.
        // This prevents console errors for new users who have never saved preferences.
        var principalId = User.FindFirst("sub") is { } subClaim && Guid.TryParse(subClaim.Value, out var id)
            ? id
            : Guid.Empty;

        return Ok(new UserPreferenceDto { PrincipalId = principalId, Scope = scope, PreferenceData = [] });
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
