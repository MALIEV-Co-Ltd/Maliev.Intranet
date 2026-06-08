// Maliev.Intranet.Client/Services/ThumbnailGenerationService.cs
using System.Collections.Concurrent;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Orchestrates client-side thumbnail generation using BabylonJS in WebAssembly.
/// Handles caching, progress reporting, and server fallback decisions.
/// </summary>
public sealed class ThumbnailGenerationService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ThumbnailGenerationService> _logger;
    private readonly ConcurrentDictionary<string, ThumbnailSetDto> _cache = new();
    private readonly ConcurrentDictionary<string, Task<ThumbnailSetDto>> _inflight = new();
    private readonly object _subscribersLock = new();
    private readonly List<EventCallback<ThumbnailProgress>> _subscribers = new();

    /// <summary>
    /// Initializes a new instance of <see cref="ThumbnailGenerationService"/>.
    /// </summary>
    public ThumbnailGenerationService(
        IJSRuntime jsRuntime,
        HttpClient httpClient,
        ILogger<ThumbnailGenerationService> logger)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to retrieve a cached thumbnail set for the given storage path and version.
    /// </summary>
    public bool TryGetCached(string storagePath, string? version, out ThumbnailSetDto? set)
    {
        var key = BuildCacheKey(storagePath, version);
        return _cache.TryGetValue(key, out set);
    }

    /// <summary>
    /// Stores a thumbnail set in the cache.
    /// </summary>
    public void SetCached(string storagePath, string? version, ThumbnailSetDto set)
    {
        var key = BuildCacheKey(storagePath, version);
        _cache[key] = set;
    }

    /// <summary>
    /// Subscribes a callback to receive thumbnail progress events.
    /// </summary>
    public void Subscribe(EventCallback<ThumbnailProgress> callback)
    {
        lock (_subscribersLock)
        {
            _subscribers.Add(callback);
        }
    }

    /// <summary>
    /// Unsubscribes a callback from receiving thumbnail progress events.
    /// </summary>
    public void Unsubscribe(EventCallback<ThumbnailProgress> callback)
    {
        lock (_subscribersLock)
        {
            _subscribers.Remove(callback);
        }
    }

    /// <summary>
    /// Notifies all subscribers of a progress event.
    /// </summary>
    public async Task NotifyAsync(ThumbnailProgress progress)
    {
        EventCallback<ThumbnailProgress>[] snapshot;
        lock (_subscribersLock)
        {
            snapshot = _subscribers.ToArray();
        }

        foreach (var callback in snapshot)
        {
            try
            {
                await callback.InvokeAsync(progress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking thumbnail progress subscriber");
            }
        }
    }

    private static string BuildCacheKey(string storagePath, string? version) =>
        $"{storagePath}:{version ?? "unknown"}";
}