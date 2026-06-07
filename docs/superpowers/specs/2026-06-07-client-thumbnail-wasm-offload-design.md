# Client-Side Thumbnail Generation (WASM/BabylonJS Offload)

**Date**: 2026-06-07
**Status**: Approved
**Service**: Maliev.Intranet (BFF + Client)

---

## Summary

Offload all thumbnail/preview image generation from `Maliev.GeometryService` (Python) to the client browser using **BabylonJS** running in WebAssembly. The server becomes a fallback only. This mirrors the existing browser-first DFM architecture, achieving:

1. **Zero-latency first preview** (~500ms vs current 5-15s server roundtrip)
2. **Massive GKE cost reduction** (no GPU/CPU for rendering on GeometryService)
3. **Full offline/air-gapped support**
4. **Architectural consistency** with the existing DFM offload

**Output parity**: All 8 views (Front, Back, Left, Right, Top, Bottom, ThumbnailSmall 256px, ThumbnailLarge 1200px) generated client-side, matching current `PreviewImagesGeneratedEvent` payload.

---

## Current State (Problem)

`Maliev.GeometryService` (Python microservice) currently:

1. Receives file upload events via RabbitMQ
2. Loads mesh (STL/OBJ/GLB/3MF) using OpenCascade/trimesh
3. Renders 6 orthographic face views + 2 isometric views to PNG
4. Uploads to GCS
5. Publishes `SmallThumbnailReadyEvent` and `PreviewImagesGeneratedEvent` via RabbitMQ
6. BFF consumers resolve signed URLs and broadcast to clients via SignalR

**Pain points**:
- 5-15s latency from upload completion to first thumbnail appearing
- GeometryService CPU/GPU cost scales with upload volume
- No offline capability
- Architectural inconsistency with the already-offloaded DFM analysis

The browser geometry runtime (`GeometryRuntimeFallbackProvider`) already packages a WASM kernel + JS worker for DFM. This work extends that runtime to include thumbnail rendering.

---

## Proposed Solution

Add a **client-side thumbnail generator** using BabylonJS, orchestrated by a Blazor service, with server fallback for edge cases.

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│ Client Browser (Blazor WASM)                                │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ThumbnailGenerationService (Blazor)                    │  │
│  │  - Monitors uploaded files                             │  │
│  │  - Triggers BabylonThumbnailGenerator                  │  │
│  │  - Reports progress (SignalR-like EventCallback)       │  │
│  │  - Handles fallback decisions                          │  │
│  └────────────────────────────────────────────────────────┘  │
│                            ↓                                  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ BabylonThumbnailGenerator (JS/BabylonJS)               │  │
│  │  - Loads mesh from signed GCS URL                      │  │
│  │  - Creates 8 cameras (6 ortho + 2 iso)                 │  │
│  │  - Renders to RenderTargetTexture                      │  │
│  │  - Encodes to base64 JPEG                              │  │
│  │  - Returns ThumbnailSet DTO                            │  │
│  └────────────────────────────────────────────────────────┘  │
│                            ↓                                  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Existing UI Components                                 │  │
│  │  - ProjectPartsBulkTable                                │  │
│  │  - PartListRow, PartMiniCard, PartDetailCard            │  │
│  │  - Projects, ProjectDetail, Catalog, GlobalSearchBox    │  │
│  └────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            ↓ (fallback only)
┌─────────────────────────────────────────────────────────────┐
│ Maliev.Intranet.Bff                                          │
│  POST /api/v1/geometry/runtime/thumbnail-fallback           │
│    → Queues legacy GeometryService job                       │
│    → SignalR delivers when done (unchanged consumers)        │
└─────────────────────────────────────────────────────────────┘
```

### Data Flow (Happy Path)

1. **File upload completes** → Client receives `storagePath` + signed download URL
2. **`ThumbnailGenerationService` triggers** generation for the new file
3. **Progress events fire** (0% → 10% download → 30% mesh load → 50% render → 80% encode → 100%)
4. **BabylonThumbnailGenerator**:
   - Downloads file via signed URL (streaming, not full preload)
   - Loads mesh (STL/OBJ/GLB/3MF) via BabylonJS `SceneLoader`
   - Normalizes mesh (center at origin, fit to unit cube)
   - Creates 8 cameras: 6 orthographic (Front/Back/Left/Right/Top/Bottom) + 2 isometric (256px, 1200px)
   - Renders each to offscreen `RenderTargetTexture` using WebGL 2
   - Reads pixels → encodes to JPEG (quality 0.85) → base64 DataURL
5. **Immediate UI update** - thumbnails appear in all consuming components
6. **SignalR broadcast** (optional) - syncs to other tabs/devices

### Data Flow (Fallback Path)

Triggered when:
- WASM OOM (file > 50MB on mobile, >200MB on desktop)
- Unsupported format (STEP, IGES, CATIA)
- Timeout (>20s desktop, >8s mobile)
- WebGL unavailable

```
Client → POST /api/v1/geometry/runtime/thumbnail-fallback
         { storagePath, signedUrl, reason: "oom" | "format" | "timeout" | "no_webgl" }
       → BFF publishes legacy event to RabbitMQ
       → GeometryService processes asynchronously
       → PreviewImagesGeneratedConsumer (unchanged) delivers via SignalR
```

---

## Components

### New Components

| Component | File | Responsibility |
|-----------|------|----------------|
| `BabylonThumbnailGenerator` | `Maliev.Intranet.Client/Geometry/BabylonThumbnailGenerator.ts` | Core WASM/JS module: mesh loading, 8-camera rendering, JPEG encoding |
| `ThumbnailGenerationService` | `Maliev.Intranet.Client/Services/ThumbnailGenerationService.cs` | Blazor service: orchestration, progress reporting, fallback decision, caching |
| `ThumbnailSet` | `Maliev.Intranet.Shared/Dtos/ThumbnailSetDto.cs` | DTO with 8 base64 DataURLs + dimensions |
| `ThumbnailFallbackRequest` | `Maliev.Intranet.Shared/Dtos/ThumbnailFallbackRequest.cs` | Fallback trigger payload |
| `GeometryController.ThumbnailFallback` | `Maliev.Intranet.Bff/Controllers/GeometryController.cs` | New endpoint: queues legacy job |
| `ThumbnailFallbackConsumer` | `Maliev.Intranet.Bff/Consumers/ThumbnailFallbackConsumer.cs` | Publishes legacy event to RabbitMQ |

### Modified Components

| Component | Change |
|-----------|--------|
| `GeometryRuntimeFallbackProvider` | Extend manifest to declare `localPreviewImages: true` capability |
| `ProjectPartsBulkTable.razor` | Subscribe to `ThumbnailGenerationService` instead of waiting for SignalR |
| `PartListRow.razor`, `PartMiniCard.razor`, `PartDetailCard.razor` | Same subscription pattern |
| `Projects.razor`, `ProjectDetail.razor` | Show skeleton → partial → complete states |
| `ProjectNew.razor.cs` | Trigger generation immediately after upload completes |

### Deprecated (Phase 3)

- `SmallThumbnailReadyConsumer` - removed (replaced by client generation)
- `PreviewImagesGeneratedConsumer` - kept as fallback handler only

---

## Implementation Details

### BabylonThumbnailGenerator (TypeScript)

```typescript
// Maliev.Intranet.Client/Geometry/BabylonThumbnailGenerator.ts
import { Engine, Scene, SceneLoader, ArcRotateCamera, Vector3, Color3, Color4, HemisphericLight, Mesh, RenderTargetTexture, Tools } from "@babylonjs/core";

export interface ThumbnailSet {
  frontSmall: string;    // base64 JPEG DataURL
  backSmall: string;
  leftSmall: string;
  rightSmall: string;
  topSmall: string;
  bottomSmall: string;
  thumbnailSmall: string; // 256px isometric
  thumbnailLarge: string; // 1200px isometric
}

export interface ThumbnailOptions {
  onProgress?: (percent: number, stage: string) => void;
  timeoutMs?: number;
  outputFormat?: "jpeg" | "webp";
  jpegQuality?: number; // 0-1
}

export class BabylonThumbnailGenerator {
  private engine: Engine | null = null;
  private scene: Scene | null = null;

  async generateAllViews(
    fileUrl: string,
    options: ThumbnailOptions = {}
  ): Promise<ThumbnailSet> {
    const { onProgress, timeoutMs = 20000, jpegQuality = 0.85 } = options;
    const abortController = new AbortController();
    const timeoutId = setTimeout(() => abortController.abort(), timeoutMs);

    try {
      onProgress?.(10, "Downloading mesh");

      // 1. Download file (streaming)
      const response = await fetch(fileUrl, { signal: abortController.signal });
      if (!response.ok) throw new Error(`Download failed: ${response.status}`);
      const buffer = await response.arrayBuffer();

      onProgress?.(30, "Loading mesh");

      // 2. Initialize offscreen engine
      this.engine = new Engine(null, false, { preserveDrawingBuffer: true });
      this.scene = new Scene(this.engine);
      this.scene.clearColor = new Color4(0.95, 0.95, 0.95, 1.0); // Light gray background
      this.scene.ambientColor = new Color3(0.5, 0.5, 0.5);

      // 3. Load mesh
      const result = await SceneLoader.ImportMeshAsync("", "", buffer, this.scene);
      const meshes = result.meshes.filter(m => m instanceof Mesh) as Mesh[];
      if (meshes.length === 0) throw new Error("No meshes loaded");

      onProgress?.(50, "Normalizing assembly");

      // 4. Normalize: center and scale entire assembly
      this.normalizeAssembly(meshes);

      // 5. Add lighting
      const light = new HemisphericLight("hemi", new Vector3(0, 1, 0), this.scene);
      light.intensity = 0.7;

      onProgress?.(60, "Rendering views");

      // 6. Create 8 cameras and render
      const cameras = this.createOrthoCameras();
      const isoSmall = this.createIsometricCamera(256);
      const isoLarge = this.createIsometricCamera(1200);

      const views: ThumbnailSet = {
        frontSmall: "",
        backSmall: "",
        leftSmall: "",
        rightSmall: "",
        topSmall: "",
        bottomSmall: "",
        thumbnailSmall: "",
        thumbnailLarge: ""
      };

      // Render in parallel
      const renderJobs = [
        this.renderView(meshes, cameras.front, 256, 256, abortController.signal).then(d => views.frontSmall = d),
        this.renderView(meshes, cameras.back, 256, 256, abortController.signal).then(d => views.backSmall = d),
        this.renderView(meshes, cameras.left, 256, 256, abortController.signal).then(d => views.leftSmall = d),
        this.renderView(meshes, cameras.right, 256, 256, abortController.signal).then(d => views.rightSmall = d),
        this.renderView(meshes, cameras.top, 256, 256, abortController.signal).then(d => views.topSmall = d),
        this.renderView(meshes, cameras.bottom, 256, 256, abortController.signal).then(d => views.bottomSmall = d),
        this.renderView(meshes, isoSmall, 256, 256, abortController.signal).then(d => views.thumbnailSmall = d),
        this.renderView(meshes, isoLarge, 1200, 1200, abortController.signal).then(d => views.thumbnailLarge = d)
      ];

      await Promise.all(renderJobs);

      onProgress?.(100, "Complete");
      return views;
    } finally {
      clearTimeout(timeoutId);
      this.dispose();
    }
  }

  private normalizeAssembly(meshes: Mesh[]): void {
    // Compute combined bounding box
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
    const scale = 2.0 / maxDim; // Fit to unit cube with padding

    // Center and scale all meshes
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

  private createIsometricCamera(size: number): ArcRotateCamera {
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
    signal: AbortSignal
  ): Promise<string> {
    if (!this.scene || !this.engine) throw new Error("Engine not initialized");

    // Create RenderTargetTexture
    const rtt = new RenderTargetTexture("rtt", { width, height }, this.scene, false, true);
    rtt.renderList = meshes;
    rtt.activeCamera = camera;
    this.scene.customRenderTargets.push(rtt);

    return new Promise((resolve, reject) => {
      const onAbort = () => reject(new Error("Render aborted"));
      signal.addEventListener("abort", onAbort);

      rtt.onAfterRenderObservable.add(() => {
        signal.removeEventListener("abort", onAbort);
        try {
          const pixels = rtt.readPixels();
          if (!pixels) {
            reject(new Error("Failed to read pixels"));
            return;
          }
          const dataUrl = this.pixelsToJpegDataUrl(pixels, width, height, 0.85);
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
    // Create canvas, draw pixels, export as JPEG
    const canvas = document.createElement("canvas");
    canvas.width = width;
    canvas.height = height;
    const ctx = canvas.getContext("2d")!;
    const imageData = ctx.createImageData(width, height);
    
    // BabylonJS returns RGBA, flip Y axis for canvas
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

### ThumbnailGenerationService (C#)

```csharp
// Maliev.Intranet.Client/Services/ThumbnailGenerationService.cs
namespace Maliev.Intranet.Client.Services;

public enum ThumbnailGenerationStage
{
    Queued,
    Downloading,
    LoadingMesh,
    Normalizing,
    Rendering,
    Encoding,
    Complete,
    Failed,
    Fallback
}

public sealed record ThumbnailProgress(
    string StoragePath,
    ThumbnailGenerationStage Stage,
    int Percent,
    string? Message);

public sealed class ThumbnailGenerationService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ThumbnailGenerationService> _logger;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, ThumbnailSetDto> _cache = [];
    private readonly ConcurrentDictionary<string, Task<ThumbnailSetDto>> _inflight = [];
    private readonly EventCallbackSubscribers<ThumbnailProgress> _subscribers = new();

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
    /// Generates all 8 thumbnail views for a file. Returns cached result if available.
    /// </summary>
    public async Task<ThumbnailSetDto> GenerateAsync(
        string storagePath,
        string signedDownloadUrl,
        CancellationToken ct = default)
    {
        if (_cache.TryGetValue(storagePath, out var cached))
        {
            return cached;
        }

        return await _inflight.GetOrAdd(storagePath, async _ =>
        {
            try
            {
                await NotifyAsync(new ThumbnailProgress(storagePath, ThumbnailGenerationStage.Downloading, 10, "Downloading mesh"), ct);

                var result = await _jsRuntime.InvokeAsync<ThumbnailSetDto>(
                    "MalievGeometry.generateThumbnails",
                    signedDownloadUrl,
                    new { timeoutMs = 20000, jpegQuality = 0.85 }
                );

                _cache[storagePath] = result;
                await NotifyAsync(new ThumbnailProgress(storagePath, ThumbnailGenerationStage.Complete, 100, "Complete"), ct);
                return result;
            }
            catch (JSException ex) when (IsRecoverableError(ex))
            {
                _logger.LogWarning(ex, "WASM thumbnail generation failed for {StoragePath}, requesting server fallback", storagePath);
                await NotifyAsync(new ThumbnailProgress(storagePath, ThumbnailGenerationStage.Fallback, 0, "Falling back to server"), ct);
                return await RequestServerFallbackAsync(storagePath, signedDownloadUrl, ex.Message, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Thumbnail generation failed for {StoragePath}", storagePath);
                await NotifyAsync(new ThumbnailProgress(storagePath, ThumbnailGenerationStage.Failed, 0, ex.Message), ct);
                throw;
            }
            finally
            {
                _inflight.TryRemove(storagePath, out _);
            }
        });
    }

    public bool TryGetCached(string storagePath, out ThumbnailSetDto? set)
    {
        return _cache.TryGetValue(storagePath, out set);
    }

    public void Subscribe(EventCallback<ThumbnailProgress> callback)
    {
        _subscribers.Add(callback);
    }

    private async Task NotifyAsync(ThumbnailProgress progress, CancellationToken ct)
    {
        foreach (var callback in _subscribers.Snapshot())
        {
            await callback.InvokeAsync(progress);
        }
    }

    private static bool IsRecoverableError(JSException ex) =>
        ex.Message.Contains("OOM", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("memory", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("aborted", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase);

    private async Task<ThumbnailSetDto> RequestServerFallbackAsync(
        string storagePath, string signedUrl, string reason, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/v1/geometry/runtime/thumbnail-fallback",
            new ThumbnailFallbackRequest
            {
                StoragePath = storagePath,
                SignedUrl = signedUrl,
                Reason = reason
            },
            ct);
        response.EnsureSuccessStatusCode();
        return new ThumbnailSetDto(); // Empty - SignalR will deliver when ready
    }

    public async ValueTask DisposeAsync()
    {
        _cache.Clear();
        _inflight.Clear();
        await ValueTask.CompletedTask;
    }
}
```

### Component Integration Example

```razor
@* Maliev.Intranet.Client/Components/Project/PartListRow.razor *@
@inject ThumbnailGenerationService ThumbnailService
@implements IAsyncDisposable

<MudListItem>
    @if (_thumbnails != null && !string.IsNullOrEmpty(_thumbnails.ThumbnailSmall))
    {
        <img src="@_thumbnails.ThumbnailSmall" alt="@Part.Name" />
    }
    else if (_progress?.Stage == ThumbnailGenerationStage.Downloading)
    {
        <MudSkeleton SkeletonType="SkeletonType.Rectangle" Width="56px" Height="42px" Animation="SkeletonAnimation.Wave" />
    }
    else if (_progress?.Stage == ThumbnailGenerationStage.Rendering)
    {
        <MudProgressCircular Size="Size.Small" Value="@_progress.Percent" />
    }
    else
    {
        <MudIcon Icon="@Icons.Material.Outlined.Image" />
    }
    @Part.Name
</MudListItem>

@code {
    [Parameter] public PartDto Part { get; set; } = default!;
    private ThumbnailSetDto? _thumbnails;
    private ThumbnailProgress? _progress;
    private EventCallback<ThumbnailProgress> _progressHandler;

    protected override async Task OnInitializedAsync()
    {
        if (ThumbnailService.TryGetCached(Part.StoragePath, out var cached))
        {
            _thumbnails = cached;
            return;
        }

        _progressHandler = EventCallback.Factory.Create<ThumbnailProgress>(this, OnProgress);
        ThumbnailService.Subscribe(_progressHandler);

        _ = Task.Run(async () =>
        {
            try
            {
                _thumbnails = await ThumbnailService.GenerateAsync(
                    Part.StoragePath,
                    Part.SignedDownloadUrl);
                await InvokeAsync(StateHasChanged);
            }
            catch { /* Error already logged */ }
        });
    }

    private void OnProgress(ThumbnailProgress progress)
    {
        if (progress.StoragePath == Part.StoragePath)
        {
            _progress = progress;
            InvokeAsync(StateHasChanged);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_progressHandler.HasDelegate)
        {
            // Unsubscribe logic
        }
    }
}
```

### Cache Invalidation Strategy (Recommendation: Version-based)

**Recommended approach**: Use a `thumbnail_version` field in `PartDto`, incremented server-side when the file content changes (e.g., re-upload, migration).

```csharp
// In PartDto
public string? ThumbnailVersion { get; set; } // e.g., file content hash

// In ThumbnailGenerationService
private string GetCacheKey(string storagePath, string? version) => $"{storagePath}:{version}";

public async Task<ThumbnailSetDto> GenerateAsync(
    string storagePath, 
    string? version,
    string signedDownloadUrl,
    CancellationToken ct = default)
{
    var cacheKey = GetCacheKey(storagePath, version);
    if (_cache.TryGetValue(cacheKey, out var cached)) return cached;
    // ... rest of generation logic with cacheKey
}
```

**Why this works**:
- File content hash changes when file is re-uploaded → new cache key → fresh generation
- Storage path stays stable across migrations → no orphan cache entries
- Simple to implement (server already computes content hash for deduplication)
- No need for explicit cache busting API

**Alternative considered**: Storage path + signed URL timestamp. Rejected because timestamp changes on every signed URL refresh, causing unnecessary regeneration.

**Alternative considered**: localStorage with TTL (24h). Rejected because doesn't handle file content changes correctly.

---

## Progress Reporting

Components display 3 states via `ThumbnailProgress`:

| Stage | UI Element |
|-------|------------|
| `Queued` | `<MudSkeleton>` rectangle with wave animation |
| `Downloading` (0-30%) | `<MudSkeleton>` + progress % overlay |
| `LoadingMesh` (30-50%) | `<MudSkeleton>` + "Loading mesh..." text |
| `Normalizing` (50-60%) | `<MudSkeleton>` + "Normalizing..." text |
| `Rendering` (60-90%) | `<MudProgressCircular>` with % |
| `Encoding` (90-100%) | `<MudProgressCircular>` with "Encoding..." text |
| `Complete` (100%) | `<img>` with final DataURL |
| `Fallback` | `<MudSkeleton>` + "Server processing..." text |
| `Failed` | `<MudIcon>` placeholder + tooltip with error |

**Broadcasting**: Progress events are local-only (no SignalR). Only the completed `ThumbnailSet` is broadcast to other tabs via existing `FileAnalysisCompleted` SignalR message.

---

## Fallback Endpoint

```csharp
// Maliev.Intranet.Bff/Controllers/GeometryController.cs
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
    await _publishEndpoint.Publish(new ThumbnailFallbackEvent
    {
        StoragePath = request.StoragePath,
        SignedUrl = request.SignedUrl,
        Reason = request.Reason,
        RequestedBy = User.Identity?.Name
    }, ct);

    return Accepted();
}
```

The existing `PreviewImagesGeneratedConsumer` handles the response (unchanged) and delivers via SignalR.

---

## Error Handling

| Error | Detection | Action |
|-------|-----------|--------|
| OOM | `JSException` with "memory" / "OOM" | Trigger fallback, log with file size |
| Unsupported format | `SceneLoader` throws | Trigger fallback with `reason: "format"` |
| Timeout | `AbortController` fires after 20s/8s | Trigger fallback with `reason: "timeout"` |
| WebGL unavailable | Startup check via `WebGL2RenderingContext` | Skip WASM entirely, always use fallback |
| Network failure | `fetch` throws | Retry once with exponential backoff, then fallback |
| Invalid mesh (empty, corrupted) | `meshes.length === 0` | Trigger fallback with `reason: "invalid_mesh"` |

---

## Testing Strategy

### Unit Tests

- `BabylonThumbnailGenerator_GeneratesAll8Views` (mocked BabylonJS)
- `ThumbnailGenerationService_CachesResults` (in-memory cache)
- `ThumbnailGenerationService_TriggersFallback_OnOOM` (mocked JS exception)
- `ThumbnailGenerationService_NotifiesSubscribers_OnProgress`

### Integration Tests (Testcontainers)

- `GeometryController_ThumbnailFallback_PublishesToRabbitMQ` (verify event published)
- `ThumbnailFallbackConsumer_TriggersGeometryService` (verify downstream call)

### Component Tests (bUnit)

- `PartListRow_ShowsSkeleton_BeforeGeneration`
- `PartListRow_ShowsImage_AfterGeneration`
- `PartListRow_ShowsFallback_OnFallbackEvent`

### E2E Tests (Playwright)

- `UploadSTL_File_ShowsThumbnailsWithin2Seconds` (no server roundtrip)
- `UploadLargeSTL_TriggersFallback_ShowsServerThumbnails` (>50MB mobile)
- `UploadCADFile_TriggersFallback_ShowsServerThumbnails` (STEP/IGES)

### Performance Benchmarks

- Desktop Chrome: <2s for 1MB STL
- Desktop Chrome: <5s for 10MB STL
- Desktop Chrome: <20s for 50MB STL
- Mobile Safari: <8s for 1MB STL
- Bundle size: <2MB gzipped (BabylonJS + loaders + WASM)

---

## Migration Phases

### Phase 1: Client Generation Only (No Deprecation)

- Add `BabylonThumbnailGenerator` + `ThumbnailGenerationService`
- Update consuming components to subscribe to service
- Server still generates thumbnails (duplicate work, but safe)
- **Outcome**: Latency reduction, cost still high (duplicate work)

### Phase 2: Add Fallback Path

- Add `GeometryController.ThumbnailFallback` endpoint
- Add `ThumbnailFallbackConsumer` to publish legacy event
- Components handle "fallback" state with skeleton + "Server processing..." text
- **Outcome**: Full functionality, fallback works, can deploy safely

### Phase 3: Server Deprecation

- Mark `SmallThumbnailReadyConsumer` as deprecated (keep for fallback)
- Update `GeometryRuntimeFallbackProvider` manifest: `serverPreviewImages: false` for browser-primary
- GeometryService stops generating thumbnails for browser-primary uploads
- **Outcome**: Cost reduction realized, server only handles fallback

### Phase 4: Cleanup

- Remove `SmallThumbnailReadyConsumer` entirely
- Remove GeometryService thumbnail code (Python)
- Update all DTOs to remove server-only fields
- **Outcome**: Full offload complete, server is fallback only

**Rollback strategy**: Each phase is independently deployable. If issues arise, revert the client subscription in consuming components (1-line change per component).

---

## Bundle Size Impact

| Asset | Size (gzipped) |
|-------|----------------|
| BabylonJS Core | ~500KB |
| STL Loader | ~50KB |
| OBJ Loader | ~40KB |
| GLB Loader | ~80KB |
| 3MF Loader | ~60KB |
| Existing WASM kernel | ~200KB |
| ThumbnailGenerator.ts | ~10KB |
| **Total** | **~940KB** |

Acceptable per user confirmation ("5G world"). Consider code-splitting loaders so only the needed format's loader is loaded.

---

## Out of Scope

- DFM analysis (already offloaded, separate concern)
- GLB export (server-side, MeshLab/OpenCascade)
- DFM overlay GLBs (server-side, requires DFM analysis)
- File format conversion (STEP → STL, etc.)

---

## Open Questions

None - all questions resolved:

1. Bundle size: ✅ Acceptable (~940KB gzipped)
2. WebGL version: ✅ Use WebGL 2 for performance, fall back to WebGL 1 if unavailable
3. Progress reporting: ✅ Required, detailed per-stage progress
4. Cache invalidation: ✅ Content-hash-based versioning
5. Multi-body files: ✅ Render entire assembly, all parts combined

---

## Success Criteria

1. **Latency**: First thumbnail appears <2s after upload completes (vs current 5-15s)
2. **Cost**: 90% reduction in GeometryService CPU/GPU usage for thumbnail generation
3. **Offline**: Thumbnail generation works with no network (except file download)
4. **Parity**: All 8 views generated with same quality as server
5. **Bundle size**: <1MB gzipped total addition
6. **Mobile**: Works on iOS Safari 14+, Android Chrome 90+
7. **Fallback**: <5% of uploads require server fallback

---

## References

- `Maliev.Intranet.Bff/Services/GeometryRuntimeFallbackProvider.cs` (existing WASM packaging)
- `Maliev.Intranet.Bff/Clients/GeometryServiceClient.cs` (existing DFM client)
- `Maliev.Intranet.Bff/Consumers/PreviewImagesGeneratedConsumer.cs` (existing consumer to deprecate)
- BabylonJS Documentation: https://doc.babylonjs.com/
- BabylonJS RenderTargetTexture: https://doc.babylonjs.com/features/featuresDeepDive/materials/using/renderTargetTexture
