using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using System.Net;
using System.Text;

namespace Maliev.Intranet.Tests.Client.Components;

public sealed class PartDetailCardFileActionTests : BunitContext
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
}
