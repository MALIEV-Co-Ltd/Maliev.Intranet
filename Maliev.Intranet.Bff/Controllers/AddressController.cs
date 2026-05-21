using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Provides browser-safe address configuration for Intranet clients.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/address")]
public sealed class AddressController(IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Gets Google Maps browser configuration for address entry.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Read)]
    [HttpGet("google-config")]
    public ActionResult<GoogleAddressConfigResponse> GetGoogleConfig()
    {
        var section = configuration.GetSection("GoogleMaps");
        var response = new GoogleAddressConfigResponse
        {
            ApiKey = section["BrowserApiKey"] ?? string.Empty,
            MapId = section["MapId"],
            DefaultLatitude = section.GetValue("DefaultLatitude", 13.7563),
            DefaultLongitude = section.GetValue("DefaultLongitude", 100.5018),
            DefaultZoom = section.GetValue("DefaultZoom", 12),
            IncludedRegionCodes = section.GetSection("IncludedRegionCodes").Get<string[]>() ?? ["th"]
        };

        return Ok(response);
    }
}
