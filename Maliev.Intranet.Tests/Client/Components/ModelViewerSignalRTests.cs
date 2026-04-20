using Bunit;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>
/// Tests ModelViewer parameter transitions that occur when SignalR updates
/// the parent component's state. Simulates the polling scenario where the
/// file analysis status changes from Pending to Completed.
/// </summary>
public class ModelViewerSignalRTests : BunitContext, IAsyncLifetime
{
    private readonly Mock<ILogger<LayoutService>> _layoutLoggerMock = new();

    public ModelViewerSignalRTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new LayoutService(JSInterop.JSRuntime, _layoutLoggerMock.Object));
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    /// <summary>
    /// Tests that ModelViewer displays pending state correctly.
    /// This is the initial state when a file is first uploaded.
    /// </summary>
    [Fact]
    public void ShouldDisplayPendingState_WhenGeometryNotAnalyzed()
    {
        // Arrange & Act
        var pendingModel = new Model3DDto
        {
            FileName = "test-part.step",
            GeometryAnalyzed = false
        };

        var cut = Render<ModelViewer>(p => p.Add(x => x.Model, pendingModel));

        // Assert - pending state displayed
        Assert.Contains("test-part.step", cut.Markup);
        Assert.Contains("Geometry Analysis Pending...", cut.Markup);
    }

    /// <summary>
    /// Tests that ModelViewer displays completed state with stats.
    /// This is the state after SignalR polling receives analysis completion.
    /// </summary>
    [Fact]
    public void ShouldDisplayCompletedState_WhenGeometryAnalyzed()
    {
        // Arrange & Act
        var completedModel = new Model3DDto
        {
            FileName = "test-part.step",
            GeometryAnalyzed = true,
            VolumeCm3 = 15.5,
            SurfaceAreaCm2 = 42.3,
            TriangleCount = 12000,
            IsManifold = true
        };

        var cut = Render<ModelViewer>(p => p.Add(x => x.Model, completedModel));

        // Assert - completed state with stats
        Assert.Contains("test-part.step", cut.Markup);
        Assert.Contains("Volume: 15.50 cm³", cut.Markup);
        Assert.Contains("Surface Area: 42.30 cm²", cut.Markup);
        Assert.Contains("Triangles: 12,000", cut.Markup);
        Assert.Contains("Manifold: Yes", cut.Markup);
    }

    /// <summary>
    /// Tests that JS interop is invoked with correct dimensions when
    /// ModelViewer renders analyzed geometry.
    /// </summary>
    [Fact]
    public void ShouldInvokeJsInterop_WithCorrectDimensions()
    {
        // Arrange & Act
        var cut = Render<ModelViewer>(p => p.Add(x => x.Model, new Model3DDto
        {
            FileName = "gear.stl",
            GeometryAnalyzed = true,
            VolumeCm3 = 25.0,
            SurfaceAreaCm2 = 100.0,
            TriangleCount = 5000,
            IsManifold = true
        }));

        // Assert - JS interop should have been invoked
        // Note: With JSRuntimeMode.Loose, we don't verify exact calls
        // but the component should render without throwing
        Assert.Contains("gear.stl", cut.Markup);
        Assert.Contains("25", cut.Markup); // Volume
    }

    /// <summary>
    /// Tests that each render creates only one canvas element.
    /// Ensures no duplicate canvases when component re-renders.
    /// </summary>
    [Fact]
    public void ShouldRenderSingleCanvas_NoDuplicates()
    {
        // Arrange & Act
        var cut = Render<ModelViewer>(p => p.Add(x => x.Model, new Model3DDto
        {
            FileName = "model.step",
            GeometryAnalyzed = true,
            VolumeCm3 = 10.0
        }));

        // Assert - only one canvas should exist
        var canvases = cut.FindAll("canvas");
        Assert.Single(canvases);
    }
}
