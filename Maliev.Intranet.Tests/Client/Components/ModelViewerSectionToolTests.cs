using Bunit;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Components;

public sealed class ModelViewerSectionToolTests : BunitContext, IAsyncLifetime
{
    public ModelViewerSectionToolTests()
    {
        Services.AddMudServices();
        Services.AddSingleton<LayoutService>(new LayoutService(JSInterop.JSRuntime, NullLogger<LayoutService>.Instance, null));
        JSInterop.Mode = JSRuntimeMode.Loose;
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void ModelViewer_WhenSectionViewIsEnabled_ShowsPlaneAndOffsetControls()
    {
        var cut = Render<ModelViewer>(parameters => parameters
            .Add(p => p.ViewerSettings, new PartViewerSettings
            {
                SectionEnabled = true,
                SectionAxis = "y",
                SectionOffsetMm = 4.5,
            }));

        Assert.Contains("Enable section view", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Offset:", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("4.5 mm", cut.Markup, StringComparison.Ordinal);

        cut.Find(".model-viewer-container").Click();

        Assert.Contains("Enable section view", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Offset:", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ModelViewer_WhenSectionButtonIsClicked_RendersPositionedPanelShell()
    {
        var cut = Render<ModelViewer>();

        cut.Find(".vp-analysis-rail button").Click();

        var panel = cut.Find(".vp-section-panel");
        Assert.Equal("DIV", panel.TagName);
        Assert.False(string.IsNullOrWhiteSpace(panel.GetAttribute("id")));
        Assert.NotNull(panel.QuerySelector("[data-section-drag-handle]"));
        Assert.Contains("Enable section view", panel.TextContent, StringComparison.Ordinal);
        Assert.Contains("Offset:", panel.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ModelViewer_TemporaryGridOwner_DoesNotPersistViewerSettings()
    {
        var changedSettings = new List<PartViewerSettings>();
        var cut = Render<ModelViewer>(parameters => parameters
            .Add(p => p.ViewerSettings, new PartViewerSettings
            {
                GridEnabled = false,
            })
            .Add(
                p => p.ViewerSettingsChanged,
                EventCallback.Factory.Create<PartViewerSettings>(
                    this,
                    changedSettings.Add)));

        Assert.False(cut.Instance.UserGridEnabled);
        Assert.False(cut.Instance.IsGridVisible);

        await cut.Instance.SetTemporaryGridVisibilityAsync("dfm:overhang", true);

        Assert.False(cut.Instance.UserGridEnabled);
        Assert.True(cut.Instance.IsGridVisible);

        await cut.Instance.SetTemporaryGridVisibilityAsync("dfm:overhang", false);

        Assert.False(cut.Instance.UserGridEnabled);
        Assert.False(cut.Instance.IsGridVisible);
        Assert.Empty(changedSettings);
    }

    [Fact]
    public async Task ModelViewer_TemporaryGridOwner_DoesNotHideUserEnabledGrid()
    {
        var changedSettings = new List<PartViewerSettings>();
        var cut = Render<ModelViewer>(parameters => parameters
            .Add(p => p.ViewerSettings, new PartViewerSettings
            {
                GridEnabled = true,
            })
            .Add(
                p => p.ViewerSettingsChanged,
                EventCallback.Factory.Create<PartViewerSettings>(
                    this,
                    changedSettings.Add)));

        Assert.True(cut.Instance.UserGridEnabled);
        Assert.True(cut.Instance.IsGridVisible);

        await cut.Instance.SetTemporaryGridVisibilityAsync("dfm:overhang", true);
        await cut.Instance.SetTemporaryGridVisibilityAsync("dfm:overhang", false);

        Assert.True(cut.Instance.UserGridEnabled);
        Assert.True(cut.Instance.IsGridVisible);
        Assert.Empty(changedSettings);
    }
}
