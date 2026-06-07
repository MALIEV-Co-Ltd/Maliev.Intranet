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
    /// <param name="storagePath">GCS storage path of the source file.</param>
    /// <param name="version">Content version hash (for cache invalidation).</param>
    /// <param name="set">The cached thumbnail set, or null if not cached.</param>
    /// <returns>True if a cached set was found for the given version.</returns>
    public bool TryGetCached(string storagePath, string? version, out ThumbnailSetDto? set)
    {
        var key = BuildCacheKey(storagePath, version);
        return _cache.TryGetValue(key, out set);
    }

    /// <summary>
    /// Stores a thumbnail set in the cache.
    /// </summary>
    /// <param name="storagePath">GCS storage path of the source file.</param>
    /// <param name="version">Content version hash.</param>
    /// <param name="set">The thumbnail set to cache.</param>
    public void SetCached(string storagePath, string? version, ThumbnailSetDto set)
    {
        var key = BuildCacheKey(storagePath, version);
        _cache[key] = set;
    }

    private static string BuildCacheKey(string storagePath, string? version) =>
        $"{storagePath}:{version ?? "unknown"}";
}