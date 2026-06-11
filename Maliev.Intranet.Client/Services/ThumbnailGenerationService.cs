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

    /// <summary>
    /// Generates all 8 thumbnail views for a file. Returns cached result if available.
    /// </summary>
    /// <param name="storagePath">GCS storage path of the source file.</param>
    /// <param name="version">Content version hash for cache invalidation.</param>
    /// <param name="signedDownloadUrl">Signed GCS download URL.</param>
    /// <returns>ThumbnailSet with all 8 views, or empty set on fallback.</returns>
    public async Task<ThumbnailSetDto> GenerateAsync(
        string storagePath,
        string? version,
        string signedDownloadUrl)
    {
        if (TryGetCached(storagePath, version, out var cached) && cached != null)
        {
            return cached;
        }

        var cacheKey = $"{storagePath}:{version ?? "unknown"}";

        return await _inflight.GetOrAdd(cacheKey, async _ =>
        {
            try
            {
                await NotifyAsync(new ThumbnailProgress(storagePath, ThumbnailGenerationStage.Downloading, 10, "Downloading mesh"));

                var result = await _jsRuntime.InvokeAsync<ThumbnailSetDto>(
                    "MalievGeometry.generateThumbnails",
                    signedDownloadUrl,
                    new
                    {
                        timeoutMs = 20000,
                        fileExtension = Path.GetExtension(storagePath)
                    });

                result.Version = version ?? string.Empty;

                if (!result.HasAny)
                {
                    _logger.LogWarning("WASM thumbnail generation returned empty result for {StoragePath}, requesting server fallback", storagePath);
                    await NotifyAsync(new ThumbnailProgress(storagePath, ThumbnailGenerationStage.Fallback, 0, "Falling back to server"));
                    return result;
                }

                SetCached(storagePath, version, result);
                await NotifyAsync(new ThumbnailProgress(storagePath, ThumbnailGenerationStage.Complete, 100, "Complete"));
                return result;
            }
            catch (JSException ex) when (IsModuleNotFoundError(ex))
            {
                // The interop script has not loaded (yet). Do NOT latch this state —
                // a transient early call would otherwise disable local thumbnails for
                // the whole session. The next caller simply retries.
                _logger.LogWarning(
                    "MalievGeometry interop is not loaded for {StoragePath} — check that " +
                    "js/geometry/JsInterop/GeometryInterop.js is referenced by the host page",
                    storagePath);
                await NotifyAsync(new ThumbnailProgress(storagePath, ThumbnailGenerationStage.Fallback, 0, "Falling back to server"));
                return new ThumbnailSetDto { Version = version ?? string.Empty };
            }
            catch (JSException ex) when (IsRecoverableError(ex))
            {
                _logger.LogWarning(ex, "WASM thumbnail generation failed for {StoragePath}, requesting server fallback", storagePath);
                await NotifyAsync(new ThumbnailProgress(storagePath, ThumbnailGenerationStage.Fallback, 0, "Falling back to server"));
                return new ThumbnailSetDto { Version = version ?? string.Empty };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Thumbnail generation failed for {StoragePath}", storagePath);
                await NotifyAsync(new ThumbnailProgress(storagePath, ThumbnailGenerationStage.Failed, 0, ex.Message));
                throw;
            }
            finally
            {
                _inflight.TryRemove(cacheKey, out Task<ThumbnailSetDto>? _);
            }
        });
    }

    private static bool IsModuleNotFoundError(JSException ex) =>
        ex.Message.Contains("MalievGeometry", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("was undefined", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("Could not find", StringComparison.OrdinalIgnoreCase);

    private static bool IsRecoverableError(JSException ex) =>
        ex.Message.Contains("OOM", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("memory", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("aborted", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("WebGL", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("Unsupported format", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("Download failed", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("No meshes", StringComparison.OrdinalIgnoreCase);
}
