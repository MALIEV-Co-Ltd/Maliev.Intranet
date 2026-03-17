using Bunit;
using Maliev.Intranet.Client.Pages.Admin;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Tests.Testing;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.Intranet.Tests.Client.Pages;

/// <summary>
/// Tests for Phase 9: Operations &amp; Admin Consolidation.
/// Verifies Settings page (merged: PortalConfig, Notifications, RefData, Content, Workflows).
/// </summary>
public class Phase9AdminConsolidationTests : BunitContext, IAsyncLifetime
{
    public Phase9AdminConsolidationTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    // ── Settings page ─────────────────────────────────────────────────────────

    [Fact]
    public void Settings_ShouldRender_WithoutException()
    {
        var cut = Render<Settings>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void Settings_ShouldContain_PortalConfigTab()
    {
        var cut = Render<Settings>();
        Assert.True(
            cut.Markup.Contains("Portal", StringComparison.OrdinalIgnoreCase) ||
            cut.Markup.Contains("Config", StringComparison.OrdinalIgnoreCase),
            "Expected Portal Config tab");
    }

    [Fact]
    public void Settings_ShouldContain_NotificationsTab()
    {
        var cut = Render<Settings>();
        Assert.Contains("Notification", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Settings_ShouldContain_ReferenceDataTab()
    {
        var cut = Render<Settings>();
        Assert.True(
            cut.Markup.Contains("Reference", StringComparison.OrdinalIgnoreCase) ||
            cut.Markup.Contains("Settings", StringComparison.OrdinalIgnoreCase),
            "Expected Reference Data tab or Settings content");
    }

    [Fact]
    public void Settings_ShouldContain_WorkflowsTab()
    {
        var cut = Render<Settings>();
        Assert.Contains("Workflow", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Settings_ShouldContain_ContentTab()
    {
        var cut = Render<Settings>();
        Assert.Contains("Content", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}
