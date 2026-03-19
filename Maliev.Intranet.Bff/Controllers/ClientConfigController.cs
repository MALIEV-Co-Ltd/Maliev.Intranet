using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Exposes client-facing configuration values (polling timeouts, etc.)
/// to the Blazor front-end.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/config")]
public class ClientConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of <see cref="ClientConfigController"/>.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public ClientConfigController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Returns client configuration values.
    /// </summary>
    [HttpGet("client")]
    public ActionResult<ClientConfigDto> GetClientConfig()
    {
        var timeoutMinutes = _configuration.GetValue<int?>("MalievClient:ProcessingTimeoutMinutes") ?? 3;
        return Ok(new ClientConfigDto
        {
            ProcessingTimeoutMinutes = timeoutMinutes,
        });
    }
}

/// <summary>
/// Client-facing configuration values.
/// </summary>
public sealed class ClientConfigDto
{
    /// <summary>
    /// How many minutes to wait for geometry analysis + preview generation before timing out.
    /// </summary>
    public required int ProcessingTimeoutMinutes { get; init; }
}
