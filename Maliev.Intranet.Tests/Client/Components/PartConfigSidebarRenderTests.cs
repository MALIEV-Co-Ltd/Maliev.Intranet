using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Components;

public sealed class PartConfigSidebarRenderTests : BunitContext, IAsyncLifetime
{
    public PartConfigSidebarRenderTests()
    {
        Services.AddMudServices();
        Services.AddLogging();
        Services.AddSingleton(new HttpClient { BaseAddress = new Uri("http://localhost/") });
        Services.AddSingleton<CurrencyService>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void ToleranceSection_WhenIsoAndItOptionsExist_GroupsThemSeparately()
    {
        var part = new PartViewModel
        {
            FileId = Guid.Empty,
            Name = "fixture.stl",
            ProcessCode = "CNC_MILL",
            AvailableTolerances =
            [
                new CatalogToleranceDto(Guid.NewGuid(), "Fine (ISO 2768-f)", "ISO2768_F", "ISO 2768", "f", "+-0.05mm", 15m, 10),
                new CatalogToleranceDto(Guid.NewGuid(), "Medium (ISO 2768-m)", "ISO2768_M", "ISO 2768", "m", "+-0.1mm", 0m, 20),
                new CatalogToleranceDto(Guid.NewGuid(), "IT6", "IT6", "ISO 286", "IT6", null, 60m, 30),
                new CatalogToleranceDto(Guid.NewGuid(), "IT7", "IT7", "ISO 286", "IT7", null, 35m, 40),
            ],
        };

        var cut = Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, []));

        Assert.Equal(2, cut.FindAll(".pcs-tolerance-group").Count);
        Assert.Contains("General tolerances", cut.Markup);
        Assert.Contains("Fit / precision grades", cut.Markup);
        Assert.NotEmpty(cut.FindAll(".pcs-tolerance-group--iso .pcs-tol-card"));
        Assert.NotEmpty(cut.FindAll(".pcs-tolerance-group--it .pcs-tol-card"));
    }

    [Fact]
    public void ProcessCards_WhenDescriptionIsMissing_RenderCompactFallbackDescription()
    {
        var part = new PartViewModel
        {
            FileId = Guid.Empty,
            Name = "fixture.step",
            ProcessCode = "CNC_MILL",
        };

        var cut = Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, [new ProcessDto(Guid.NewGuid(), "CNC_MILL", "CNC Milling", null, 10)]));

        var card = cut.Find(".pcs-process-card");

        Assert.Equal("CNC Milling", card.QuerySelector(".pcs-process-name")?.TextContent.Trim());
        Assert.Equal(
            "Milled parts",
            card.QuerySelector(".pcs-process-description")?.TextContent.Trim());
    }

    [Fact]
    public void ProcessCards_WhenCatalogTextIsLong_RenderCompactCardText()
    {
        var part = new PartViewModel
        {
            FileId = Guid.Empty,
            Name = "fixture.step",
            ProcessCode = "FDM",
        };

        var cut = Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(
                p => p.Processes,
                [
                    new ProcessDto(
                        Guid.NewGuid(),
                        "FDM",
                        "3D Printing (FDM)",
                        "Fused Deposition Modeling - thermoplastic filament",
                        20),
                ]));

        var card = cut.Find(".pcs-process-card");

        Assert.Equal("FDM", card.QuerySelector(".pcs-process-name")?.TextContent.Trim());
        Assert.Equal(
            "3D print",
            card.QuerySelector(".pcs-process-description")?.TextContent.Trim());
    }

    [Fact]
    public void SelectableConfiguratorItems_RenderImagePreviewsWithFallbacks()
    {
        var materialId = Guid.NewGuid();
        var finishId = Guid.NewGuid();
        var part = new PartViewModel
        {
            FileId = Guid.Empty,
            Name = "fixture.step",
            ProcessCode = "CNC_MILL",
            MaterialId = materialId,
            FinishId = finishId,
            FinishCode = "ANODIZE_CLEAR",
            AvailableMaterials =
            [
                new CatalogMaterialDto(materialId, "POM-C", "POM-C", "Plastic", null, "Acetal engineering plastic", 10),
            ],
            AvailableFinishes =
            [
                new CatalogSurfaceFinishDto(finishId, "Anodized clear", "ANODIZE_CLEAR", 1.6m, 12m, "Clear Type II anodize", 10),
            ],
            AvailableProcessOptions =
            [
                new ProcessConfigOptionDto(
                    Guid.NewGuid(),
                    "anodize_color",
                    "Anodize color",
                    "dropdown",
                    "Black",
                    "[\"Black\",\"Blue\"]",
                    null,
                    null,
                    false,
                    10),
                new ProcessConfigOptionDto(
                    Guid.NewGuid(),
                    "deburr_edges",
                    "Deburr edges",
                    "boolean",
                    null,
                    null,
                    null,
                    "Break sharp edges before shipment.",
                    false,
                    20),
            ],
        };

        var cut = Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, []));

        Assert.NotEmpty(cut.FindAll(".pcs-option-image"));
        Assert.NotEmpty(cut.FindAll(".pcs-option-image-fallback"));
        Assert.NotNull(cut.Find(".pcs-mat-card img[src='/images/materials/white-plastic-part-material.png']"));
        Assert.NotNull(cut.Find(".pcs-color-choice img[src='/images/materials/black-plastic-part-material.png']"));
        Assert.NotNull(cut.Find(".pcs-fin-card img[src='/images/materials/finish-anodized-clear-part-surface.png']"));
        Assert.NotNull(cut.Find(".pcs-color-choice img[src='/images/materials/finish-painted-blue-part-surface.png']"));
        Assert.NotNull(cut.Find(".pcs-choice-card img[src='/images/materials/deburr-edges-part-detail.png']"));
    }

    [Fact]
    public void ConfiguratorOptionImages_UsePartBasedRepresentationsForAllOptionGroups()
    {
        var materialId = Guid.NewGuid();
        var finishId = Guid.NewGuid();
        var toleranceId = Guid.NewGuid();
        var part = new PartViewModel
        {
            FileId = Guid.Empty,
            Name = "fixture.step",
            ProcessCode = "CNC_MILL",
            MaterialId = materialId,
            FinishId = finishId,
            ToleranceId = toleranceId,
            RoughnessCode = "RA_3_2",
            AvailableMaterials =
            [
                new CatalogMaterialDto(materialId, "Aluminum 6061-T6", "AL6061", "Metal", null, "Most common CNC aluminum alloy.", 10),
            ],
            AvailableFinishes =
            [
                new CatalogSurfaceFinishDto(finishId, "Bead blasted", "BEAD_BLAST", 1.6m, 12m, "Uniform satin texture", 10),
            ],
            AvailableTolerances =
            [
                new CatalogToleranceDto(toleranceId, "Medium (ISO 2768-m)", "ISO2768_M", "ISO 2768", "m", "+-0.1mm", 0m, 20),
            ],
            AvailableProcessOptions =
            [
                new ProcessConfigOptionDto(
                    Guid.NewGuid(),
                    "deburr_edges",
                    "Deburr all edges",
                    "boolean",
                    null,
                    null,
                    null,
                    "Break sharp edges before shipment.",
                    false,
                    20),
            ],
        };

        var cut = Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, []));

        Assert.NotNull(cut.Find(".pcs-mat-card img[src='/images/materials/aluminum-6061-part-material.png']"));
        Assert.NotNull(cut.Find(".pcs-fin-card img[src='/images/materials/finish-bead-blast-part-surface.png']"));
        Assert.NotNull(cut.Find(".pcs-roughness-card img[src='/images/materials/roughness-ra-3-2-part-surface.png']"));
        Assert.NotNull(cut.Find(".pcs-feature-card img[src='/images/materials/feature-tapped-holes-part.png']"));
        Assert.NotNull(cut.Find(".pcs-feature-card img[src='/images/materials/feature-thread-inserts-part.png']"));
        Assert.NotNull(cut.Find(".pcs-choice-card img[src='/images/materials/deburr-edges-part-detail.png']"));
        Assert.NotNull(cut.Find("[data-config-section='inspection'] img[src='/images/materials/inspection-standard-part-check.png']"));
    }

    [Fact]
    public void ConfiguratorOptionImages_RenderHoverPreviewPopouts()
    {
        var sidebarSource = ReadRepoFile(
                "Maliev.Intranet.Client",
                "Components",
                "Project",
                "PartConfigSidebar.razor")
            .ReplaceLineEndings("\n");
        var imageFrameSource = ReadRepoFile(
                "Maliev.Intranet.Client",
                "Components",
                "Project",
                "OptionImageFrame.razor")
            .ReplaceLineEndings("\n");

        Assert.Contains("OptionImageFrame", sidebarSource, StringComparison.Ordinal);
        Assert.Contains("class=\"pcs-image-preview-popout\"", imageFrameSource, StringComparison.Ordinal);
        Assert.Contains("<img class=\"pcs-image-preview\"", imageFrameSource, StringComparison.Ordinal);
        Assert.Contains(".pcs-option-image-frame:hover .pcs-image-preview-popout", sidebarSource, StringComparison.Ordinal);
        Assert.Contains(".pcs-option-image-frame:focus-within .pcs-image-preview-popout", sidebarSource, StringComparison.Ordinal);
        Assert.Contains("width: min(220px, 64vw);", sidebarSource, StringComparison.Ordinal);
        Assert.Contains("overflow: visible;", sidebarSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfiguratorOptionImages_RaiseHoveredCardAboveSiblingCards()
    {
        var source = ReadRepoFile(
                "Maliev.Intranet.Client",
                "Components",
                "Project",
                "PartConfigSidebar.razor")
            .ReplaceLineEndings("\n");

        Assert.Contains(".pcs-mat-card:hover,\n            .pcs-mat-card:focus-within,", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-fin-card:hover,\n            .pcs-fin-card:focus-within,", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-choice-card:hover,\n            .pcs-choice-card:focus-within,", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-color-choice:hover,\n            .pcs-color-choice:focus-within", source, StringComparison.Ordinal);
        Assert.Contains("z-index: 90;", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-option-image-frame:hover,\n            .pcs-option-image-frame:focus-within {\n                z-index: 100;", source, StringComparison.Ordinal);
        Assert.Contains("z-index: 110;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("isolation: isolate;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfiguratorOptionImages_RaiseHoveredPreviewAboveSidebarAncestorsAndFooter()
    {
        var source = ReadRepoFile(
                "Maliev.Intranet.Client",
                "Components",
                "Project",
                "PartConfigSidebar.razor")
            .ReplaceLineEndings("\n");

        Assert.Contains(".pcs-root:has(.pcs-option-image-frame:hover),", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-root:has(.pcs-option-image-frame:focus-within)", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-scroll:has(.pcs-option-image-frame:hover),", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-scroll:has(.pcs-option-image-frame:focus-within)", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-section:has(.pcs-option-image-frame:hover),", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-section:has(.pcs-option-image-frame:focus-within)", source, StringComparison.Ordinal);
        Assert.Contains("overflow: visible;\n                z-index: 220;", source, StringComparison.Ordinal);
        Assert.Contains("pointer-events: auto;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void OptionImageStyles_UseConsistentBoundedPreviewSize()
    {
        var source = ReadRepoFile(
                "Maliev.Intranet.Client",
                "Components",
                "Project",
                "PartConfigSidebar.razor")
            .ReplaceLineEndings("\n");

        Assert.Contains("grid-template-columns: 66px minmax(0, 1fr) 18px;", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-mat-swatch,\n            .pcs-fin-swatch,\n            .pcs-choice-swatch,\n            .pcs-color-chip {\n                width: 58px;\n                height: 58px;", source, StringComparison.Ordinal);
        Assert.Contains("overflow: visible;", source, StringComparison.Ordinal);
        Assert.Contains("object-fit: contain;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SurfaceFinishCards_ReservePriceAndCheckmarkColumns()
    {
        var source = ReadRepoFile(
                "Maliev.Intranet.Client",
                "Components",
                "Project",
                "PartConfigSidebar.razor")
            .ReplaceLineEndings("\n");

        Assert.Contains(".pcs-fin-card {\n                grid-template-columns: 66px minmax(0, 1fr) max-content 18px;", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-fin-price {\n                grid-column: 3;", source, StringComparison.Ordinal);
        Assert.Contains("white-space: nowrap;", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-fin-card .pcs-card-check {\n                grid-column: 4;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ThreadInsertFeatureImage_IsDistinctFromGenericSteelInsertImage()
    {
        var threadInsertPath = FindRepoFile(
            "Maliev.Intranet.Client",
            "wwwroot",
            "images",
            "materials",
            "feature-thread-inserts-part.png");
        var genericInsertPath = FindRepoFile(
            "Maliev.Intranet.Client",
            "wwwroot",
            "images",
            "materials",
            "feature-steel-inserts-part.png");

        Assert.NotEqual(File.ReadAllBytes(genericInsertPath), File.ReadAllBytes(threadInsertPath));
    }

    [Fact]
    public void ManufacturingProcessSection_TranslatesMouseWheelToHorizontalScroll()
    {
        var source = ReadRepoFile(
                "Maliev.Intranet.Client",
                "Components",
                "Project",
                "PartConfigSidebar.razor")
            .ReplaceLineEndings("\n");

        Assert.Contains("data-config-section=\"manufacturing-process\"", source, StringComparison.Ordinal);
        Assert.Contains("data-horizontal-wheel=\"true\"", source, StringComparison.Ordinal);
        Assert.Contains("scrollLeft += event.deltaY", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AnodizedGreenColorImage_UsesDedicatedAnodizedRepresentation()
    {
        var source = ReadRepoFile(
                "Maliev.Intranet.Client",
                "Components",
                "Project",
                "PartConfigSidebar.razor.cs")
            .ReplaceLineEndings("\n");

        Assert.Contains("[\"anodizedgreen\"] = \"finish-anodized-green-part-surface.png\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("[\"anodizedgreen\"] = \"finish-painted-green-part-surface.png\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MaterialPreviewAssets_MatchReferenceCanvasSize()
    {
        var referencePath = FindRepoFile(
            "Maliev.Intranet.Client",
            "wwwroot",
            "images",
            "materials",
            "aluminum-6061-part-material.png");
        var directory = Path.GetDirectoryName(referencePath)!;
        var referenceSize = ReadPngDimensions(referencePath);

        Assert.Equal((1254, 1254), referenceSize);

        foreach (var file in Directory.EnumerateFiles(directory, "*.png"))
        {
            Assert.Equal(referenceSize, ReadPngDimensions(file));
        }
    }

    [Fact]
    public void ProcessCardStyles_RenderActiveBorderAndCheckAboveImageInDarkMode()
    {
        var source = ReadRepoFile(
                "Maliev.Intranet.Client",
                "Components",
                "Project",
                "PartConfigSidebar.razor")
            .ReplaceLineEndings("\n");

        Assert.Contains(".pcs-process-card--active::after", source, StringComparison.Ordinal);
        Assert.Contains("border: 2px solid var(--mud-palette-primary);", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-process-card--active::after {\n                content: \"\";", source, StringComparison.Ordinal);
        Assert.Contains("z-index: 3;", source, StringComparison.Ordinal);
        Assert.Contains("color: var(--mud-palette-primary-contrast-text);", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-process-check {\n                position: absolute;", source, StringComparison.Ordinal);
        Assert.Contains("z-index: 4;", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-process-check .mud-icon-root {\n                color: currentColor;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessDependentConfiguration_WhenProcessIsNotSelected_IsHidden()
    {
        var part = new PartViewModel
        {
            FileId = Guid.Empty,
            Name = "fixture.step",
        };

        var cut = Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, []));

        Assert.NotNull(cut.Find("[data-config-section='manufacturing-process']"));
        Assert.Empty(cut.FindAll("[data-config-section='material']"));
        Assert.Empty(cut.FindAll("[data-config-section='surface-finish']"));
        Assert.Empty(cut.FindAll("[data-config-section='tolerance']"));
        Assert.Empty(cut.FindAll("[data-config-section='part-features']"));
        Assert.Empty(cut.FindAll("[data-config-section='inspection']"));
        Assert.Empty(cut.FindAll("[data-config-section='quantity']"));
        Assert.DoesNotContain(">Material<", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(">Surface Finish<", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(">Tolerance<", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(">Part Features<", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(">Inspection<", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(">Quantity<", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PartFeaturesAndInspection_WhenProcessIsSelected_RenderEnabled()
    {
        var part = new PartViewModel
        {
            FileId = Guid.Empty,
            Name = "fixture.step",
            ProcessCode = "CNC_MILL",
        };

        var cut = Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, []));

        Assert.All(cut.FindAll(".pcs-feature-card"), card =>
        {
            Assert.False(card.HasAttribute("disabled"));
            Assert.Equal("false", card.GetAttribute("aria-disabled"));
        });

        Assert.All(cut.FindAll("[data-config-section='inspection'] .pcs-choice-card"), card =>
        {
            Assert.False(card.HasAttribute("disabled"));
            Assert.Equal("false", card.GetAttribute("aria-disabled"));
        });
    }

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
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Unable to locate {Path.Combine(relativeParts)} from {AppContext.BaseDirectory}.");
    }

    private static (int Width, int Height) ReadPngDimensions(string path)
    {
        Span<byte> header = stackalloc byte[24];
        using var stream = File.OpenRead(path);
        var read = stream.Read(header);
        Assert.True(read == header.Length, $"Unable to read PNG header for {path}.");

        var width = ReadBigEndianInt32(header[16..20]);
        var height = ReadBigEndianInt32(header[20..24]);
        return (width, height);
    }

    private static int ReadBigEndianInt32(ReadOnlySpan<byte> bytes)
    {
        return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
    }
}
