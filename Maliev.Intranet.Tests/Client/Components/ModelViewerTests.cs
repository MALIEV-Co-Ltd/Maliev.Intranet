using Bunit;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Shared.Dtos;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>Tests for the ModelViewer component.</summary>
public class ModelViewerTests : BunitContext, IAsyncLifetime
{
    /// <summary>Initializes a new instance of the <see cref="ModelViewerTests"/> class.</summary>
    public ModelViewerTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Render<MudPopoverProvider>();
    }

    /// <summary>Initializes the test asynchronously.</summary>
    public Task InitializeAsync() => Task.CompletedTask;
    /// <summary>Disposes resources used by the test asynchronously.</summary>
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    /// <summary>Verifies that a canvas element is rendered by the model viewer.</summary>
    public void ShouldRenderCanvas()
    {
        var cut = Render<ModelViewer>();
        cut.Find("canvas");
    }

    [Fact]
    /// <summary>Verifies that the pending analysis state is displayed when the model has not been analyzed.</summary>
    public void ShouldDisplayPendingState_WhenNotAnalyzed()
    {
        var model = new Model3DDto { FileName = "test.stl", GeometryAnalyzed = false };
        var cut = Render<ModelViewer>(p => p.Add(x => x.Model, model));

        Assert.Contains("test.stl", cut.Markup);
        Assert.Contains("Geometry Analysis Pending...", cut.Markup);
    }

    [Fact]
    /// <summary>Verifies that geometry statistics are displayed when the model has been analyzed.</summary>
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
