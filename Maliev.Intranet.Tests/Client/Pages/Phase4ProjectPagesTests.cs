using Bunit;
using Maliev.Intranet.Client.Pages;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Testing;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Moq;

namespace Maliev.Intranet.Tests.Client.Pages;

/// <summary>
/// Tests for Phase 4: Project Lifecycle Pages — Projects list, ProjectNew, ProjectDetail.
/// </summary>
public class Phase4ProjectPagesTests : BunitContext, IAsyncLifetime
{
    public Phase4ProjectPagesTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);
        Services.AddScoped<BreadcrumbService>();

        var draftServiceMock = new Mock<IProjectDraftService>();
        draftServiceMock.Setup(s => s.LoadDraftAsync(It.IsAny<string?>())).ReturnsAsync((DraftProjectState?)null);
        draftServiceMock.Setup(s => s.SaveDraftAsync(It.IsAny<DraftProjectState>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
        draftServiceMock.Setup(s => s.ClearDraftAsync(It.IsAny<string?>())).Returns(Task.CompletedTask);
        Services.AddSingleton(draftServiceMock.Object);

        Services.AddLogging();
        Services.AddAuthorization();
        Services.AddScoped<AuthenticationStateProvider, TestAuthenticationStateProvider>();

        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    // ── Projects list page ────────────────────────────────────────────────────

    [Fact]
    public void ProjectsPage_ShouldRender_WithoutException()
    {
        var cut = Render<Projects>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void ProjectsPage_ShouldContain_NewProjectButton()
    {
        var cut = Render<Projects>();
        Assert.Contains("New Project", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectsPage_ShouldShowLoadingOrGrid()
    {
        var cut = Render<Projects>();
        var markup = cut.Markup;
        Assert.True(
            markup.Contains("mud-skeleton", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("mud-data-grid", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("No projects", StringComparison.OrdinalIgnoreCase),
            "Expected loading state, data grid, or empty state");
    }

    [Fact]
    public void ProjectsPage_ShouldContain_SearchInput()
    {
        var cut = Render<Projects>();
        Assert.Contains("Search", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // ── ProjectNew page ───────────────────────────────────────────────────────

    [Fact]
    public void ProjectNewPage_ShouldRender_WithoutException()
    {
        var cut = Render<ProjectNew>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void ProjectNewPage_ShouldContain_CustomerField()
    {
        var cut = Render<ProjectNew>();
        Assert.Contains("Customer", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectNewPage_ShouldContain_TitleField()
    {
        var cut = Render<ProjectNew>();
        Assert.Contains("Title", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // ── ProjectDetail page ────────────────────────────────────────────────────

    [Fact]
    public void ProjectDetailPage_ShouldRender_WithIdParameter()
    {
        var id = Guid.NewGuid();
        var cut = Render<ProjectDetail>(parameters => parameters.Add(p => p.Id, id));
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void ProjectDetailPage_ShouldShowLoadingOrContent()
    {
        var id = Guid.NewGuid();
        var cut = Render<ProjectDetail>(parameters => parameters.Add(p => p.Id, id));
        var markup = cut.Markup;
        Assert.True(
            markup.Contains("mud-skeleton", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Project", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Not Found", StringComparison.OrdinalIgnoreCase),
            "Expected loading state, project content, or not-found");
    }
}
