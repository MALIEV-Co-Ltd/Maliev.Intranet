using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for retrieving aggregated dashboard data.
/// </summary>
[RequirePermission(MalievPermissions.Dashboard.View, AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[Route("api/[controller]")]
public class DashboardController(
    OrderServiceClient orderClient,
    QuotationServiceClient quotationClient,
    PaymentServiceClient paymentClient,
    EmployeeServiceClient employeeClient) : ControllerBase
{
    /// <summary>
    /// Gets the aggregated dashboard view model.
    /// </summary>
    /// <param name="widgets">Comma-separated list of requested widgets.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A summary of widgets and alerts for the dashboard.</returns>
    [HttpGet]
    public async Task<ActionResult<DashboardViewModel>> Get([FromQuery] string? widgets = null, CancellationToken ct = default)
    {
        var requestedWidgets = widgets?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (requestedWidgets.Length == 0)
        {
            requestedWidgets = ["Revenue", "ActiveOrders", "PendingQuotes", "Headcount"];
        }

        var model = new DashboardViewModel
        {
            Widgets = new List<WidgetData>(),
            Alerts = new List<SystemAlert>()
        };

        // Parallel execution for better performance
        var revenueTask = requestedWidgets.Contains("Revenue") ? paymentClient.GetPaymentStatsAsync(ct) : Task.FromResult<PaymentStatsDto?>(null);
        var ordersTask = requestedWidgets.Contains("ActiveOrders") ? orderClient.GetActiveOrderCountAsync(ct) : Task.FromResult(0);
        var quotesTask = requestedWidgets.Contains("PendingQuotes") ? quotationClient.GetPendingQuotationCountAsync(ct) : Task.FromResult(0);
        var employeesTask = requestedWidgets.Contains("Headcount") ? employeeClient.GetTotalHeadcountAsync(ct) : Task.FromResult(0);

        await Task.WhenAll(revenueTask, ordersTask, quotesTask, employeesTask);

        if (requestedWidgets.Contains("Revenue"))
        {
            var stats = await revenueTask;
            model.Widgets.Add(new WidgetData
            {
                Title = "Total Revenue (Today)",
                Type = "Stat",
                SourceService = "PaymentService",
                Data = JsonSerializer.SerializeToElement($"฿{stats?.TodayTotal.ToString("N0") ?? "0"}")
            });
        }

        if (requestedWidgets.Contains("ActiveOrders"))
        {
            var count = await ordersTask;
            model.Widgets.Add(new WidgetData
            {
                Title = "Active Orders",
                Type = "Stat",
                SourceService = "OrderService",
                Data = JsonSerializer.SerializeToElement(count.ToString())
            });
        }

        if (requestedWidgets.Contains("PendingQuotes"))
        {
            var count = await quotesTask;
            model.Widgets.Add(new WidgetData
            {
                Title = "Pending Quotes",
                Type = "Stat",
                SourceService = "QuotationService",
                Data = JsonSerializer.SerializeToElement(count.ToString())
            });
        }

        if (requestedWidgets.Contains("Headcount"))
        {
            var count = await employeesTask;
            model.Widgets.Add(new WidgetData
            {
                Title = "Total Headcount",
                Type = "Stat",
                SourceService = "EmployeeService",
                Data = JsonSerializer.SerializeToElement(count.ToString())
            });
        }

        if (requestedWidgets.Contains("OrderTrend"))
        {
            model.Widgets.Add(new WidgetData
            {
                Title = "Order Trend",
                Type = "Chart",
                SourceService = "OrderService",
                Data = JsonSerializer.SerializeToElement(new { Labels = new[] { "Jan", "Feb", "Mar" }, Values = new[] { 10.0, 25.0, 40.0 } })
            });
        }

        return Ok(model);
    }
}
