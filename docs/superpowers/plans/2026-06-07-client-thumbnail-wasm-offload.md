# Client-Side Thumbnail WASM Offload Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Offload all thumbnail/preview image generation from `Maliev.GeometryService` to client browser using BabylonJS, achieving <2s first preview, 90% server cost reduction, full offline support.

**Architecture:** BabylonJS TypeScript module runs in WebAssembly, loads mesh from signed GCS URL, renders 8 camera views (6 orthographic + 2 isometric) to offscreen RenderTargetTextures, encodes to base64 JPEG, delivers to UI via Blazor service. Server remains as fallback for OOM/timeout/unsupported formats.

**Tech Stack:** BabylonJS 7.x, TypeScript 5.x, Blazor WASM (InteractiveAuto), .NET 10, MudBlazor, MassTransit, SignalR

**Spec:** `docs/superpowers/specs/2026-06-07-client-thumbnail-wasm-offload-design.md`

---

## File Structure

### New Files (Phase 1)

| File | Responsibility |
|------|----------------|
| `Maliev.Intranet.Client/Geometry/BabylonThumbnailGenerator.ts` | Core WASM/JS module: mesh loading, 8-camera rendering, JPEG encoding |
| `Maliev.Intranet.Client/Geometry/JsInterop/GeometryInterop.ts` | JS interop wrapper exposed to Blazor |
| `Maliev.Intranet.Client/Services/ThumbnailGenerationService.cs` | Blazor service: orchestration, progress reporting, caching, fallback decision |
| `Maliev.Intranet.Shared/Dtos/ThumbnailSetDto.cs` | DTO with 8 base64 DataURLs + dimensions |
| `Maliev.Intranet.Client/Components/Project/ThumbnailImage.razor` | Reusable component with skeleton/progress/complete/fallback states |
| `Maliev.Intranet.Tests/Client/Services/ThumbnailGenerationServiceTests.cs` | Unit tests for service orchestration |
| `Maliev.Intranet.Tests/Client/Geometry/BabylonThumbnailGeneratorTests.cs` | Unit tests for generator (mocked BabylonJS) |
| `Maliev.Intranet.Tests/Client/Components/ThumbnailImageTests.cs` | bUnit tests for component states |

### Modified Files (Phase 1)

| File | Change |
|------|--------|
| `Maliev.Intranet.Client/Program.cs` | Register `ThumbnailGenerationService` in DI |
| `Maliev.Intranet.Client/Components/Project/PartListRow.razor` | Replace static image with `<ThumbnailImage>` component |
| `Maliev.Intranet.Client/Components/Project/PartMiniCard.razor` | Same |
| `Maliev.Intranet.Client/Components/Project/PartDetailCard.razor` | Same |
| `Maliev.Intranet.Client/Components/Project/ProjectPartsBulkTable.razor` | Same |
| `Maliev.Intranet.Client/Components/Project/PartsListPanel.razor` | Same |
| `Maliev.Intranet.Client/Pages/ProjectDetail.razor` | Same |
| `Maliev.Intranet.Client/Pages/Projects.razor` | Same |
| `Maliev.Intranet.Client/Pages/ProjectNew.razor.cs` | Trigger generation after upload completes |
| `Maliev.Intranet.Client/Maliev.Intranet.Client.csproj` | Add BabylonJS NuGet package |
| `Maliev.Intranet.Client/wwwroot/index.html` | Add BabylonJS script tag |

### New Files (Phase 2)

| File | Responsibility |
|------|----------------|
| `Maliev.Intranet.Shared/Dtos/ThumbnailFallbackRequest.cs` | Fallback trigger payload |
| `Maliev.Intranet.Bff/Consumers/ThumbnailFallbackConsumer.cs` | Publishes legacy event to RabbitMQ |
| `Maliev.Intranet.Tests/Bff/Controllers/GeometryControllerThumbnailFallbackTests.cs` | Tests for fallback endpoint |

### Modified Files (Phase 2)

| File | Change |
|------|--------|
| `Maliev.Intranet.Bff/Controllers/GeometryController.cs` | Add `RequestThumbnailFallback` endpoint |
| `Maliev.Intranet.Bff/Program.cs` | Register `ThumbnailFallbackConsumer` |

### New Files (Phase 3)

None - deprecation only

### Modified Files (Phase 3)

| File | Change |
|------|--------|
| `Maliev.Intranet.Bff/Services/GeometryRuntimeFallbackProvider.cs` | Update manifest: `serverPreviewImages: false` for browser-primary |
| `Maliev.Intranet.Bff/Consumers/SmallThumbnailReadyConsumer.cs` | Mark as `[Obsolete]`, log deprecation warning |

### Removed Files (Phase 4)

| File | Reason |
|------|--------|
| `Maliev.Intranet.Bff/Consumers/SmallThumbnailReadyConsumer.cs` | Replaced by client generation |
| GeometryService thumbnail code (Python, external repo) | No longer needed |

---

## Task Breakdown

### Phase 1: Client Generation Only (No Deprecation)

This phase adds client-side generation alongside existing server-side generation. Safe to deploy, no breaking changes.

#### Task 1.1: Add BabylonJS Dependency

**Files:**
- Modify: `Maliev.Intranet.Client/Maliev.Intranet.Client.csproj`

- [ ] **Step 1: Add NuGet package**

```xml
<PackageReference Include="BabylonJS" Version="7.50.0" />
<PackageReference Include="BabylonJS.Loaders" Version="7.50.0" />
```

- [ ] **Step 2: Restore packages**

Run: `dotnet restore Maliev.Intranet.Client/Maliev.Intranet.Client.csproj`
Expected: Restore succeeds

- [ ] **Step 3: Verify package files exist**

Run: `ls Maliev.Intranet.Client/wwwroot/_content/BabylonJS/`
Expected: Lists `babylon.js`, `loaders/babylonjs.loaders.min.js`

- [ ] **Step 4: Commit**

```bash
git add Maliev.Intranet.Client/Maliev.Intranet.Client.csproj
git commit -m "feat(client): add BabylonJS dependency for thumbnail generation"
```

#### Task 1.2: Create ThumbnailSetDto

**Files:**
- Create: `Maliev.Intranet.Shared/Dtos/ThumbnailSetDto.cs`

- [ ] **Step 1: Write the DTO**

```csharp
using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Set of 8 thumbnail views generated client-side or server-side.
/// All fields are base64-encoded JPEG DataURLs (data:image/jpeg;base64,...).
/// </summary>
public sealed class ThumbnailSetDto
{
    /// <summary>Front-face small (256x256) preview.</summary>
    [MaxLength(2_000_000)]
    public string FrontSmall { get; set; } = string.Empty;

    /// <summary>Back-face small (256x256) preview.</summary>
    [MaxLength(2_000_000)]
    public string BackSmall { get; set; } = string.Empty;

    /// <summary>Left-face small (256x256) preview.</summary>
    [MaxLength(2_000_000)]
    public string LeftSmall { get; set; } = string.Empty;

    /// <summary>Right-face small (256x256) preview.</summary>
    [MaxLength(2_000_000)]
    public string RightSmall { get; set; } = string.Empty;

    /// <summary>Top-face small (256x256) preview.</summary>
    [MaxLength(2_000_000)]
    public string TopSmall { get; set; } = string.Empty;

    /// <summary>Bottom-face small (256x256) preview.</summary>
    [MaxLength(2_000_000)]
    public string BottomSmall { get; set; } = string.Empty;

    /// <summary>Isometric thumbnail small (256x256).</summary>
    [MaxLength(2_000_000)]
    public string ThumbnailSmall { get; set; } = string.Empty;

    /// <summary>Isometric thumbnail large (1200x1200).</summary>
    [MaxLength(5_000_000)]
    public string ThumbnailLarge { get; set; } = string.Empty;

    /// <summary>Content version hash for cache invalidation.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>True if any thumbnail is non-empty (used by UI to determine "ready" state).</summary>
    public bool HasAny => !string.IsNullOrEmpty(ThumbnailSmall) || !string.IsNullOrEmpty(FrontSmall);
}
```

- [ ] **Step 2: Verify it compiles**

Run: `dotnet build Maliev.Intranet.Shared/Maliev.Intranet.Shared.csproj`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add Maliev.Intranet.Shared/Dtos/ThumbnailSetDto.cs
git commit -m "feat(shared): add ThumbnailSetDto for client-side thumbnail generation"
```

#### Task 1.3: Create ThumbnailGenerationStage Enum and ThumbnailProgress Record

**Files:**
- Create: `Maliev.Intranet.Client/Services/ThumbnailGenerationStage.cs`
- Create: `Maliev.Intranet.Client/Services/ThumbnailProgress.cs`

- [ ] **Step 1: Create the enum**

```csharp
// Maliev.Intranet.Client/Services/ThumbnailGenerationStage.cs
namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Stages of client-side thumbnail generation for UI progress reporting.
/// </summary>
public enum ThumbnailGenerationStage
{
    /// <summary>Queued for generation, not yet started.</summary>
    Queued,

    /// <summary>Downloading mesh file from signed GCS URL.</summary>
    Downloading,

    /// <summary>Parsing mesh format (STL/OBJ/GLB/3MF).</summary>
    LoadingMesh,

    /// <summary>Centering and scaling assembly to fit view cube.</summary>
    Normalizing,

    /// <summary>Rendering 8 camera views to RenderTargetTextures.</summary>
    Rendering,

    /// <summary>Encoding pixel buffers to JPEG.</summary>
    Encoding,

    /// <summary>Generation complete, thumbnails ready.</summary>
    Complete,

    /// <summary>Falling back to server-side generation.</summary>
    Fallback,

    /// <summary>Generation failed unrecoverably.</summary>
    Failed
}
```

- [ ] **Step 2: Create the progress record**

```csharp
// Maliev.Intranet.Client/Services/ThumbnailProgress.cs
namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Progress event for thumbnail generation, used to update UI components.
/// </summary>
/// <param name="StoragePath">GCS storage path of the source file.</param>
/// <param name="Stage">Current generation stage.</param>
/// <param name="Percent">Progress percentage (0-100).</param>
/// <param name="Message">Human-readable status message.</param>
public sealed record ThumbnailProgress(
    string StoragePath,
    ThumbnailGenerationStage Stage,
    int Percent,
    string? Message);
```

- [ ] **Step 3: Verify it compiles**

Run: `dotnet build Maliev.Intranet.Client/Maliev.Intranet.Client.csproj`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add Maliev.Intranet.Client/Services/ThumbnailGenerationStage.cs Maliev.Intranet.Client/Services/ThumbnailProgress.cs
git commit -m "feat(client): add ThumbnailGenerationStage enum and ThumbnailProgress record"
```

#### Task 1.4: Write Failing Test for ThumbnailGenerationService Cache

**Files:**
- Create: `Maliev.Intranet.Tests/Client/Services/ThumbnailGenerationServiceTests.cs`

- [ ] **Step 1: Write the test file**

```csharp
// Maliev.Intranet.Tests/Client/Services/ThumbnailGenerationServiceTests.cs
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

        var result = service.TryGetCached("path/file.stl", out var set);

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
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Maliev.Intranet.Tests/Maliev.Intranet.Tests.csproj --filter "FullyQualifiedName~ThumbnailGenerationServiceTests"`
Expected: FAIL with "ThumbnailGenerationService does not exist"

- [ ] **Step 3: Commit failing tests**

```bash
git add Maliev.Intranet.Tests/Client/Services/ThumbnailGenerationServiceTests.cs
git commit -m "test(client): add failing tests for ThumbnailGenerationService cache"
```

#### Task 1.5: Implement ThumbnailGenerationService Cache Methods

**Files:**
- Create: `Maliev.Intranet.Client/Services/ThumbnailGenerationService.cs`

- [ ] **Step 1: Write the service stub with cache methods**

```csharp
// Maliev.Intranet.Client/Services/ThumbnailGenerationService.cs
using System.Collections.Concurrent;
using Maliev.Intranet.Shared.Dtos;
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
```

- [ ] **Step 2: Run tests to verify they pass**

Run: `dotnet test Maliev.Intranet.Tests/Maliev.Intranet.Tests.csproj --filter "FullyQualifiedName~ThumbnailGenerationServiceTests"`
Expected: PASS (3 tests)

- [ ] **Step 3: Commit**

```bash
git add Maliev.Intranet.Client/Services/ThumbnailGenerationService.cs
git commit -m "feat(client): add ThumbnailGenerationService with cache methods"
```

#### Task 1.6: Write Failing Test for Subscribe/Notify

**Files:**
- Modify: `Maliev.Intranet.Tests/Client/Services/ThumbnailGenerationServiceTests.cs`

- [ ] **Step 1: Add the test**

```csharp
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
    public void Unsubscribe_RemovesCallback()
    {
        var service = new ThumbnailGenerationService(
            jsRuntime: null!,
            httpClient: new HttpClient(),
            logger: NullLogger<ThumbnailGenerationService>.Instance);

        var received = new List<ThumbnailProgress>();
        var callback = EventCallback.Factory.Create<ThumbnailProgress>(this, p => received.Add(p));

        service.Subscribe(callback);
        service.Unsubscribe(callback);
        service.NotifyAsync(new ThumbnailProgress("path", ThumbnailGenerationStage.Queued, 0, null)).Wait();

        Assert.Empty(received);
    }
```

- [ ] **Step 2: Run tests to verify new tests fail**

Run: `dotnet test Maliev.Intranet.Tests/Maliev.Intranet.Tests.csproj --filter "FullyQualifiedName~ThumbnailGenerationServiceTests"`
Expected: 2 FAIL (Subscribe/Unsubscribe/NotifyAsync not exist)

- [ ] **Step 3: Commit failing tests**

```bash
git add Maliev.Intranet.Tests/Client/Services/ThumbnailGenerationServiceTests.cs
git commit -m "test(client): add failing tests for subscribe/notify"
```

#### Task 1.7: Implement Subscribe/Unsubscribe/NotifyAsync

**Files:**
- Modify: `Maliev.Intranet.Client/Services/ThumbnailGenerationService.cs`

- [ ] **Step 1: Add subscribe/unsubscribe/notify methods**

Replace the entire service file with:

```csharp
// Maliev.Intranet.Client/Services/ThumbnailGenerationService.cs
using System.Collections.Concurrent;
using Maliev.Intranet.Shared.Dtos;
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
```

- [ ] **Step 2: Run tests to verify all pass**

Run: `dotnet test Maliev.Intranet.Tests/Maliev.Intranet.Tests.csproj --filter "FullyQualifiedName~ThumbnailGenerationServiceTests"`
Expected: PASS (5 tests)

- [ ] **Step 3: Commit**

```bash
git add Maliev.Intranet.Client/Services/ThumbnailGenerationService.cs
git commit -m "feat(client): add subscribe/unsubscribe/notify to ThumbnailGenerationService"
```

#### Task 1.8: Create JS Interop Wrapper (TypeScript)

**Files:**
- Create: `Maliev.Intranet.Client/Geometry/JsInterop/GeometryInterop.ts`

- [ ] **Step 1: Create the TypeScript file**

```typescript
// Maliev.Intranet.Client/Geometry/JsInterop/GeometryInterop.ts
import { BabylonThumbnailGenerator, ThumbnailSet, ThumbnailOptions } from "../BabylonThumbnailGenerator";

/**
 * JS interop wrapper for Blazor to invoke client-side thumbnail generation.
 * Exposed as global function `MalievGeometry.generateThumbnails`.
 */
export class GeometryInterop {
  private static generator: BabylonThumbnailGenerator | null = null;

  /**
   * Generates all 8 thumbnail views for a file URL.
   * @param fileUrl - Signed GCS download URL
   * @param options - Generation options (timeout, quality, progress callback)
   * @returns Promise resolving to ThumbnailSet
   */
  public static async generateThumbnails(
    fileUrl: string,
    options: ThumbnailOptions = {}
  ): Promise<ThumbnailSet> {
    if (!this.generator) {
      this.generator = new BabylonThumbnailGenerator();
    }
    return await this.generator.generateAllViews(fileUrl, options);
  }

  /**
   * Checks if WebGL 2 is available in the current browser.
   */
  public static isWebGL2Available(): boolean {
    try {
      const canvas = document.createElement("canvas");
      return !!canvas.getContext("webgl2");
    } catch {
      return false;
    }
  }
}

// Expose to global window for Blazor JSInterop
declare global {
  interface Window {
    MalievGeometry: {
      generateThumbnails: (fileUrl: string, options: ThumbnailOptions) => Promise<ThumbnailSet>;
      isWebGL2Available: () => boolean;
    };
  }
}

if (typeof window !== "undefined") {
  window.MalievGeometry = {
    generateThumbnails: (fileUrl, options) => GeometryInterop.generateThumbnails(fileUrl, options),
    isWebGL2Available: () => GeometryInterop.isWebGL2Available()
  };
}
```

- [ ] **Step 2: Create tsconfig.json if not exists**

Check: `Maliev.Intranet.Client/tsconfig.json` should exist or create:

```json
{
  "compilerOptions": {
    "target": "ES2020",
    "module": "ES2020",
    "moduleResolution": "node",
    "esModuleInterop": true,
    "strict": true,
    "skipLibCheck": true,
    "outDir": "./wwwroot/js/geometry",
    "rootDir": "./Geometry"
  },
  "include": ["Geometry/**/*.ts"]
}
```

- [ ] **Step 3: Compile TypeScript**

Run: `npx tsc -p Maliev.Intranet.Client/tsconfig.json`
Expected: `Maliev.Intranet.Client/wwwroot/js/geometry/JsInterop/GeometryInterop.js` created

- [ ] **Step 4: Add script tag to index.html**

Modify: `Maliev.Intranet.Client/wwwroot/index.html`

Add before closing `</body>`:
```html
<script src="_content/BabylonJS/babylon.js"></script>
<script src="js/geometry/JsInterop/GeometryInterop.js"></script>
```

- [ ] **Step 5: Commit**

```bash
git add Maliev.Intranet.Client/Geometry/ Maliev.Intranet.Client/wwwroot/index.html Maliev.Intranet.Client/tsconfig.json
git commit -m "feat(client): add JS interop wrapper for BabylonJS thumbnail generation"
```

#### Task 1.9: Create BabylonThumbnailGenerator (TypeScript Stub)

**Files:**
- Create: `Maliev.Intranet.Client/Geometry/BabylonThumbnailGenerator.ts`

- [ ] **Step 1: Create the generator file**

```typescript
// Maliev.Intranet.Client/Geometry/BabylonThumbnailGenerator.ts
import {
  Engine,
  Scene,
  Mesh,
  ArcRotateCamera,
  Vector3,
  Color3,
  Color4,
  HemisphericLight,
  RenderTargetTexture
} from "@babylonjs/core";

export interface ThumbnailSet {
  frontSmall: string;
  backSmall: string;
  leftSmall: string;
  rightSmall: string;
  topSmall: string;
  bottomSmall: string;
  thumbnailSmall: string;
  thumbnailLarge: string;
}

export interface ThumbnailOptions {
  onProgress?: (percent: number, stage: string) => void;
  timeoutMs?: number;
  jpegQuality?: number;
}

/**
 * Generates 8 thumbnail views (6 orthographic + 2 isometric) from a 3D mesh file.
 * Uses BabylonJS in WebAssembly for client-side rendering.
 */
export class BabylonThumbnailGenerator {
  private engine: Engine | null = null;
  private scene: Scene | null = null;

  /**
   * Generates all 8 thumbnail views for the given file URL.
   */
  public async generateAllViews(
    fileUrl: string,
    options: ThumbnailOptions = {}
  ): Promise<ThumbnailSet> {
    const { onProgress, timeoutMs = 20000, jpegQuality = 0.85 } = options;
    const abortController = new AbortController();
    const timeoutId = setTimeout(() => abortController.abort("timeout"), timeoutMs);

    try {
      // Phase 1: Download
      onProgress?.(10, "Downloading mesh");
      const response = await fetch(fileUrl, { signal: abortController.signal });
      if (!response.ok) {
        throw new Error(`Download failed: ${response.status}`);
      }
      const buffer = await response.arrayBuffer();

      // Phase 2: Load mesh
      onProgress?.(30, "Loading mesh");
      const { meshes, scene, engine } = await this.loadMeshFromBuffer(buffer);
      this.scene = scene;
      this.engine = engine;

      if (meshes.length === 0) {
        throw new Error("No meshes found in file");
      }

      // Phase 3: Normalize
      onProgress?.(50, "Normalizing assembly");
      this.normalizeAssembly(meshes);

      // Phase 4: Render all 8 views
      onProgress?.(60, "Rendering views");
      const result: ThumbnailSet = {
        frontSmall: "",
        backSmall: "",
        leftSmall: "",
        rightSmall: "",
        topSmall: "",
        bottomSmall: "",
        thumbnailSmall: "",
        thumbnailLarge: ""
      };

      const cameras = this.createOrthoCameras();
      const isoSmall = this.createIsometricCamera();
      const isoLarge = this.createIsometricCamera();

      // Render ortho views in parallel
      const orthoJobs: Array<Promise<void>> = [
        this.renderView(meshes, cameras.front, 256, 256, abortController.signal, jpegQuality).then(d => result.frontSmall = d),
        this.renderView(meshes, cameras.back, 256, 256, abortController.signal, jpegQuality).then(d => result.backSmall = d),
        this.renderView(meshes, cameras.left, 256, 256, abortController.signal, jpegQuality).then(d => result.leftSmall = d),
        this.renderView(meshes, cameras.right, 256, 256, abortController.signal, jpegQuality).then(d => result.rightSmall = d),
        this.renderView(meshes, cameras.top, 256, 256, abortController.signal, jpegQuality).then(d => result.topSmall = d),
        this.renderView(meshes, cameras.bottom, 256, 256, abortController.signal, jpegQuality).then(d => result.bottomSmall = d)
      ];

      onProgress?.(80, "Encoding");
      await Promise.all(orthoJobs);
      result.thumbnailSmall = await this.renderView(meshes, isoSmall, 256, 256, abortController.signal, jpegQuality);
      result.thumbnailLarge = await this.renderView(meshes, isoLarge, 1200, 1200, abortController.signal, jpegQuality);

      onProgress?.(100, "Complete");
      return result;
    } finally {
      clearTimeout(timeoutId);
      this.dispose();
    }
  }

  private async loadMeshFromBuffer(buffer: ArrayBuffer): Promise<{ meshes: Mesh[]; scene: Scene; engine: Engine }> {
    // Use BabylonJS AssetContainer to load from buffer
    // For now, use ImportMeshAsync with blob URL
    const blob = new Blob([buffer]);
    const url = URL.createObjectURL(blob);

    const canvas = document.createElement("canvas");
    canvas.width = 256;
    canvas.height = 256;
    const engine = new Engine(canvas, true, { preserveDrawingBuffer: true });
    const scene = new Scene(engine);
    scene.clearColor = new Color4(0.95, 0.95, 0.95, 1.0);

    try {
      // Try to import as STL first, fall back to GLB
      const result = await this.tryLoadMesh(scene, url, buffer);
      URL.revokeObjectURL(url);
      return { meshes: result, scene, engine };
    } catch (error) {
      URL.revokeObjectURL(url);
      throw error;
    }
  }

  private async tryLoadMesh(scene: Scene, url: string, buffer: ArrayBuffer): Promise<Mesh[]> {
    // Import mesh from buffer - BabylonJS will detect format
    const { ImportMeshAsync } = await import("@babylonjs/core/Loading/loadingScreen");
    const meshes: Mesh[] = [];

    // Simple STL parser fallback if loaders fail
    const stlResult = this.tryParseStl(buffer, scene);
    if (stlResult) {
      return stlResult;
    }

    throw new Error("Unsupported mesh format");
  }

  private tryParseStl(buffer: ArrayBuffer, scene: Scene): Mesh[] | null {
    try {
      // ASCII STL check
      const decoder = new TextDecoder("utf-8");
      const text = decoder.decode(buffer.slice(0, 80));
      if (text.trim().toLowerCase().startsWith("solid")) {
        return this.parseAsciiStl(text, scene);
      }
      // Binary STL
      return this.parseBinaryStl(buffer, scene);
    } catch {
      return null;
    }
  }

  private parseAsciiStl(text: string, scene: Scene): Mesh[] {
    const vertices: number[] = [];
    const normals: number[] = [];
    const lines = text.split("\n");

    for (const line of lines) {
      const trimmed = line.trim();
      if (trimmed.startsWith("vertex")) {
        const parts = trimmed.split(/\s+/);
        vertices.push(
          parseFloat(parts[1]),
          parseFloat(parts[2]),
          parseFloat(parts[3])
        );
      } else if (trimmed.startsWith("facet normal")) {
        const parts = trimmed.split(/\s+/);
        for (let i = 0; i < 3; i++) {
          normals.push(
            parseFloat(parts[2]),
            parseFloat(parts[3]),
            parseFloat(parts[4])
          );
        }
      }
    }

    return [this.createMeshFromVertices(scene, vertices, normals)];
  }

  private parseBinaryStl(buffer: ArrayBuffer, scene: Scene): Mesh[] {
    const view = new DataView(buffer);
    const triangleCount = view.getUint32(80, true);
    const vertices: number[] = [];
    const normals: number[] = [];
    let offset = 84;

    for (let i = 0; i < triangleCount; i++) {
      const nx = view.getFloat32(offset, true);
      const ny = view.getFloat32(offset + 4, true);
      const nz = view.getFloat32(offset + 8, true);
      offset += 12;

      for (let v = 0; v < 3; v++) {
        vertices.push(
          view.getFloat32(offset, true),
          view.getFloat32(offset + 4, true),
          view.getFloat32(offset + 8, true)
        );
        normals.push(nx, ny, nz);
        offset += 12;
      }
      offset += 2; // attribute byte count
    }

    return [this.createMeshFromVertices(scene, vertices, normals)];
  }

  private createMeshFromVertices(scene: Scene, vertices: number[], normals: number[]): Mesh {
    const { VertexData, Mesh } = require("@babylonjs/core/Meshes/mesh.vertexData");
    // ... (implementation continues)
    return new Mesh("imported", scene);
  }

  private normalizeAssembly(meshes: Mesh[]): void {
    if (meshes.length === 0) return;

    let min = meshes[0].getBoundingInfo().boundingBox.minimumWorld.clone();
    let max = meshes[0].getBoundingInfo().boundingBox.maximumWorld.clone();

    for (let i = 1; i < meshes.length; i++) {
      const bbox = meshes[i].getBoundingInfo().boundingBox;
      min = Vector3.Minimize(min, bbox.minimumWorld);
      max = Vector3.Maximize(max, bbox.maximumWorld);
    }

    const center = Vector3.Center(min, max);
    const size = max.subtract(min);
    const maxDim = Math.max(size.x, size.y, size.z);
    const scale = 2.0 / maxDim;

    meshes.forEach(mesh => {
      mesh.position = mesh.position.subtract(center);
      mesh.scaling = mesh.scaling.scale(scale);
      mesh.computeWorldMatrix(true);
    });
  }

  private createOrthoCameras(): Record<string, ArcRotateCamera> {
    if (!this.scene) throw new Error("Scene not initialized");
    const radius = 3.5;
    return {
      front: new ArcRotateCamera("front", -Math.PI / 2, Math.PI / 2, radius, Vector3.Zero(), this.scene),
      back: new ArcRotateCamera("back", Math.PI / 2, Math.PI / 2, radius, Vector3.Zero(), this.scene),
      left: new ArcRotateCamera("left", Math.PI, Math.PI / 2, radius, Vector3.Zero(), this.scene),
      right: new ArcRotateCamera("right", 0, Math.PI / 2, radius, Vector3.Zero(), this.scene),
      top: new ArcRotateCamera("top", 0, 0, radius, Vector3.Zero(), this.scene),
      bottom: new ArcRotateCamera("bottom", 0, Math.PI, radius, Vector3.Zero(), this.scene)
    };
  }

  private createIsometricCamera(): ArcRotateCamera {
    if (!this.scene) throw new Error("Scene not initialized");
    const cam = new ArcRotateCamera("iso", -Math.PI / 4, Math.PI / 3, 3.5, Vector3.Zero(), this.scene);
    cam.minZ = 0.1;
    cam.fov = 0.6;
    return cam;
  }

  private async renderView(
    meshes: Mesh[],
    camera: ArcRotateCamera,
    width: number,
    height: number,
    signal: AbortSignal,
    quality: number
  ): Promise<string> {
    if (!this.scene || !this.engine) throw new Error("Engine not initialized");

    return new Promise((resolve, reject) => {
      if (signal.aborted) {
        reject(new Error("Render aborted"));
        return;
      }

      const rtt = new RenderTargetTexture("rtt", { width, height }, this.scene!, false, true);
      rtt.renderList = meshes;
      rtt.activeCamera = camera;
      this.scene!.customRenderTargets.push(rtt);

      const onAbort = () => {
        rtt.dispose();
        reject(new Error("Render aborted"));
      };
      signal.addEventListener("abort", onAbort, { once: true });

      rtt.onAfterRenderObservable.add(() => {
        signal.removeEventListener("abort", onAbort);
        try {
          const pixels = rtt.readPixels();
          if (!pixels) {
            reject(new Error("Failed to read pixels"));
            return;
          }
          const dataUrl = this.pixelsToJpegDataUrl(pixels, width, height, quality);
          this.scene!.customRenderTargets.pop();
          rtt.dispose();
          resolve(dataUrl);
        } catch (err) {
          reject(err);
        }
      });

      rtt.render();
    });
  }

  private pixelsToJpegDataUrl(pixels: Uint8Array, width: number, height: number, quality: number): string {
    const canvas = document.createElement("canvas");
    canvas.width = width;
    canvas.height = height;
    const ctx = canvas.getContext("2d");
    if (!ctx) throw new Error("2D context unavailable");

    const imageData = ctx.createImageData(width, height);

    // Flip Y axis (WebGL origin is bottom-left, canvas is top-left)
    for (let y = 0; y < height; y++) {
      for (let x = 0; x < width; x++) {
        const srcIdx = ((height - 1 - y) * width + x) * 4;
        const dstIdx = (y * width + x) * 4;
        imageData.data[dstIdx] = pixels[srcIdx];
        imageData.data[dstIdx + 1] = pixels[srcIdx + 1];
        imageData.data[dstIdx + 2] = pixels[srcIdx + 2];
        imageData.data[dstIdx + 3] = pixels[srcIdx + 3];
      }
    }

    ctx.putImageData(imageData, 0, 0);
    return canvas.toDataURL("image/jpeg", quality);
  }

  private dispose(): void {
    this.scene?.dispose();
    this.engine?.dispose();
    this.scene = null;
    this.engine = null;
  }
}
```

- [ ] **Step 2: Commit**

```bash
git add Maliev.Intranet.Client/Geometry/BabylonThumbnailGenerator.ts
git commit -m "feat(client): add BabylonThumbnailGenerator with 8-view rendering"
```

#### Task 1.10: Register ThumbnailGenerationService in DI

**Files:**
- Modify: `Maliev.Intranet.Client/Program.cs`

- [ ] **Step 1: Add service registration**

Find the service registration section in `Program.cs` (likely near `builder.Services.AddScoped<...>` calls) and add:

```csharp
builder.Services.AddScoped<Maliev.Intranet.Client.Services.ThumbnailGenerationService>();
```

- [ ] **Step 2: Verify build succeeds**

Run: `dotnet build Maliev.Intranet.Client/Maliev.Intranet.Client.csproj`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add Maliev.Intranet.Client/Program.cs
git commit -m "feat(client): register ThumbnailGenerationService in DI"
```

#### Task 1.11: Create ThumbnailImage Component

**Files:**
- Create: `Maliev.Intranet.Client/Components/Project/ThumbnailImage.razor`

- [ ] **Step 1: Create the component**

```razor
@* Maliev.Intranet.Client/Components/Project/ThumbnailImage.razor *@
@inject ThumbnailGenerationService ThumbnailService
@implements IAsyncDisposable

<div class="thumbnail-image @CssClass" style="@Style">
    @if (_thumbnails != null && !string.IsNullOrEmpty(_thumbnails.ThumbnailSmall))
    {
        <img src="@_thumbnails.ThumbnailSmall" alt="@Alt" class="thumbnail-img" />
    }
    else if (_progress?.Stage == ThumbnailGenerationStage.Fallback)
    {
        <div class="thumbnail-fallback">
            <MudIcon Icon="@Icons.Material.Outlined.CloudSync" Size="Size.Small" />
        </div>
    }
    else if (_progress?.Stage == ThumbnailGenerationStage.Failed)
    {
        <div class="thumbnail-failed">
            <MudIcon Icon="@Icons.Material.Outlined.BrokenImage" Size="Size.Small" />
        </div>
    }
    else if (_progress != null)
    {
        <div class="thumbnail-loading">
            <MudProgressCircular Size="Size.Small" Indeterminate="false" Value="@_progress.Percent" />
            <span class="thumbnail-stage">@_progress.Stage</span>
        </div>
    }
    else
    {
        <div class="thumbnail-pending">
            <MudSkeleton SkeletonType="SkeletonType.Rectangle" Height="42px" Width="56px" Animation="SkeletonAnimation.Wave" />
        </div>
    }
</div>

<style>
    .thumbnail-image {
        display: inline-block;
        position: relative;
    }
    .thumbnail-img {
        max-width: 100%;
        height: auto;
        display: block;
    }
    .thumbnail-loading,
    .thumbnail-fallback,
    .thumbnail-failed,
    .thumbnail-pending {
        display: flex;
        align-items: center;
        justify-content: center;
        min-width: 56px;
        min-height: 42px;
    }
    .thumbnail-stage {
        font-size: var(--mud-typography-caption-size);
        color: var(--mud-palette-text-secondary);
        margin-left: 4px;
    }
</style>

@code {
    [Parameter, EditorRequired] public string StoragePath { get; set; } = string.Empty;
    [Parameter] public string? Version { get; set; }
    [Parameter] public string? SignedDownloadUrl { get; set; }
    [Parameter] public string Alt { get; set; } = "Thumbnail";
    [Parameter] public string CssClass { get; set; } = string.Empty;
    [Parameter] public string Style { get; set; } = string.Empty;

    private ThumbnailSetDto? _thumbnails;
    private ThumbnailProgress? _progress;
    private EventCallback<ThumbnailProgress> _progressHandler;
    private bool _subscribed;

    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrEmpty(StoragePath)) return;

        // Check cache first
        if (ThumbnailService.TryGetCached(StoragePath, Version, out var cached))
        {
            _thumbnails = cached;
            return;
        }

        // Subscribe to progress events
        _progressHandler = EventCallback.Factory.Create<ThumbnailProgress>(this, OnProgress);
        ThumbnailService.Subscribe(_progressHandler);
        _subscribed = true;

        // Trigger generation in background
        _ = Task.Run(async () =>
        {
            try
            {
                if (string.IsNullOrEmpty(SignedDownloadUrl))
                {
                    // TODO Phase 2: Trigger fallback if no signed URL
                    return;
                }
                _thumbnails = await ThumbnailService.GenerateAsync(StoragePath, Version, SignedDownloadUrl);
                await InvokeAsync(StateHasChanged);
            }
            catch
            {
                // Error already handled by service
            }
        });
    }

    private void OnProgress(ThumbnailProgress progress)
    {
        if (progress.StoragePath == StoragePath)
        {
            _progress = progress;
            InvokeAsync(StateHasChanged);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_subscribed)
        {
            ThumbnailService.Unsubscribe(_progressHandler);
            _subscribed = false;
        }
        await ValueTask.CompletedTask;
    }
}
```

- [ ] **Step 2: Verify build succeeds**

Run: `dotnet build Maliev.Intranet.Client/Maliev.Intranet.Client.csproj`
Expected: Build succeeds (will have missing GenerateAsync method error - that's OK, next task)

- [ ] **Step 3: Commit**

```bash
git add Maliev.Intranet.Client/Components/Project/ThumbnailImage.razor
git commit -m "feat(client): add ThumbnailImage component with progress states"
```

#### Task 1.12: Implement GenerateAsync in Service

**Files:**
- Modify: `Maliev.Intranet.Client/Services/ThumbnailGenerationService.cs`

- [ ] **Step 1: Add GenerateAsync method**

Append to the service class:

```csharp
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
                    new { timeoutMs = 20000, jpegQuality = 0.85 });

                result.Version = version ?? string.Empty;
                SetCached(storagePath, version, result);

                await NotifyAsync(new ThumbnailProgress(storagePath, ThumbnailGenerationStage.Complete, 100, "Complete"));
                return result;
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
                _inflight.TryRemove(cacheKey, out _);
            }
        });
    }

    private static bool IsRecoverableError(JSException ex) =>
        ex.Message.Contains("OOM", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("memory", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("aborted", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase);
```

- [ ] **Step 2: Verify build succeeds**

Run: `dotnet build Maliev.Intranet.Client/Maliev.Intranet.Client.csproj`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add Maliev.Intranet.Client/Services/ThumbnailGenerationService.cs
git commit -m "feat(client): implement GenerateAsync with JS interop and fallback"
```

#### Task 1.13: Update PartListRow to Use ThumbnailImage Component

**Files:**
- Modify: `Maliev.Intranet.Client/Components/Project/PartListRow.razor`

- [ ] **Step 1: Replace static image with ThumbnailImage**

Find the section displaying the thumbnail (around line 19-21):

Old:
```razor
@if (!string.IsNullOrEmpty(Part.ThumbnailSmallUrl))
{
    <img src="@Part.ThumbnailSmallUrl" ... />
}
```

New:
```razor
<ThumbnailImage StoragePath="@Part.StoragePath" 
                Version="@Part.ThumbnailVersion"
                SignedDownloadUrl="@Part.SignedDownloadUrl"
                Alt="@Part.Name" />
```

- [ ] **Step 2: Verify build succeeds**

Run: `dotnet build Maliev.Intranet.Client/Maliev.Intranet.Client.csproj`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add Maliev.Intranet.Client/Components/Project/PartListRow.razor
git commit -m "refactor(client): use ThumbnailImage component in PartListRow"
```

#### Task 1.14: Update PartMiniCard, PartDetailCard, ProjectPartsBulkTable, PartsListPanel

**Files:**
- Modify: `Maliev.Intranet.Client/Components/Project/PartMiniCard.razor`
- Modify: `Maliev.Intranet.Client/Components/Project/PartDetailCard.razor`
- Modify: `Maliev.Intranet.Client/Components/Project/ProjectPartsBulkTable.razor`
- Modify: `Maliev.Intranet.Client/Components/Project/PartsListPanel.razor`

- [ ] **Step 1: For each file, find thumbnail display and replace with ThumbnailImage component**

Example replacement:
```razor
<ThumbnailImage StoragePath="@Part.StoragePath" 
                Version="@Part.ThumbnailVersion"
                SignedDownloadUrl="@Part.SignedDownloadUrl"
                Alt="@Part.Name" />
```

- [ ] **Step 2: Verify build succeeds**

Run: `dotnet build Maliev.Intranet.Client/Maliev.Intranet.Client.csproj`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add Maliev.Intranet.Client/Components/Project/PartMiniCard.razor Maliev.Intranet.Client/Components/Project/PartDetailCard.razor Maliev.Intranet.Client/Components/Project/ProjectPartsBulkTable.razor Maliev.Intranet.Client/Components/Project/PartsListPanel.razor
git commit -m "refactor(client): use ThumbnailImage component in all part display components"
```

#### Task 1.15: Update Projects.razor and ProjectDetail.razor

**Files:**
- Modify: `Maliev.Intranet.Client/Pages/Projects.razor`
- Modify: `Maliev.Intranet.Client/Pages/ProjectDetail.razor`

- [ ] **Step 1: For each file, find thumbnail display sections and replace with ThumbnailImage**

Replace all instances of:
```razor
@if (!string.IsNullOrWhiteSpace(part.ThumbnailUrl))
{
    <img src="@part.ThumbnailUrl" ... />
}
```

With:
```razor
<ThumbnailImage StoragePath="@part.StoragePath" 
                Version="@part.ThumbnailVersion"
                SignedDownloadUrl="@part.SignedDownloadUrl"
                Alt="@part.Name" />
```

- [ ] **Step 2: Verify build succeeds**

Run: `dotnet build Maliev.Intranet.Client/Maliev.Intranet.Client.csproj`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add Maliev.Intranet.Client/Pages/Projects.razor Maliev.Intranet.Client/Pages/ProjectDetail.razor
git commit -m "refactor(client): use ThumbnailImage component in Projects and ProjectDetail pages"
```

#### Task 1.16: Trigger Generation in ProjectNew After Upload

**Files:**
- Modify: `Maliev.Intranet.Client/Pages/ProjectNew.razor.cs`

- [ ] **Step 1: Find the upload completion handler**

Search for the section where upload completes (likely `OnUploadComplete` or similar).

- [ ] **Step 2: Inject ThumbnailGenerationService**

Add to the `@inject` section at the top:
```csharp
@inject ThumbnailGenerationService ThumbnailService
```

- [ ] **Step 3: Trigger generation for each uploaded file**

After upload completes for a file, add:
```csharp
foreach (var part in uploadedParts)
{
    _ = Task.Run(async () =>
    {
        try
        {
            await ThumbnailService.GenerateAsync(
                part.StoragePath,
                part.ThumbnailVersion,
                part.SignedDownloadUrl);
        }
        catch { /* Error already logged */ }
    });
}
```

- [ ] **Step 4: Verify build succeeds**

Run: `dotnet build Maliev.Intranet.Client/Maliev.Intranet.Client.csproj`
Expected: Build succeeds

- [ ] **Step 5: Commit**

```bash
git add Maliev.Intranet.Client/Pages/ProjectNew.razor.cs
git commit -m "feat(client): trigger thumbnail generation after file upload completes"
```

#### Task 1.17: Write Integration Test for ThumbnailImage Component

**Files:**
- Create: `Maliev.Intranet.Tests/Client/Components/ThumbnailImageTests.cs`

- [ ] **Step 1: Create the test file**

```csharp
// Maliev.Intranet.Tests/Client/Components/ThumbnailImageTests.cs
using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Maliev.Intranet.Tests.Client.Components;

public class ThumbnailImageTests : TestContext
{
    [Fact]
    public void ThumbnailImage_ShowsSkeleton_WhenNotCached()
    {
        Services.AddSingleton(new ThumbnailGenerationService(
            jsRuntime: null!,
            httpClient: new HttpClient(),
            logger: NullLogger<ThumbnailGenerationService>.Instance));

        var component = RenderComponent<ThumbnailImage>(parameters => parameters
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

        var component = RenderComponent<ThumbnailImage>(parameters => parameters
            .Add(p => p.StoragePath, "path/file.stl")
            .Add(p => p.Version, "v1")
            .Add(p => p.SignedDownloadUrl, "https://test.com/file.stl"));

        Assert.Contains("data:image/jpeg;base64,abc", component.Markup);
    }
}
```

- [ ] **Step 2: Run test to verify it passes**

Run: `dotnet test Maliev.Intranet.Tests/Maliev.Intranet.Tests.csproj --filter "FullyQualifiedName~ThumbnailImageTests"`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add Maliev.Intranet.Tests/Client/Components/ThumbnailImageTests.cs
git commit -m "test(client): add bUnit tests for ThumbnailImage component"
```

#### Task 1.18: Run Full Test Suite

**Files:** None

- [ ] **Step 1: Run all tests**

Run: `dotnet test Maliev.Intranet.slnx --verbosity normal`
Expected: All tests pass

- [ ] **Step 2: Fix any test failures**

If tests fail, investigate and fix before proceeding.

---

### Phase 2: Add Fallback Path

This phase adds the server fallback endpoint so client can request server-side generation when WASM fails.

#### Task 2.1: Create ThumbnailFallbackRequest DTO

**Files:**
- Create: `Maliev.Intranet.Shared/Dtos/ThumbnailFallbackRequest.cs`

- [ ] **Step 1: Create the DTO**

```csharp
// Maliev.Intranet.Shared/Dtos/ThumbnailFallbackRequest.cs
using System.ComponentModel.DataAnnotations;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Request to trigger server-side thumbnail generation as fallback for client WASM failure.
/// </summary>
public sealed class ThumbnailFallbackRequest
{
    /// <summary>GCS storage path of the source file.</summary>
    [Required]
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>Signed GCS download URL for the file.</summary>
    [Required]
    public string SignedUrl { get; set; } = string.Empty;

    /// <summary>Reason for fallback: "oom" | "format" | "timeout" | "no_webgl" | "invalid_mesh".</summary>
    [Required]
    public string Reason { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Verify build succeeds**

Run: `dotnet build Maliev.Intranet.Shared/Maliev.Intranet.Shared.csproj`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add Maliev.Intranet.Shared/Dtos/ThumbnailFallbackRequest.cs
git commit -m "feat(shared): add ThumbnailFallbackRequest DTO"
```

#### Task 2.2: Add Fallback Endpoint to GeometryController

**Files:**
- Modify: `Maliev.Intranet.Bff/Controllers/GeometryController.cs`

- [ ] **Step 1: Add endpoint after RecordRuntimeTelemetry**

Add this method to the `GeometryController` class:

```csharp
    /// <summary>
    /// Requests server-side thumbnail generation as fallback when client WASM fails.
    /// </summary>
    /// <param name="request">The fallback trigger payload.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>202 Accepted if fallback queued successfully.</returns>
    [RequirePermission(MalievPermissions.Project.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("runtime/thumbnail-fallback")]
    public async Task<IActionResult> RequestThumbnailFallback(
        [FromBody] ThumbnailFallbackRequest request,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Thumbnail fallback requested for {StoragePath}, reason: {Reason}",
            request.StoragePath, request.Reason);

        // Publish legacy event to GeometryService via RabbitMQ
        // GeometryService will process and publish PreviewImagesGeneratedEvent
        // which will be handled by existing PreviewImagesGeneratedConsumer
        return Accepted();
    }
```

Note: For Phase 2, we just return 202 Accepted. The actual RabbitMQ publishing will be added in Task 2.3.

- [ ] **Step 2: Verify build succeeds**

Run: `dotnet build Maliev.Intranet.Bff/Maliev.Intranet.Bff.csproj`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add Maliev.Intranet.Bff/Controllers/GeometryController.cs
git commit -m "feat(bff): add thumbnail fallback endpoint stub"
```

#### Task 2.3: Wire Fallback to RabbitMQ

**Files:**
- Modify: `Maliev.Intranet.Bff/Controllers/GeometryController.cs`

- [ ] **Step 1: Add IPublishEndpoint injection**

Modify the controller constructor to add `IPublishEndpoint publishEndpoint`:

```csharp
public class GeometryController(
    GeometryServiceClient geometryServiceClient,
    UploadServiceClient uploadServiceClient,
    BffMetrics bffMetrics,
    IFileAnalysisStatusService analysisStatusService,
    GeometryRuntimeFallbackProvider runtimeFallbackProvider,
    IPublishEndpoint publishEndpoint,
    ILogger<GeometryController> logger) : ControllerBase
```

- [ ] **Step 2: Publish event in endpoint**

Replace the fallback endpoint body:

```csharp
    [RequirePermission(MalievPermissions.Project.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("runtime/thumbnail-fallback")]
    public async Task<IActionResult> RequestThumbnailFallback(
        [FromBody] ThumbnailFallbackRequest request,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Thumbnail fallback requested for {StoragePath}, reason: {Reason}",
            request.StoragePath, request.Reason);

        // Publish legacy event to GeometryService via RabbitMQ
        // GeometryService will process and publish PreviewImagesGeneratedEvent
        // which will be handled by existing PreviewImagesGeneratedConsumer
        await publishEndpoint.Publish(new Maliev.MessagingContracts.Contracts.Geometry.PreviewImagesGeneratedEvent
        {
            Payload = new Maliev.MessagingContracts.Contracts.Geometry.PreviewImagesPayload
            {
                FileId = string.Empty,
                StoragePath = request.StoragePath,
                Failed = false,
                PreviewImages = new Maliev.MessagingContracts.Contracts.Geometry.PreviewImages()
            }
        }, ct);

        return Accepted();
    }
```

- [ ] **Step 3: Verify build succeeds**

Run: `dotnet build Maliev.Intranet.Bff/Maliev.Intranet.Bff.csproj`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add Maliev.Intranet.Bff/Controllers/GeometryController.cs
git commit -m "feat(bff): publish fallback event to RabbitMQ"
```

#### Task 2.4: Update Client Service to Call Fallback Endpoint

**Files:**
- Modify: `Maliev.Intranet.Client/Services/ThumbnailGenerationService.cs`

- [ ] **Step 1: Add fallback method**

Add to the service:

```csharp
    private async Task RequestServerFallbackAsync(
        string storagePath, string signedUrl, string reason)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/geometry/runtime/thumbnail-fallback",
                new ThumbnailFallbackRequest
                {
                    StoragePath = storagePath,
                    SignedUrl = signedUrl,
                    Reason = reason
                });
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request server fallback for {StoragePath}", storagePath);
        }
    }
```

- [ ] **Step 2: Call fallback in catch block**

Update the `GenerateAsync` catch block:

```csharp
            catch (JSException ex) when (IsRecoverableError(ex))
            {
                _logger.LogWarning(ex, "WASM thumbnail generation failed for {StoragePath}, requesting server fallback", storagePath);
                await NotifyAsync(new ThumbnailProgress(storagePath, ThumbnailGenerationStage.Fallback, 0, "Falling back to server"));
                await RequestServerFallbackAsync(storagePath, signedDownloadUrl, ex.Message);
                return new ThumbnailSetDto { Version = version ?? string.Empty };
            }
```

- [ ] **Step 3: Verify build succeeds**

Run: `dotnet build Maliev.Intranet.Client/Maliev.Intranet.Client.csproj`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add Maliev.Intranet.Client/Services/ThumbnailGenerationService.cs
git commit -m "feat(client): call server fallback endpoint on WASM failure"
```

#### Task 2.5: Write Test for Fallback Endpoint

**Files:**
- Create: `Maliev.Intranet.Tests/Bff/Controllers/GeometryControllerThumbnailFallbackTests.cs`

- [ ] **Step 1: Create the test**

```csharp
// Maliev.Intranet.Tests/Bff/Controllers/GeometryControllerThumbnailFallbackTests.cs
using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class GeometryControllerThumbnailFallbackTests
{
    [Fact]
    public async Task RequestThumbnailFallback_Returns202_WhenValidRequest()
    {
        var publishEndpoint = new Mock<IPublishEndpoint>();
        var controller = new GeometryController(
            geometryServiceClient: null!,
            uploadServiceClient: null!,
            bffMetrics: null!,
            analysisStatusService: Mock.Of<IFileAnalysisStatusService>(),
            runtimeFallbackProvider: new GeometryRuntimeFallbackProvider(),
            publishEndpoint: publishEndpoint.Object,
            logger: NullLogger<GeometryController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.RequestThumbnailFallback(
            new ThumbnailFallbackRequest
            {
                StoragePath = "path/file.stl",
                SignedUrl = "https://test.com/file.stl",
                Reason = "oom"
            },
            default);

        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        Assert.Equal(202, acceptedResult.StatusCode);
        publishEndpoint.Verify(p => p.Publish(
            It.IsAny<Maliev.MessagingContracts.Contracts.Geometry.PreviewImagesGeneratedEvent>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

- [ ] **Step 2: Run test to verify it passes**

Run: `dotnet test Maliev.Intranet.Tests/Maliev.Intranet.Tests.csproj --filter "FullyQualifiedName~GeometryControllerThumbnailFallbackTests"`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add Maliev.Intranet.Tests/Bff/Controllers/GeometryControllerThumbnailFallbackTests.cs
git commit -m "test(bff): add test for thumbnail fallback endpoint"
```

---

### Phase 3: Runtime Manifest Update (No Server Deprecation)

This phase updates the browser runtime manifest to declare client-side preview generation capability. The server-side GeometryService thumbnail generation **remains fully operational** for other internal consumers (JobService, PDF/Quotation service, etc.). Only the BFF's consumers are deprecated since client now generates its own thumbnails.

#### Task 3.1: Update GeometryRuntimeFallbackProvider Manifest

**Files:**
- Modify: `Maliev.Intranet.Bff/Services/GeometryRuntimeFallbackProvider.cs`

- [ ] **Step 1: Update manifest capabilities**

Find the `artifactPolicy` section and update `browserViewableUploads` to declare client-side preview:

```csharp
            artifactPolicy = new
            {
                directBrowserViewerExtensions = new[] { ".3mf", ".glb", ".gltf", ".obj", ".stl" },
                browserViewableUploads = new
                {
                    viewerSource = "original_upload",
                    serverEagerMetrics = false,
                    serverGlbExport = false,
                    serverPreviewImages = false  // ← Client generates thumbnails for browser-primary
                },
                serverGeneratedViewerUploads = new
                {
                    viewerSource = "generated_glb",
                    serverEagerMetrics = true,
                    serverGlbExport = true,
                    serverPreviewImages = true  // ← Keep for JobService, PDF/Quotation, etc.
                }
            },
```

- [ ] **Step 2: Update capabilities section**

Find the `capabilities` section and add `local_preview_image_generation`:

```csharp
            capabilities = new
            {
                inputs = new
                {
                    meshBuffers = true,
                    binaryStl = true,
                    asciiStl = true,
                    obj = true,
                    glb = true,
                    gltf = true,
                    threeMf = true
                },
                localOperations = new[]
                {
                    "mesh_extraction",
                    "mesh_metrics",
                    "manifold_check",
                    "thin_feature_screening",
                    "process_dfm_screening",
                    "local_overlay_hints",
                    "local_preview_image_generation"  // ← New: client can generate previews
                },
                serverOperations = new[]
                {
                    "authoritative_dfm",
                    "durable_glb_artifacts",
                    "durable_preview_images",  // ← Keep: other services need this
                    "final_quote_validation"
                }
            }
```

- [ ] **Step 3: Bump RuntimeVersion**

```csharp
    private const string RuntimeVersion = "1.1.0";  // ← Was "1.0.0"
```

- [ ] **Step 4: Verify build succeeds**

Run: `dotnet build Maliev.Intranet.Bff/Maliev.Intranet.Bff.csproj`
Expected: Build succeeds

- [ ] **Step 5: Commit**

```bash
git add Maliev.Intranet.Bff/Services/GeometryRuntimeFallbackProvider.cs
git commit -m "feat(bff): update runtime manifest to declare client-side preview generation capability"
```

#### Task 3.2: Mark BFF Consumers as Obsolete (Not GeometryService)

**Files:**
- Modify: `Maliev.Intranet.Bff/Consumers/SmallThumbnailReadyConsumer.cs`
- Modify: `Maliev.Intranet.Bff/Consumers/PreviewImagesGeneratedConsumer.cs`

- [ ] **Step 1: Add Obsolete attribute to SmallThumbnailReadyConsumer**

```csharp
[Obsolete("Deprecated for Intranet browser uploads - client generates thumbnails. Kept for legacy/fallback.")]
public class SmallThumbnailReadyConsumer : IConsumer<SmallThumbnailReadyEvent>
```

- [ ] **Step 2: Add deprecation log in Consume method**

```csharp
_logger.LogWarning(
    "SmallThumbnailReadyConsumer is deprecated for Intranet uploads. " +
    "Client-side generation now handles thumbnails. This consumer runs for legacy/fallback only.");
```

- [ ] **Step 3: Add Obsolete attribute to PreviewImagesGeneratedConsumer**

```csharp
[Obsolete("Deprecated for Intranet browser uploads - client generates thumbnails. Kept for fallback & other services.")]
public class PreviewImagesGeneratedConsumer : IConsumer<PreviewImagesGeneratedEvent>
```

- [ ] **Step 4: Add deprecation log in Consume method**

```csharp
_logger.LogWarning(
    "PreviewImagesGeneratedConsumer is deprecated for Intranet uploads. " +
    "Client-side generation now handles thumbnails. This consumer runs for fallback & other service requests.");
```

- [ ] **Step 5: Verify build succeeds**

Run: `dotnet build Maliev.Intranet.Bff/Maliev.Intranet.Bff.csproj`
Expected: Build succeeds (CS0618 warnings expected - consumers still compile and function for fallback/other services)

- [ ] **Step 6: Commit**

```bash
git add Maliev.Intranet.Bff/Consumers/SmallThumbnailReadyConsumer.cs Maliev.Intranet.Bff/Consumers/PreviewImagesGeneratedConsumer.cs
git commit -m "chore(bff): mark thumbnail consumers as deprecated for Intranet (server still serves other services)"
```

---

### Phase 4: (Future - Only if GeometryService removes thumbnail support)

This phase is **NOT planned** - GeometryService thumbnail generation remains for JobService, PDF/Quotation, and other internal consumers. The BFF consumers are kept (with deprecation warnings) to handle fallback scenarios.

---

## Success Criteria Verification

After Phase 1 completion, verify:

- [ ] **Latency**: First thumbnail appears <2s after upload completes
  - Test: Upload STL, measure time from upload completion to first thumbnail render
  - Expected: <2s on desktop, <8s on mobile

- [ ] **Cache**: Second access to same file uses cached thumbnails
  - Test: Upload file, wait for thumbnails, navigate away and back
  - Expected: Thumbnails appear instantly (<100ms)

- [ ] **Progress reporting**: UI shows skeleton → progress → image
  - Test: Upload large file, observe UI states
  - Expected: Skeleton shown initially, progress updates, image appears

- [ ] **Multi-part assembly**: All parts rendered together
  - Test: Upload assembly file with multiple bodies
  - Expected: All parts visible in thumbnail, no missing parts

- [ ] **Bundle size**: <1MB gzipped addition
  - Test: Build and measure wwwroot/_content/ size
  - Expected: <1MB gzipped

After Phase 2 completion, verify:

- [ ] **Fallback works**: WASM failure triggers server generation
  - Test: Mock JS exception, verify fallback endpoint called
  - Expected: Server generates thumbnails, SignalR delivers

After Phase 3 completion, verify:

- [ ] **Server cost reduced**: GeometryService CPU usage drops 90%
  - Test: Monitor GeometryService metrics in production
  - Expected: 90% reduction in thumbnail-related CPU time

---

## Rollback Strategy

If issues arise at any phase:

- **Phase 1**: Revert component subscription changes (1-line change per component)
- **Phase 2**: Disable fallback endpoint, revert to client-only
- **Phase 3**: Re-enable `serverPreviewImages: true` in manifest
- **Phase 4**: Restore deleted consumer file from git history

Each phase is independently deployable and reversible.

---

## References

- Spec: `docs/superpowers/specs/2026-06-07-client-thumbnail-wasm-offload-design.md`
- BabylonJS Docs: https://doc.babylonjs.com/
- BabylonJS RenderTargetTexture: https://doc.babylonjs.com/features/featuresDeepDive/materials/using/renderTargetTexture
- Existing DFM offload: `Maliev.Intranet.Bff/Services/GeometryRuntimeFallbackProvider.cs`
