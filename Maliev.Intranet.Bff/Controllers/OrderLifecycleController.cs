using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using MudBlazor;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Returns aggregated lifecycle data for an order, combining order status, invoice and payment state
/// into the 7-stage <see cref="OrderLifecycleDto"/> consumed by the <c>OrderLifecycleTracker</c> component.
/// </summary>
/// <param name="orderClient">Typed Order service HTTP client.</param>
/// <param name="invoiceClient">Typed Invoice service HTTP client.</param>
[RequirePermission(MalievPermissions.Order.Read, AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orders/{id:guid}/lifecycle")]
public class OrderLifecycleController(
    OrderServiceClient orderClient,
    InvoiceServiceClient invoiceClient) : ControllerBase
{
    /// <summary>
    /// Aggregates the full order lifecycle view from multiple downstream services.
    /// Individual downstream failures are silently degraded (stage remains Pending) so a single
    /// missing service does not break the entire view.
    /// </summary>
    /// <param name="id">The order GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The <see cref="OrderLifecycleDto"/>, or 404 if the order does not exist.</returns>
    [HttpGet]
    public async Task<ActionResult<OrderLifecycleDto>> Get(Guid id, CancellationToken ct)
    {
        // 1. Base order — must exist
        var order = await orderClient.GetOrderByIdAsync(id.ToString(), ct);
        if (order == null) return NotFound();

        // 2. Try to retrieve the most recent invoice linked to this order's PO number (graceful degrade)
        InvoiceSummaryDto? invoice = null;
        if (!string.IsNullOrEmpty(order.CustomerPoNumber))
        {
            invoice = await SafeGet(() => invoiceClient.GetFirstInvoiceByPoNumberAsync(order.CustomerPoNumber, ct));
        }

        // 3. Derive timeline timestamps from the existing order timeline
        DateTime? GetTimestamp(string status) =>
            order.Timeline.LastOrDefault(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase))?.Timestamp;

        // 4. Build the 7-stage list
        var status = order.Status;
        var stages = new List<OrderLifecycleStageDto>
        {
            new()
            {
                Stage           = "Quoted",
                Label           = "Quoted",
                Icon            = Icons.Material.Outlined.RequestQuote,
                Status          = status is "New" ? "Pending" : "Completed",
                NavigateTo      = !string.IsNullOrEmpty(order.CustomerPoNumber)
                                    ? $"/sales/quotations?po={Uri.EscapeDataString(order.CustomerPoNumber)}"
                                    : null,
                CompletedAt     = status is "New" ? null : GetTimestamp("Quoted")
            },
            new()
            {
                Stage           = "Confirmed",
                Label           = "Order Confirmed",
                Icon            = Icons.Material.Outlined.CheckCircle,
                Status          = status is "New" ? "Current"
                                : status is "Processing" or "InProduction" or "QC"
                                       or "Delivered" or "Invoiced" or "Paid" ? "Completed"
                                : "Pending",
                EntityId        = id,
                EntityReference = order.OrderNumber,
                NavigateTo      = $"/sales/orders/{id}",
                CompletedAt     = GetTimestamp("Processing") ?? GetTimestamp("Confirmed")
            },
            new()
            {
                Stage       = "InProduction",
                Label       = "In Production",
                Icon        = Icons.Material.Outlined.Factory,
                Status      = status is "InProduction" ? "Current"
                            : status is "QC" or "Delivered" or "Invoiced" or "Paid" ? "Completed"
                            : status is "Processing" ? "Current"
                            : "Pending",
                NavigateTo  = $"/manufacturing/production-queue?orderId={id}",
                CompletedAt = GetTimestamp("InProduction")
            },
            new()
            {
                Stage       = "QC",
                Label       = "Quality Check",
                Icon        = Icons.Material.Outlined.FactCheck,
                Status      = status is "QC" ? "Current"
                            : status is "Delivered" or "Invoiced" or "Paid" ? "Completed"
                            : "Pending",
                CompletedAt = GetTimestamp("QC")
            },
            new()
            {
                Stage       = "Delivered",
                Label       = "Delivered",
                Icon        = Icons.Material.Outlined.LocalShipping,
                Status      = status is "Delivered" or "Invoiced" or "Paid" ? "Completed"
                            : status is "QC" ? "Current"
                            : "Pending",
                NavigateTo  = $"/finance/delivery-notes?orderId={id}",
                CompletedAt = GetTimestamp("Delivered")
            },
            new()
            {
                Stage           = "Invoiced",
                Label           = "Invoiced",
                Icon            = Icons.Material.Outlined.Receipt,
                Status          = invoice?.Status is "Sent" or "Paid" or "Finalized" ? "Completed"
                                : invoice?.Status is "Draft" ? "Current"
                                : status is "Delivered" or "Invoiced" or "Paid" ? "Current"
                                : "Pending",
                EntityId        = invoice?.Id,
                EntityReference = invoice?.InvoiceNumber,
                NavigateTo      = invoice != null ? $"/finance/invoices/{invoice.Id}" : null,
                CompletedAt     = invoice?.IssueDate
            },
            new()
            {
                Stage       = "Paid",
                Label       = "Paid",
                Icon        = Icons.Material.Outlined.Payments,
                Status      = status is "Paid" || invoice?.Status is "Paid" ? "Completed" : "Pending",
                NavigateTo  = "/finance/payments",
                CompletedAt = GetTimestamp("Paid")
            }
        };

        // 5. If no explicit Current stage, promote the first Pending stage after the last Completed
        if (!stages.Any(s => s.Status == "Current"))
        {
            var firstPending = stages.FirstOrDefault(s => s.Status == "Pending");
            if (firstPending != null) firstPending.Status = "Current";
        }

        var currentStage = stages.FirstOrDefault(s => s.Status == "Current")?.Stage
                        ?? stages.LastOrDefault(s => s.Status == "Completed")?.Stage
                        ?? "Quoted";

        return Ok(new OrderLifecycleDto
        {
            OrderId = id,
            OrderNumber = order.OrderNumber,
            Stages = stages,
            CurrentStage = currentStage
        });
    }

    private static async Task<T?> SafeGet<T>(Func<Task<T?>> factory) where T : class
    {
        try { return await factory(); }
        catch { return null; }
    }
}
