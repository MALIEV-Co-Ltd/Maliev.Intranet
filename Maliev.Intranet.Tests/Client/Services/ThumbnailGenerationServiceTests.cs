using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
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
        Func<ThumbnailProgress, Task> callback1 = p => { received1.Add(p); return Task.CompletedTask; };
        Func<ThumbnailProgress, Task> callback2 = p => { received2.Add(p); return Task.CompletedTask; };

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
        Func<ThumbnailProgress, Task> callback = p => { received.Add(p); return Task.CompletedTask; };

        service.Subscribe(callback);
        service.Unsubscribe(callback);
        await service.NotifyAsync(new ThumbnailProgress("path", ThumbnailGenerationStage.Queued, 0, null));

        Assert.Empty(received);
    }

    [Fact]
    public async Task GenerateAsync_PassesStoragePathExtensionToThumbnailInterop()
    {
        var jsRuntime = new CapturingJsRuntime();
        var service = new ThumbnailGenerationService(
            jsRuntime,
            httpClient: new HttpClient(),
            logger: NullLogger<ThumbnailGenerationService>.Instance);

        await service.GenerateAsync(
            "projects/project-1/bracket.stl",
            "v1",
            "https://storage.local/download/signed-object?X-Goog-Signature=abc");

        Assert.Equal("MalievGeometry.generateThumbnails", jsRuntime.Identifier);
        Assert.NotNull(jsRuntime.Arguments);
        Assert.Equal("https://storage.local/download/signed-object?X-Goog-Signature=abc", jsRuntime.Arguments[0]);
        Assert.Equal(".stl", jsRuntime.Arguments[1]?.GetType().GetProperty("fileExtension")?.GetValue(jsRuntime.Arguments[1]));
    }

    [Fact]
    public async Task GenerateAsync_WhenStepFormatIsUnsupported_ReturnsFallbackInsteadOfThrowing()
    {
        var jsRuntime = new ThrowingJsRuntime("Unsupported format for local thumbnail generation: .step");
        var service = new ThumbnailGenerationService(
            jsRuntime,
            httpClient: new HttpClient(),
            logger: NullLogger<ThumbnailGenerationService>.Instance);

        var result = await service.GenerateAsync(
            "projects/project-1/bracket.step",
            "v1",
            "https://storage.local/download/bracket.step?sig=abc");

        Assert.False(result.HasAny);
        Assert.Equal("v1", result.Version);
    }

    private sealed class CapturingJsRuntime : IJSRuntime
    {
        public string? Identifier { get; private set; }
        public object?[]? Arguments { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Identifier = identifier;
            Arguments = args;
            return ValueTask.FromResult((TValue)(object)new ThumbnailSetDto { ThumbnailSmall = "data:image/png;base64,abc" });
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }

    private sealed class ThrowingJsRuntime(string message) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            throw new JSException(message);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }
}
