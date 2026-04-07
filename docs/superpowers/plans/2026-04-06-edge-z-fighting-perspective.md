# Edge Z-Fighting Fix (Perspective Mode) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate flashing/Z-fighting on mesh edges when edge rendering is enabled in perspective camera mode.

**Architecture:** BabylonJS `EdgesRenderer` uses WebGL `gl.polygonOffset(-zOffset, -zOffsetUnits)`. The current code sets a constant `zOffset = 10` and never sets `zOffsetUnits` (defaults to 0). Two independent failure modes cause the flashing: (1) on faces viewed head-on, the slope-dependent factor contributes nothing — only `zOffsetUnits` saves it, but it is zero; (2) at large camera distances in perspective mode, the non-linear depth buffer compresses the effective polygon offset, making a fixed factor insufficient. The fix introduces a dynamic `zOffset` that scales with `cam.radius / fitRadius`, and sets `zOffsetUnits = 4096` to anchor coverage on flat-facing polygons.

**Tech Stack:** BabylonJS 9.1.0 (EdgesRenderer accessed via `mesh._edgesRenderer`), vanilla JS module.

---

## Background: How the depth bias works

WebGL polygon offset: `depth_adjustment = factor × DZ + units × r`  
- `DZ` = maximum depth slope of the polygon in NDC  
- `r` = minimum resolvable depth value (`≈ 6×10⁻⁸` for 24-bit depth buffer)  
- BabylonJS `EdgesRenderer` applies this as `gl.polygonOffset(-zOffset, -zOffsetUnits)`, so **positive values push the edge toward the camera** (opposite of raw WebGL sign convention).

**Why `zOffsetUnits = 0` causes flashing on flat faces:**  
When a face is viewed exactly head-on (camera looking straight at the surface), `DZ = 0`, so `factor × DZ = 0` regardless of the factor value. Zero total offset → the edge and mesh surface fight for the same depth → flashing.

**Why a fixed `zOffset = 10` causes flashing when zoomed out:**  
In perspective projection, depth precision degrades non-linearly with distance. The same world-space gap between edge and surface maps to a smaller NDC difference at greater distances, so a fixed factor provides less effective separation.

---

## Files

- Modify: `Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js`
  - Add helper `getEdgesZOffset(canvasId) → number`
  - Update `toggleEdges` to use helper + set `zOffsetUnits`
  - Update `_attachEdgeZoomObserver` to dynamically recalculate both `zOffset` and `zOffsetUnits` per camera move

---

### Task 1: Add `getEdgesZOffset` helper

**Files:**
- Modify: `Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js` — insert after `getEdgesWidth` (~line 960)

**Context:** `getEdgesWidth` already exists right before `toggleEdges`. Place `getEdgesZOffset` immediately after it (before `export function toggleEdges`).

- [ ] **Step 1: Insert the helper function**

  Add this block immediately after the closing `}` of `getEdgesWidth`:

  ```javascript
  /**
   * Returns the polygon offset factor for edge rendering.
   * In orthographic mode a minimal value suffices (depth is linear).
   * In perspective mode the factor scales with sqrt(radius / fitRadius) so that
   * the non-linear depth compression at larger distances is compensated.
   * Using sqrt avoids over-correcting at moderate zoom-out levels.
   */
  function getEdgesZOffset(canvasId) {
      if (cameraProjection[canvasId] === 'orthographic') return 1;
      const cam  = mainCameras[canvasId];
      const fitR = fitRadiusMap[canvasId];
      if (!cam || !fitR) return 15;
      return Math.max(15, Math.round(15 * Math.sqrt(cam.radius / fitR)));
  }
  ```

- [ ] **Step 2: Build to confirm no syntax errors**

  ```bash
  cd B:/maliev/Maliev.Intranet
  dotnet build Maliev.Intranet.Client/Maliev.Intranet.Client.csproj --no-restore -v quiet
  ```

  Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

  ```bash
  git add Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js
  git commit -m "feat(viewer): add getEdgesZOffset helper for dynamic perspective depth bias"
  ```

---

### Task 2: Apply dynamic zOffset + zOffsetUnits in `toggleEdges`

**Files:**
- Modify: `Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js` — `toggleEdges` function (~line 962)

**Context:** `toggleEdges` currently has:
```javascript
const zOffset = cameraProjection[canvasId] === 'perspective' ? 10 : 1;
// ...
mesh.enableEdgesRendering(0.98, false, zOffset);
mesh.edgesWidth = width;
mesh.edgesColor = edgeColor;
```

`enableEdgesRendering` sets the initial `zOffset` on the renderer at creation time.  
After enabling, `mesh._edgesRenderer` is available and its `zOffsetUnits` can be set.  
`zOffsetUnits = 4096` gives ~0.00025 NDC units of constant depth bias (invisible but resolves flat-face Z-fighting).

- [ ] **Step 1: Replace the static `zOffset` constant with the helper call and add `zOffsetUnits`**

  Replace this block inside `toggleEdges`:
  ```javascript
      // zOffset pushes edges in front of the mesh to prevent z-fighting.
      // Perspective needs higher offset (10) because depth varies across the view.
      // Orthographic uses minimal offset (1) since depth is consistent.
      const zOffset = cameraProjection[canvasId] === 'perspective' ? 10 : 1;
      scene.meshes.forEach(mesh => {
          // Skip system meshes: ground grid, axis gizmo parts, bounding box lines
          if (mesh.name === '__grid__' || mesh.name.startsWith('__axis') || mesh.name.startsWith('bbox_')) return;
          if (enabled) {
              try {
                  mesh.disableEdgesRendering();
                  // Tighter epsilon catches chamfers and shallow draft angles on CAD parts.
                  // zOffset prevents z-fighting with the model surface, especially in perspective mode.
                  mesh.enableEdgesRendering(0.98, false, zOffset);
                  mesh.edgesWidth = width;
                  mesh.edgesColor = edgeColor;
              } catch (_) {}
  ```

  With:
  ```javascript
      // zOffset (factor) scales with camera distance in perspective to compensate for
      // non-linear depth precision. zOffsetUnits provides a constant depth bias that
      // prevents Z-fighting on flat faces viewed head-on (where the factor contribution is ~0).
      const zOffset     = getEdgesZOffset(canvasId);
      const zOffsetUnits = cameraProjection[canvasId] === 'orthographic' ? 0 : 4096;
      scene.meshes.forEach(mesh => {
          // Skip system meshes: ground grid, axis gizmo parts, bounding box lines
          if (mesh.name === '__grid__' || mesh.name.startsWith('__axis') || mesh.name.startsWith('bbox_')) return;
          if (enabled) {
              try {
                  mesh.disableEdgesRendering();
                  // Tighter epsilon catches chamfers and shallow draft angles on CAD parts.
                  mesh.enableEdgesRendering(0.98, false, zOffset);
                  mesh.edgesWidth = width;
                  mesh.edgesColor = edgeColor;
                  if (mesh._edgesRenderer && 'zOffsetUnits' in mesh._edgesRenderer) {
                      mesh._edgesRenderer.zOffsetUnits = zOffsetUnits;
                  }
              } catch (_) {}
  ```

- [ ] **Step 2: Build**

  ```bash
  dotnet build Maliev.Intranet.Client/Maliev.Intranet.Client.csproj --no-restore -v quiet
  ```

  Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

  ```bash
  git add Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js
  git commit -m "fix(viewer): use dynamic zOffset + zOffsetUnits in toggleEdges to fix edge Z-fighting"
  ```

---

### Task 3: Update `_attachEdgeZoomObserver` to recompute both values per camera move

**Files:**
- Modify: `Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js` — `_attachEdgeZoomObserver` function (~line 1002)

**Context:** The current observer captures `const zOffset = 10` in its closure and updates `mesh._edgesRenderer.zOffset = zOffset` on every view-matrix change. It never calls `getEdgesZOffset`, so the depth bias stays at 10 regardless of how far the user zooms out. It also never sets `zOffsetUnits`.

Current code:
```javascript
function _attachEdgeZoomObserver(canvasId) {
    _detachEdgeZoomObserver(canvasId); // remove any previous
    const cam   = mainCameras[canvasId];
    const scene = scenes[canvasId];
    if (!cam || !scene) return;
    const zOffset = 10; // Higher zOffset for perspective to prevent z-fighting
    edgeZoomObservers[canvasId] = cam.onViewMatrixChangedObservable.add(() => {
        if (!edgesEnabled[canvasId] || cameraProjection[canvasId] === 'orthographic') return;
        const w = getEdgesWidth(canvasId);
        scene.meshes.forEach(mesh => {
            if (mesh.name === '__grid__' || mesh.name.startsWith('__axis') || mesh.name.startsWith('bbox_')) return;
            if (mesh._edgesRenderer) {
                mesh.edgesWidth = w;
                // Re-apply zOffset to maintain proper depth ordering
                mesh._edgesRenderer.zOffset = zOffset;
            }
        });
    });
}
```

- [ ] **Step 1: Replace the entire function**

  Replace the function body to remove the captured constant and call both helpers dynamically:

  ```javascript
  function _attachEdgeZoomObserver(canvasId) {
      _detachEdgeZoomObserver(canvasId); // remove any previous
      const cam   = mainCameras[canvasId];
      const scene = scenes[canvasId];
      if (!cam || !scene) return;
      edgeZoomObservers[canvasId] = cam.onViewMatrixChangedObservable.add(() => {
          if (!edgesEnabled[canvasId] || cameraProjection[canvasId] === 'orthographic') return;
          const w      = getEdgesWidth(canvasId);
          const zOff   = getEdgesZOffset(canvasId);
          scene.meshes.forEach(mesh => {
              if (mesh.name === '__grid__' || mesh.name.startsWith('__axis') || mesh.name.startsWith('bbox_')) return;
              if (mesh._edgesRenderer) {
                  mesh.edgesWidth = w;
                  mesh._edgesRenderer.zOffset = zOff;
                  if ('zOffsetUnits' in mesh._edgesRenderer) {
                      mesh._edgesRenderer.zOffsetUnits = 4096;
                  }
              }
          });
      });
  }
  ```

- [ ] **Step 2: Build**

  ```bash
  dotnet build Maliev.Intranet.Client/Maliev.Intranet.Client.csproj --no-restore -v quiet
  ```

  Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Visual verification checklist**

  Run the app and navigate to a project page with a 3D model. Enable edge rendering. Verify:
  - [ ] Edges are stable (no flashing/Z-fighting) when looking at flat faces head-on
  - [ ] Edges remain stable when zoomed out significantly (5–10×)
  - [ ] Edges remain stable when zoomed in close
  - [ ] Orthographic mode edges are unaffected (same appearance as before)
  - [ ] Switching between perspective and orthographic mode while edges are on shows clean transition

- [ ] **Step 4: Commit**

  ```bash
  git add Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js
  git commit -m "fix(viewer): dynamically recompute zOffset and zOffsetUnits per-frame to eliminate edge Z-fighting at all zoom levels"
  ```

---

## Self-Review

**Spec coverage:**
- ✓ Flat-face Z-fighting (head-on views): addressed by `zOffsetUnits = 4096`
- ✓ Zoom-dependent Z-fighting: addressed by dynamic `getEdgesZOffset` scaling with `sqrt(radius/fitRadius)`
- ✓ Zoom observer always stale (captured constant): addressed by removing the captured `const zOffset = 10` and calling helpers dynamically
- ✓ Orthographic unaffected: `getEdgesZOffset` returns 1 for ortho, `zOffsetUnits` remains 0 for ortho

**Placeholder scan:** No TBD / TODO / similar items found.

**Type consistency:** `getEdgesZOffset` returns `number` everywhere it is used. `zOffsetUnits` assignment is guarded by `'zOffsetUnits' in mesh._edgesRenderer` for runtime safety in case the property is not present in a future BabylonJS version.
