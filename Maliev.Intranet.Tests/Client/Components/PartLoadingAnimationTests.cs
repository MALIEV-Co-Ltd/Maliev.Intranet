using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

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
        Assert.Contains("--queue-loader-duration: 6.0s", source);
        Assert.Contains("width: var(--queue-loader-size)", source);
        Assert.Contains("height: var(--queue-loader-size)", source);
        Assert.Contains(".part-queue-loader__stage", source);
        Assert.Contains(".part-queue-loader__cubie", source);
        Assert.Contains("animation: part-queue-loader-drop var(--queue-loader-duration)", source);
        Assert.Contains("animation-delay: calc(var(--delay) * -1)", source);
        Assert.Contains("will-change: opacity, transform", source);
        Assert.Contains("@keyframes part-queue-loader-drop", source);
        Assert.Contains("transform: translateY(-32px)", source);
        Assert.Contains("72%", source);
        Assert.Contains("84%", source);
        Assert.Contains("100%", source);
        Assert.Contains(".part-queue-loader__face-sticker--top", source);
        Assert.Contains(".part-queue-loader__face-sticker--right", source);
        Assert.Contains(".part-queue-loader__face-sticker--front", source);
        Assert.DoesNotContain("filter: blur", source, StringComparison.Ordinal);
        Assert.DoesNotContain("rotate(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("animation: part-queue-loader 1.5s infinite", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PartQueueLoaderCss_StartsInMotionAndAvoidsBlankLoopGaps()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "Project", "PartQueueLoader.razor.css");
        var cut = Render<PartQueueLoader>();

        Assert.Contains("animation-delay: calc(var(--delay) * -1)", source);
        Assert.DoesNotContain("animation-delay: var(--delay)", source, StringComparison.Ordinal);

        var durationSeconds = ExtractCssSeconds(source, "--queue-loader-duration");
        var maxDelaySeconds = cut.FindAll("g.part-queue-loader__cubie")
            .Select(cubie => ExtractInlineSeconds(cubie.GetAttribute("style") ?? string.Empty, "--delay"))
            .Max();
        var visibleStartPercent = ExtractFirstOpacityPercent(source, "1");
        var invisiblePercent = ExtractFirstOpacityPercentAfter(source, "0", visibleStartPercent);
        var visibleWindowSeconds = (invisiblePercent - visibleStartPercent) / 100d * durationSeconds;

        Assert.True(
            visibleWindowSeconds + maxDelaySeconds >= durationSeconds,
            $"Cubies can all be invisible for {durationSeconds - visibleWindowSeconds - maxDelaySeconds:0.###}s each loop.");
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

    private static double ExtractCssSeconds(string source, string propertyName)
    {
        var match = Regex.Match(source, $@"{Regex.Escape(propertyName)}:\s*(?<value>[0-9.]+)s", RegexOptions.CultureInvariant);
        Assert.True(match.Success, $"Expected CSS seconds property {propertyName}.");

        return double.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
    }

    private static double ExtractInlineSeconds(string style, string propertyName)
    {
        var match = Regex.Match(style, $@"{Regex.Escape(propertyName)}:\s*(?<value>[0-9.]+)s", RegexOptions.CultureInvariant);
        Assert.True(match.Success, $"Expected inline seconds property {propertyName}.");

        return double.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
    }

    private static double ExtractFirstOpacityPercent(string source, string opacity)
    {
        var matches = Regex.Matches(
            source,
            $@"(?ms)(?<percent>[0-9.]+)%\s*\{{[^}}]*opacity:\s*{Regex.Escape(opacity)};",
            RegexOptions.CultureInvariant);
        Assert.True(matches.Count > 0, $"Expected opacity {opacity} keyframe.");

        return matches
            .Select(match => double.Parse(match.Groups["percent"].Value, CultureInfo.InvariantCulture))
            .Min();
    }

    private static double ExtractFirstOpacityPercentAfter(string source, string opacity, double afterPercent)
    {
        var matches = Regex.Matches(
            source,
            $@"(?ms)(?<percent>[0-9.]+)%\s*\{{[^}}]*opacity:\s*{Regex.Escape(opacity)};",
            RegexOptions.CultureInvariant);
        var laterMatches = matches
            .Select(match => double.Parse(match.Groups["percent"].Value, CultureInfo.InvariantCulture))
            .Where(percent => percent > afterPercent)
            .ToArray();

        Assert.True(laterMatches.Length > 0, $"Expected opacity {opacity} keyframe after {afterPercent}%.");

        return laterMatches.Min();
    }
}
