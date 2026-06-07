using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Components;
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

    [Fact]
    public async Task NotifyAsync_InvokesAllSubscribers()
    {
        var service = new ThumbnailGenerationService(
            jsRuntime: null!,
            httpClient: new HttpClient(),
            logger: NullLogger<ThumbnailGenerationService>.Instance);

        var received1 = new List<ThumbnailProgress>();
        var received2 = new List<ThumbnailProgress>();
        var callback1 = EventCallback.Factory.Create<ThumbnailProgress>(this, p => received1.Add(p));
        var callback2 = EventCallback.Factory.Create<ThumbnailProgress>(this, p => received2.Add(p));

        service.Subscribe(callback1);
        service.Subscribe(callback2);

        var progress = new ThumbnailProgress("path/file.stl", ThumbnailGenerationStage.Downloading, 10, "test");
        await service.NotifyAsync(progress);

        Assert.Single(received1);
        Assert.Single(received2);
        Assert.Equal("path/file.stl", received1[0].StoragePath);
    }

    [Fact]
    public async Task Unsubscribe_RemovesCallback()
    {
        var service = new ThumbnailGenerationService(
            jsRuntime: null!,
            httpClient: new HttpClient(),
            logger: NullLogger<ThumbnailGenerationService>.Instance);

        var received = new List<ThumbnailProgress>();
        var callback = EventCallback.Factory.Create<ThumbnailProgress>(this, p => received.Add(p));

        service.Subscribe(callback);
        service.Unsubscribe(callback);
        await service.NotifyAsync(new ThumbnailProgress("path", ThumbnailGenerationStage.Queued, 0, null));

        Assert.Empty(received);
    }
}