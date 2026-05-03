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
    public void WasmLogoLoadingAnimationCss_UsesLogoMaskAndConicFill()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Shared", "WasmLogoLoadingAnimation.razor.css");

        Assert.Contains("url('/images/logo.svg')", source);
        Assert.Contains("--maliev-logo-loader-fill, #000000", source);
        Assert.Contains("--maliev-logo-loader-fill, #ffffff", source);
        Assert.Contains("conic-gradient", source);
        Assert.Contains("background-size: var(--blazor-load-percentage, 0%) 100%", source);
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
