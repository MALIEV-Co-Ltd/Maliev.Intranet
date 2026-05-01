using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Proxies manufacturing catalog requests (processes, materials, finishes, tolerances, config options)
/// to the Material Service.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/catalog")]
public class ManufacturingCatalogController(MaterialServiceClient client) : ControllerBase
{
    /// <summary>Returns all active manufacturing processes.</summary>
    [RequirePermission(MalievPermissions.Material.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("processes")]
    public async Task<ActionResult<List<ProcessDto>>> GetProcesses(CancellationToken ct)
    {
        var result = await client.GetProcessesAsync(ct);
        return Ok(result ?? []);
    }

    /// <summary>Returns materials available for the specified process code.</summary>
    [RequirePermission(MalievPermissions.Material.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("processes/{processCode}/materials")]
    public async Task<ActionResult<List<CatalogMaterialDto>>> GetMaterialsByProcess(string processCode, CancellationToken ct)
    {
        var result = await client.GetMaterialsByProcessAsync(processCode, ct);
        return Ok(result ?? []);
    }

    /// <summary>Returns surface finishes available for the specified process code.</summary>
    [RequirePermission(MalievPermissions.Material.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("processes/{processCode}/finishes")]
    public async Task<ActionResult<List<CatalogSurfaceFinishDto>>> GetFinishesByProcess(string processCode, CancellationToken ct)
    {
        var result = await client.GetFinishesByProcessAsync(processCode, ct);
        return Ok(result ?? []);
    }

    /// <summary>Returns tolerance classes available for the specified process code.</summary>
    [RequirePermission(MalievPermissions.Material.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("processes/{processCode}/tolerances")]
    public async Task<ActionResult<List<CatalogToleranceDto>>> GetTolerancesByProcess(string processCode, CancellationToken ct)
    {
        var result = await client.GetTolerancesByProcessAsync(processCode, ct);
        return Ok(result ?? []);
    }

    /// <summary>Returns dynamic configuration options for the specified process code.</summary>
    [RequirePermission(MalievPermissions.Material.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("processes/{processCode}/config-options")]
    public async Task<ActionResult<List<ProcessConfigOptionDto>>> GetConfigOptionsByProcess(string processCode, CancellationToken ct)
    {
        var result = await client.GetConfigOptionsByProcessAsync(processCode, ct);
        return Ok(result ?? []);
    }

    /// <summary>Returns surface finishes compatible with a specific material.</summary>
    [RequirePermission(MalievPermissions.Material.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("materials/{materialId:guid}/finishes")]
    public async Task<ActionResult<List<CatalogSurfaceFinishDto>>> GetFinishesByMaterial(Guid materialId, CancellationToken ct)
    {
        var result = await client.GetFinishesByMaterialAsync(materialId, ct);
        return Ok(result ?? []);
    }
}
