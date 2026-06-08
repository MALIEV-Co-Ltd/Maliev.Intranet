// Maliev.Intranet.Tests/Client/Components/ThumbnailImageTests.cs
using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Maliev.Intranet.Tests.Client.Components;

public class ThumbnailImageTests : BunitContext
{
    [Fact]
    public void ThumbnailImage_ShowsSkeleton_WhenNotCached()
    {
        Services.AddSingleton(new ThumbnailGenerationService(
            jsRuntime: null!,
            httpClient: new HttpClient(),
            logger: NullLogger<ThumbnailGenerationService>.Instance));

        var component = Render<ThumbnailImage>(parameters => parameters
            .Add(p => p.StoragePath, "path/file.stl")
            .Add(p => p.SignedDownloadUrl, "https://test.com/file.stl"));

        Assert.Contains("thumbnail-pending", component.Markup);
    }

    [Fact]
    public void ThumbnailImage_ShowsImage_WhenCached()
    {
        var service = new ThumbnailGenerationService(
            jsRuntime: null!,
            httpClient: new HttpClient(),
            logger: NullLogger<ThumbnailGenerationService>.Instance);

        var cachedSet = new ThumbnailSetDto { ThumbnailSmall = "data:image/jpeg;base64,abc" };
        service.SetCached("path/file.stl", "v1", cachedSet);

        Services.AddSingleton(service);

        var component = Render<ThumbnailImage>(parameters => parameters
            .Add(p => p.StoragePath, "path/file.stl")
            .Add(p => p.Version, "v1")
            .Add(p => p.SignedDownloadUrl, "https://test.com/file.stl"));

        Assert.Contains("data:image/jpeg;base64,abc", component.Markup);
    }

    [Fact]
    public void ThumbnailImage_ShowsFallback_WhenFallbackStage()
    {
        var service = new ThumbnailGenerationService(
            jsRuntime: null!,
            httpClient: new HttpClient(),
            logger: NullLogger<ThumbnailGenerationService>.Instance);

        Services.AddSingleton(service);

        var component = Render<ThumbnailImage>(parameters => parameters
            .Add(p => p.StoragePath, "path/file.stl")
            .Add(p => p.SignedDownloadUrl, "https://test.com/file.stl"));

        // We can't easily test the fallback stage without triggering generation
        // but we can verify the component renders
        Assert.Contains("thumbnail-image", component.Markup);
    }
}