using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for employee-related operations, proxying to the Employee Service.
/// </summary>
/// <param name="client">The employee service client.</param>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EmployeesController(EmployeeServiceClient client) : ControllerBase
{
    /// <summary>
    /// Retrieves a paged list of employees.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paged list of employees.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<EmployeeSummaryDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await client.GetEmployeesAsync(page, pageSize);
        return result != null ? Ok(result) : Ok(new PagedResponse<EmployeeSummaryDto>());
    }

    /// <summary>
    /// Retrieves detailed information for a single employee.
    /// </summary>
    /// <param name="id">The employee ID.</param>
    /// <returns>The employee details.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeDetailDto>> GetById(Guid id)
    {
        var result = await client.GetEmployeeByIdAsync(id);
        return result != null ? Ok(result) : NotFound();
    }
}
