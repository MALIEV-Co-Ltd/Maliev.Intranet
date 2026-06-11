using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using System.Net;
using System.Text;

namespace Maliev.Intranet.Tests.Client.Components;

public sealed class PartDetailCardFileActionTests : BunitContext, IAsyncLifetime
{
    private readonly MockHttpMessageHandler _httpHandler = new();

    public PartDetailCardFileActionTests()
    {
        Services.AddMudServices(conf => conf.PopoverOptions.CheckForPopoverProvider = false);
        Services.AddLogging();
        Services.AddSingleton(new HttpClient(_httpHandler) { BaseAddress = new Uri("http://localhost/") });
        Services.AddSingleton<CurrencyService>();
        Services.AddSingleton<LayoutService>();
        Services.AddSingleton(new FileTypesSettings
        {
            ThreeDExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".stl",
                ".step",
                ".stp",
                ".3mf",
                ".obj",
                ".igs",
                ".iges",
                ".fbx",
                ".glb",
                ".gltf",
            },
            DocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            OfficeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ArchiveExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            DrawingExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            SupplementaryExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        });

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void DownloadOriginal_WhenPartNameContainsStorageHash_UsesOriginalFileName()
    {
        _httpHandler.HandlerFunc = (request, _) =>
        {
            Assert.Equal("/api/v1/uploads/preview-url", request.RequestUri?.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"url":"https://storage.example/signed-object"}""", Encoding.UTF8, "application/json")
            });
        };

        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "bbea8ba6_stud bolt.step",
            StoragePath = "projects/project-1/bbea8ba6_stud bolt.step",
            FileSizeBytes = 689_000,
        };

        var cut = Render<PartDetailCard>(parameters => parameters.Add(component => component.Part, part));

        cut.Find("button[title='Download original file']").Click();

        cut.WaitForAssertion(() =>
        {
            var invocation = Assert.Single(JSInterop.Invocations, call => call.Identifier == "malievFiles.downloadFromUrl");
            Assert.Equal("https://storage.example/signed-object", invocation.Arguments[0]?.ToString());
            Assert.Equal("stud bolt.step", invocation.Arguments[1]?.ToString());
        });
    }

    [Theory]
    [InlineData(5_000, "Weight: 5 g")]
    [InlineData(32_000_000, "Weight: 32 kg")]
    public void EstimatedWeight_UsesReadableUnits(double volumeMm3, string expectedLabel)
    {
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "bracket.stl",
            StoragePath = "projects/project-1/bracket.stl",
            ProcessCode = "FDM",
            VolumeMm3 = volumeMm3,
        };

        var cut = Render<PartDetailCard>(parameters => parameters.Add(component => component.Part, part));

        Assert.Contains(expectedLabel, cut.Markup);
    }

    [Fact]
    public void EstimatedWeight_ShowsFormulaTooltip()
    {
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "aluminum-bracket.stl",
            StoragePath = "projects/project-1/aluminum-bracket.stl",
            ProcessCode = "CNC_MILLING",
            VolumeMm3 = 10_000,
            MaterialCode = "AL6061",
        };

        var cut = Render<PartDetailCard>(parameters => parameters.Add(component => component.Part, part));

        Assert.Contains("Weight = volume", cut.Markup);
        Assert.Contains("10.00 cm", cut.Markup);
        Assert.Contains("2.7 g/cm", cut.Markup);
    }

    [Fact]
    public void EstimatedWeight_UsesSelectedCatalogDensity()
    {
        var materialId = Guid.NewGuid();
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "custom-polymer.stl",
            StoragePath = "projects/project-1/custom-polymer.stl",
            ProcessCode = "SLA",
            VolumeMm3 = 1_000,
            MaterialId = materialId,
            MaterialCode = "CUSTOM_POLYMER",
            AvailableMaterials =
            [
                new CatalogMaterialDto(materialId, "Custom Polymer 1.42", "CUSTOM_POLYMER", "Polymer", 1.42m, null, 10),
            ],
        };

        var cut = Render<PartDetailCard>(parameters => parameters.Add(component => component.Part, part));

        Assert.Contains("Weight: 1.4 g", cut.Markup);
        Assert.Contains("1.42 g/cm", cut.Markup);
        Assert.Contains("Custom Polymer 1.42", cut.Markup);
    }
}
