using Bunit;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
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
}
