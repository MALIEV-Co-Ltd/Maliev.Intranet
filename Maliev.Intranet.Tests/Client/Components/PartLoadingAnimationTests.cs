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
    public void PartQueueLoaderCss_UsesQueuedThumbnailAnimation()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartQueueLoader.razor.css");

        Assert.Contains("background: transparent", source);
        Assert.Contains(".part-queue-loader::before", source);
        Assert.Contains("width: 80px", source);
        Assert.Contains("border: 10px solid #000", source);
        Assert.Contains("radial-gradient(farthest-side, #fff 98%, #0000)", source);
        Assert.Contains("#000;", source);
        Assert.Contains("filter: blur(4px) contrast(10)", source);
        Assert.Contains("@keyframes part-queue-loader", source);
        Assert.Contains("50% -20px", source);
        Assert.Contains("-20px 50%", source);
        Assert.Contains("60px 50%", source);
        Assert.Contains("50% 60px", source);
    }

    [Fact]
    public void PartDetailCard_UsesCanvasSizedLoadersForQueuedAndProcessingStates()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartDetailCard.razor");

        Assert.Contains("<PartQueueLoader Class=\"part-queue-loader--canvas\"", source);
        Assert.Contains("<PartProcessingLoader Class=\"part-processing-loader--canvas\"", source);
        Assert.Contains("Indeterminate=\"false\"", source);
        Assert.DoesNotContain("Indeterminate=\"@(!Part.Uploading)\"", source, StringComparison.Ordinal);
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
