using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for retrieving aggregated dashboard data.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    /// <summary>
    /// Gets the aggregated dashboard view model.
    /// </summary>
    /// <returns>A summary of widgets and alerts for the dashboard.</returns>
    [HttpGet]
    public ActionResult<DashboardViewModel> Get()
    {
        var model = new DashboardViewModel
        {
            Widgets = new List<WidgetData>
            {
                new() { Title = "Pending Orders", Type = "Stat", Data = JsonSerializer.SerializeToElement(12), SourceService = "OrderService" },
                new() { Title = "Active Quotations", Type = "Stat", Data = JsonSerializer.SerializeToElement(5), SourceService = "QuotationService" },
                new() { Title = "Customer Growth", Type = "Chart", Data = JsonSerializer.SerializeToElement(new { Labels = new[] {"Mon", "Tue", "Wed"}, Values = new[] {10, 15, 12} }), SourceService = "CustomerService" }
            },
            Alerts = new List<SystemAlert>
            {
                new() { Id = Guid.NewGuid(), Message = "Daily accounting report ready for review", Severity = "Info", Timestamp = DateTime.Now.AddMinutes(-30) },
                new() { Id = Guid.NewGuid(), Message = "Server latency detected in Order Service", Severity = "Warning", Timestamp = DateTime.Now.AddMinutes(-5) }
            }
        };

        return Ok(model);
    }
}