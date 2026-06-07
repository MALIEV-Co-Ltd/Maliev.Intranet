using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Maliev.Intranet.Tests.Client.Services;

public class ThumbnailGenerationServiceTests
{
    [Fact]
    public void TryGetCached_WhenCacheEmpty_ReturnsFalse()
    {
        var service = new ThumbnailGenerationService(
            jsRuntime: null!,
            httpClient: new HttpClient(),
            logger: NullLogger<ThumbnailGenerationService>.Instance);

        var result = service.TryGetCached("path/file.stl", null, out var set);

        Assert.False(result);
        Assert.Null(set);
    }

    [Fact]
    public void TryGetCached_WhenCached_ReturnsTrue()
    {
        var service = new ThumbnailGenerationService(
            jsRuntime: null!,
            httpClient: new HttpClient(),
            logger: NullLogger<ThumbnailGenerationService>.Instance);

        var cachedSet = new ThumbnailSetDto { ThumbnailSmall = "data:image/jpeg;base64,abc" };
        service.SetCached("path/file.stl", "v1", cachedSet);

        var result = service.TryGetCached("path/file.stl", "v1", out var set);

        Assert.True(result);
        Assert.Same(cachedSet, set);
    }

    [Fact]
    public void TryGetCached_WhenVersionMismatch_ReturnsFalse()
    {
        var service = new ThumbnailGenerationService(
            jsRuntime: null!,
            httpClient: new HttpClient(),
            logger: NullLogger<ThumbnailGenerationService>.Instance);

        service.SetCached("path/file.stl", "v1", new ThumbnailSetDto { ThumbnailSmall = "abc" });

        var result = service.TryGetCached("path/file.stl", "v2", out var set);

        Assert.False(result);
        Assert.Null(set);
    }
}