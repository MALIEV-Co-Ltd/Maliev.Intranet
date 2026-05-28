namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>
/// Source-level regression tests for the sphere render-mode toggle
/// (bottom-left, outside the toolbar) in ModelViewer.razor.
/// </summary>
public sealed class ModelViewerSphereToggleSourceTests
{
    private static string Razor => ReadRepoFile("Maliev.Intranet.Client", "Components", "ModelViewer.razor");
    private static string Css   => ReadRepoFile("Maliev.Intranet.Client", "Components", "ModelViewer.razor.css");

    // ── PNG assets ──────────────────────────────────────────────────────────────

    [Fact]
    public void SphereRealisticPngExists()
    {
        var path = FindRepoFile("Maliev.Intranet.Client", "wwwroot", "images", "sphere-realistic.png");
        Assert.True(File.Exists(path), $"Expected sphere-realistic.png at: {path}");
    }

    [Fact]
    public void SphereCadPngExists()
    {
        var path = FindRepoFile("Maliev.Intranet.Client", "wwwroot", "images", "sphere-cad.png");
        Assert.True(File.Exists(path), $"Expected sphere-cad.png at: {path}");
    }

    // ── Razor: sphere toggle container ──────────────────────────────────────────

    [Fact]
    public void Razor_HasSphereToggleDiv()
        => Assert.Contains("vp-sphere-toggle", Razor, StringComparison.Ordinal);

    [Fact]
    public void Razor_HasRealisticSphereImage()
        => Assert.Contains("sphere-realistic.png", Razor, StringComparison.Ordinal);

    [Fact]
    public void Razor_HasCadSphereImage()
        => Assert.Contains("sphere-cad.png", Razor, StringComparison.Ordinal);

    [Fact]
    public void Razor_SphereButtonUsesVpSphereActiveClass()
        => Assert.Contains("vp-sphere-active", Razor, StringComparison.Ordinal);

    [Fact]
    public void Razor_SphereButtonsCallSetRenderModeAsync()
        => Assert.Contains("SetRenderModeAsync", Razor, StringComparison.Ordinal);

    [Fact]
    public void Razor_RealisticSphereButtonActivatesRealisticMode()
        => Assert.Contains("\"realistic\"", Razor, StringComparison.Ordinal);

    [Fact]
    public void Razor_CadSphereButtonActivatesSolidMode()
        => Assert.Contains("\"solid\"", Razor, StringComparison.Ordinal);

    // ── CSS: sphere toggle layout ────────────────────────────────────────────────

    [Fact]
    public void Css_HasSphereToggleClass()
        => Assert.Contains(".vp-sphere-toggle", Css, StringComparison.Ordinal);

    [Fact]
    public void Css_SphereToggleIsAbsolutePositioned()
    {
        var idx = Css.IndexOf(".vp-sphere-toggle", StringComparison.Ordinal);
        var block = Css.Substring(idx, Math.Min(300, Css.Length - idx));
        Assert.Contains("position: absolute", block, StringComparison.Ordinal);
    }

    [Fact]
    public void Css_SphereToggleIsBottomLeft()
    {
        var idx = Css.IndexOf(".vp-sphere-toggle", StringComparison.Ordinal);
        var block = Css.Substring(idx, Math.Min(300, Css.Length - idx));
        Assert.Contains("bottom: 12px", block, StringComparison.Ordinal);
        Assert.Contains("left: 12px", block, StringComparison.Ordinal);
    }

    [Fact]
    public void Css_SphereToggleIsFlexColumn()
    {
        var idx = Css.IndexOf(".vp-sphere-toggle", StringComparison.Ordinal);
        var block = Css.Substring(idx, Math.Min(300, Css.Length - idx));
        Assert.Contains("flex-direction: column", block, StringComparison.Ordinal);
    }

    // ── CSS: sphere button styles ────────────────────────────────────────────────

    [Fact]
    public void Css_HasVpSphereBtnDeepRule()
        => Assert.Contains("::deep .vp-sphere-btn", Css, StringComparison.Ordinal);

    [Fact]
    public void Css_SphereBtnIsCircular()
        => Assert.Contains("border-radius: 50%", Css, StringComparison.Ordinal);

    [Fact]
    public void Css_SphereBtnHasTransition()
        => Assert.Contains("transition:", Css, StringComparison.Ordinal);

    [Fact]
    public void Css_SphereBtnHasRestOpacity()
        => Assert.Contains("opacity: 0.55", Css, StringComparison.Ordinal);

    [Fact]
    public void Css_SphereBtnActiveHasFullOpacity()
    {
        var idx = Css.IndexOf(".vp-sphere-active", StringComparison.Ordinal);
        var block = Css.Substring(idx, Math.Min(200, Css.Length - idx));
        Assert.Contains("opacity: 1", block, StringComparison.Ordinal);
    }

    [Fact]
    public void Css_SphereBtnActiveHasBoxShadow()
    {
        var idx = Css.IndexOf(".vp-sphere-active", StringComparison.Ordinal);
        var block = Css.Substring(idx, Math.Min(200, Css.Length - idx));
        Assert.Contains("box-shadow:", block, StringComparison.Ordinal);
    }

    [Fact]
    public void Css_SphereBtnHoverScalesUp()
        => Assert.Contains("transform: scale(1.08)", Css, StringComparison.Ordinal);

    [Fact]
    public void Css_MobileOverrideRepositionsSphereToggle()
    {
        var mediaIdx = Css.LastIndexOf("@media", StringComparison.Ordinal);
        var mediaBlock = Css.Substring(mediaIdx);
        Assert.Contains("vp-sphere-toggle", mediaBlock, StringComparison.Ordinal);
    }

    // ── helpers ──────────────────────────────────────────────────────────────────

    private static string ReadRepoFile(params string[] relativeParts)
    {
        return File.ReadAllText(FindRepoFile(relativeParts));
    }

    private static string FindRepoFile(params string[] relativeParts)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
                return candidate;
            current = current.Parent;
        }
        throw new FileNotFoundException(
            $"Unable to locate {Path.Combine(relativeParts)} from {AppContext.BaseDirectory}.");
    }
}
