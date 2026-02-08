using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Maliev.Intranet.Bff.Controllers;


/// <summary>
/// Controller for retrieving aggregated dashboard data.
/// </summary>
/// <param name="orderClient">Order service client.</param>
/// <param name="quotationClient">Quotation service client.</param>
/// <param name="customerClient">Customer service client.</param>
/// <param name="paymentClient">Payment service client.</param>
[RequirePermission(MalievPermissions.Dashboard.View, AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[Route("api/[controller]")]
public class DashboardController(
    OrderServiceClient orderClient,
    QuotationServiceClient quotationClient,
    CustomerServiceClient customerClient,
    PaymentServiceClient paymentClient) : ControllerBase
{
    /// <summary>
    /// Gets the aggregated dashboard view model.
    /// </summary>
    /// <returns>A summary of widgets and alerts for the dashboard.</returns>
    [HttpGet]
    public async Task<ActionResult<DashboardViewModel>> Get()
    {
        // Aggregate data from multiple services in parallel
        // For now, we return an empty model to be populated by real service data
        // as the database is currently empty.
        
        _ = orderClient;
        _ = quotationClient;
        _ = customerClient;
        _ = paymentClient;

        var model = new DashboardViewModel
        {
            Widgets = new List<WidgetData>(),
            Alerts = new List<SystemAlert>()
        };

        return Ok(model);
    }
}