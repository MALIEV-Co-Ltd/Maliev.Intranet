# Fix DFM Inconsistency + BabylonJS Black Flash

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix two bugs: (1) uploading two identical STEP files produces inconsistent DFM results, and (2) BabylonJS viewer flashes a black loading screen instead of showing the polkadot background.

**Architecture:** Two independent fixes across BFF, Client, and JS layers. The DFM fix addresses a SignalR race condition by persisting DFM reports in the BFF cache and resolving them client-side based on the selected manufacturing process. The viewer fix enables WebGL alpha transparency and hides the canvas until the first successful render.

**Tech Stack:** ASP.NET Core BFF (MassTransit, SignalR, IMemoryCache), Blazor WASM client, BabylonJS (WebGL)

---

## Root Cause Analysis

### Bug 1: DFM Inconsistency (3 root causes)

| # | Root Cause | Location | Impact |
|---|---|---|---|
| 1A | `DfmAnalysisReadyConsumer` only pushes via SignalR — **never updates the BFF cache** | `DfmAnalysisReadyConsumer.cs:53-60` | Catch-up poll (`FetchCurrentStatusAsync`) cannot recover DFM data if SignalR event was missed |
| 1B | Client SignalR handler selects a **single** report based on `ProcessCode` and **discards the rest** | `ProjectNew.razor.cs:234-238` | If no process selected, defaults to FDM which may be null; other reports are lost |
| 1C | DFM report is **never re-evaluated** when the user changes the manufacturing process | `ProjectNew.razor.cs:486-527` (`OnPartChanged`) | Wrong or null DFM report persists even after correct process is selected |

**Event flow timeline:**
```
File Uploaded → GeometryService processes
  → FileAnalyzedEvent → FileAnalyzedConsumer (caches + SignalR "GlbReady")
  → DfmAnalysisReadyEvent → DfmAnalysisReadyConsumer (SignalR only, NO cache)
                                 ↑ If client missed SignalR → DFM data lost forever
```

### Bug 2: BabylonJS Black Flash

| # | Root Cause | Location |
|---|---|---|
| 2A | `new BABYLON.Engine(canvas, true)` — second param is `antialias`, NOT `alpha`. WebGL context created **without alpha support** | `babylon-viewer.js:187` |
| 2B | `scene.clearColor = Color4(0,0,0,0)` is ignored because WebGL context has no alpha — renders as **opaque black** | `babylon-viewer.js:192` |
| 2C | `engine.loadingScreen = null` is set **after** engine creation — one black frame already rendered | `babylon-viewer.js:188` |
| 2D | `engine.resize()` at line 190 forces buffer clear to black before scene is ready | `babylon-viewer.js:190` |

**The polkadot background** (`PartDetailCard.razor:112-117` `.pdc-polkadot`) sits behind the canvas. With no WebGL alpha, the canvas composites as opaque black over the polkadot.

---

## File Structure

| File | Action | Responsibility |
|---|---|---|
| `Maliev.Intranet.Bff/Services/IFileAnalysisStatusService.cs` | Modify | Add `SetDfmReportsAsync` method |
| `Maliev.Intranet.Bff/Services/FileAnalysisStatusService.cs` | Modify | Implement `SetDfmReportsAsync` |
| `Maliev.Intranet.Bff/Consumers/DfmAnalysisReadyConsumer.cs` | Modify | Inject cache service, store DFM reports |
| `Maliev.Intranet.Client/Components/Project/PartViewModel.cs` | Modify | Add per-process DFM report storage + resolver |
| `Maliev.Intranet.Client/Pages/ProjectNew.razor.cs` | Modify | Store full DFM payload, resolve on process change |
| `Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js` | Modify | Enable WebGL alpha, hide canvas until first render |
| `Maliev.Intranet.Tests/Bff/FileAnalysisStatusServiceTests.cs` | Create | Unit tests for `SetDfmReportsAsync` |
| `Maliev.Intranet.Tests/Bff/DfmAnalysisReadyConsumerTests.cs` | Create | Unit tests for cache update behavior |

---

## Task 1: Add `SetDfmReportsAsync` to BFF Cache Service

**Files:**
- Modify: `Maliev.Intranet.Bff/Services/IFileAnalysisStatusService.cs`
- Modify: `Maliev.Intranet.Bff/Services/FileAnalysisStatusService.cs`
- Create: `Maliev.Intranet.Tests/Bff/FileAnalysisStatusServiceTests.cs`

This new method updates **only** the `DfmReport` field while preserving all other fields (status, dimensions, thumbnails, etc.). This is critical because `DfmAnalysisReadyEvent` arrives *after* `FileAnalyzedEvent`, and we must not overwrite the completed status or GLB paths.

- [ ] **Step 1: Add method to interface**

In `IFileAnalysisStatusService.cs`, add after the `SetAnalysisFailedAsync` method (line 45):

```csharp
/// <summary>
/// Updates only the DFM report data for a cached file analysis entry,
/// preserving all other fields (status, dimensions, thumbnails, etc.).
/// Called by <see cref="Consumers.DfmAnalysisReadyConsumer"/> when
/// <c>DfmAnalysisReadyEvent</c> arrives after the initial file analysis.
/// No-op if no existing cache entry is found.
/// </summary>
Task SetDfmReportsAsync(string uploadId, object dfmReports, CancellationToken cancellationToken = default);
```

- [ ] **Step 2: Implement in `FileAnalysisStatusService.cs`**

Add after `SetAnalysisFailedAsync` (after line 182):

```csharp
/// <inheritdoc />
public Task SetDfmReportsAsync(string uploadId, object dfmReports, CancellationToken cancellationToken = default)
{
    var existing = Get(uploadId);
    if (existing == null)
    {
        _logger.LogDebug("SetDfmReportsAsync: no entry found for key={Key}, skipping", uploadId);
        return Task.CompletedTask;
    }

    var status = new FileAnalysisStatusDto
    {
        UploadId = existing.UploadId,
        Status = existing.Status,
        Dimensions = existing.Dimensions,
        IsManifold = existing.IsManifold,
        ThumbnailUrl = existing.ThumbnailUrl,
        HiResThumbnailUrl = existing.HiResThumbnailUrl,
        PreviewUrls = existing.PreviewUrls,
        GlbStoragePath = existing.GlbStoragePath,
        PreviewProcessingStatus = existing.PreviewProcessingStatus,
        ErrorCode = existing.ErrorCode,
        ProcessedAt = existing.ProcessedAt,
        DfmReport = dfmReports,
    };
    Set(uploadId, status);
    _logger.LogInformation("SetDfmReportsAsync: updated DFM reports for key={Key}", uploadId);
    return Task.CompletedTask;
}
```

- [ ] **Step 3: Write unit tests**

Create `Maliev.Intranet.Tests/Bff/FileAnalysisStatusServiceTests.cs`:

```csharp
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.Intranet.Tests.Bff;

public class FileAnalysisStatusServiceTests
{
    private readonly FileAnalysisStatusService _sut;

    public FileAnalysisStatusServiceTests()
    {
        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
        var logger = Mock.Of<ILogger<FileAnalysisStatusService>>();
        _sut = new FileAnalysisStatusService(cache, logger);
    }

    [Fact]
    public async Task SetDfmReportsAsync_WithExistingEntry_UpdatesOnlyDfmReport()
    {
        var key = "uploads/test/file.step";
        await _sut.SetAnalysisCompletedAsync(key, "glb/path.glb", null);

        var dfmReports = new { FdmReport = "fdm-data", SlaReport = "sla-data", CncReport = "cnc-data" };
        await _sut.SetDfmReportsAsync(key, dfmReports);

        var status = await _sut.GetStatusAsync(key);
        Assert.NotNull(status);
        Assert.Equal(FileAnalysisStatus.Completed, status.Status);
        Assert.Equal("glb/path.glb", status.GlbStoragePath);
        Assert.NotNull(status.DfmReport);
    }

    [Fact]
    public async Task SetDfmReportsAsync_WithNoExistingEntry_IsNoOp()
    {
        var key = "uploads/nonexistent/file.step";
        var dfmReports = new { FdmReport = "fdm-data" };

        await _sut.SetDfmReportsAsync(key, dfmReports);

        var status = await _sut.GetStatusAsync(key);
        Assert.Null(status);
    }

    [Fact]
    public async Task SetDfmReportsAsync_DoesNotOverwriteDimensions()
    {
        var key = "uploads/test/file.step";
        var dims = new FileAnalysisDimensionsDto { X = 10, Y = 20, Z = 30, VolumeMm3 = 1000 };
        await _sut.SetDimensionsAsync(key, dims, true);
        await _sut.SetAnalysisCompletedAsync(key, "glb/path.glb", null);

        await _sut.SetDfmReportsAsync(key, new { FdmReport = "data" });

        var status = await _sut.GetStatusAsync(key);
        Assert.NotNull(status);
        Assert.NotNull(status.Dimensions);
        Assert.Equal(10.0, status.Dimensions.X);
        Assert.Equal(20.0, status.Dimensions.Y);
        Assert.Equal(30.0, status.Dimensions.Z);
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test Maliev.Intranet.Tests --filter "FullyQualifiedName~FileAnalysisStatusServiceTests" -v n`
Expected: All 3 tests PASS

- [ ] **Step 5: Commit**

```bash
git add Maliev.Intranet.Bff/Services/IFileAnalysisStatusService.cs Maliev.Intranet.Bff/Services/FileAnalysisStatusService.cs Maliev.Intranet.Tests/Bff/FileAnalysisStatusServiceTests.cs
git commit -m "feat(bff): add SetDfmReportsAsync to persist DFM reports in analysis cache"
```

---

## Task 2: Update `DfmAnalysisReadyConsumer` to Cache DFM Reports

**Files:**
- Modify: `Maliev.Intranet.Bff/Consumers/DfmAnalysisReadyConsumer.cs`
- Create: `Maliev.Intranet.Tests/Bff/DfmAnalysisReadyConsumerTests.cs`

The consumer currently only pushes via SignalR. After this change, it also persists the DFM reports in the BFF cache so that the client's catch-up poll (`FetchCurrentStatusAsync`) can recover them if the SignalR event was missed.

- [ ] **Step 1: Modify `DfmAnalysisReadyConsumer.cs`**

Replace the entire file:

```csharp
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Services;
using Maliev.MessagingContracts.Contracts.Geometry;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Consumers;

/// <summary>
/// MassTransit consumer that handles <see cref="DfmAnalysisReadyEvent"/> messages published by GeometryService.
/// Persists DFM reports in the analysis cache and broadcasts results via SignalR.
/// </summary>
public class DfmAnalysisReadyConsumer : IConsumer<DfmAnalysisReadyEvent>
{
    private readonly IHubContext<NotificationHub> _hub;
    private readonly IFileAnalysisStatusService _analysisStatusService;
    private readonly ILogger<DfmAnalysisReadyConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DfmAnalysisReadyConsumer"/>.
    /// </summary>
    public DfmAnalysisReadyConsumer(
        IHubContext<NotificationHub> hub,
        IFileAnalysisStatusService analysisStatusService,
        ILogger<DfmAnalysisReadyConsumer> logger)
    {
        _hub = hub;
        _analysisStatusService = analysisStatusService;
        _logger = logger;
    }

    /// <summary>
    /// Processes an incoming <see cref="DfmAnalysisReadyEvent"/>: persists DFM reports in the
    /// BFF cache and pushes results to connected Blazor clients via SignalR.
    /// </summary>
    /// <param name="context">The MassTransit consume context.</param>
    public async Task Consume(ConsumeContext<DfmAnalysisReadyEvent> context)
    {
        if (context.Message?.Payload == null)
        {
            _logger.LogWarning("DfmAnalysisReadyConsumer: received null message or payload");
            return;
        }

        var payload = context.Message.Payload;
        _logger.LogInformation(
            "DfmAnalysisReadyConsumer: received event for file {FileId}, storagePath={StoragePath}",
            payload.FileId, payload.StoragePath);

        if (string.IsNullOrEmpty(payload.StoragePath))
        {
            _logger.LogWarning(
                "DfmAnalysisReadyConsumer: missing StoragePath for FileId={FileId} — skipping",
                payload.FileId);
            return;
        }

        var dfmReports = new
        {
            FdmReport = payload.FdmReport,
            SlaReport = payload.SlaReport,
            CncReport = payload.CncReport,
        };

        await _analysisStatusService.SetDfmReportsAsync(
            payload.StoragePath, dfmReports, context.CancellationToken);

        _logger.LogInformation(
            "DfmAnalysisReadyConsumer: cached DFM reports for storagePath={StoragePath}",
            payload.StoragePath);

        await _hub.Clients.Group($"file:{payload.StoragePath}").SendAsync(
            "DfmAnalysisReady",
            new DfmAnalysisReadyPayload(
                StoragePath: payload.StoragePath,
                FdmReport: payload.FdmReport,
                SlaReport: payload.SlaReport,
                CncReport: payload.CncReport),
            context.CancellationToken);
    }
}
```

- [ ] **Step 2: Write consumer unit test**

Create `Maliev.Intranet.Tests/Bff/DfmAnalysisReadyConsumerTests.cs`:

```csharp
using Maliev.Intranet.Bff.Consumers;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared.Dtos;
using Maliev.MessagingContracts.Contracts.Geometry;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.Intranet.Tests.Bff;

public class DfmAnalysisReadyConsumerTests
{
    private readonly Mock<IHubContext<NotificationHub>> _hubMock;
    private readonly FileAnalysisStatusService _analysisStatusService;
    private readonly Mock<ILogger<DfmAnalysisReadyConsumer>> _loggerMock;

    public DfmAnalysisReadyConsumerTests()
    {
        _hubMock = new Mock<IHubContext<NotificationHub>>();
        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
        var cacheLogger = Mock.Of<ILogger<FileAnalysisStatusService>>();
        _analysisStatusService = new FileAnalysisStatusService(cache, cacheLogger);
        _loggerMock = new Mock<ILogger<DfmAnalysisReadyConsumer>>();
    }

    [Fact]
    public async Task Consume_WithValidEvent_CachesDfmReports()
    {
        var storagePath = "uploads/test/part.step";
        await _analysisStatusService.SetAnalysisCompletedAsync(storagePath, "glb/path.glb");

        var consumer = new DfmAnalysisReadyConsumer(
            _hubMock.Object, _analysisStatusService, _loggerMock.Object);

        var payload = new DfmAnalysisReadyEventPayload(
            FileId: Guid.NewGuid(),
            StoragePath: storagePath,
            FdmReport: null,
            SlaReport: null,
            CncReport: null,
            OccurredAtUtc: DateTimeOffset.UtcNow);

        var message = new DfmAnalysisReadyEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "DfmAnalysisReadyEvent",
            MessageType: MessageType.Event,
            MessageVersion: "1.0",
            PublishedBy: "GeometryService",
            ConsumedBy: ["Maliev.Intranet.Bff"],
            CorrelationId: Guid.NewGuid(),
            CausationId: Guid.NewGuid(),
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: payload);

        var consumeContext = Mock.Of<ConsumeContext<DfmAnalysisReadyEvent>>(c =>
            c.Message == message && c.CancellationToken == CancellationToken.None);

        await consumer.Consume(consumeContext);

        var status = await _analysisStatusService.GetStatusAsync(storagePath);
        Assert.NotNull(status);
        Assert.NotNull(status.DfmReport);
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test Maliev.Intranet.Tests --filter "FullyQualifiedName~DfmAnalysisReadyConsumerTests" -v n`
Expected: PASS

- [ ] **Step 4: Build the full solution**

Run: `dotnet build Maliev.Intranet.slnx`
Expected: BUILD SUCCEEDED (no warnings)

- [ ] **Step 5: Commit**

```bash
git add Maliev.Intranet.Bff/Consumers/DfmAnalysisReadyConsumer.cs Maliev.Intranet.Tests/Bff/DfmAnalysisReadyConsumerTests.cs
git commit -m "fix(bff): DfmAnalysisReadyConsumer now persists DFM reports in analysis cache"
```

---

## Task 3: Store Full DFM Payload in `PartViewModel` + Resolve by Process

**Files:**
- Modify: `Maliev.Intranet.Client/Components/Project/PartViewModel.cs`

Add per-process DFM report properties so the full payload is retained. Add a resolver method that selects the correct report based on `ProcessCode`.

- [ ] **Step 1: Add DFM properties and resolver method**

In `PartViewModel.cs`, after the existing `DfmReport` property (line 108), add:

```csharp
/// <summary>FDM-specific DFM analysis report from <c>DfmAnalysisReadyEvent</c>.</summary>
public object? FdmDfmReport { get; set; }

/// <summary>SLA/DLP-specific DFM analysis report from <c>DfmAnalysisReadyEvent</c>.</summary>
public object? SlaDfmReport { get; set; }

/// <summary>CNC-specific DFM analysis report from <c>DfmAnalysisReadyEvent</c>.</summary>
public object? CncDfmReport { get; set; }

/// <summary>
/// Resolves <see cref="DfmReport"/> from the per-process DFM report properties
/// based on the currently selected <see cref="ProcessCode"/>.
/// Call this after setting any of FdmDfmReport/SlaDfmReport/CncDfmReport,
/// or after ProcessCode changes.
/// </summary>
public void ResolveDfmReport()
{
    DfmReport = ProcessCode?.ToUpperInvariant() switch
    {
        "SLA" or "DLP" => SlaDfmReport,
        "CNC" => CncDfmReport,
        _ => FdmDfmReport,
    };
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build Maliev.Intranet.slnx`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: Commit**

```bash
git add Maliev.Intranet.Client/Components/Project/PartViewModel.cs
git commit -m "feat(client): add per-process DFM report storage and resolver to PartViewModel"
```

---

## Task 4: Update SignalR Handler + Catch-Up Poll + Process Change

**Files:**
- Modify: `Maliev.Intranet.Client/Pages/ProjectNew.razor.cs`

Three changes in this file:
1. **SignalR `DfmAnalysisReady` handler** — store ALL three reports, call `ResolveDfmReport()`
2. **`FetchCurrentStatusAsync`** — detect multi-report cache payload and populate all three properties
3. **`OnPartChanged`** — re-resolve DFM report when process changes

- [ ] **Step 1: Update the `DfmAnalysisReady` SignalR handler**

In `ProjectNew.razor.cs`, replace lines 229-242 with:

```csharp
_hubConnection.On<SignalRDfmAnalysisPayload>("DfmAnalysisReady", async payload =>
{
    var part = _parts.FirstOrDefault(p => p.StoragePath == payload.StoragePath);
    if (part == null) return;

    part.FdmDfmReport = payload.FdmReport;
    part.SlaDfmReport = payload.SlaReport;
    part.CncDfmReport = payload.CncReport;
    part.ResolveDfmReport();

    await InvokeAsync(StateHasChanged);
});
```

- [ ] **Step 2: Update `FetchCurrentStatusAsync` to handle multi-report cache payload**

In `FetchCurrentStatusAsync`, replace line 409 (`part.DfmReport = status.DfmReport;`) with:

```csharp
if (status.DfmReport is JsonElement je && je.ValueKind == JsonValueKind.Object
    && je.TryGetProperty("FdmReport", out _))
{
    var dfmPayload = JsonSerializer.Deserialize<SignalRDfmAnalysisPayload>(je.GetRawText());
    part.FdmDfmReport = dfmPayload?.FdmReport;
    part.SlaDfmReport = dfmPayload?.SlaReport;
    part.CncDfmReport = dfmPayload?.CncReport;
    part.ResolveDfmReport();
}
else
{
    part.DfmReport = status.DfmReport;
}
```

Also ensure `using System.Text.Json;` is present at the top of the file (it already exists at line 343 usage).

- [ ] **Step 3: Re-resolve DFM on process change**

In `OnPartChanged`, add DFM re-resolution. After the `TriggerAutoSave();` call on line 525, add before it:

```csharp
if (part.FdmDfmReport != null || part.SlaDfmReport != null || part.CncDfmReport != null)
    part.ResolveDfmReport();
```

The final `OnPartChanged` method should look like:

```csharp
private async Task OnPartChanged(PartViewModel part)
{
    if (part.ProcessId.HasValue && !string.IsNullOrEmpty(part.ProcessCode)
        && part.AvailableMaterials.Count == 0)
    {
        // ... existing catalog loading code unchanged ...
    }
    else if (!part.ProcessId.HasValue || string.IsNullOrEmpty(part.ProcessCode))
    {
        part.AvailableMaterials = [];
        part.AvailableFinishes = [];
        part.AvailableTolerances = [];
    }

    if (part.FdmDfmReport != null || part.SlaDfmReport != null || part.CncDfmReport != null)
        part.ResolveDfmReport();

    TriggerAutoSave();
    TriggerPricingAsync(part);
}
```

- [ ] **Step 4: Add `using System.Text.Json` import**

Verify the file already has `using System.Text.Json;` (it's used on line 343 for `JsonSerializer.Deserialize<BffUploadResponse>`). If not present, add it.

- [ ] **Step 5: Build to verify**

Run: `dotnet build Maliev.Intranet.slnx`
Expected: BUILD SUCCEEDED

- [ ] **Step 6: Commit**

```bash
git add Maliev.Intranet.Client/Pages/ProjectNew.razor.cs
git commit -m "fix(client): store full DFM payload, resolve on process change, handle cache catch-up"
```

---

## Task 5: Fix BabylonJS Black Flash — Enable WebGL Alpha Transparency

**Files:**
- Modify: `Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js`

The fix has two parts:
1. Enable `alpha: true` on the WebGL context so `clearColor` alpha is respected
2. Hide the canvas until the first successful render to prevent any visual artifact

- [ ] **Step 1: Enable WebGL alpha in Engine constructor**

In `babylon-viewer.js`, replace line 187:

```javascript
const engine = new BABYLON.Engine(canvas, true);
```

with:

```javascript
const engine = new BABYLON.Engine(canvas, true, { premultipliedAlpha: false, alpha: true });
```

This tells WebGL to create a context with alpha support, allowing `scene.clearColor = Color4(0,0,0,0)` to produce a transparent background that composites correctly over the polkadot parent.

- [ ] **Step 2: Hide canvas until first successful render**

In `babylon-viewer.js`, after line 176 (`canvas.style.background = 'transparent';`), add:

```javascript
canvas.style.opacity = '0';
canvas.style.transition = 'opacity 0.3s ease-in';
```

Then, inside the `SceneLoader.Append` success callback (line 219), at the beginning of the callback body (after `modelLoadState[canvasId] = 'loaded';`), add:

```javascript
// Fade in the canvas now that the model has rendered
requestAnimationFrame(() => { canvas.style.opacity = '1'; });
```

Also in the error callback (around line 411), add:

```javascript
canvas.style.opacity = '1';
```

This ensures:
- Canvas is invisible during engine init, CDN script loading, and model loading
- Canvas fades in smoothly (0.3s) once the first frame with the model is ready
- Polkadot background is always visible through the transparent canvas
- If the model fails to load, the canvas still becomes visible (for the error overlay)

- [ ] **Step 3: Verify `engine.loadingScreen = null` is still present**

Confirm line 188 still has `engine.loadingScreen = null;`. This should remain to prevent BabylonJS's default loading UI from appearing.

- [ ] **Step 4: Build to verify no breakage**

Run: `dotnet build Maliev.Intranet.slnx`
Expected: BUILD SUCCEEDED

- [ ] **Step 5: Commit**

```bash
git add Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js
git commit -m "fix(viewer): enable WebGL alpha transparency and hide canvas until first render"
```

---

## Task 6: Full Build + Manual Verification Checklist

**Files:** None (verification only)

- [ ] **Step 1: Full solution build**

Run: `dotnet build Maliev.Intranet.slnx`
Expected: BUILD SUCCEEDED with zero warnings

- [ ] **Step 2: Run all tests**

Run: `dotnet test Maliev.Intranet.Tests -v n`
Expected: All tests PASS

- [ ] **Step 3: Manual verification checklist**

Run the BFF and client, then verify:

**DFM Consistency:**
- [ ] Upload two identical STEP files simultaneously
- [ ] Both files should eventually show identical DFM analysis results
- [ ] Select a process (e.g., CNC) — DFM report should update to CNC-specific issues
- [ ] Change process to FDM — DFM report should update to FDM-specific issues
- [ ] Refresh the page (draft restore) — DFM results should persist via catch-up poll

**BabylonJS Viewer:**
- [ ] Upload a file and wait for 3D viewer to appear
- [ ] Polkadot background must be visible at ALL times — no black flash
- [ ] Model must render ON TOP of the polkadot background (transparent canvas)
- [ ] The fade-in transition should be smooth (no jarring appearance)
- [ ] If model fails to load, the error state still shows over polkadot background

- [ ] **Step 4: Final commit if any fixes needed**

```bash
git add -A
git commit -m "chore: address review findings from DFM + viewer fix verification"
```

---

## Self-Review

### Spec Coverage
- ✅ **DFM inconsistency fix**: All 3 root causes addressed (1A: cache update, 1B: full payload storage, 1C: re-evaluation on process change)
- ✅ **Black flash fix**: Both WebGL alpha + canvas hiding addressed
- ✅ **Polkadot always visible**: Canvas opacity management ensures polkadot shows through

### Placeholder Scan
- No TBD, TODO, or "implement later" patterns
- All code blocks contain complete implementation
- All test files contain complete test methods

### Type Consistency
- `SetDfmReportsAsync(string uploadId, object dfmReports, CancellationToken)` — defined in interface, implemented in service, called from consumer with `new { FdmReport, SlaReport, CncReport }` anonymous type
- `PartViewModel.ResolveDfmReport()` — defined in PartViewModel, called from SignalR handler, catch-up poll, and OnPartChanged
- `SignalRDfmAnalysisPayload` — used consistently in SignalR handler and catch-up poll deserialization
- `JsonElement` — used in catch-up poll for JSON shape detection (from `System.Text.Json`)

### Edge Cases Considered
- **DFM event arrives before cache entry exists**: `SetDfmReportsAsync` returns early (no-op) if no existing entry
- **FileAnalyzedEvent arrives after DfmAnalysisReadyEvent**: `FileAnalyzedConsumer` uses `payload.DfmReport ?? existing?.DfmReport` which preserves the multi-report from DfmAnalysisReadyConsumer
- **ProcessCode is null**: `ResolveDfmReport` falls to `_` case → FdmReport (same as before)
- **Model load failure**: Canvas still becomes visible for error display
