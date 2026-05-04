using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for managing and displaying the health status of microservices.
/// </summary>
[RequirePermission(MalievPermissions.System.HealthRead, AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system-health")]
public class SystemHealthController(
    ISystemHealthProbeService probeService,
    ISystemHealthHistoryService historyService) : ControllerBase
{
    /// <summary>
    /// Aggregates and returns the health status of all registered HTTP microservices.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A SystemHealthDto containing status of all services.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SystemHealthDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemHealthDto>> GetSystemHealth(CancellationToken ct)
    {
        var checkedAt = DateTime.UtcNow;
        var services = (await probeService.CheckAllAsync(ct)).ToList();

        var result = new SystemHealthDto
        {
            OverallTimestamp = checkedAt,
            CheckedAt = checkedAt,
            OverallStatus = ResolveOverallStatus(services),
            Services = services
        };

        return Ok(result);
    }

    /// <summary>
    /// Returns bucketed system health history for the last seven days.
    /// </summary>
    /// <param name="days">Number of visible days, up to seven.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Bucketed system health history.</returns>
    [HttpGet("history")]
    [ProducesResponseType(typeof(SystemHealthHistoryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemHealthHistoryDto>> GetSystemHealthHistory([FromQuery] int days = 7, CancellationToken ct = default)
    {
        var result = await historyService.GetHistoryAsync(days, ct);
        return Ok(result);
    }

    private static string ResolveOverallStatus(IReadOnlyList<ServiceHealthStatus> services)
    {
        if (services.Any(s => s.IsCritical && s.Status is "Unhealthy" or "Unreachable"))
        {
            return "Unhealthy";
        }

        return services.Any(s => s.Status is "Unhealthy" or "Unreachable")
            ? "Degraded"
            : "Healthy";
    }
}
