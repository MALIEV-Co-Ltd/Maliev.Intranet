using System.Security.Claims;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Data;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Maliev.Intranet.Tests.Bff.Controllers;

/// <summary>
/// Boundary tests for <see cref="AlertsController"/> — exercises the three endpoints
/// directly against PostgreSQL.
/// </summary>
public sealed class AlertsControllerBoundaryTests : IAsyncLifetime
{
    private const string EmployeeId = "test-employee-001";
    private const string OtherEmployeeId = "test-employee-002";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("intranet_alerts_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private IntranetDbContext _db = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<IntranetDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        _db = new IntranetDbContext(options);
        await _db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_db is not null)
        {
            await _db.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    // ── GET /api/v1/alerts ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAlerts_NoAlerts_ReturnsEmptyList()
    {
        var controller = BuildController(EmployeeId);

        var result = await controller.GetAlertsAsync();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<Maliev.Intranet.Shared.Dtos.AlertSummaryDto>>(ok.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetAlerts_OneUnreadAlert_ReturnsThatAlert()
    {
        await SeedAlertAsync(Guid.Parse("11111111-0000-0000-0000-000000000001"), "PRJ-2026-0001");
        var controller = BuildController(EmployeeId);

        var result = await controller.GetAlertsAsync();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<Maliev.Intranet.Shared.Dtos.AlertSummaryDto>>(ok.Value);
        var alert = Assert.Single(list);
        Assert.Equal("PRJ-2026-0001", alert.ProjectNumber);
    }

    [Fact]
    public async Task GetAlerts_PlatformUserIdClaim_ReturnsUnreadAlerts()
    {
        await SeedAlertAsync(Guid.Parse("11111111-0000-0000-0000-000000000002"), "PRJ-2026-0001");
        var controller = BuildControllerWithClaims(new Claim("user_id", EmployeeId));

        var result = await controller.GetAlertsAsync();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<Maliev.Intranet.Shared.Dtos.AlertSummaryDto>>(ok.Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task GetAlerts_AlertAlreadyReadByThisEmployee_NotReturned()
    {
        var alertId = Guid.Parse("22222222-0000-0000-0000-000000000001");
        await SeedAlertAsync(alertId, "PRJ-2026-0002");
        await SeedReceiptAsync(alertId, EmployeeId);
        var controller = BuildController(EmployeeId);

        var result = await controller.GetAlertsAsync();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<Maliev.Intranet.Shared.Dtos.AlertSummaryDto>>(ok.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetAlerts_AlertReadByOtherEmployee_ReturnedForThisEmployee()
    {
        var alertId = Guid.Parse("33333333-0000-0000-0000-000000000001");
        await SeedAlertAsync(alertId, "PRJ-2026-0003");
        await SeedReceiptAsync(alertId, OtherEmployeeId);
        var controller = BuildController(EmployeeId);

        var result = await controller.GetAlertsAsync();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<Maliev.Intranet.Shared.Dtos.AlertSummaryDto>>(ok.Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task GetAlerts_ExpiredAlert_NotReturned()
    {
        await SeedAlertAsync(
            Guid.Parse("44444444-0000-0000-0000-000000000001"),
            "PRJ-2026-0004",
            expiresAtUtc: DateTime.UtcNow.AddDays(-1));
        var controller = BuildController(EmployeeId);

        var result = await controller.GetAlertsAsync();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<Maliev.Intranet.Shared.Dtos.AlertSummaryDto>>(ok.Value);
        Assert.Empty(list);
    }

    // ── POST /api/v1/alerts/{id}/read ─────────────────────────────────────────

    [Fact]
    public async Task MarkRead_InsertReceiptAndReturns204()
    {
        var alertId = Guid.Parse("55555555-0000-0000-0000-000000000001");
        await SeedAlertAsync(alertId, "PRJ-2026-0005");
        var controller = BuildController(EmployeeId);

        var result = await controller.MarkReadAsync(alertId);

        Assert.IsType<NoContentResult>(result);

        var receipt = await _db.AlertReadReceipts
            .FindAsync(new object[] { alertId, EmployeeId });
        Assert.NotNull(receipt);
        Assert.Equal(EmployeeId, receipt.EmployeeId);
    }

    [Fact]
    public async Task MarkRead_PlatformSubClaim_InsertsReceiptAndReturns204()
    {
        var alertId = Guid.Parse("55555555-0000-0000-0000-000000000002");
        await SeedAlertAsync(alertId, "PRJ-2026-0005");
        var controller = BuildControllerWithClaims(new Claim("sub", EmployeeId));

        var result = await controller.MarkReadAsync(alertId);

        Assert.IsType<NoContentResult>(result);

        var receipt = await _db.AlertReadReceipts
            .FindAsync(new object[] { alertId, EmployeeId });
        Assert.NotNull(receipt);
    }

    [Fact]
    public async Task MarkRead_CalledTwice_Returns204BothTimes_NoDuplicate()
    {
        var alertId = Guid.Parse("66666666-0000-0000-0000-000000000001");
        await SeedAlertAsync(alertId, "PRJ-2026-0006");
        var controller = BuildController(EmployeeId);

        var result1 = await controller.MarkReadAsync(alertId);
        Assert.IsType<NoContentResult>(result1);

        var result2 = await controller.MarkReadAsync(alertId);
        Assert.IsType<NoContentResult>(result2);

        var receiptCount = _db.AlertReadReceipts.Count(r => r.NotificationId == alertId && r.EmployeeId == EmployeeId);
        Assert.Equal(1, receiptCount);
    }

    // ── POST /api/v1/alerts/read-all ────────────────────────────────────────────

    [Fact]
    public async Task MarkAllRead_InsertsReceiptsForAllUnexpiredUnread_Returns204()
    {
        var id1 = Guid.Parse("77777777-0000-0000-0000-000000000001");
        var id2 = Guid.Parse("77777777-0000-0000-0000-000000000002");
        var id3 = Guid.Parse("77777777-0000-0000-0000-000000000003");
        await SeedAlertAsync(id1, "PRJ-2026-0007");
        await SeedAlertAsync(id2, "PRJ-2026-0008");
        await SeedAlertAsync(id3, "PRJ-2026-0009", expiresAtUtc: DateTime.UtcNow.AddDays(-1));
        var controller = BuildController(EmployeeId);

        var result = await controller.MarkAllReadAsync();

        Assert.IsType<NoContentResult>(result);

        var receiptCount = _db.AlertReadReceipts.Count(r => r.EmployeeId == EmployeeId);
        Assert.Equal(2, receiptCount);
    }

    [Fact]
    public void AlertsController_RequiresAuthenticatedSessionPermission()
    {
        var methods = new[]
        {
            typeof(AlertsController).GetMethod(nameof(AlertsController.GetAlertsAsync)),
            typeof(AlertsController).GetMethod(nameof(AlertsController.MarkReadAsync)),
            typeof(AlertsController).GetMethod(nameof(AlertsController.MarkAllReadAsync))
        };

        foreach (var method in methods)
        {
            var attribute = Assert.Single(method!.GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: false));
            var permission = Assert.IsType<RequirePermissionAttribute>(attribute);
            Assert.Equal(MalievPermissions.Auth.SessionsRead, permission.Permission);
            Assert.Equal("Bearer,Cookies", permission.AuthenticationSchemes);
        }
    }

    [Fact]
    public async Task MarkAllRead_CalledTwice_Returns204BothTimes_NoDuplicateReceipts()
    {
        var alertId = Guid.Parse("88888888-0000-0000-0000-000000000001");
        await SeedAlertAsync(alertId, "PRJ-2026-0010");
        var controller = BuildController(EmployeeId);

        await controller.MarkAllReadAsync();
        await controller.MarkAllReadAsync();

        var receiptCount = _db.AlertReadReceipts.Count(r => r.EmployeeId == EmployeeId);
        Assert.Equal(1, receiptCount);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private AlertsController BuildController(string employeeId)
    {
        return BuildControllerWithClaims(new Claim(ClaimTypes.NameIdentifier, employeeId));
    }

    private AlertsController BuildControllerWithClaims(params Claim[] claims)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var controller = new AlertsController(_db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
        return controller;
    }

    private async Task SeedAlertAsync(Guid id, string projectNumber, DateTime? expiresAtUtc = null)
    {
        _db.AlertNotifications.Add(new AlertNotification
        {
            Id = id,
            Type = "QuoteAccepted",
            ProjectId = Guid.NewGuid(),
            ProjectNumber = projectNumber,
            CustomerName = string.Empty,
            PartCount = 2,
            ProcessTypes = "CNC",
            OccurredAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc ?? DateTime.UtcNow.AddDays(7)
        });
        await _db.SaveChangesAsync();
    }

    private async Task SeedReceiptAsync(Guid notificationId, string employeeId)
    {
        _db.AlertReadReceipts.Add(new AlertReadReceipt
        {
            NotificationId = notificationId,
            EmployeeId = employeeId,
            ReadAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
