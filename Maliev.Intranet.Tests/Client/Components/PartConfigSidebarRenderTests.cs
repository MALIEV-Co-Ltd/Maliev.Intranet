using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Components.Web;
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
    public async Task ProcessCard_WhenAlreadySelected_DoesNotRunDfmAnalysisAgain()
    {
        var process = new ProcessDto(Guid.NewGuid(), "FDM", "FDM", null, 10);
        var dfmRequestCount = 0;
        Services.AddSingleton(new HttpClient(new MockHttpMessageHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Post &&
                request.RequestUri?.AbsolutePath == "/api/v1/geometry/f662e601-227c-4ac6-949a-f33b8f598db4/dfm/FDM")
            {
                dfmRequestCount++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new DfmAnalysisResponse
                    {
                        UploadId = "f662e601-227c-4ac6-949a-f33b8f598db4",
                        ProcessCode = "FDM",
                        Status = "analysis_complete",
                        DfmReport = new DfmReport { ReportType = "FDM" }
                    })
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }))
        { BaseAddress = new Uri("http://localhost/") });

        var part = new PartViewModel
        {
            FileId = Guid.Parse("f662e601-227c-4ac6-949a-f33b8f598db4"),
            Name = "fixture.stl",
            StoragePath = "projects/test/fixture.stl",
            ProcessId = process.Id,
            ProcessCode = process.Code,
        };

        var cut = Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, [process]));

        await cut.Find(".pcs-process-change-btn").ClickAsync(new MouseEventArgs());
        await cut.Find(".pcs-process-card--active").ClickAsync(new MouseEventArgs());

        Assert.Equal(0, dfmRequestCount);
    }

    [Fact]
    public async Task AnalyzeProcessForDfm_WhenBrowserLocalReportIsCurrent_PreservesCatalogOptions()
    {
        var process = new ProcessDto(Guid.NewGuid(), "CNC_MILL", "CNC milling", null, 10);
        var material = new CatalogMaterialDto(Guid.NewGuid(), "Brass C360", "BRASS_C360", "Metal", null, null, 10);
        var finish = new CatalogSurfaceFinishDto(Guid.NewGuid(), "As-machined", "AS_MACHINED", 1.6m, 0m, null, 10);
        var tolerance = new CatalogToleranceDto(Guid.NewGuid(), "General tolerances", "GENERAL", "ISO 2768", "m", "+-0.1mm", 0m, 10);
        var report = new DfmReport { ReportType = "CNC_MILL" };
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "browser-local.stl",
            ProcessId = process.Id,
            ProcessCode = process.Code,
            DfmReport = report,
            CncDfmReport = report,
            AvailableMaterials = [material],
            AvailableFinishes = [finish],
            AvailableTolerances = [tolerance],
        };

        var cut = Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, [process]));

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "AnalyzeProcessForDfm", process));

        Assert.Single(part.AvailableMaterials);
        Assert.Same(material, part.AvailableMaterials[0]);
        Assert.Single(part.AvailableFinishes);
        Assert.Same(finish, part.AvailableFinishes[0]);
        Assert.Single(part.AvailableTolerances);
        Assert.Same(tolerance, part.AvailableTolerances[0]);
    }

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
            "Multi-axis subtractive milling from solid billet",
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
            "Fused Deposition Modeling - thermoplastic filament",
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
        Assert.NotNull(cut.Find(".pcs-mat-card img[src='/images/materials/white-plastic-part-material.webp']"));
        Assert.NotNull(cut.Find(".pcs-color-choice img[src='/images/materials/black-plastic-part-material.webp']"));
        Assert.NotNull(cut.Find(".pcs-fin-card img[src='/images/materials/finish-anodized-clear-part-surface.webp']"));
        Assert.NotNull(cut.Find(".pcs-color-choice img[src='/images/materials/finish-painted-blue-part-surface.webp']"));
        Assert.NotNull(cut.Find(".pcs-choice-card img[src='/images/materials/deburr-edges-part-detail.webp']"));
    }

    [Fact]
    public void FinishColorOptions_WhenAnodizeIsSelected_RenderAfterSurfaceFinishBeforeTolerance()
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
            FinishCode = "ANODIZE_TYPE_II",
            ToleranceId = toleranceId,
            AvailableMaterials =
            [
                new CatalogMaterialDto(materialId, "Aluminum 6061", "AL6061", "Metal", null, "CNC aluminum", 10),
            ],
            AvailableFinishes =
            [
                new CatalogSurfaceFinishDto(finishId, "Anodized Type II", "ANODIZE_TYPE_II", 1.6m, 12m, "Decorative colored anodize", 10),
            ],
            AvailableTolerances =
            [
                new CatalogToleranceDto(toleranceId, "Medium (ISO 2768-m)", "ISO2768_M", "ISO 2768", "m", "+-0.1mm", 0m, 20),
            ],
            AvailableProcessOptions =
            [
                new ProcessConfigOptionDto(
                    Guid.NewGuid(),
                    "anodize_color",
                    "Anodize color",
                    "dropdown",
                    "Black",
                    "[\"Clear\",\"Black\",\"Blue\"]",
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

        var surfaceIndex = cut.Markup.IndexOf("data-config-section=\"surface-finish\"", StringComparison.Ordinal);
        var finishOptionsIndex = cut.Markup.IndexOf("data-config-section=\"color-options\"", StringComparison.Ordinal);
        var toleranceIndex = cut.Markup.IndexOf("data-config-section=\"tolerance\"", StringComparison.Ordinal);

        Assert.True(surfaceIndex >= 0);
        Assert.True(finishOptionsIndex > surfaceIndex);
        Assert.True(toleranceIndex > finishOptionsIndex);
        Assert.Contains("Anodize color", cut.Find("[data-config-section='color-options']").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Anodize color", cut.Find("[data-config-section='process-options']").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void FinishColorOptions_WhenPowderCoatHasNoCatalogColorOption_RenderStandardColorChoicesBeforeTolerance()
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
            FinishCode = "POWDER_COATED",
            ToleranceId = toleranceId,
            AvailableMaterials =
            [
                new CatalogMaterialDto(materialId, "Aluminum 6061", "AL6061", "Metal", null, "CNC aluminum", 10),
            ],
            AvailableFinishes =
            [
                new CatalogSurfaceFinishDto(finishId, "Powder Coated", "POWDER_COATED", 3.2m, 8m, "Durable colored coating", 10),
            ],
            AvailableTolerances =
            [
                new CatalogToleranceDto(toleranceId, "Medium (ISO 2768-m)", "ISO2768_M", "ISO 2768", "m", "+-0.1mm", 0m, 20),
            ],
        };

        var cut = Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, []));

        var finishOptions = cut.Find("[data-config-section='color-options']");
        var finishOptionsIndex = cut.Markup.IndexOf("data-config-section=\"color-options\"", StringComparison.Ordinal);
        var toleranceIndex = cut.Markup.IndexOf("data-config-section=\"tolerance\"", StringComparison.Ordinal);

        Assert.True(toleranceIndex > finishOptionsIndex);
        Assert.Contains("Powder coat color", finishOptions.TextContent, StringComparison.Ordinal);
        Assert.Contains("Black", finishOptions.TextContent, StringComparison.Ordinal);
        Assert.Contains("Blue", finishOptions.TextContent, StringComparison.Ordinal);
        Assert.Contains("Custom", finishOptions.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void MaterialColorOption_WhenMijfNaturalGreyIsAvailable_UsesNaturalGreyThumbnail()
    {
        var materialId = Guid.NewGuid();
        var part = new PartViewModel
        {
            FileId = Guid.Empty,
            Name = "fixture.step",
            ProcessCode = "MJF",
            MaterialId = materialId,
            AvailableMaterials =
            [
                new CatalogMaterialDto(materialId, "PA12", "PA12", "Plastic", null, "MJF nylon powder", 10),
            ],
            AvailableProcessOptions =
            [
                new ProcessConfigOptionDto(
                    Guid.NewGuid(),
                    "material_color",
                    "Material color",
                    "dropdown",
                    "Natural Grey",
                    "[\"Natural Grey\",\"Natural Gray\",\"Gray\"]",
                    null,
                    null,
                    false,
                    10),
            ],
        };

        var cut = Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, []));

        Assert.NotNull(cut.Find(".pcs-color-choice img[src='/images/materials/natural-grey-plastic-part-material.webp']"));
    }

    [Fact]
    public void PowderFusionMaterial_WhenPa12IsSelected_UsesNaturalGreyPowderThumbnail()
    {
        var materialId = Guid.NewGuid();
        var part = new PartViewModel
        {
            FileId = Guid.Empty,
            Name = "fixture.step",
            ProcessCode = "SLS",
            MaterialId = materialId,
            AvailableMaterials =
            [
                new CatalogMaterialDto(materialId, "PA12 Nylon", "PA12", "Plastic", null, "SLS nylon powder", 10),
            ],
        };

        var cut = Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, []));

        Assert.NotNull(cut.Find(".pcs-mat-card img[src='/images/materials/natural-grey-plastic-part-material.webp']"));
    }

    [Fact]
    public void PowderFusionColorOption_WhenFinishIsRaw_IsLimitedToNaturalGrey()
    {
        var materialId = Guid.NewGuid();
        var finishId = Guid.NewGuid();
        var part = new PartViewModel
        {
            FileId = Guid.Empty,
            Name = "fixture.step",
            ProcessCode = "MJF",
            MaterialId = materialId,
            FinishId = finishId,
            FinishCode = "AS_PRINTED",
            AvailableMaterials =
            [
                new CatalogMaterialDto(materialId, "PA12", "PA12", "Plastic", null, "MJF nylon powder", 10),
            ],
            AvailableFinishes =
            [
                new CatalogSurfaceFinishDto(finishId, "As printed", "AS_PRINTED", 0m, 0m, "Raw natural grey powder finish", 10),
            ],
            AvailableProcessOptions =
            [
                new ProcessConfigOptionDto(
                    Guid.NewGuid(),
                    "material_color",
                    "Material color",
                    "dropdown",
                    "Black",
                    "[\"Black\",\"White\",\"Natural Grey\"]",
                    null,
                    null,
                    false,
                    10),
            ],
        };

        var cut = Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, []));

        var colorChoices = cut.FindAll(".pcs-color-choice");

        Assert.Single(colorChoices);
        Assert.Contains("Natural Grey", colorChoices.First().TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain(colorChoices, choice => choice.TextContent.Contains("Black", StringComparison.Ordinal));
        Assert.DoesNotContain(colorChoices, choice => choice.TextContent.Contains("White", StringComparison.Ordinal));
        Assert.NotNull(cut.Find(".pcs-color-choice img[src='/images/materials/natural-grey-plastic-part-material.webp']"));
    }

    [Fact]
    public void PowderFusionColorOption_WhenFinishIsDyed_AllowsOnlyDyeColors()
    {
        var materialId = Guid.NewGuid();
        var finishId = Guid.NewGuid();
        var part = new PartViewModel
        {
            FileId = Guid.Empty,
            Name = "fixture.step",
            ProcessCode = "SLS",
            MaterialId = materialId,
            FinishId = finishId,
            FinishCode = "DYED_BLACK",
            AvailableMaterials =
            [
                new CatalogMaterialDto(materialId, "PA12", "PA12", "Plastic", null, "SLS nylon powder", 10),
            ],
            AvailableFinishes =
            [
                new CatalogSurfaceFinishDto(finishId, "Dyed black", "DYED_BLACK", 0m, 0m, "Black dye post processing", 10),
            ],
            AvailableProcessOptions =
            [
                new ProcessConfigOptionDto(
                    Guid.NewGuid(),
                    "material_color",
                    "Material color",
                    "dropdown",
                    "Natural Grey",
                    "[\"Black\",\"White\",\"Natural Grey\"]",
                    null,
                    null,
                    false,
                    10),
            ],
        };

        var cut = Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, []));

        var colorChoices = cut.FindAll(".pcs-color-choice");

        Assert.Equal(7, colorChoices.Count);
        Assert.Contains(colorChoices, choice => choice.TextContent.Contains("Black", StringComparison.Ordinal));
        Assert.Contains(colorChoices, choice => choice.TextContent.Contains("Red", StringComparison.Ordinal));
        Assert.Contains(colorChoices, choice => choice.TextContent.Contains("Blue", StringComparison.Ordinal));
        Assert.Contains(colorChoices, choice => choice.TextContent.Contains("Green", StringComparison.Ordinal));
        Assert.Contains(colorChoices, choice => choice.TextContent.Contains("Yellow", StringComparison.Ordinal));
        Assert.Contains(colorChoices, choice => choice.TextContent.Contains("Orange", StringComparison.Ordinal));
        Assert.Contains(colorChoices, choice => choice.TextContent.Contains("Pink", StringComparison.Ordinal));
        Assert.DoesNotContain(colorChoices, choice => choice.TextContent.Contains("Natural Grey", StringComparison.Ordinal));
        Assert.NotNull(cut.Find(".pcs-color-choice img[src='/images/materials/dyed-black-powder-fusion-part-material.webp']"));
        Assert.NotNull(cut.Find(".pcs-color-choice img[src='/images/materials/dyed-red-powder-fusion-part-material.webp']"));
        Assert.NotNull(cut.Find(".pcs-color-choice img[src='/images/materials/dyed-blue-powder-fusion-part-material.webp']"));
        Assert.NotNull(cut.Find(".pcs-color-choice img[src='/images/materials/dyed-green-powder-fusion-part-material.webp']"));
        Assert.NotNull(cut.Find(".pcs-color-choice img[src='/images/materials/dyed-yellow-powder-fusion-part-material.webp']"));
        Assert.NotNull(cut.Find(".pcs-color-choice img[src='/images/materials/dyed-orange-powder-fusion-part-material.webp']"));
        Assert.NotNull(cut.Find(".pcs-color-choice img[src='/images/materials/dyed-pink-powder-fusion-part-material.webp']"));
    }

    [Fact]
    public void PowderFusionSurfaceFinish_WhenRaw_UsesNaturalGreyPowderThumbnail()
    {
        var finishId = Guid.NewGuid();
        var part = new PartViewModel
        {
            FileId = Guid.Empty,
            Name = "fixture.step",
            ProcessCode = "MJF",
            FinishId = finishId,
            FinishCode = "AS_PRINTED",
            AvailableFinishes =
            [
                new CatalogSurfaceFinishDto(finishId, "As printed", "AS_PRINTED", 0m, 0m, "Raw natural grey powder finish", 10),
            ],
        };

        var cut = Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, []));

        Assert.NotNull(cut.Find(".pcs-fin-card img[src='/images/materials/natural-grey-plastic-part-material.webp']"));
    }

    [Fact]
    public void PowderFusionSurfaceFinish_WhenDyed_UsesDyedBlackPowderThumbnail()
    {
        var finishId = Guid.NewGuid();
        var part = new PartViewModel
        {
            FileId = Guid.Empty,
            Name = "fixture.step",
            ProcessCode = "MJF",
            FinishId = finishId,
            FinishCode = "DYED_BLACK",
            AvailableFinishes =
            [
                new CatalogSurfaceFinishDto(finishId, "Dyed black", "DYED_BLACK", 0m, 0m, "Black dye post processing", 10),
            ],
        };

        var cut = Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, []));

        Assert.NotNull(cut.Find(".pcs-fin-card img[src='/images/materials/dyed-black-powder-fusion-part-material.webp']"));
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

        Assert.NotNull(cut.Find(".pcs-mat-card img[src='/images/materials/aluminum-6061-part-material.webp']"));
        Assert.NotNull(cut.Find(".pcs-fin-card img[src='/images/materials/finish-bead-blast-part-surface.webp']"));
        Assert.NotNull(cut.Find(".pcs-roughness-card img[src='/images/materials/roughness-ra-3-2-part-surface.webp']"));
        Assert.NotNull(cut.Find(".pcs-feature-card img[src='/images/materials/feature-tapped-holes-part.webp']"));
        Assert.NotNull(cut.Find(".pcs-feature-card img[src='/images/materials/feature-thread-inserts-part.webp']"));
        Assert.NotNull(cut.Find(".pcs-choice-card img[src='/images/materials/deburr-edges-part-detail.webp']"));
        Assert.NotNull(cut.Find("[data-config-section='inspection'] img[src='/images/materials/inspection-standard-part-check.webp']"));
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
    public void ConfiguratorOptionImages_RaiseOnlyImageHoveredCardAboveSiblingCards()
    {
        var source = ReadRepoFile(
                "Maliev.Intranet.Client",
                "Components",
                "Project",
                "PartConfigSidebar.razor")
            .ReplaceLineEndings("\n");

        Assert.DoesNotContain(".pcs-mat-card:hover,\n            .pcs-mat-card:focus-within,", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".pcs-fin-card:hover,\n            .pcs-fin-card:focus-within,", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".pcs-choice-card:hover,\n            .pcs-choice-card:focus-within,", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".pcs-color-choice:hover,\n            .pcs-color-choice:focus-within,", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-mat-card:has(.pcs-option-image-frame:hover),", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-fin-card:has(.pcs-option-image-frame:hover),", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-choice-card:has(.pcs-option-image-frame:hover),", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-color-choice:has(.pcs-option-image-frame:hover)", source, StringComparison.Ordinal);
        Assert.Contains("z-index: 320;", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-option-image-frame:hover,\n            .pcs-option-image-frame:focus-within {\n                z-index: 330;", source, StringComparison.Ordinal);
        Assert.Contains("z-index: 340;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("isolation: isolate;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfiguratorOptionImages_DoNotRaiseScrollAreaAboveRoutingFooter()
    {
        var source = ReadRepoFile(
                "Maliev.Intranet.Client",
                "Components",
                "Project",
                "PartConfigSidebar.razor")
            .ReplaceLineEndings("\n");

        Assert.Contains(".pcs-root:has(.pcs-option-image-frame:hover),", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-root:has(.pcs-option-image-frame:focus-within)", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".pcs-root:has(.pcs-option-image-frame:hover),\n            .pcs-root:has(.pcs-option-image-frame:focus-within) {\n                overflow: visible;\n                z-index:", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".pcs-scroll:has(.pcs-option-image-frame:hover)", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".pcs-scroll:has(.pcs-option-image-frame:focus-within)", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-section:has(.pcs-option-image-frame:hover),", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-section:has(.pcs-option-image-frame:focus-within)", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-image-preview-popout {\n                position: absolute;", source, StringComparison.Ordinal);
        Assert.Contains("pointer-events: none;", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".pcs-image-preview-popout {\n                position: absolute;\n                left: calc(100% + 10px);\n                top: 50%;\n                z-index: 340;\n                width: min(220px, 64vw);\n                aspect-ratio: 1 / 1;\n                padding: 6px;\n                border: 1px solid color-mix(in oklch, var(--mud-palette-primary) 32%, var(--maliev-border));\n                border-radius: 8px;\n                background: var(--maliev-panel);\n                box-shadow: 0 14px 34px rgba(15, 23, 42, 0.22);\n                opacity: 0;\n                pointer-events: auto;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfiguratorScrollContainer_PreservesOverflowWhenPreviewIsHovered()
    {
        var source = ReadRepoFile(
                "Maliev.Intranet.Client",
                "Components",
                "Project",
                "PartConfigSidebar.razor")
            .ReplaceLineEndings("\n");

        Assert.Contains(".pcs-scroll {\n                flex: 1;\n                overflow-y: auto;", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".pcs-scroll:has(.pcs-option-image-frame:hover) {\n                overflow: visible;", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".pcs-scroll:has(.pcs-option-image-frame:focus-within) {\n                overflow: visible;", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-footer {\n                border-top: 1px solid var(--maliev-border);\n                background: var(--maliev-panel);\n                flex-shrink: 0;\n                position: relative;\n                z-index: 180;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfiguratorOptionImages_RaiseHoveredPreviewAboveAllOptionListsAndItems()
    {
        var source = ReadRepoFile(
                "Maliev.Intranet.Client",
                "Components",
                "Project",
                "PartConfigSidebar.razor")
            .ReplaceLineEndings("\n");

        Assert.Contains(".pcs-cards,\n            .pcs-feature-toggle-grid,\n            .pcs-option-row,\n            .pcs-tol-grid,\n            .pcs-color-choice-grid", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-cards:has(.pcs-option-image-frame:hover),", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-feature-toggle-grid:has(.pcs-option-image-frame:hover),", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-option-row:has(.pcs-option-image-frame:hover),", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-color-choice-grid:has(.pcs-option-image-frame:hover)", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-mat-card:has(.pcs-option-image-frame:hover),", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-fin-card:has(.pcs-option-image-frame:hover),", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-choice-card:has(.pcs-option-image-frame:hover),", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-color-choice:has(.pcs-option-image-frame:hover)", source, StringComparison.Ordinal);
        Assert.Contains("z-index: 260;", source, StringComparison.Ordinal);
        Assert.Contains("z-index: 320;", source, StringComparison.Ordinal);
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
            "feature-thread-inserts-part.webp");
        var genericInsertPath = FindRepoFile(
            "Maliev.Intranet.Client",
            "wwwroot",
            "images",
            "materials",
            "feature-steel-inserts-part.webp");

        Assert.NotEqual(File.ReadAllBytes(genericInsertPath), File.ReadAllBytes(threadInsertPath));
    }

    [Fact]
    public void ManufacturingProcessSection_ScrollsSelectedProcessToStartAndDimsInactiveCards()
    {
        var markup = ReadRepoFile(
                "Maliev.Intranet.Client",
                "Components",
                "Project",
                "PartConfigSidebar.razor")
            .ReplaceLineEndings("\n");
        var codeBehind = ReadRepoFile(
                "Maliev.Intranet.Client",
                "Components",
                "Project",
                "PartConfigSidebar.razor.cs")
            .ReplaceLineEndings("\n");
        var script = ReadRepoFile(
                "Maliev.Intranet.Client",
                "wwwroot",
                "js",
                "part-config-sidebar.js")
            .ReplaceLineEndings("\n");
        var appHost = ReadRepoFile("Maliev.Intranet.Bff", "Components", "App.razor");
        var wasmHost = ReadRepoFile("Maliev.Intranet.Client", "wwwroot", "index.html");

        Assert.Contains("@ref=\"_processRowElement\"", markup, StringComparison.Ordinal);
        Assert.Contains("data-process-code=\"@proc.Code\"", markup, StringComparison.Ordinal);
        Assert.Contains("pcs-process-card--dimmed", markup, StringComparison.Ordinal);
        Assert.Contains("aria-pressed=\"@active\"", markup, StringComparison.Ordinal);
        Assert.Contains("scroll-behavior: smooth;", markup, StringComparison.Ordinal);
        Assert.Contains(".pcs-process-card--dimmed {", markup, StringComparison.Ordinal);
        Assert.Contains("opacity: 0.58;", markup, StringComparison.Ordinal);
        Assert.Contains("ScrollProcessIntoStartAsync(p.Code)", codeBehind, StringComparison.Ordinal);
        Assert.Contains("window.malievPartConfigSidebar", script, StringComparison.Ordinal);
        Assert.Contains("scrollTo({", script, StringComparison.Ordinal);
        Assert.Contains("behavior: prefersReducedMotion ? \"auto\" : \"smooth\"", script, StringComparison.Ordinal);
        Assert.Contains("<script src=\"js/part-config-sidebar.js\"></script>", appHost, StringComparison.Ordinal);
        Assert.Contains("<script src=\"js/part-config-sidebar.js\"></script>", wasmHost, StringComparison.Ordinal);
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

        Assert.Contains("[\"anodizedgreen\"] = \"finish-anodized-green-part-surface.webp\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("[\"anodizedgreen\"] = \"finish-painted-green-part-surface.webp\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MaterialPreviewAssets_AreBoundedWebpThumbnails()
    {
        var referencePath = FindRepoFile(
            "Maliev.Intranet.Client",
            "wwwroot",
            "images",
            "materials",
            "aluminum-6061-part-material.webp");
        var directory = Path.GetDirectoryName(referencePath)!;
        var webpFiles = Directory.EnumerateFiles(directory, "*.webp").ToArray();

        Assert.NotEmpty(webpFiles);
        Assert.Empty(Directory.EnumerateFiles(directory, "*.png"));

        foreach (var file in webpFiles)
        {
            var dimensions = ReadWebpDimensions(file);
            Assert.InRange(dimensions.Width, 1, 512);
            Assert.InRange(dimensions.Height, 1, 512);
            Assert.True(
                new FileInfo(file).Length <= 128 * 1024,
                $"Thumbnail {Path.GetFileName(file)} is larger than expected.");
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
        Assert.Contains("--pcs-process-active: var(--maliev-info, #3b82f6);", source, StringComparison.Ordinal);
        Assert.Contains("border: 2px solid var(--pcs-process-active);", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-process-card--active::after {\n                content: \"\";", source, StringComparison.Ordinal);
        Assert.Contains("z-index: 3;", source, StringComparison.Ordinal);
        Assert.Contains("background: var(--pcs-process-active);", source, StringComparison.Ordinal);
        Assert.Contains("color: #fff;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("color: var(--mud-palette-primary-contrast-text);", source, StringComparison.Ordinal);
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
    public void PartFeaturesAndInspection_WhenRequiredCatalogSelectionIsMissing_RemainHidden()
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

        Assert.Empty(cut.FindAll("[data-config-section='part-features']"));
        Assert.Empty(cut.FindAll("[data-config-section='inspection']"));
    }

    [Fact]
    public void PartFeaturesAndInspection_WhenPrimaryConfigurationIsComplete_RenderEnabled()
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

    private static (int Width, int Height) ReadWebpDimensions(string path)
    {
        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length >= 30, $"Unable to read WebP header for {path}.");
        Assert.Equal("RIFF", ReadAscii(bytes, 0, 4));
        Assert.Equal("WEBP", ReadAscii(bytes, 8, 4));

        var offset = 12;
        while (offset + 8 <= bytes.Length)
        {
            var chunk = ReadAscii(bytes, offset, 4);
            var chunkSize = ReadLittleEndianInt32(bytes.AsSpan(offset + 4, 4));
            var dataOffset = offset + 8;
            Assert.True(dataOffset + chunkSize <= bytes.Length, $"Invalid WebP chunk in {path}.");

            return chunk switch
            {
                "VP8X" when chunkSize >= 10 => (
                    ReadLittleEndianUInt24(bytes.AsSpan(dataOffset + 4, 3)) + 1,
                    ReadLittleEndianUInt24(bytes.AsSpan(dataOffset + 7, 3)) + 1),
                "VP8L" when chunkSize >= 5 => ReadWebpLosslessDimensions(bytes.AsSpan(dataOffset, 5)),
                "VP8 " when chunkSize >= 10 => (
                    ReadLittleEndianInt16(bytes.AsSpan(dataOffset + 6, 2)) & 0x3fff,
                    ReadLittleEndianInt16(bytes.AsSpan(dataOffset + 8, 2)) & 0x3fff),
                _ => throw new InvalidDataException($"Unsupported WebP chunk {chunk} in {path}."),
            };
        }

        throw new InvalidDataException($"Unable to locate WebP image data in {path}.");
    }

    private static string ReadAscii(byte[] bytes, int offset, int count) =>
        System.Text.Encoding.ASCII.GetString(bytes, offset, count);

    private static int ReadLittleEndianInt16(ReadOnlySpan<byte> bytes) =>
        bytes[0] | (bytes[1] << 8);

    private static int ReadLittleEndianInt32(ReadOnlySpan<byte> bytes) =>
        bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24);

    private static int ReadLittleEndianUInt24(ReadOnlySpan<byte> bytes) =>
        bytes[0] | (bytes[1] << 8) | (bytes[2] << 16);

    private static (int Width, int Height) ReadWebpLosslessDimensions(ReadOnlySpan<byte> bytes)
    {
        Assert.Equal(0x2f, bytes[0]);
        var bits = bytes[1] | (bytes[2] << 8) | (bytes[3] << 16) | (bytes[4] << 24);
        return ((bits & 0x3fff) + 1, ((bits >> 14) & 0x3fff) + 1);
    }

    private static async Task InvokePrivateTask(object instance, string methodName, params object?[] parameters)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        var task = (Task?)method.Invoke(instance, parameters);
        Assert.NotNull(task);
        await task;
    }
}
