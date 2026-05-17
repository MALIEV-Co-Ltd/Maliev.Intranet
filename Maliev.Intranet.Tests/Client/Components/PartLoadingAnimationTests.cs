using System.Runtime.CompilerServices;

using Bunit;
using Maliev.Intranet.Client.Components.Project;

namespace Maliev.Intranet.Tests.Client.Components;

public sealed class PartLoadingAnimationTests : BunitContext, IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void PartProcessingLoader_RendersCanvasSizedLoader()
    {
        var cut = Render<PartProcessingLoader>(parameters => parameters
            .Add(p => p.Class, "part-processing-loader--canvas"));

        var loader = cut.Find(".part-processing-loader");
        Assert.Contains("part-processing-loader--canvas", loader.ClassList);
        Assert.Equal("status", loader.GetAttribute("role"));
        Assert.Equal("Processing part", loader.GetAttribute("aria-label"));
    }

    [Fact]
    public void PartQueueLoader_RendersThumbnailSizedLoader()
    {
        var cut = Render<PartQueueLoader>(parameters => parameters
            .Add(p => p.Class, "part-queue-loader--thumb"));

        var loader = cut.Find(".part-queue-loader");
        Assert.Contains("part-queue-loader--thumb", loader.ClassList);
        Assert.Equal("status", loader.GetAttribute("role"));
        Assert.Equal("Part queued", loader.GetAttribute("aria-label"));

        var stage = cut.Find("svg.part-queue-loader__stage");
        Assert.Equal("-100 -100 200 200", stage.GetAttribute("viewBox"));
        Assert.Equal("true", stage.GetAttribute("aria-hidden"));

        var cubies = cut.FindAll("g.part-queue-loader__cubie");
        Assert.Equal(27, cubies.Count);
        Assert.Contains("--delay: 0s", cubies.First().GetAttribute("style"), StringComparison.Ordinal);
        Assert.Contains("--delay: 2.167s", cubies.Last().GetAttribute("style"), StringComparison.Ordinal);
        Assert.Equal(81, cut.FindAll("polygon.part-queue-loader__face-body").Count);
        Assert.Equal(81, cut.FindAll("polygon.part-queue-loader__face-sticker").Count);
    }

    [Fact]
    public void PartProcessingLoaderCss_UsesCanvasScaledProcessingAnimation()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartProcessingLoader.razor.css");

        Assert.Contains("--s: 25px", source);
        Assert.Contains("width: calc(var(--s) + var(--_d))", source);
        Assert.Contains(".part-processing-loader::before,", source);
        Assert.Contains(".part-processing-loader::after", source);
        Assert.Contains("clip-path: polygon", source);
        Assert.Contains("conic-gradient(", source);
        Assert.Contains("from -90deg at calc(100% - var(--_d)) var(--_d)", source);
        Assert.Contains("animation-delay: 0.6s", source);
        Assert.Contains("@keyframes part-processing-loader", source);
        Assert.Contains("16.67%", source);
        Assert.Contains("33.33%", source);
        Assert.Contains("transform: translateY(-10px)", source);
        Assert.Contains("transform: translateY(10px)", source);
    }

    [Fact]
    public void PartQueueLoaderCss_UsesGlobeCubeCanvasAnimation()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartQueueLoader.razor.css");

        Assert.Contains("background: transparent", source);
        Assert.Contains("color: var(--maliev-ink)", source);
        Assert.Contains("--queue-loader-duration: 7.8s", source);
        Assert.Contains("width: var(--queue-loader-size)", source);
        Assert.Contains("height: var(--queue-loader-size)", source);
        Assert.Contains(".part-queue-loader__stage", source);
        Assert.Contains(".part-queue-loader__cubie", source);
        Assert.Contains("animation: part-queue-loader-drop var(--queue-loader-duration)", source);
        Assert.Contains("animation-delay: var(--delay)", source);
        Assert.Contains("@keyframes part-queue-loader-drop", source);
        Assert.Contains("transform: translateY(-32px)", source);
        Assert.Contains("54%", source);
        Assert.Contains("66%", source);
        Assert.Contains("100%", source);
        Assert.Contains(".part-queue-loader__face-sticker--top", source);
        Assert.Contains(".part-queue-loader__face-sticker--right", source);
        Assert.Contains(".part-queue-loader__face-sticker--front", source);
        Assert.DoesNotContain("filter: blur", source, StringComparison.Ordinal);
        Assert.DoesNotContain("rotate(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("animation: part-queue-loader 1.5s infinite", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PartDetailCard_UsesCubeLoaderForEveryCanvasLoadingState()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartDetailCard.razor");

        Assert.Contains("<PartQueueLoader Class=\"part-queue-loader--canvas\"", source);
        Assert.Contains("Label=\"@ResolveCanvasLoadingLabel()\"", source);
        Assert.DoesNotContain("<PartProcessingLoader Class=\"part-processing-loader--canvas\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("<MudProgressCircular Indeterminate=\"false\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Indeterminate=\"@(!Part.Uploading)\"", source, StringComparison.Ordinal);
    }

    private static string ReadRepoFile(params string[] relativeParts)
    {
        foreach (var root in new[] { GetSourceDirectory(), AppContext.BaseDirectory, Environment.CurrentDirectory }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var current = new DirectoryInfo(root);
            while (current is not null)
            {
                var candidate = Path.Combine(new[] { current.FullName }.Concat(relativeParts).ToArray());
                if (File.Exists(candidate))
                    return File.ReadAllText(candidate);

                current = current.Parent;
            }
        }

        throw new FileNotFoundException($"Unable to locate {Path.Combine(relativeParts)} from {AppContext.BaseDirectory}.");
    }

    private static string GetSourceDirectory([CallerFilePath] string sourceFile = "") => Path.GetDirectoryName(sourceFile) ?? Environment.CurrentDirectory;
}
