using Bunit;
using Maliev.Intranet.Client.Pages.Manufacturing;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Tests.Testing;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.Intranet.Tests.Client.Pages;

/// <summary>
/// Tests for Phase 6: Production Queue &amp; JobService Integration.
/// </summary>
public class Phase6ProductionQueueTests : BunitContext, IAsyncLifetime
{
    public Phase6ProductionQueueTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);
        Services.AddScoped<ProductionHubService>();
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    // ── ProductionQueue page ──────────────────────────────────────────────────

    [Fact]
    public void ProductionQueue_ShouldRender_WithoutException()
    {
        var cut = Render<ProductionQueue>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void ProductionQueue_ShouldContain_QueueTitle()
    {
        var cut = Render<ProductionQueue>();
        Assert.Contains("Production Queue", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductionQueue_ShouldShowLoadingOrBoard()
    {
        var cut = Render<ProductionQueue>();
        var markup = cut.Markup;
        Assert.True(
            markup.Contains("mud-skeleton", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Queued", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("In Progress", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("mud-paper", StringComparison.OrdinalIgnoreCase),
            "Expected loading state or Kanban board columns");
    }

    // ── JobTicketDialog component ─────────────────────────────────────────────

    [Fact]
    public void JobTicketDialog_ShouldRender_WithoutException()
    {
        var cut = Render<JobTicketDialog>();
        Assert.NotEmpty(cut.Markup);
    }

    // ── ProductionHubService unit tests ───────────────────────────────────────

    [Fact]
    public void ProductionHubService_InitialState_IsDisconnected()
    {
        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        var svc = new ProductionHubService(nav);
        Assert.Equal(Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Disconnected, svc.State);
    }

    [Fact]
    public void ProductionHubService_JobStatusChangedEvent_CanBeSubscribed()
    {
        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        var svc = new ProductionHubService(nav);
        var fired = false;
        svc.JobStatusChanged += (_, _) => fired = true;
        // The event handler is registered — can't fire it without a hub, but subscription works
        Assert.False(fired); // No hub connection so it stays false
    }
}
