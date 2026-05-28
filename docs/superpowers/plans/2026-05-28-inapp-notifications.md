# In-App Employee Notification System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a persistent, per-employee in-app notification bell that lights up the moment a customer accepts a quote, so production planning can start immediately without anyone checking email.

**Architecture:** `ProjectQuotationAcceptedEvent` (already published by ProjectService via MassTransit) is consumed by a new BFF consumer that inserts one row into `alert_notifications` (PostgreSQL) and broadcasts an `AlertSummary` to all connected clients via the existing SignalR hub. The Blazor WASM client holds unread alerts in a scoped `AlertService`, renders them in a new `NotificationBell` component placed in the TopBar, and marks them read via two new API endpoints.

**Tech Stack:** .NET 10, Blazor WASM, EF Core + PostgreSQL (`IntranetDbContext`), MassTransit (RabbitMQ), SignalR (`NotificationHub`), MudBlazor

---

## File Map

### BFF — New files
| File | Purpose |
|------|---------|
| `Maliev.Intranet.Bff/Data/AlertNotification.cs` | EF entity for one quote-accepted alert row |
| `Maliev.Intranet.Bff/Data/AlertReadReceipt.cs` | EF entity — one row per (notification, employee) pair |
| `Maliev.Intranet.Bff/Consumers/ProjectQuotationAcceptedConsumer.cs` | MassTransit consumer — insert row + broadcast |
| `Maliev.Intranet.Bff/Controllers/AlertsController.cs` | REST API — GET unread, POST read, POST read-all |

### BFF — Modified files
| File | Change |
|------|--------|
| `Maliev.Intranet.Bff/Data/IntranetDbContext.cs` | Add two DbSets + model config |
| `Maliev.Intranet.Bff/Program.cs` | Register consumer + receive endpoint |
| *(generated)* `Maliev.Intranet.Bff/Data/Migrations/…_AddAlertNotifications.cs` | Two new tables |

### Shared — New file
| File | Purpose |
|------|---------|
| `Maliev.Intranet.Shared/Dtos/AlertDtos.cs` | `AlertSummaryDto` — travels BFF → client over HTTP and SignalR |

### Client — New files
| File | Purpose |
|------|---------|
| `Maliev.Intranet.Client/Services/AlertService.cs` | Scoped singleton — in-memory list, HTTP load, Changed event |
| `Maliev.Intranet.Client/Components/NotificationBell.razor` | Bell icon + badge + dropdown panel |

### Client — Modified files
| File | Change |
|------|--------|
| `Maliev.Intranet.Client/Layout/MainLayout.razor` | Inject AlertService, add ReceiveAlert handler, call InitializeAsync |
| `Maliev.Intranet.Client/Layout/TopBar.razor` | Inject AlertService, insert `<NotificationBell />` |
| `Maliev.Intranet.Client/Layout/TopBar.razor.css` | Notification panel, badge, item styles |

### Tests — New files
| File | Purpose |
|------|---------|
| `Maliev.Intranet.Tests/Bff/Consumers/AlertSourceTests.cs` | Source-level wiring assertions |
| `Maliev.Intranet.Tests/Bff/Controllers/AlertsControllerBoundaryTests.cs` | Controller behaviour with in-memory DB |

---

## Task 1: Write failing source-level tests

**Files:**
- Create: `Maliev.Intranet.Tests/Bff/Consumers/AlertSourceTests.cs`

These tests define the contract before a line of production code is written. All will fail until later tasks implement the targets.

- [ ] **Step 1: Create `AlertSourceTests.cs`**

```csharp
using System.Reflection;
using Maliev.Intranet.Bff.Consumers;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Data;
using Maliev.Intranet.Client.Services;
using Maliev.MessagingContracts.Contracts.Projects;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Maliev.Intranet.Tests.Bff.Consumers;

/// <summary>
/// Source-level assertions that all alert notification wiring is in place.
/// These tests read source files or reflect on types — they do not start the app.
/// </summary>
public class AlertSourceTests
{
    // ── BFF wiring ──────────────────────────────────────────────────────────

    [Fact]
    public void ProjectQuotationAcceptedConsumer_ImplementsIConsumerInterface()
    {
        var consumerType = typeof(ProjectQuotationAcceptedConsumer);
        Assert.True(
            typeof(IConsumer<ProjectQuotationAcceptedEvent>).IsAssignableFrom(consumerType),
            $"{consumerType.Name} must implement IConsumer<ProjectQuotationAcceptedEvent>");
    }

    [Fact]
    public void AlertNotification_ExistsInIntranetDbContext()
    {
        var props = typeof(IntranetDbContext).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.True(
            props.Any(p => p.PropertyType == typeof(DbSet<AlertNotification>)),
            "IntranetDbContext must expose DbSet<AlertNotification>");
    }

    [Fact]
    public void AlertReadReceipt_ExistsInIntranetDbContext()
    {
        var props = typeof(IntranetDbContext).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.True(
            props.Any(p => p.PropertyType == typeof(DbSet<AlertReadReceipt>)),
            "IntranetDbContext must expose DbSet<AlertReadReceipt>");
    }

    [Fact]
    public void AlertsController_Exists()
    {
        Assert.NotNull(typeof(AlertsController));
    }

    [Fact]
    public void AlertsController_HasGetAlertsMethod()
    {
        Assert.NotNull(typeof(AlertsController).GetMethod("GetAlertsAsync"));
    }

    [Fact]
    public void AlertsController_HasMarkReadMethod()
    {
        Assert.NotNull(typeof(AlertsController).GetMethod("MarkReadAsync"));
    }

    [Fact]
    public void AlertsController_HasMarkAllReadMethod()
    {
        Assert.NotNull(typeof(AlertsController).GetMethod("MarkAllReadAsync"));
    }

    // ── Program.cs wiring ────────────────────────────────────────────────────

    [Fact]
    public void ProgramCs_RegistersProjectQuotationAcceptedConsumer()
    {
        var source = ReadRepoFile("Maliev.Intranet.Bff", "Program.cs");
        Assert.Contains("AddConsumer<ProjectQuotationAcceptedConsumer>", source, StringComparison.Ordinal);
    }

    // ── Client wiring ────────────────────────────────────────────────────────

    [Fact]
    public void AlertService_Exists()
    {
        Assert.NotNull(typeof(AlertService));
    }

    [Fact]
    public void AlertService_HasInitializeAsyncMethod()
    {
        Assert.NotNull(typeof(AlertService).GetMethod("InitializeAsync"));
    }

    [Fact]
    public void AlertService_HasAddAlertMethod()
    {
        Assert.NotNull(typeof(AlertService).GetMethod("AddAlert"));
    }

    [Fact]
    public void AlertService_HasMarkReadAsyncMethod()
    {
        Assert.NotNull(typeof(AlertService).GetMethod("MarkReadAsync"));
    }

    [Fact]
    public void AlertService_HasMarkAllReadAsyncMethod()
    {
        Assert.NotNull(typeof(AlertService).GetMethod("MarkAllReadAsync"));
    }

    [Fact]
    public void MainLayout_RegistersReceiveAlertSignalRHandler()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Layout", "MainLayout.razor");
        Assert.Contains("ReceiveAlert", source, StringComparison.Ordinal);
        Assert.Contains("AlertService.AddAlert", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MainLayout_CallsAlertServiceInitializeAsync()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Layout", "MainLayout.razor");
        Assert.Contains("AlertService.InitializeAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TopBar_IncludesNotificationBellComponent()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor");
        Assert.Contains("<NotificationBell", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TopBarCss_ContainsNotificationPanelStyle()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Layout", "TopBar.razor.css");
        Assert.Contains(".notif-panel", source, StringComparison.Ordinal);
        Assert.Contains(".notif-item", source, StringComparison.Ordinal);
        Assert.Contains(".notif-badge", source, StringComparison.Ordinal);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private static string ReadRepoFile(params string[] relativeParts)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
            current = current.Parent;
        }
        throw new FileNotFoundException(
            $"Unable to locate {Path.Combine(relativeParts)} from {AppContext.BaseDirectory}.");
    }
}
```

- [ ] **Step 2: Run tests to confirm they all fail**

```
cd /b/maliev/Maliev.Intranet
dotnet test Maliev.Intranet.Tests --filter "AlertSourceTests" -p:NuGetAudit=false --no-restore 2>&1 | tail -20
```

Expected: Multiple FAIL — types do not exist yet.

- [ ] **Step 3: Commit the failing tests**

```bash
cd /b/maliev/Maliev.Intranet
git add Maliev.Intranet.Tests/Bff/Consumers/AlertSourceTests.cs
git commit -m "test: add failing alert source-level tests (red phase)"
```

---

## Task 2: Alert entity classes

**Files:**
- Create: `Maliev.Intranet.Bff/Data/AlertNotification.cs`
- Create: `Maliev.Intranet.Bff/Data/AlertReadReceipt.cs`

- [ ] **Step 1: Create `AlertNotification.cs`**

```csharp
namespace Maliev.Intranet.Bff.Data;

/// <summary>
/// Persisted record of a quote-accepted alert broadcast to all employees.
/// Each row represents one ProjectQuotationAcceptedEvent received by the BFF.
/// </summary>
public class AlertNotification
{
    /// <summary>Unique alert identifier (generated by the consumer on arrival).</summary>
    public Guid Id { get; set; }

    /// <summary>Alert category — always "QuoteAccepted" in this version.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Project identifier from the event payload.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Human-readable project number, e.g. PRJ-2026-0027.</summary>
    public string ProjectNumber { get; set; } = string.Empty;

    /// <summary>Customer display name. Empty when not available in the event payload.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Number of active parts in this project at acceptance time.</summary>
    public int PartCount { get; set; }

    /// <summary>Comma-separated unique process types, e.g. "CNC, FDM".</summary>
    public string ProcessTypes { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the event arrived at the BFF consumer.</summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>UTC timestamp after which this alert is excluded from all queries (occurred + 7 days).</summary>
    public DateTime ExpiresAtUtc { get; set; }
}
```

- [ ] **Step 2: Create `AlertReadReceipt.cs`**

```csharp
namespace Maliev.Intranet.Bff.Data;

/// <summary>
/// One-row-per-(notification, employee) record that marks an alert as read.
/// Primary key is (NotificationId, EmployeeId) — composite, unique, cascade-deletes with the parent notification.
/// </summary>
public class AlertReadReceipt
{
    /// <summary>FK to the parent <see cref="AlertNotification"/>.</summary>
    public Guid NotificationId { get; set; }

    /// <summary>
    /// Employee identity — matches the NameIdentifier claim used throughout the BFF.
    /// Stored as a string because the claim is a platform user UUID.
    /// </summary>
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the employee marked the alert read.</summary>
    public DateTime ReadAtUtc { get; set; }

    /// <summary>Navigation property to the parent notification.</summary>
    public AlertNotification Notification { get; set; } = null!;
}
```

- [ ] **Step 3: Build to verify no compile errors**

```
cd /b/maliev/Maliev.Intranet
dotnet build Maliev.Intranet.Bff -p:NuGetAudit=false --no-restore 2>&1 | tail -10
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 4: Commit the entities**

```bash
git add Maliev.Intranet.Bff/Data/AlertNotification.cs Maliev.Intranet.Bff/Data/AlertReadReceipt.cs
git commit -m "feat: add AlertNotification and AlertReadReceipt entities"
```

---

## Task 3: Update IntranetDbContext

**Files:**
- Modify: `Maliev.Intranet.Bff/Data/IntranetDbContext.cs`

- [ ] **Step 1: Add DbSets and model configuration**

Replace the entire content of `Maliev.Intranet.Bff/Data/IntranetDbContext.cs` with:

```csharp
using Microsoft.EntityFrameworkCore;

namespace Maliev.Intranet.Bff.Data;

/// <summary>
/// PostgreSQL persistence context for Intranet BFF-owned application data.
/// </summary>
public class IntranetDbContext(DbContextOptions<IntranetDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Historical system health probe samples.
    /// </summary>
    public DbSet<HealthCheckSample> HealthCheckSamples => Set<HealthCheckSample>();

    /// <summary>
    /// Quote-accepted alert notifications broadcast to all employees.
    /// </summary>
    public DbSet<AlertNotification> AlertNotifications => Set<AlertNotification>();

    /// <summary>
    /// Per-employee read receipts for alert notifications.
    /// </summary>
    public DbSet<AlertReadReceipt> AlertReadReceipts => Set<AlertReadReceipt>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── HealthCheckSample ─────────────────────────────────────────────────
        var sample = modelBuilder.Entity<HealthCheckSample>();
        sample.ToTable("health_check_samples");
        sample.HasKey(x => x.Id);
        sample.Property(x => x.Id).HasColumnName("id");
        sample.Property(x => x.SampledAtUtc).HasColumnName("sampled_at_utc").IsRequired();
        sample.Property(x => x.ServiceName).HasColumnName("service_name").HasMaxLength(128).IsRequired();
        sample.Property(x => x.DomainGroup).HasColumnName("domain_group").HasMaxLength(64).IsRequired();
        sample.Property(x => x.RoutePrefix).HasColumnName("route_prefix").HasMaxLength(128).IsRequired();
        sample.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        sample.Property(x => x.IsCritical).HasColumnName("is_critical").IsRequired();
        sample.Property(x => x.LivenessPath).HasColumnName("liveness_path").HasMaxLength(256).IsRequired();
        sample.Property(x => x.ReadinessPath).HasColumnName("readiness_path").HasMaxLength(256).IsRequired();
        sample.Property(x => x.LivenessResponseTimeMs).HasColumnName("liveness_response_time_ms").IsRequired();
        sample.Property(x => x.ReadinessResponseTimeMs).HasColumnName("readiness_response_time_ms").IsRequired();
        sample.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(1000);
        sample.Property(x => x.ErrorBody).HasColumnName("error_body").HasMaxLength(2000);
        sample.HasIndex(x => x.SampledAtUtc);
        sample.HasIndex(x => new { x.ServiceName, x.SampledAtUtc });
        sample.HasIndex(x => new { x.DomainGroup, x.SampledAtUtc });

        // ── AlertNotification ────────────────────────────────────────────────
        var alert = modelBuilder.Entity<AlertNotification>();
        alert.ToTable("alert_notifications");
        alert.HasKey(x => x.Id);
        alert.Property(x => x.Id).HasColumnName("id");
        alert.Property(x => x.Type).HasColumnName("type").HasMaxLength(64).IsRequired();
        alert.Property(x => x.ProjectId).HasColumnName("project_id").IsRequired();
        alert.Property(x => x.ProjectNumber).HasColumnName("project_number").HasMaxLength(32).IsRequired();
        alert.Property(x => x.CustomerName).HasColumnName("customer_name").HasMaxLength(256).IsRequired();
        alert.Property(x => x.PartCount).HasColumnName("part_count").IsRequired();
        alert.Property(x => x.ProcessTypes).HasColumnName("process_types").HasMaxLength(256).IsRequired();
        alert.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();
        alert.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
        alert.HasIndex(x => x.ExpiresAtUtc);

        // ── AlertReadReceipt ─────────────────────────────────────────────────
        var receipt = modelBuilder.Entity<AlertReadReceipt>();
        receipt.ToTable("alert_read_receipts");
        receipt.HasKey(x => new { x.NotificationId, x.EmployeeId });
        receipt.Property(x => x.NotificationId).HasColumnName("notification_id");
        receipt.Property(x => x.EmployeeId).HasColumnName("employee_id").HasMaxLength(128);
        receipt.Property(x => x.ReadAtUtc).HasColumnName("read_at_utc").IsRequired();
        receipt.HasOne(x => x.Notification)
            .WithMany()
            .HasForeignKey(x => x.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 2: Build to verify no compile errors**

```
cd /b/maliev/Maliev.Intranet
dotnet build Maliev.Intranet.Bff -p:NuGetAudit=false --no-restore 2>&1 | tail -10
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Run the two DbContext source tests from Task 1 — they should now pass**

```
dotnet test Maliev.Intranet.Tests --filter "AlertSourceTests" -p:NuGetAudit=false --no-restore 2>&1 | grep -E "passed|failed|AlertNotification|AlertReadReceipt"
```

Expected: `AlertNotification_ExistsInIntranetDbContext` → passed, `AlertReadReceipt_ExistsInIntranetDbContext` → passed.

- [ ] **Step 4: Commit**

```bash
git add Maliev.Intranet.Bff/Data/IntranetDbContext.cs
git commit -m "feat: add AlertNotification and AlertReadReceipt to IntranetDbContext"
```

---

## Task 4: EF Core migration

**Files:**
- Create: `Maliev.Intranet.Bff/Data/Migrations/…_AddAlertNotifications.cs` (generated)

- [ ] **Step 1: Generate the migration**

```
cd /b/maliev/Maliev.Intranet
dotnet ef migrations add AddAlertNotifications --project Maliev.Intranet.Bff --startup-project Maliev.Intranet.Bff --context IntranetDbContext 2>&1 | tail -10
```

Expected: `Build succeeded.` followed by `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 2: Verify migration creates both tables**

Open the generated file at `Maliev.Intranet.Bff/Data/Migrations/…_AddAlertNotifications.cs` and confirm:
- `migrationBuilder.CreateTable(name: "alert_notifications", ...)` is present
- `migrationBuilder.CreateTable(name: "alert_read_receipts", ...)` is present
- Index on `expires_at_utc` is present

If the file looks wrong (e.g., has extra drops or unexpected columns), investigate before committing.

- [ ] **Step 3: Build to verify migration compiles**

```
cd /b/maliev/Maliev.Intranet
dotnet build Maliev.Intranet.Bff -p:NuGetAudit=false --no-restore 2>&1 | tail -10
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add Maliev.Intranet.Bff/Data/Migrations/
git commit -m "feat: add EF Core migration AddAlertNotifications"
```

---

## Task 5: AlertSummaryDto in Shared project

**Files:**
- Create: `Maliev.Intranet.Shared/Dtos/AlertDtos.cs`

- [ ] **Step 1: Create `AlertDtos.cs`**

```csharp
namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Lightweight alert summary that travels from the BFF to the Blazor client
/// over both the REST API (/api/v1/alerts) and the SignalR hub (ReceiveAlert).
/// </summary>
public class AlertSummaryDto
{
    /// <summary>Unique alert identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Project identifier — used to build the navigation URL.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Human-readable project number, e.g. PRJ-2026-0027.</summary>
    public string ProjectNumber { get; set; } = string.Empty;

    /// <summary>Customer display name (empty when not available in the event payload).</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Number of active parts at acceptance time.</summary>
    public int PartCount { get; set; }

    /// <summary>Comma-separated unique process types, e.g. "CNC, FDM".</summary>
    public string ProcessTypes { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the event arrived.</summary>
    public DateTime OccurredAtUtc { get; set; }
}
```

- [ ] **Step 2: Build Shared project**

```
cd /b/maliev/Maliev.Intranet
dotnet build Maliev.Intranet.Shared -p:NuGetAudit=false --no-restore 2>&1 | tail -5
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add Maliev.Intranet.Shared/Dtos/AlertDtos.cs
git commit -m "feat: add AlertSummaryDto to Shared project"
```

---

## Task 6: AlertsController

**Files:**
- Create: `Maliev.Intranet.Bff/Controllers/AlertsController.cs`

- [ ] **Step 1: Create `AlertsController.cs`**

```csharp
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
/// All endpoints require an authenticated employee session.
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
    [RequirePermission(MalievPermissions.Employee.ProfileRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<List<AlertSummaryDto>>> GetAlertsAsync(CancellationToken ct = default)
    {
        var employeeId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
    [RequirePermission(MalievPermissions.Employee.ProfileRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkReadAsync(Guid id, CancellationToken ct = default)
    {
        var employeeId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(employeeId))
            return Unauthorized();

        // Idempotent upsert — ignore conflict if already read.
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
    [RequirePermission(MalievPermissions.Employee.ProfileRead, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllReadAsync(CancellationToken ct = default)
    {
        var employeeId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
}
```

- [ ] **Step 2: Build BFF project**

```
cd /b/maliev/Maliev.Intranet
dotnet build Maliev.Intranet.Bff -p:NuGetAudit=false --no-restore 2>&1 | tail -10
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Run source tests — controller tests should now pass**

```
dotnet test Maliev.Intranet.Tests --filter "AlertSourceTests" -p:NuGetAudit=false --no-restore 2>&1 | grep -E "passed|failed|AlertsController"
```

Expected: `AlertsController_Exists`, `AlertsController_HasGetAlertsMethod`, `AlertsController_HasMarkReadMethod`, `AlertsController_HasMarkAllReadMethod` → all passed.

- [ ] **Step 4: Commit**

```bash
git add Maliev.Intranet.Bff/Controllers/AlertsController.cs
git commit -m "feat: add AlertsController with GET/POST read endpoints"
```

---

## Task 7: ProjectQuotationAcceptedConsumer

**Files:**
- Create: `Maliev.Intranet.Bff/Consumers/ProjectQuotationAcceptedConsumer.cs`

> **Note on CustomerName:** `ProjectQuotationAcceptedEvent` does not include a `CustomerName` field — only `CustomerId`. The consumer stores `string.Empty` for this version. To add customer names in a future iteration, inject a `ProjectServiceClient` with service account auth and call it here.

- [ ] **Step 1: Create `ProjectQuotationAcceptedConsumer.cs`**

```csharp
using Maliev.Intranet.Bff.Data;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Shared.Dtos;
using Maliev.MessagingContracts.Contracts.Projects;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Consumers;

/// <summary>
/// Consumes <see cref="ProjectQuotationAcceptedEvent"/> published by ProjectService.
/// Persists one <see cref="AlertNotification"/> row in PostgreSQL and immediately
/// broadcasts an <see cref="AlertSummaryDto"/> to all connected Intranet clients via SignalR.
///
/// Failure handling:
/// - DB write failure: logged; MassTransit will retry the message.
/// - SignalR broadcast failure: logged and swallowed — clients reload on next page visit.
/// - This consumer is best-effort: a project status change already succeeded before the event fires.
/// </summary>
public class ProjectQuotationAcceptedConsumer(
    IntranetDbContext db,
    IHubContext<NotificationHub> hub,
    ILogger<ProjectQuotationAcceptedConsumer> logger) : IConsumer<ProjectQuotationAcceptedEvent>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ProjectQuotationAcceptedEvent> context)
    {
        var payload = context.Message?.Payload;
        if (payload is null)
        {
            logger.LogWarning("ProjectQuotationAcceptedConsumer: received null payload — skipping");
            return;
        }

        logger.LogInformation(
            "ProjectQuotationAcceptedConsumer: quote accepted for project {ProjectNumber} (id={ProjectId})",
            payload.ProjectNumber, payload.ProjectId);

        var now = DateTime.UtcNow;
        var processTypes = payload.Parts.Count > 0
            ? string.Join(", ", payload.Parts.Select(p => p.ProcessType).Distinct())
            : string.Empty;

        var notification = new AlertNotification
        {
            Id = Guid.NewGuid(),
            Type = "QuoteAccepted",
            ProjectId = payload.ProjectId,
            ProjectNumber = payload.ProjectNumber,
            CustomerName = string.Empty,  // Not in event payload — see class doc
            PartCount = payload.Parts.Count,
            ProcessTypes = processTypes,
            OccurredAtUtc = now,
            ExpiresAtUtc = now.AddDays(7)
        };

        try
        {
            db.AlertNotifications.Add(notification);
            await db.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation(
                "ProjectQuotationAcceptedConsumer: persisted alert {AlertId} for project {ProjectNumber}",
                notification.Id, notification.ProjectNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "ProjectQuotationAcceptedConsumer: failed to persist alert for project {ProjectNumber} — rethrowing for MassTransit retry",
                payload.ProjectNumber);
            throw;  // Rethrow so MassTransit retries the message
        }

        var summary = new AlertSummaryDto
        {
            Id = notification.Id,
            ProjectId = notification.ProjectId,
            ProjectNumber = notification.ProjectNumber,
            CustomerName = notification.CustomerName,
            PartCount = notification.PartCount,
            ProcessTypes = notification.ProcessTypes,
            OccurredAtUtc = notification.OccurredAtUtc
        };

        try
        {
            await hub.Clients.All.SendAsync("ReceiveAlert", summary, context.CancellationToken);

            logger.LogInformation(
                "ProjectQuotationAcceptedConsumer: broadcast ReceiveAlert for alert {AlertId}",
                notification.Id);
        }
        catch (Exception ex)
        {
            // SignalR broadcast failure must not fail the consumer — clients reload from DB on next visit.
            logger.LogWarning(ex,
                "ProjectQuotationAcceptedConsumer: SignalR broadcast failed for alert {AlertId} — clients will load from DB on reconnect",
                notification.Id);
        }
    }
}
```

- [ ] **Step 2: Build to verify no compile errors**

```
cd /b/maliev/Maliev.Intranet
dotnet build Maliev.Intranet.Bff -p:NuGetAudit=false --no-restore 2>&1 | tail -10
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add Maliev.Intranet.Bff/Consumers/ProjectQuotationAcceptedConsumer.cs
git commit -m "feat: add ProjectQuotationAcceptedConsumer"
```

---

## Task 8: Register consumer in Program.cs

**Files:**
- Modify: `Maliev.Intranet.Bff/Program.cs`

- [ ] **Step 1: Add consumer registration to the MassTransit block**

Find this block in `Program.cs` (around line 572):

```csharp
builder.AddMassTransitWithRabbitMq(
    configure: mt =>
    {
        mt.AddConsumer<FileAnalyzedConsumer>();
        mt.AddConsumer<FileMetricsReadyConsumer>();
        mt.AddConsumer<SmallThumbnailReadyConsumer>();
        mt.AddConsumer<PreviewImagesGeneratedConsumer>();
        mt.AddConsumer<DfmAnalysisReadyConsumer>();
        mt.AddConsumer<PriceCalculatedConsumer>();
        mt.AddConsumer<FileAnalysisFailedConsumer>();
    },
```

Add `mt.AddConsumer<ProjectQuotationAcceptedConsumer>();` as the last item inside the `configure` block:

```csharp
builder.AddMassTransitWithRabbitMq(
    configure: mt =>
    {
        mt.AddConsumer<FileAnalyzedConsumer>();
        mt.AddConsumer<FileMetricsReadyConsumer>();
        mt.AddConsumer<SmallThumbnailReadyConsumer>();
        mt.AddConsumer<PreviewImagesGeneratedConsumer>();
        mt.AddConsumer<DfmAnalysisReadyConsumer>();
        mt.AddConsumer<PriceCalculatedConsumer>();
        mt.AddConsumer<FileAnalysisFailedConsumer>();
        mt.AddConsumer<ProjectQuotationAcceptedConsumer>();
    },
```

- [ ] **Step 2: Add receive endpoint inside the `configureRabbitMq` lambda**

Find the end of the `configureRabbitMq: (ctx, cfg) =>` block. Add the new endpoint right before the closing `});` of that lambda (after the `"intranet-bff-pricing-calculated"` endpoint):

```csharp
            cfg.ReceiveEndpoint("intranet-bff-project-quotation-accepted", ep =>
            {
                ep.ConfigureConsumer<ProjectQuotationAcceptedConsumer>(ctx);
                // No explicit exchange Bind — MassTransit uses the default exchange for
                // ProjectQuotationAcceptedEvent, which ProjectService publishes to via Publish().
            });
```

- [ ] **Step 3: Add `AlertService` registration alongside the other client services**

Find the block registering client services (around line 60):
```csharp
    builder.Services.AddScoped<Maliev.Intranet.Client.Services.ProductionHubService>();
```

Add immediately after it:
```csharp
    builder.Services.AddScoped<Maliev.Intranet.Client.Services.AlertService>();
```

- [ ] **Step 4: Build to verify no compile errors**

```
cd /b/maliev/Maliev.Intranet
dotnet build Maliev.Intranet.Bff -p:NuGetAudit=false --no-restore 2>&1 | tail -10
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 5: Run source tests — consumer registration test should pass**

```
dotnet test Maliev.Intranet.Tests --filter "AlertSourceTests" -p:NuGetAudit=false --no-restore 2>&1 | grep -E "passed|failed|Consumer|Program"
```

Expected: `ProjectQuotationAcceptedConsumer_ImplementsIConsumerInterface` and `ProgramCs_RegistersProjectQuotationAcceptedConsumer` → passed.

- [ ] **Step 6: Commit**

```bash
git add Maliev.Intranet.Bff/Program.cs
git commit -m "feat: register ProjectQuotationAcceptedConsumer and AlertService in Program.cs"
```

---

## Task 9: AlertService (client-side)

**Files:**
- Create: `Maliev.Intranet.Client/Services/AlertService.cs`

- [ ] **Step 1: Create `AlertService.cs`**

```csharp
using Maliev.Intranet.Shared.Dtos;
using System.Net.Http.Json;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Scoped singleton that holds the current employee's unread alerts in memory.
/// Loaded once from the BFF on startup via <see cref="InitializeAsync"/>.
/// Real-time updates arrive via SignalR calls to <see cref="AddAlert"/>.
/// </summary>
public class AlertService(HttpClient http, ILogger<AlertService> logger)
{
    private readonly List<AlertSummaryDto> _alerts = [];

    /// <summary>Read-only snapshot of the current in-memory unread alerts list.</summary>
    public IReadOnlyList<AlertSummaryDto> Alerts => _alerts.AsReadOnly();

    /// <summary>Number of unread alerts currently in memory.</summary>
    public int UnreadCount => _alerts.Count;

    /// <summary>
    /// Fired whenever the alerts list changes (add, mark-read, mark-all-read).
    /// Subscribers call <c>StateHasChanged()</c> in response.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Loads unread alerts from the BFF. Called once from MainLayout after the
    /// SignalR hub connection starts. Safe to call multiple times — each call
    /// replaces the in-memory list.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var alerts = await http.GetFromJsonAsync<List<AlertSummaryDto>>("api/v1/alerts");
            _alerts.Clear();
            if (alerts is not null)
                _alerts.AddRange(alerts);
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            // Silent failure — show empty panel rather than crash the layout
            logger.LogWarning(ex, "AlertService: failed to load alerts from BFF — showing empty panel");
        }
    }

    /// <summary>
    /// Prepends a real-time alert received from the SignalR <c>ReceiveAlert</c> handler.
    /// </summary>
    public void AddAlert(AlertSummaryDto alert)
    {
        _alerts.Insert(0, alert);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Marks a single alert as read: calls the BFF and removes it from the in-memory list.
    /// If the HTTP call fails the alert stays in the list (safe failure, no data loss).
    /// </summary>
    public async Task MarkReadAsync(Guid id)
    {
        try
        {
            var response = await http.PostAsync($"api/v1/alerts/{id}/read", null);
            if (response.IsSuccessStatusCode)
            {
                _alerts.RemoveAll(a => a.Id == id);
                Changed?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                logger.LogWarning("AlertService.MarkReadAsync: BFF returned {StatusCode} for alert {Id}", response.StatusCode, id);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AlertService.MarkReadAsync: HTTP call failed for alert {Id} — keeping alert unread", id);
        }
    }

    /// <summary>
    /// Marks all current alerts as read: calls the BFF and clears the in-memory list.
    /// If the HTTP call fails the list is preserved (safe failure).
    /// </summary>
    public async Task MarkAllReadAsync()
    {
        try
        {
            var response = await http.PostAsync("api/v1/alerts/read-all", null);
            if (response.IsSuccessStatusCode)
            {
                _alerts.Clear();
                Changed?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                logger.LogWarning("AlertService.MarkAllReadAsync: BFF returned {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AlertService.MarkAllReadAsync: HTTP call failed — keeping alerts unread");
        }
    }
}
```

- [ ] **Step 2: Build client project**

```
cd /b/maliev/Maliev.Intranet
dotnet build Maliev.Intranet.Client -p:NuGetAudit=false --no-restore 2>&1 | tail -10
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Run source tests — AlertService tests should now pass**

```
dotnet test Maliev.Intranet.Tests --filter "AlertSourceTests" -p:NuGetAudit=false --no-restore 2>&1 | grep -E "passed|failed|AlertService"
```

Expected: All five `AlertService_*` tests → passed.

- [ ] **Step 4: Commit**

```bash
git add Maliev.Intranet.Client/Services/AlertService.cs
git commit -m "feat: add AlertService for client-side alert state management"
```

---

## Task 10: NotificationBell.razor + CSS

**Files:**
- Create: `Maliev.Intranet.Client/Components/NotificationBell.razor`
- Modify: `Maliev.Intranet.Client/Layout/TopBar.razor.css`

- [ ] **Step 1: Create `NotificationBell.razor`**

```razor
@using Maliev.Intranet.Client.Services
@using Maliev.Intranet.Shared.Dtos
@inject AlertService AlertService
@inject NavigationManager Navigation
@implements IDisposable

<div class="notif-bell-wrapper">
    <MudTooltip Text="Notifications" Placement="Placement.Bottom">
        <button type="button"
                class="@BellButtonClass"
                aria-label="@AriaLabel"
                aria-haspopup="listbox"
                aria-expanded="@_panelOpen.ToString().ToLowerInvariant()"
                @onclick="TogglePanel">
            <MudIcon Icon="@Icons.Material.Outlined.Notifications" Size="Size.Small" />
            @if (AlertService.UnreadCount > 0)
            {
                <span class="notif-badge" aria-label="@AlertService.UnreadCount unread notifications">
                    @(AlertService.UnreadCount > 99 ? "99+" : AlertService.UnreadCount.ToString())
                </span>
            }
        </button>
    </MudTooltip>

    @if (_panelOpen)
    {
        <div class="notif-panel" role="listbox" aria-label="Notifications">
            <div class="notif-panel-header">
                <span class="notif-panel-title">
                    Notifications
                    @if (AlertService.UnreadCount > 0)
                    {
                        <span class="notif-panel-count">@AlertService.UnreadCount unread</span>
                    }
                </span>
                @if (AlertService.UnreadCount > 0)
                {
                    <button type="button" class="notif-mark-all" @onclick="MarkAllReadAsync">
                        Mark all as read
                    </button>
                }
            </div>

            <div class="notif-list">
                @if (AlertService.Alerts.Count == 0)
                {
                    <div class="notif-empty">
                        <MudIcon Icon="@Icons.Material.Outlined.NotificationsNone" Size="Size.Large" />
                        <p>All caught up</p>
                    </div>
                }
                else
                {
                    @foreach (var alert in AlertService.Alerts)
                    {
                        <div class="notif-item unread"
                             role="option"
                             tabindex="0"
                             @onclick="() => OpenAlertAsync(alert)"
                             @onkeydown="e => OnItemKeyDown(e, alert)">
                            <div class="notif-icon-wrap">
                                <MudIcon Icon="@Icons.Material.Outlined.AssignmentTurnedIn" Size="Size.Small" Color="Color.Primary" />
                            </div>
                            <div class="notif-body">
                                <div class="notif-headline">
                                    Quote accepted — <strong>@alert.ProjectNumber</strong>
                                </div>
                                <div class="notif-detail">
                                    @alert.PartCount part@(alert.PartCount == 1 ? "" : "s")
                                    @if (!string.IsNullOrWhiteSpace(alert.ProcessTypes))
                                    {
                                        <> · @alert.ProcessTypes</>
                                    }
                                    @if (!string.IsNullOrWhiteSpace(alert.CustomerName))
                                    {
                                        <> · @alert.CustomerName</>
                                    }
                                </div>
                                <div class="notif-time">
                                    @FormatAge(alert.OccurredAtUtc) · Production planning required
                                </div>
                            </div>
                        </div>
                    }
                }
            </div>

            <div class="notif-footer">
                <a href="/projects">View all projects</a>
            </div>
        </div>
    }
</div>

@code {
    private bool _panelOpen;

    private string BellButtonClass => AlertService.UnreadCount > 0
        ? "notif-bell-btn notif-bell-active mud-button-root mud-icon-button"
        : "notif-bell-btn mud-button-root mud-icon-button";

    private string AriaLabel => AlertService.UnreadCount > 0
        ? $"Notifications — {AlertService.UnreadCount} unread"
        : "Notifications";

    protected override void OnInitialized()
    {
        AlertService.Changed += OnAlertsChanged;
        Navigation.LocationChanged += OnLocationChanged;
    }

    public void Dispose()
    {
        AlertService.Changed -= OnAlertsChanged;
        Navigation.LocationChanged -= OnLocationChanged;
    }

    private void OnAlertsChanged(object? sender, EventArgs e) =>
        InvokeAsync(StateHasChanged);

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _panelOpen = false;
        InvokeAsync(StateHasChanged);
    }

    private void TogglePanel() => _panelOpen = !_panelOpen;

    private async Task OpenAlertAsync(AlertSummaryDto alert)
    {
        _panelOpen = false;
        await AlertService.MarkReadAsync(alert.Id);
        Navigation.NavigateTo($"/projects/{alert.ProjectId}?tab=production-plan");
    }

    private void OnItemKeyDown(KeyboardEventArgs e, AlertSummaryDto alert)
    {
        if (e.Key is "Enter" or " ")
            _ = OpenAlertAsync(alert);
    }

    private async Task MarkAllReadAsync()
    {
        _panelOpen = false;
        await AlertService.MarkAllReadAsync();
    }

    private static string FormatAge(DateTime occurredAtUtc)
    {
        var age = DateTime.UtcNow - occurredAtUtc;
        return age.TotalMinutes < 1 ? "Just now"
            : age.TotalMinutes < 60 ? $"{(int)age.TotalMinutes} minute{((int)age.TotalMinutes == 1 ? "" : "s")} ago"
            : age.TotalHours < 24 ? $"{(int)age.TotalHours} hour{((int)age.TotalHours == 1 ? "" : "s")} ago"
            : $"{(int)age.TotalDays} day{((int)age.TotalDays == 1 ? "" : "s")} ago";
    }
}
```

- [ ] **Step 2: Add notification styles to `TopBar.razor.css`**

Open `Maliev.Intranet.Client/Layout/TopBar.razor.css`. Append the following block at the very end of the file:

```css
/* ── Notification Bell ────────────────────────────────────────────────────── */

.notif-bell-wrapper {
    position: relative;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
}

.notif-bell-btn {
    position: relative;
    display: flex;
    align-items: center;
    justify-content: center;
    width: 32px;
    height: 32px;
    border-radius: var(--maliev-radius);
    border: 0;
    background: transparent;
    cursor: pointer;
    color: var(--maliev-muted);
    transition: background 0.12s, color 0.12s;
}

.notif-bell-btn:hover {
    background: var(--maliev-panel-3);
    color: var(--maliev-text);
}

.notif-bell-btn:focus-visible {
    outline: none;
    box-shadow: var(--maliev-focus-ring);
}

.notif-bell-active {
    color: var(--maliev-text);
}

/* Red badge */
.notif-badge {
    position: absolute;
    top: 0;
    right: 0;
    min-width: 14px;
    height: 14px;
    border-radius: 7px;
    background: var(--mud-palette-error);
    color: #fff;
    font-size: 9px;
    font-weight: 700;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0 3px;
    border: 1.5px solid var(--maliev-surface);
    line-height: 1;
    pointer-events: none;
}

/* Panel */
.notif-panel {
    position: absolute;
    top: calc(100% + 10px);
    right: -8px;
    width: 360px;
    background: var(--maliev-panel-1);
    border: 1px solid var(--maliev-border);
    border-radius: var(--maliev-radius-lg);
    box-shadow: 0 16px 48px rgba(0,0,0,0.25);
    z-index: 200;
    overflow: hidden;
}

.notif-panel-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 14px 16px 10px;
    border-bottom: 1px solid var(--maliev-border);
}

.notif-panel-title {
    color: var(--maliev-text);
    font-size: 13px;
    font-weight: 600;
    display: flex;
    align-items: center;
    gap: 8px;
}

.notif-panel-count {
    color: var(--maliev-muted);
    font-weight: 400;
    font-size: 11px;
}

.notif-mark-all {
    color: var(--maliev-accent);
    font-size: 11px;
    cursor: pointer;
    background: none;
    border: none;
    padding: 0;
    font-family: inherit;
}

.notif-mark-all:hover {
    text-decoration: underline;
}

.notif-list {
    max-height: 320px;
    overflow-y: auto;
}

.notif-item {
    display: flex;
    gap: 12px;
    padding: 12px 16px;
    border-bottom: 1px solid var(--maliev-border-subtle, rgba(255,255,255,0.04));
    cursor: pointer;
    transition: background 0.1s;
    position: relative;
}

.notif-item:hover {
    background: var(--maliev-panel-2);
}

.notif-item:focus-visible {
    outline: none;
    box-shadow: var(--maliev-focus-ring);
}

.notif-item.unread {
    background: rgba(107, 127, 240, 0.05);
}

.notif-item.unread::before {
    content: '';
    position: absolute;
    left: 0;
    top: 0;
    bottom: 0;
    width: 3px;
    background: var(--maliev-accent);
    border-radius: 0 2px 2px 0;
}

.notif-icon-wrap {
    width: 32px;
    height: 32px;
    flex-shrink: 0;
    background: rgba(107, 127, 240, 0.12);
    border-radius: var(--maliev-radius);
    display: flex;
    align-items: center;
    justify-content: center;
}

.notif-body {
    flex: 1;
    min-width: 0;
}

.notif-headline {
    color: var(--maliev-text);
    font-size: 12px;
    font-weight: 500;
    line-height: 1.4;
}

.notif-detail {
    color: var(--maliev-muted);
    font-size: 11px;
    margin-top: 2px;
    line-height: 1.4;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.notif-time {
    color: var(--maliev-muted-2, #6b7280);
    font-size: 10px;
    margin-top: 4px;
}

.notif-empty {
    padding: 32px 16px;
    text-align: center;
    color: var(--maliev-muted);
    font-size: 12px;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 8px;
}

.notif-footer {
    padding: 10px 16px;
    border-top: 1px solid var(--maliev-border);
    text-align: center;
}

.notif-footer a {
    color: var(--maliev-accent);
    font-size: 11px;
    text-decoration: none;
}

.notif-footer a:hover {
    text-decoration: underline;
}
```

- [ ] **Step 3: Build client project**

```
cd /b/maliev/Maliev.Intranet
dotnet build Maliev.Intranet.Client -p:NuGetAudit=false --no-restore 2>&1 | tail -10
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add Maliev.Intranet.Client/Components/NotificationBell.razor
git add Maliev.Intranet.Client/Layout/TopBar.razor.css
git commit -m "feat: add NotificationBell component and notification CSS"
```

---

## Task 11: Wire up MainLayout.razor and TopBar.razor

**Files:**
- Modify: `Maliev.Intranet.Client/Layout/MainLayout.razor`
- Modify: `Maliev.Intranet.Client/Layout/TopBar.razor`

- [ ] **Step 1: Update `MainLayout.razor`**

Add the `AlertService` inject directive after the existing `@inject` lines:
```razor
@inject AlertService AlertService
```

Inside `OnAfterRenderAsync`, after `await _hubConnection.StartAsync();`, add two lines:

```csharp
// Wire up real-time alert delivery
_hubConnection.On<AlertSummaryDto>("ReceiveAlert", alert =>
{
    AlertService.AddAlert(alert);
    InvokeAsync(StateHasChanged);
});

await _hubConnection.StartAsync();
await AlertService.InitializeAsync();
```

**Important**: The `On<T>` registration must be added BEFORE `StartAsync()`. The final hub setup block should look like this:

```csharp
_hubConnection = new HubConnectionBuilder()
    .WithUrlAndCookies(Navigation.ToAbsoluteUri("/hubs/notifications").ToString(), CookieProvider.CookieHeader)
    .WithAutomaticReconnect()
    .Build();

_hubConnection.On<string>("ReceiveNotification", (message) =>
{
    Snackbar.Add(message, Severity.Info, config =>
    {
        config.VisibleStateDuration = 5000;
        config.HideTransitionDuration = 120;
        config.ShowTransitionDuration = 120;
    });
    InvokeAsync(StateHasChanged);
});

_hubConnection.On<AlertSummaryDto>("ReceiveAlert", alert =>
{
    AlertService.AddAlert(alert);
    InvokeAsync(StateHasChanged);
});

await _hubConnection.StartAsync();
await AlertService.InitializeAsync();
```

Also add a `using` for the shared DTOs at the top of the `@code` block directives (or in the `@using` list at the file top):
```razor
@using Maliev.Intranet.Shared.Dtos
```

- [ ] **Step 2: Update `TopBar.razor`**

Add inject for `AlertService` after the existing `@inject` lines:
```razor
@inject AlertService AlertService
```

Add `@using Maliev.Intranet.Client.Components` if not already present.

Find this section (AI Assistant button + divider):
```razor
        @* AI Assistant *@
        <MudTooltip Text="AI Assistant" Placement="Placement.Bottom">
            <MudIconButton Icon="@Icons.Material.Outlined.Chat"
                           Class="topbar-chat-toggle"
                           Size="Size.Medium"
                           OnClick="@ToggleChat"
                           Color="@(ChatDrawerOpen ? Color.Primary : Color.Default)" />
        </MudTooltip>

        <div class="topbar-divider" />
```

Insert `<NotificationBell />` between the AI Assistant button and the divider:
```razor
        @* AI Assistant *@
        <MudTooltip Text="AI Assistant" Placement="Placement.Bottom">
            <MudIconButton Icon="@Icons.Material.Outlined.Chat"
                           Class="topbar-chat-toggle"
                           Size="Size.Medium"
                           OnClick="@ToggleChat"
                           Color="@(ChatDrawerOpen ? Color.Primary : Color.Default)" />
        </MudTooltip>

        @* Notification Bell *@
        <NotificationBell />

        <div class="topbar-divider" />
```

- [ ] **Step 3: Build the full solution**

```
cd /b/maliev/Maliev.Intranet
dotnet build -p:NuGetAudit=false --no-restore 2>&1 | tail -15
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)` (all projects)

- [ ] **Step 4: Run all alert source tests — they should all pass now**

```
dotnet test Maliev.Intranet.Tests --filter "AlertSourceTests" -p:NuGetAudit=false --no-restore 2>&1 | tail -20
```

Expected: All 17 tests pass, 0 fail.

- [ ] **Step 5: Commit**

```bash
git add Maliev.Intranet.Client/Layout/MainLayout.razor
git add Maliev.Intranet.Client/Layout/TopBar.razor
git commit -m "feat: wire AlertService + NotificationBell into MainLayout and TopBar"
```

---

## Task 12: Controller boundary tests

**Files:**
- Create: `Maliev.Intranet.Tests/Bff/Controllers/AlertsControllerBoundaryTests.cs`

These tests exercise the controller directly with an EF Core in-memory database — no web server, no MassTransit.

- [ ] **Step 1: Create `AlertsControllerBoundaryTests.cs`**

```csharp
using System.Security.Claims;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maliev.Intranet.Tests.Bff.Controllers;

/// <summary>
/// Boundary tests for <see cref="AlertsController"/> — exercises the three endpoints
/// directly against an EF Core in-memory database.
/// </summary>
public sealed class AlertsControllerBoundaryTests : IDisposable
{
    private readonly IntranetDbContext _db;
    private const string EmployeeId = "test-employee-001";
    private const string OtherEmployeeId = "test-employee-002";

    public AlertsControllerBoundaryTests()
    {
        var options = new DbContextOptionsBuilder<IntranetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new IntranetDbContext(options);
    }

    public void Dispose() => _db.Dispose();

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
        await SeedReceiptAsync(alertId, OtherEmployeeId);  // Other employee read it
        var controller = BuildController(EmployeeId);        // This employee has NOT read it

        var result = await controller.GetAlertsAsync();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<Maliev.Intranet.Shared.Dtos.AlertSummaryDto>>(ok.Value);
        Assert.Single(list);  // Still unread for EmployeeId
    }

    [Fact]
    public async Task GetAlerts_ExpiredAlert_NotReturned()
    {
        await SeedAlertAsync(
            Guid.Parse("44444444-0000-0000-0000-000000000001"),
            "PRJ-2026-0004",
            expiresAtUtc: DateTime.UtcNow.AddDays(-1));  // Already expired
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

        // Verify receipt was written
        var receipt = await _db.AlertReadReceipts
            .FindAsync(new object[] { alertId, EmployeeId });
        Assert.NotNull(receipt);
        Assert.Equal(EmployeeId, receipt.EmployeeId);
    }

    [Fact]
    public async Task MarkRead_CalledTwice_Returns204BothTimes_NoDuplicate()
    {
        var alertId = Guid.Parse("66666666-0000-0000-0000-000000000001");
        await SeedAlertAsync(alertId, "PRJ-2026-0006");
        var controller = BuildController(EmployeeId);

        // First call
        var result1 = await controller.MarkReadAsync(alertId);
        Assert.IsType<NoContentResult>(result1);

        // Second call (idempotent)
        var result2 = await controller.MarkReadAsync(alertId);
        Assert.IsType<NoContentResult>(result2);

        // Still only one receipt row
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
        await SeedAlertAsync(id3, "PRJ-2026-0009", expiresAtUtc: DateTime.UtcNow.AddDays(-1));  // expired
        var controller = BuildController(EmployeeId);

        var result = await controller.MarkAllReadAsync();

        Assert.IsType<NoContentResult>(result);

        var receiptCount = _db.AlertReadReceipts.Count(r => r.EmployeeId == EmployeeId);
        Assert.Equal(2, receiptCount);  // id3 was expired, not marked
    }

    [Fact]
    public async Task MarkAllRead_CalledTwice_Returns204BothTimes_NoDuplicateReceipts()
    {
        var alertId = Guid.Parse("88888888-0000-0000-0000-000000000001");
        await SeedAlertAsync(alertId, "PRJ-2026-0010");
        var controller = BuildController(EmployeeId);

        await controller.MarkAllReadAsync();
        await controller.MarkAllReadAsync();  // second call — must not crash

        var receiptCount = _db.AlertReadReceipts.Count(r => r.EmployeeId == EmployeeId);
        Assert.Equal(1, receiptCount);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private AlertsController BuildController(string employeeId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, employeeId) };
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
```

- [ ] **Step 2: Run the boundary tests**

```
cd /b/maliev/Maliev.Intranet
dotnet test Maliev.Intranet.Tests --filter "AlertsControllerBoundaryTests" -p:NuGetAudit=false --no-restore 2>&1 | tail -20
```

Expected: All 9 tests pass, 0 fail.

- [ ] **Step 3: Run the full test suite**

```
dotnet test Maliev.Intranet.Tests -p:NuGetAudit=false --no-restore 2>&1 | tail -10
```

Expected: All tests pass (check count matches or exceeds previous run + 26 new tests).

- [ ] **Step 4: Commit**

```bash
git add Maliev.Intranet.Tests/Bff/Controllers/AlertsControllerBoundaryTests.cs
git commit -m "test: add AlertsControllerBoundaryTests — GET/POST read/read-all + idempotency"
```

---

## Task 13: Final build verification

- [ ] **Step 1: Clean build of the entire solution**

```
cd /b/maliev/Maliev.Intranet
dotnet build -p:NuGetAudit=false --no-restore 2>&1 | tail -15
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)` (all projects including BFF, Client, Shared, Tests).

- [ ] **Step 2: Run the full test suite one last time**

```
dotnet test Maliev.Intranet.Tests -p:NuGetAudit=false --no-restore -v normal 2>&1 | tail -30
```

Expected: All tests pass. The test count should include all original tests plus the 26+ new alert tests (17 source tests + 9 boundary tests).

- [ ] **Step 3: Final commit**

```bash
cd /b/maliev/Maliev.Intranet
git log --oneline -10
```

Verify the commit history shows each task committed separately and clearly.
