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
    public void ProjectStatusChangedAlertConsumer_ImplementsIConsumerInterface()
    {
        var consumerType = typeof(ProjectStatusChangedAlertConsumer);
        Assert.True(
            typeof(IConsumer<ProjectStatusChangedEvent>).IsAssignableFrom(consumerType),
            $"{consumerType.Name} must implement IConsumer<ProjectStatusChangedEvent>");
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
    public void AlertNotification_TracksEventDeduplicationKey()
    {
        var property = typeof(AlertNotification).GetProperty("EventDeduplicationKey");

        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
    }

    [Fact]
    public void AlertNotification_ModelEnforcesUniqueEventDeduplicationKey()
    {
        var source = ReadRepoFile("Maliev.Intranet.Bff", "Data", "IntranetDbContext.cs");

        Assert.Contains("alert.Property(x => x.EventDeduplicationKey)", source, StringComparison.Ordinal);
        Assert.Contains("alert.HasIndex(x => x.EventDeduplicationKey).IsUnique()", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectQuotationAcceptedConsumer_DeduplicatesBeforePersistingAlert()
    {
        var source = ReadRepoFile("Maliev.Intranet.Bff", "Consumers", "ProjectQuotationAcceptedConsumer.cs");

        Assert.Contains("BuildEventDeduplicationKey", source, StringComparison.Ordinal);
        Assert.Contains("db.AlertNotifications.AnyAsync", source, StringComparison.Ordinal);
        Assert.Contains("EventDeduplicationKey = eventDeduplicationKey", source, StringComparison.Ordinal);
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

    [Fact]
    public void ProgramCs_RegistersProjectStatusChangedAlertConsumer()
    {
        var source = ReadRepoFile("Maliev.Intranet.Bff", "Program.cs");
        Assert.Contains("AddConsumer<ProjectStatusChangedAlertConsumer>", source, StringComparison.Ordinal);
        Assert.Contains("intranet-bff-project-status-changed", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectStatusChangedAlertConsumer_OnlyAlertsWhenProjectIsPaid()
    {
        var source = ReadRepoFile("Maliev.Intranet.Bff", "Consumers", "ProjectStatusChangedAlertConsumer.cs");
        Assert.Contains("NewStatus", source, StringComparison.Ordinal);
        Assert.Contains("\"Paid\"", source, StringComparison.Ordinal);
        Assert.Contains("Type = \"ProjectPaid\"", source, StringComparison.Ordinal);
        Assert.Contains("ReceiveAlert", source, StringComparison.Ordinal);
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
    public void NotificationBell_RendersProjectPaidAlertsDistinctly()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "NotificationBell.razor");
        Assert.Contains("AlertHeadline", source, StringComparison.Ordinal);
        Assert.Contains("ProjectPaid", source, StringComparison.Ordinal);
        Assert.Contains("Payment confirmed", source, StringComparison.Ordinal);
        Assert.Contains("Production release ready", source, StringComparison.Ordinal);
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
    public void NotificationBellCss_ContainsNotificationPanelStyle()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "NotificationBell.razor.css");
        Assert.Contains(".notif-panel", source, StringComparison.Ordinal);
        Assert.Contains(".notif-item", source, StringComparison.Ordinal);
        Assert.Contains(".notif-badge", source, StringComparison.Ordinal);
    }

    [Fact]
    public void NotificationBellCss_OwnsPanelStylesBecauseTopBarCssIsolationCannotStyleChildContent()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "NotificationBell.razor.css");
        Assert.Contains(".notif-panel", source, StringComparison.Ordinal);
        Assert.Contains("position: absolute;", source, StringComparison.Ordinal);
        Assert.Contains("z-index: 1200;", source, StringComparison.Ordinal);
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
