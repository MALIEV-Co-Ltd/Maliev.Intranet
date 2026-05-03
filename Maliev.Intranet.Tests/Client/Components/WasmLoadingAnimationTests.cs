using Bunit;
using Maliev.Intranet.Client.Components.Shared;

namespace Maliev.Intranet.Tests.Client.Components;

public sealed class WasmLoadingAnimationTests : BunitContext, IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void WasmLoadingAnimation_RendersFourBoxesWithFourFacesEach()
    {
        var cut = Render<WasmLoadingAnimation>();

        var boxes = cut.FindAll(".box");
        Assert.Equal(4, boxes.Count);
        Assert.All(boxes, box => Assert.Equal(4, box.Children.Length));
        Assert.Contains("boxes", cut.Markup);
        Assert.Contains("aria-hidden=\"true\"", cut.Markup);
    }
}
