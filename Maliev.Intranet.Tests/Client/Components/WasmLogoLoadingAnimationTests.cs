using Bunit;
using Maliev.Intranet.Client.Components.Shared;

namespace Maliev.Intranet.Tests.Client.Components;

public sealed class WasmLogoLoadingAnimationTests : BunitContext, IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void WasmLogoLoadingAnimation_RendersLogoLoaderMarkup()
    {
        var cut = Render<WasmLogoLoadingAnimation>();

        var loader = cut.Find(".loader");
        Assert.Equal("img", loader.GetAttribute("role"));
        Assert.Equal("MALIEV", loader.GetAttribute("aria-label"));
        Assert.Contains("maliev-logo-loader", cut.Markup);
    }

    [Fact]
    public void WasmLogoLoadingAnimationCss_UsesLogoMaskAndProgressFill()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Shared", "WasmLogoLoadingAnimation.razor.css");

        Assert.Contains("url('/images/logo.svg')", source);
        Assert.Contains("--logo-empty: var(--wasm-logo-empty, #ffffff)", source);
        Assert.Contains("--logo-fill: var(--wasm-logo-fill, #000000)", source);
        Assert.Contains("--logo-progress: var(--wasm-logo-progress, 0%)", source);
        Assert.Contains("--logo-shadow: var(--maliev-logo-loader-shadow", source);
        Assert.Contains("drop-shadow(0 20px 32px rgba(10, 20, 40, 0.34))", source);
        Assert.Contains("filter: var(--logo-shadow)", source);
        Assert.Contains("linear-gradient(", source);
        Assert.Contains("90deg", source);
        Assert.DoesNotContain("conic-gradient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("background-size: var(--blazor-load-percentage, 0%) 100%", source, StringComparison.Ordinal);
        Assert.DoesNotContain("--logo-progress: var(--blazor-load-percentage", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".loader::after", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@keyframes maliev-logo-load", source, StringComparison.Ordinal);
        Assert.DoesNotContain("animation:", source, StringComparison.Ordinal);
        Assert.DoesNotContain(":host-context", source, StringComparison.Ordinal);
    }

    private static string ReadRepoFile(params string[] relativeParts)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);

            current = current.Parent;
        }

        throw new FileNotFoundException($"Unable to locate {Path.Combine(relativeParts)} from {AppContext.BaseDirectory}.");
    }
}
