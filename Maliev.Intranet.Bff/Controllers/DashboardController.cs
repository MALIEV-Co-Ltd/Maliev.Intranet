using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for retrieving aggregated dashboard data.
/// </summary>
[RequirePermission(MalievPermissions.Dashboard.View, AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DashboardController(
    OrderServiceClient orderClient,
    QuotationServiceClient quotationClient,
    PaymentServiceClient paymentClient,
    EmployeeServiceClient employeeClient,
    InvoiceServiceClient invoiceClient,
    ILeaveServiceClient leaveClient,
    ProjectServiceClient projectClient) : ControllerBase
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
                NavigateTo = "/finance/payments",
                Data = JsonSerializer.SerializeToElement($"THB {stats?.TodayTotal.ToString("N0") ?? "0"}")
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
                NavigateTo = "/sales/orders",
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
                NavigateTo = "/sales/quotations",
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
                NavigateTo = "/hr/directory",
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
                NavigateTo = "/sales/orders",
                Data = JsonSerializer.SerializeToElement(new { Labels = new[] { "Jan", "Feb", "Mar" }, Values = new[] { 10.0, 25.0, 40.0 } })
            });
        }

        return Ok(model);
    }

    /// <summary>
    /// Gets aggregated action items requiring the current user's attention.
    /// Each category is fetched in parallel with a 3-second per-source timeout.
    /// Any individual source failure degrades gracefully to 0 without failing the whole response.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of action item categories with counts and navigation links.</returns>
    [HttpGet("action-items")]
    public async Task<ActionResult<DashboardActionItemsDto>> GetActionItems(CancellationToken ct = default)
    {
        // Helper: run a count call with a 3-second per-source timeout; return 0 on any failure
        static async Task<int> SafeCount(Func<CancellationToken, Task<int>> fn, CancellationToken parentCt)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(parentCt);
                cts.CancelAfter(TimeSpan.FromSeconds(3));
                return await fn(cts.Token);
            }
            catch
            {
                return 0;
            }
        }

        // Fire all available sources in parallel (4 of 6 spec items).
        // Remaining 2 — "Projects needing pricing" and "Production jobs delayed" —
        // depend on ProjectServiceClient and JobServiceClient which are added in Phase 4.
        var onHoldOrdersTask = SafeCount(t => orderClient.GetOnHoldOrderCountAsync(t), ct);
        var agingQuotesTask = SafeCount(t => quotationClient.GetAgingQuotationCountAsync(7, t), ct);
        var overdueInvoicesTask = SafeCount(t => invoiceClient.GetOverdueInvoiceCountAsync(t), ct);
        var configuringProjectsTask = SafeCount(t => projectClient.GetConfiguringCountAsync(t), ct);

        var employeeId = await GetEmployeeIdAsync(ct);
        var pendingLeaveTask = employeeId == Guid.Empty
            ? Task.FromResult(0)
            : SafeCount(t => leaveClient.GetPendingApprovalCountAsync(employeeId, t), ct);

        await Task.WhenAll(onHoldOrdersTask, agingQuotesTask, overdueInvoicesTask, pendingLeaveTask, configuringProjectsTask);

        var result = new DashboardActionItemsDto();

        var onHoldOrders = await onHoldOrdersTask;
        var agingQuotes = await agingQuotesTask;
        var overdueInvoices = await overdueInvoicesTask;
        var pendingLeave = await pendingLeaveTask;
        var configuringProjects = await configuringProjectsTask;

        if (onHoldOrders > 0)
        {
            result.Categories.Add(new ActionItemCategoryDto
            {
                Label = $"{onHoldOrders} order{(onHoldOrders == 1 ? "" : "s")} on hold or delayed",
                Icon = "Icons.Material.Outlined.PauseCircle",
                Count = onHoldOrders,
                NavigateTo = "/sales/orders?status=OnHold,Delayed",
                Severity = "Error"
            });
        }

        if (agingQuotes > 0)
        {
            result.Categories.Add(new ActionItemCategoryDto
            {
                Label = $"{agingQuotes} quotation{(agingQuotes == 1 ? "" : "s")} awaiting response (>7 days)",
                Icon = "Icons.Material.Outlined.HourglassBottom",
                Count = agingQuotes,
                NavigateTo = "/sales/quotations?aging=true",
                Severity = "Warning"
            });
        }

        if (overdueInvoices > 0)
        {
            result.Categories.Add(new ActionItemCategoryDto
            {
                Label = $"{overdueInvoices} overdue invoice{(overdueInvoices == 1 ? "" : "s")}",
                Icon = "Icons.Material.Outlined.ReceiptLong",
                Count = overdueInvoices,
                NavigateTo = "/finance/invoices?status=Overdue",
                Severity = "Error"
            });
        }

        if (pendingLeave > 0)
        {
            result.Categories.Add(new ActionItemCategoryDto
            {
                Label = $"{pendingLeave} leave request{(pendingLeave == 1 ? "" : "s")} pending your approval",
                Icon = "Icons.Material.Outlined.BeachAccess",
                Count = pendingLeave,
                NavigateTo = "/hr/leave?tab=approvals",
                Severity = "Info",
                ManagerOnly = true
            });
        }

        if (configuringProjects > 0)
        {
            result.Categories.Add(new ActionItemCategoryDto
            {
                Label = $"{configuringProjects} project{(configuringProjects == 1 ? "" : "s")} waiting for pricing",
                Icon = "Icons.Material.Outlined.FolderSpecial",
                Count = configuringProjects,
                NavigateTo = "/sales/projects?status=Configuring",
                Severity = "Warning"
            });
        }

        return Ok(result);
    }

    private async Task<Guid> GetEmployeeIdAsync(CancellationToken ct)
    {
        var employeeIdClaim = User.FindFirst("employee_id")?.Value;
        if (Guid.TryParse(employeeIdClaim, out var employeeId))
        {
            return employeeId;
        }

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (Guid.TryParse(userIdString, out var principalId))
        {
            var employee = await employeeClient.GetByPrincipalIdAsync(principalId, ct);
            if (employee != null)
            {
                return employee.Id;
            }
        }

        return Guid.Empty;
    }
}

