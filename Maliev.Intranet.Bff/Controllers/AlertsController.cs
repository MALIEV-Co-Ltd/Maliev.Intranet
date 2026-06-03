using System.Security.Claims;
using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Data;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// REST API for per-employee alert notifications.
/// All endpoints require an authenticated employee session and return only the
/// requesting employee's own alert state.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AlertsController(IntranetDbContext db) : ControllerBase
{
    /// <summary>
    /// Returns all unread alerts for the requesting employee.
    /// An alert is unread when there is no read receipt row for this employee.
    /// Expired alerts (older than 7 days) are excluded.
    /// </summary>
    [RequirePermission(MalievPermissions.Auth.SessionsRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<List<AlertSummaryDto>>> GetAlertsAsync(CancellationToken ct = default)
    {
        var employeeId = GetEmployeeId();
        if (string.IsNullOrEmpty(employeeId))
            return Unauthorized();

        var now = DateTime.UtcNow;
        var alerts = await db.AlertNotifications
            .Where(n => n.ExpiresAtUtc > now)
            .Where(n => !db.AlertReadReceipts
                .Any(r => r.NotificationId == n.Id && r.EmployeeId == employeeId))
            .OrderByDescending(n => n.OccurredAtUtc)
            .Take(50)
            .Select(n => new AlertSummaryDto
            {
                Id = n.Id,
                ProjectId = n.ProjectId,
                ProjectNumber = n.ProjectNumber,
                CustomerName = n.CustomerName,
                PartCount = n.PartCount,
                ProcessTypes = n.ProcessTypes,
                OccurredAtUtc = n.OccurredAtUtc
            })
            .ToListAsync(ct);

        return Ok(alerts);
    }

    /// <summary>
    /// Marks a single alert as read for the requesting employee.
    /// Idempotent — duplicate calls return 204 without error.
    /// </summary>
    [RequirePermission(MalievPermissions.Auth.SessionsRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkReadAsync(Guid id, CancellationToken ct = default)
    {
        var employeeId = GetEmployeeId();
        if (string.IsNullOrEmpty(employeeId))
            return Unauthorized();

        var existing = await db.AlertReadReceipts
            .FindAsync(new object[] { id, employeeId }, ct);

        if (existing is null)
        {
            db.AlertReadReceipts.Add(new AlertReadReceipt
            {
                NotificationId = id,
                EmployeeId = employeeId,
                ReadAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }

        return NoContent();
    }

    /// <summary>
    /// Marks all non-expired, unread alerts as read for the requesting employee.
    /// Idempotent — safe to call multiple times.
    /// </summary>
    [RequirePermission(MalievPermissions.Auth.SessionsRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllReadAsync(CancellationToken ct = default)
    {
        var employeeId = GetEmployeeId();
        if (string.IsNullOrEmpty(employeeId))
            return Unauthorized();

        var now = DateTime.UtcNow;
        var unread = await db.AlertNotifications
            .Where(n => n.ExpiresAtUtc > now)
            .Where(n => !db.AlertReadReceipts
                .Any(r => r.NotificationId == n.Id && r.EmployeeId == employeeId))
            .Select(n => n.Id)
            .ToListAsync(ct);

        if (unread.Count > 0)
        {
            var receipts = unread.Select(nId => new AlertReadReceipt
            {
                NotificationId = nId,
                EmployeeId = employeeId,
                ReadAtUtc = now
            });
            db.AlertReadReceipts.AddRange(receipts);
            await db.SaveChangesAsync(ct);
        }

        return NoContent();
    }

    private string? GetEmployeeId() =>
        User.FindFirstValue("user_id") ??
        User.FindFirstValue("sub") ??
        User.FindFirstValue(ClaimTypes.NameIdentifier);
}
