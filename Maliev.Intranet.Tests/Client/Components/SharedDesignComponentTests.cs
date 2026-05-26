using Bunit;
using Maliev.Intranet.Client.Components.Shared;
using Maliev.Intranet.Shared;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.Intranet.Tests.Client.Components;

public class SharedDesignComponentTests : BunitContext, IAsyncLifetime
{
    public SharedDesignComponentTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void StatusBadge_Normalizes_StatusClassAndDisplayText()
    {
        var cut = Render<StatusBadge>(parameters => parameters.Add(p => p.Status, "in progress"));

        Assert.Contains("status-in-progress", cut.Markup);
        Assert.Contains("In Progress", cut.Markup);
    }

    [Fact]
    public void StatusBadge_DefinesSystemHealthStateColors()
    {
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Components", "Shared", "StatusBadge.razor.css");

        Assert.Contains("border-radius: var(--maliev-radius-pill);", styles, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-ring);", styles, StringComparison.Ordinal);
        Assert.Contains(".status-healthy", styles, StringComparison.Ordinal);
        Assert.Contains("var(--maliev-ok-bg)", styles, StringComparison.Ordinal);
        Assert.Contains(".status-unreachable", styles, StringComparison.Ordinal);
        Assert.Contains(".status-unhealthy", styles, StringComparison.Ordinal);
        Assert.Contains("var(--maliev-danger-bg)", styles, StringComparison.Ordinal);
        Assert.Contains(".status-degraded", styles, StringComparison.Ordinal);
        Assert.Contains("var(--maliev-warn-bg)", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedSurfaces_UseShadowAsBorderCards()
    {
        var panelCard = ReadRepoFile("Maliev.Intranet.Client", "Components", "Shared", "PanelCard.razor.css");
        var statTile = ReadRepoFile("Maliev.Intranet.Client", "Components", "Shared", "StatTile.razor.css");
        var moduleHeader = ReadRepoFile("Maliev.Intranet.Client", "Components", "Shared", "ModuleHeader.razor.css");
        var pageHeader = ReadRepoFile("Maliev.Intranet.Client", "Components", "Shared", "PageHeader.razor.css");

        Assert.Contains("border: 0;", panelCard, StringComparison.Ordinal);
        Assert.Contains("border-radius: var(--maliev-radius-md);", panelCard, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-card);", panelCard, StringComparison.Ordinal);

        Assert.Contains("border: 0;", statTile, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-card);", statTile, StringComparison.Ordinal);
        Assert.Contains("letter-spacing: 0;", statTile, StringComparison.Ordinal);

        Assert.Contains("border-bottom: 0;", moduleHeader, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-ring);", moduleHeader, StringComparison.Ordinal);
        Assert.Contains("border-bottom: 0;", pageHeader, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-ring);", pageHeader, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedButtons_UseMudPrimaryAndRingTokens()
    {
        var primary = ReadRepoFile("Maliev.Intranet.Client", "Components", "Shared", "PrimaryButton.razor.css");
        var secondary = ReadRepoFile("Maliev.Intranet.Client", "Components", "Shared", "SecondaryButton.razor.css");

        Assert.Contains("background: var(--mud-palette-primary);", primary, StringComparison.Ordinal);
        Assert.Contains("color: var(--mud-palette-primary-text);", primary, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-ring);", primary, StringComparison.Ordinal);

        Assert.Contains("background: var(--maliev-panel);", secondary, StringComparison.Ordinal);
        Assert.Contains("color: var(--maliev-ink-2);", secondary, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-ring);", secondary, StringComparison.Ordinal);
    }

    [Fact]
    public void ModulePagePrimitives_UseRingSurfacesAndZeroTracking()
    {
        var modulePages = ReadRepoFile("Maliev.Intranet.Client", "wwwroot", "css", "module-pages.css");
        var mudOverrides = ReadRepoFile("Maliev.Intranet.Client", "wwwroot", "css", "mudblazor-overrides.css");

        Assert.Contains("box-shadow: var(--maliev-shadow-ring);", modulePages, StringComparison.Ordinal);
        Assert.Contains("letter-spacing: 0;", modulePages, StringComparison.Ordinal);
        Assert.Contains("border-collapse: separate;", modulePages, StringComparison.Ordinal);

        Assert.Contains(".mud-table-root", mudOverrides, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-ring);", mudOverrides, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-focus-ring);", mudOverrides, StringComparison.Ordinal);
    }

    [Fact]
    public void MudBlazorOverrides_ApplyGatewayDesignToStandardComponents()
    {
        var styles = ReadRepoFile("Maliev.Intranet.Client", "wwwroot", "css", "mudblazor-overrides.css");

        Assert.Contains(".mud-paper", styles, StringComparison.Ordinal);
        Assert.Contains("background-color: var(--maliev-panel);", styles, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--maliev-shadow-card);", styles, StringComparison.Ordinal);
        Assert.Contains(".mud-popover", styles, StringComparison.Ordinal);
        Assert.Contains(".mud-menu", styles, StringComparison.Ordinal);
        Assert.Contains(".mud-button-root.mud-button-filled-primary", styles, StringComparison.Ordinal);
        Assert.Contains("background-color: var(--mud-palette-primary) !important;", styles, StringComparison.Ordinal);
        Assert.Contains(".mud-button-root.mud-button-outlined", styles, StringComparison.Ordinal);
        Assert.Contains(".mud-button-root.mud-button-text", styles, StringComparison.Ordinal);
        Assert.Contains(".mud-table-container", styles, StringComparison.Ordinal);
        Assert.Contains(".mud-tabs-toolbar", styles, StringComparison.Ordinal);
        Assert.Contains("border-radius: var(--maliev-radius-pill);", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void Avatar_RendersInitials_FromEmployeeName()
    {
        var cut = Render<Avatar>(parameters => parameters.Add(p => p.Name, "Nattapol Thanakit"));

        Assert.Contains("NT", cut.Markup);
    }

    [Fact]
    public void CustomerCardCompact_RendersProfileImageWhenAvailable()
    {
        var cut = Render<CustomerCardCompact>(parameters => parameters
            .Add(p => p.Customer, new CustomerSummaryDto
            {
                Id = Guid.NewGuid(),
                Name = "AsianRider",
                Email = "shoung0690@gmail.com",
                ProfileImageUrl = "https://lh3.googleusercontent.com/a/asian-rider"
            }));

        var image = cut.Find(".ccc-avatar-image");
        Assert.Equal("https://lh3.googleusercontent.com/a/asian-rider", image.GetAttribute("src"));
        Assert.Equal("no-referrer", image.GetAttribute("referrerpolicy"));
    }

    [Fact]
    public void ModuleHeader_RendersTitleSubtitleAndActions()
    {
        var cut = Render<ModuleHeader>(parameters => parameters
            .Add(p => p.Title, "Customers")
            .Add(p => p.Subtitle, "Account operations")
            .AddChildContent("<button>Create</button>"));

        Assert.Contains("Customers", cut.Markup);
        Assert.Contains("Account operations", cut.Markup);
        Assert.Contains("Create", cut.Markup);
    }

    [Fact]
    public void SearchBox_RaisesValueChanged_OnInput()
    {
        string? value = null;
        var cut = Render<SearchBox>(parameters => parameters
            .Add(p => p.Placeholder, "Search customers")
            .Add(p => p.ValueChanged, next => value = next));

        cut.Find("input").Input("acme");

        Assert.Equal("acme", value);
        Assert.Contains("Search customers", cut.Markup);
    }

    private static string ReadRepoFile(params string[] relativeParts)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Unable to locate {Path.Combine(relativeParts)} from {AppContext.BaseDirectory}.");
    }
}
