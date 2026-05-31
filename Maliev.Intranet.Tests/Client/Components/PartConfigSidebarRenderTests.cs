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
        Assert.NotNull(cut.Find(".pcs-mat-card img[src='/images/materials/white-pom-material-image.png']"));
        Assert.NotNull(cut.Find(".pcs-color-choice img[src='/images/materials/black-pom-material-image.png']"));
        Assert.NotNull(cut.Find(".pcs-fin-card img[src='/images/materials/finish-anodized-clear-material-image.png']"));
        Assert.NotNull(cut.Find(".pcs-color-choice img[src='/images/materials/finish-anodized-blue-material-image.png']"));
        Assert.NotNull(cut.Find(".pcs-choice-card img[src='/images/materials/deburr-edges-material-image.png']"));
    }

    [Fact]
    public void PartFeaturesAndInspection_WhenProcessIsNotSelected_RenderDisabled()
    {
        var part = new PartViewModel
        {
            FileId = Guid.Empty,
            Name = "fixture.step",
        };

        var cut = Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, []));

        Assert.All(cut.FindAll(".pcs-feature-card"), card =>
        {
            Assert.True(card.HasAttribute("disabled"));
            Assert.Equal("true", card.GetAttribute("aria-disabled"));
        });

        Assert.All(cut.FindAll("[data-config-section='inspection'] .pcs-choice-card"), card =>
        {
            Assert.True(card.HasAttribute("disabled"));
            Assert.Equal("true", card.GetAttribute("aria-disabled"));
        });
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
}
