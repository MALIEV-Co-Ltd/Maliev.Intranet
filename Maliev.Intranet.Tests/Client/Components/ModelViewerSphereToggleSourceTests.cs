using System.Buffers.Binary;
using System.IO.Compression;

namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>
/// Source-level regression tests for the sphere render-mode toggle
/// (bottom-left, outside the toolbar) in ModelViewer.razor.
/// </summary>
public sealed class ModelViewerSphereToggleSourceTests
{
    private static string Razor => ReadRepoFile("Maliev.Intranet.Client", "Components", "ModelViewer.razor");
    private static string Css => ReadRepoFile("Maliev.Intranet.Client", "Components", "ModelViewer.razor.css");

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

    [Fact]
    public void SphereRealisticPngIsAlphaCutout()
    {
        var path = FindRepoFile("Maliev.Intranet.Client", "wwwroot", "images", "sphere-realistic.png");
        AssertPngHasTransparentCornersAndOpaqueCenter(path);
    }

    [Fact]
    public void SphereCadPngIsAlphaCutout()
    {
        var path = FindRepoFile("Maliev.Intranet.Client", "wwwroot", "images", "sphere-cad.png");
        AssertPngHasTransparentCornersAndOpaqueCenter(path);
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

    private static void AssertPngHasTransparentCornersAndOpaqueCenter(string path)
    {
        var image = ReadRgbaPng(path);

        Assert.Equal(6, image.ColorType);
        Assert.Equal(8, image.BitDepth);
        Assert.True(AlphaAt(image, 0, 0) == 0, $"{Path.GetFileName(path)} top-left corner should be transparent.");
        Assert.True(AlphaAt(image, image.Width - 1, 0) == 0, $"{Path.GetFileName(path)} top-right corner should be transparent.");
        Assert.True(AlphaAt(image, 0, image.Height - 1) == 0, $"{Path.GetFileName(path)} bottom-left corner should be transparent.");
        Assert.True(AlphaAt(image, image.Width - 1, image.Height - 1) == 0, $"{Path.GetFileName(path)} bottom-right corner should be transparent.");
        Assert.True(AlphaAt(image, image.Width / 2, image.Height / 2) > 200, $"{Path.GetFileName(path)} center should remain opaque.");
    }

    private static byte AlphaAt(RgbaPng image, int x, int y)
        => image.Pixels[(y * image.Width + x) * 4 + 3];

    private static RgbaPng ReadRgbaPng(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var expectedSignature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        Assert.True(bytes.AsSpan(0, expectedSignature.Length).SequenceEqual(expectedSignature), $"{path} is not a PNG file.");

        var offset = 8;
        var width = 0;
        var height = 0;
        byte bitDepth = 0;
        byte colorType = 0;
        using var compressed = new MemoryStream();
        while (offset < bytes.Length)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4));
            var type = System.Text.Encoding.ASCII.GetString(bytes, offset + 4, 4);
            var dataOffset = offset + 8;

            if (type == "IHDR")
            {
                width = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(dataOffset, 4));
                height = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(dataOffset + 4, 4));
                bitDepth = bytes[dataOffset + 8];
                colorType = bytes[dataOffset + 9];
                Assert.Equal(0, bytes[dataOffset + 12]);
            }
            else if (type == "IDAT")
            {
                compressed.Write(bytes, dataOffset, length);
            }
            else if (type == "IEND")
            {
                break;
            }

            offset = dataOffset + length + 4;
        }

        Assert.Equal(6, colorType);
        Assert.Equal(8, bitDepth);

        compressed.Position = 0;
        using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
        using var inflated = new MemoryStream();
        zlib.CopyTo(inflated);

        var raw = inflated.ToArray();
        var stride = width * 4;
        var pixels = new byte[height * stride];
        var sourceOffset = 0;
        for (var y = 0; y < height; y++)
        {
            var filter = raw[sourceOffset++];
            var rowOffset = y * stride;
            for (var x = 0; x < stride; x++)
            {
                var value = raw[sourceOffset++];
                var left = x >= 4 ? pixels[rowOffset + x - 4] : (byte)0;
                var up = y > 0 ? pixels[rowOffset - stride + x] : (byte)0;
                var upLeft = y > 0 && x >= 4 ? pixels[rowOffset - stride + x - 4] : (byte)0;
                pixels[rowOffset + x] = filter switch
                {
                    0 => value,
                    1 => unchecked((byte)(value + left)),
                    2 => unchecked((byte)(value + up)),
                    3 => unchecked((byte)(value + ((left + up) / 2))),
                    4 => unchecked((byte)(value + Paeth(left, up, upLeft))),
                    _ => throw new InvalidDataException($"Unsupported PNG filter {filter}.")
                };
            }
        }

        return new RgbaPng(width, height, bitDepth, colorType, pixels);
    }

    private static byte Paeth(byte left, byte up, byte upLeft)
    {
        var predictor = left + up - upLeft;
        var leftDistance = Math.Abs(predictor - left);
        var upDistance = Math.Abs(predictor - up);
        var upLeftDistance = Math.Abs(predictor - upLeft);
        if (leftDistance <= upDistance && leftDistance <= upLeftDistance)
            return left;
        return upDistance <= upLeftDistance ? up : upLeft;
    }

    private sealed record RgbaPng(int Width, int Height, byte BitDepth, byte ColorType, byte[] Pixels);
}
