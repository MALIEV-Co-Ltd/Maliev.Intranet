using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Components;

public class ProjectTopBarTests : BunitContext, IAsyncLifetime
{
    private static readonly List<CurrencyDto> Currencies =
    [
        new() { Id = Guid.NewGuid(), Code = "THB", Name = "Thai Baht", Symbol = "฿", IsPrimary = true }
    ];

    public ProjectTopBarTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    private RenderedComponent<ProjectTopBar> RenderTopBar(
        bool autoSaving = false,
        DateTimeOffset? lastSavedAt = null,
        string title = "Test Project")
    {
        return Render<ProjectTopBar>(parameters => parameters
            .Add(p => p.Title, title)
            .Add(p => p.AutoSaving, autoSaving)
            .Add(p => p.LastSavedAt, lastSavedAt)
            .Add(p => p.Currencies, Currencies)
            .Add(p => p.SelectedCurrency, Currencies[0]));
    }

    [Fact]
    public void TopBar_WhenNeverSaved_DoesNotShowSavedText()
    {
        var cut = RenderTopBar(autoSaving: false, lastSavedAt: null);

        Assert.DoesNotContain("Saved", cut.Markup);
    }

    [Fact]
    public void TopBar_WhenAutoSaving_ShowsSpinnerIcon()
    {
        var cut = RenderTopBar(autoSaving: true, lastSavedAt: null);

        Assert.Contains("pn-save-icon--spin", cut.Markup);
    }

    [Fact]
    public void TopBar_WhenAutoSaving_ShowsSpinnerEvenWithLastSavedAt()
    {
        var cut = RenderTopBar(autoSaving: true, lastSavedAt: DateTimeOffset.UtcNow);

        Assert.Contains("pn-save-icon--spin", cut.Markup);
        Assert.DoesNotContain("Saved", cut.Markup);
    }

    [Fact]
    public void TopBar_WhenSavedAndNotAutoSaving_ShowsSavedBadge()
    {
        var cut = RenderTopBar(autoSaving: false, lastSavedAt: DateTimeOffset.UtcNow);

        Assert.Contains("Saved", cut.Markup);
        Assert.Contains("badge", cut.Markup);
    }

    [Fact]
    public void TopBar_WhenNeverSaved_DoesNotShowSavedBadge()
    {
        var cut = RenderTopBar(autoSaving: false, lastSavedAt: null);

        Assert.DoesNotContain(">Saved<", cut.Markup);
    }
}
