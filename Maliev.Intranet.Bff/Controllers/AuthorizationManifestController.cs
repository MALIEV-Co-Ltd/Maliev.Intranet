using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Security;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>Provides the protected endpoint and hub authorization inventory for operations.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/authorization-manifest")]
[RequirePermission(MalievPermissions.IAM.Permissions.Read, AuthenticationSchemes = "Bearer,Cookies")]
public sealed class AuthorizationManifestController(EndpointDataSource endpointDataSource) : ControllerBase
{
    /// <summary>Returns the current authorization manifest generated from application metadata.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(EndpointAuthorizationManifest), StatusCodes.Status200OK)]
    public ActionResult<EndpointAuthorizationManifest> Get()
    {
        return Ok(EndpointAuthorizationManifestBuilder.Build(
            endpointDataSource,
            EndpointAuthorizationManifestBuilder.IntranetHubs));
    }
}
