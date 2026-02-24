using Bunit;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Shared;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Web;

namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>Tests for the ThinkingChain component.</summary>
public class ThinkingChainTests : BunitContext, IAsyncLifetime
{
    /// <summary>Initializes a new instance of the <see cref="ThinkingChainTests"/> class.</summary>
    public ThinkingChainTests()
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
    /// <summary>Verifies that nothing is rendered when no thinking steps are provided.</summary>
    public void ShouldRenderNothing_WhenNoSteps()
    {
        var cut = Render<ThinkingChain>(p => p.Add(x => x.Steps, new List<ThinkingStepDto>()));
        // style tag exists, but class="thinking-chain" div should NOT
        Assert.DoesNotContain("class=\"thinking-chain\"", cut.Markup);
    }

    [Fact]
    /// <summary>Verifies that the thinking steps are rendered when they are provided.</summary>
    public void ShouldRenderSteps_WhenProvided()
    {
        var steps = new List<ThinkingStepDto>
        {
            new() { Title = "Searching data...", Type = "function_call" },
            new() { Title = "Found 2 results", Type = "function_result", Detail = "Result 1, Result 2" }
        };

        var cut = Render<ThinkingChain>(p => p
            .Add(x => x.Steps, steps)
            .Add(x => x.IsProcessing, true)
        );

        Assert.Contains("Searching data...", cut.Markup);
        Assert.Contains("Found 2 results", cut.Markup);
        Assert.Contains("Result 1, Result 2", cut.Markup);
    }

    [Fact]
    /// <summary>Verifies that clicking the header toggles the expansion state of the thinking chain.</summary>
    public async Task ShouldToggleExpansion_OnHeaderClick()
    {
        var steps = new List<ThinkingStepDto>
        {
            new() { Title = "Step 1", Type = "function_result", Detail = "Detailed reasoning" }
        };

        var cut = Render<ThinkingChain>(p => p
            .Add(x => x.Steps, steps)
            .Add(x => x.IsProcessing, false) // Finished processing
        );

        // Initial state for finished processing is collapsed (IsExpanded = false)
        Assert.DoesNotContain("Detailed reasoning", cut.Markup);

        // Click header to expand
        var header = cut.Find(".cursor-pointer");
        await header.ClickAsync(new MouseEventArgs());

        // Verify expanded
        Assert.Contains("Detailed reasoning", cut.Markup);

        // Click again to collapse
        await header.ClickAsync(new MouseEventArgs());
        Assert.DoesNotContain("Detailed reasoning", cut.Markup);
    }
}
