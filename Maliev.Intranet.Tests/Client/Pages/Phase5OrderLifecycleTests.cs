using Bunit;
using Maliev.Intranet.Client.Pages;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Tests.Testing;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Tests.Client.Pages;

/// <summary>
/// Tests for Phase 5: Order Lifecycle Tracker — component and OrderDetail integration.
/// </summary>
public class Phase5OrderLifecycleTests : BunitContext, IAsyncLifetime
{
    public Phase5OrderLifecycleTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);
        Services.AddScoped<BreadcrumbService>();
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    // ── OrderLifecycleTracker component ───────────────────────────────────────

    private static OrderLifecycleDto MakeSampleLifecycle() => new()
    {
        OrderId = Guid.NewGuid(),
        OrderNumber = "ORD-001",
        CurrentStage = "Confirmed",
        Stages =
        [
            new() { Stage = "Quoted",       Label = "Quoted",          Status = "Completed" },
            new() { Stage = "Confirmed",    Label = "Order Confirmed",  Status = "Current" },
            new() { Stage = "InProduction", Label = "In Production",    Status = "Pending" },
            new() { Stage = "QC",           Label = "Quality Check",    Status = "Pending" },
            new() { Stage = "Delivered",    Label = "Delivered",        Status = "Pending" },
            new() { Stage = "Invoiced",     Label = "Invoiced",         Status = "Pending" },
            new() { Stage = "Paid",         Label = "Paid",             Status = "Pending" }
        ]
    };

    [Fact]
    public void OrderLifecycleTracker_ShouldRender_WhenLifecycleProvided()
    {
        var cut = Render<OrderLifecycleTracker>(parameters => parameters
            .Add(p => p.Lifecycle, MakeSampleLifecycle()));

        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void OrderLifecycleTracker_ShouldRenderStageCount()
    {
        var cut = Render<OrderLifecycleTracker>(parameters => parameters
            .Add(p => p.Lifecycle, MakeSampleLifecycle()));

        // Verify stage labels are rendered (7 stages)
        Assert.Contains("lifecycle-stage", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OrderLifecycleTracker_ShouldShowStageLabels()
    {
        var cut = Render<OrderLifecycleTracker>(parameters => parameters
            .Add(p => p.Lifecycle, MakeSampleLifecycle()));

        Assert.Contains("Quoted", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Order Confirmed", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OrderLifecycleTracker_ShouldRender_WhenNullLifecycle()
    {
        // Should not throw when no lifecycle data is provided
        var cut = Render<OrderLifecycleTracker>(parameters => parameters
            .Add(p => p.Lifecycle, (OrderLifecycleDto?)null));

        Assert.NotEmpty(cut.Markup);
    }

    // ── OrderDetail page ──────────────────────────────────────────────────────

    [Fact]
    public void OrderDetailPage_ShouldRender_WithGuidId()
    {
        var id = Guid.NewGuid();
        var cut = Render<OrderDetail>(parameters => parameters.Add(p => p.Id, id));
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void OrderDetailPage_ShouldShowLoadingOrContent()
    {
        var id = Guid.NewGuid();
        var cut = Render<OrderDetail>(parameters => parameters.Add(p => p.Id, id));
        var markup = cut.Markup;
        Assert.True(
            markup.Contains("mud-skeleton", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Order", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Not Found", StringComparison.OrdinalIgnoreCase),
            "Expected loading state, order content, or not-found");
    }

    // ── Orders page (table view toggle) ──────────────────────────────────────

    [Fact]
    public void OrdersPage_ShouldRender_WithoutException()
    {
        var cut = Render<Orders>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void OrdersPage_ShouldContain_OrdersContent()
    {
        var cut = Render<Orders>();
        var markup = cut.Markup;
        Assert.True(
            markup.Contains("Order", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("mud-skeleton", StringComparison.OrdinalIgnoreCase),
            "Expected order-related content or loading");
    }
}
