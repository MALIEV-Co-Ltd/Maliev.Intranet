using Bunit;
using Maliev.Intranet.Client.Pages.HR;
using Maliev.Intranet.Client.Pages;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Tests.Testing;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;

namespace Maliev.Intranet.Tests.Client.Pages;

/// <summary>
/// Tests for Phase 8: HR Page Consolidation.
/// Verifies PeopleDirectory, LeaveAndOnboarding, Development, ComplianceAndAnalytics, and redirect stubs.
/// </summary>
public class Phase8HrConsolidationTests : BunitContext, IAsyncLifetime
{
    private NavigationManager? _nav;

    public Phase8HrConsolidationTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);
        Services.AddScoped<BreadcrumbService>();
        Render<MudPopoverProvider>();
        _nav = Services.GetRequiredService<NavigationManager>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    // ── People Directory ──────────────────────────────────────────────────────

    [Fact]
    public void PeopleDirectory_ShouldRender_WithoutException()
    {
        var cut = Render<PeopleDirectory>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void PeopleDirectory_ShouldContain_DirectoryTab()
    {
        var cut = Render<PeopleDirectory>();
        Assert.Contains("Directory", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PeopleDirectory_ShouldContain_EmployeeManagementTab()
    {
        var cut = Render<PeopleDirectory>();
        Assert.Contains("Employee", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // ── Leave & Onboarding ────────────────────────────────────────────────────

    [Fact]
    public void LeaveAndOnboarding_ShouldRender_WithoutException()
    {
        var cut = Render<LeaveAndOnboarding>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void LeaveAndOnboarding_ShouldContain_LeaveTab()
    {
        var cut = Render<LeaveAndOnboarding>();
        Assert.True(
            cut.Markup.Contains("Time Off", StringComparison.OrdinalIgnoreCase) ||
            cut.Markup.Contains("Leave", StringComparison.OrdinalIgnoreCase),
            "Expected Leave or Time Off tab");
    }

    [Fact]
    public void LeaveAndOnboarding_ShouldContain_OnboardingTab()
    {
        var cut = Render<LeaveAndOnboarding>();
        Assert.Contains("Onboarding", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // ── Development (Training + Performance) ─────────────────────────────────

    [Fact]
    public void Development_ShouldRender_WithoutException()
    {
        var cut = Render<Development>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void Development_ShouldContain_TrainingTab()
    {
        var cut = Render<Development>();
        Assert.Contains("Training", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Development_ShouldContain_PerformanceTab()
    {
        var cut = Render<Development>();
        Assert.Contains("Performance", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // ── Compliance & Analytics ────────────────────────────────────────────────

    [Fact]
    public void ComplianceAndAnalytics_ShouldRender_WithoutException()
    {
        var cut = Render<ComplianceAndAnalytics>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void ComplianceAndAnalytics_ShouldContain_ComplianceTab()
    {
        var cut = Render<ComplianceAndAnalytics>();
        Assert.Contains("Compliance", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ComplianceAndAnalytics_ShouldContain_AnalyticsTab()
    {
        var cut = Render<ComplianceAndAnalytics>();
        Assert.Contains("Analytics", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // ── Redirect stubs ────────────────────────────────────────────────────────

    [Fact]
    public void TimeOffStub_ShouldRedirectTo_LeaveTab()
    {
        Render<TimeOff>();
        Assert.Contains("/hr/leave?tab=timeoff", _nav?.Uri, StringComparison.OrdinalIgnoreCase);
    }
}
