using Bunit;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Shared.Dtos;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.Intranet.Tests.Client.Components;

public class ModelViewerTests : BunitContext, IAsyncLifetime
{
    public ModelViewerTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void ShouldRenderCanvas()
    {
        var cut = Render<ModelViewer>();
        cut.Find("canvas");
    }

    [Fact]
    public void ShouldDisplayPendingState_WhenNotAnalyzed()
    {
        var model = new Model3DDto { FileName = "test.stl", GeometryAnalyzed = false };
        var cut = Render<ModelViewer>(p => p.Add(x => x.Model, model));

        Assert.Contains("test.stl", cut.Markup);
        Assert.Contains("Geometry Analysis Pending...", cut.Markup);
    }

    [Fact]
    public void ShouldDisplayStats_WhenAnalyzed()
    {
        var model = new Model3DDto
        {
            FileName = "gear.stl",
            GeometryAnalyzed = true,
            VolumeCm3 = (double?)12.34m,
            SurfaceAreaCm2 = (double?)56.78m,
            TriangleCount = 5000,
            IsManifold = true
        };
        var cut = Render<ModelViewer>(p => p.Add(x => x.Model, model));

        Assert.Contains("gear.stl", cut.Markup);
        Assert.Contains("Volume: 12.34 cm³", cut.Markup);
        Assert.Contains("Surface Area: 56.78 cm²", cut.Markup);
        Assert.Contains("Triangles: 5,000", cut.Markup);
        Assert.Contains("Manifold: Yes", cut.Markup);
    }
}
