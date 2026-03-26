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
[Route("api/catalog")]
public class ManufacturingCatalogController(MaterialServiceClient client) : ControllerBase
{
    /// <summary>Returns all active manufacturing processes.</summary>
    [HttpGet("processes")]
    [RequirePermission(MalievPermissions.Material.Read, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<List<ProcessDto>>> GetProcesses(CancellationToken ct)
    {
        var result = await client.GetProcessesAsync(ct);
        return Ok(result ?? []);
    }

    /// <summary>Returns materials available for the specified process code.</summary>
    [HttpGet("processes/{processCode}/materials")]
    [RequirePermission(MalievPermissions.Material.Read, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<List<CatalogMaterialDto>>> GetMaterialsByProcess(string processCode, CancellationToken ct)
    {
        var result = await client.GetMaterialsByProcessAsync(processCode, ct);
        return Ok(result ?? []);
    }

    /// <summary>Returns surface finishes available for the specified process code.</summary>
    [HttpGet("processes/{processCode}/finishes")]
    [RequirePermission(MalievPermissions.Material.Read, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<List<CatalogSurfaceFinishDto>>> GetFinishesByProcess(string processCode, CancellationToken ct)
    {
        var result = await client.GetFinishesByProcessAsync(processCode, ct);
        return Ok(result ?? []);
    }

    /// <summary>Returns tolerance classes available for the specified process code.</summary>
    [HttpGet("processes/{processCode}/tolerances")]
    [RequirePermission(MalievPermissions.Material.Read, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<List<CatalogToleranceDto>>> GetTolerancesByProcess(string processCode, CancellationToken ct)
    {
        var result = await client.GetTolerancesByProcessAsync(processCode, ct);
        return Ok(result ?? []);
    }

    /// <summary>Returns dynamic configuration options for the specified process code.</summary>
    [HttpGet("processes/{processCode}/config-options")]
    [RequirePermission(MalievPermissions.Material.Read, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<List<ProcessConfigOptionDto>>> GetConfigOptionsByProcess(string processCode, CancellationToken ct)
    {
        var result = await client.GetConfigOptionsByProcessAsync(processCode, ct);
        return Ok(result ?? []);
    }

    /// <summary>Returns surface finishes compatible with a specific material.</summary>
    [HttpGet("materials/{materialId:guid}/finishes")]
    [RequirePermission(MalievPermissions.Material.Read, AuthenticationSchemes = "Bearer,Cookies")]
    public async Task<ActionResult<List<CatalogSurfaceFinishDto>>> GetFinishesByMaterial(Guid materialId, CancellationToken ct)
    {
        var result = await client.GetFinishesByMaterialAsync(materialId, ct);
        return Ok(result ?? []);
    }
}
