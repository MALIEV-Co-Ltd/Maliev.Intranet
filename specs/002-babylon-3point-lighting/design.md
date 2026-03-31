# Babylon Viewer 3-Point Lighting Upgrade

**Date:** 2026-03-31
**Status:** Approved
**Repo:** `Maliev.Intranet`
**File:** `Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js`

---

## 1. Overview

Replace the current 2-light setup (1× HemisphericLight + 1× DirectionalLight) with a proper **3-point CAD lighting system** (Key + Fill + Back + Ambient) in `babylon-viewer.js`. This brings the viewer closer to industry-standard CAD software (Onshape, Fusion 360, SolidWorks) and improves part clarity for DFM (Design for Manufacturability) feedback on `quote.maliev.com`.

---

## 2. Light Configuration

All positions use BabylonJS world-space direction vectors. The coordinate system is **Z-up** (X=right, Y=depth, Z=up), matching the GLB preview convention.

### 2.1 Light Definitions

| Role   | Name   | Direction Vector (Z-up) | Type            | Shadows? |
|--------|--------|------------------------|-----------------|----------|
| Key    | `key`  | (−0.70, −1.00, −0.80) | DirectionalLight | ✅ Yes   |
| Fill   | `fill` | (+0.80, −0.40, −0.30) | DirectionalLight | ❌ No    |
| Back   | `back` | ( 0.00, +1.00, −0.50) | DirectionalLight | ❌ No    |
| Ambient| `hemi` | ( 0, 0, 1)             | HemisphericLight | ❌ No    |

### 2.2 Direction Vectors (Z-up World)

- **Key:** front-right-above — (−0.70, −1.00, −0.80) normalized. Primary illumination and shadow source. Matches the current directional light position.
- **Fill:** left-front-above — (+0.80, −0.40, −0.30) normalized. Lower elevation than key, opposite side. Softens key-side shadows.
- **Back:** behind-above — (0.00, +1.00, −0.50) normalized. Creates rim/edge separation so parts pop from the background.
- **Hemi:** hemisphere from above (0, 0, 1). Sky color on top, darker ground color below. Prevents pure black shadow areas.

### 2.3 Intensity Settings

| Light  | Dark Mode | Light Mode | Notes                                       |
|--------|-----------|------------|---------------------------------------------|
| Key    | 0.80      | 1.50       | Unchanged from current `dir` intensity      |
| Fill   | 0.30      | 0.50       | 37.5% / 33.3% of key intensity             |
| Back   | 0.40      | 0.65       | 50% / 43% of key intensity                 |
| Hemi   | 0.20      | 0.30       | Lower than current 0.45/0.65 to avoid blowout |

### 2.4 HemisphericLight Settings

```javascript
// Sky (diffuse) — low, slightly warm white
hemi.diffuse   = new BABYLON.Color3(0.95, 0.95, 1.00);

// Ground color — prevents pure black in shadow cavities
hemi.groundColor = isDark
    ? new BABYLON.Color3(0.08, 0.08, 0.12)   // unchanged
    : new BABYLON.Color3(0.50, 0.55, 0.60);  // unchanged

// Specular — minimal, prevents fake shine
hemi.specular  = new BABYLON.Color3(0.05, 0.05, 0.05);

// Intensity
hemi.intensity = isDark ? 0.20 : 0.30;
```

---

## 3. Shadow Configuration

Shadows are cast by the **key light only** (matches real-world studio photography and CAD convention). Fill and back lights are shadow-less to keep the image clean.

### 3.1 Shadow Generator Settings

| Setting                    | Value                        | Notes                                  |
|---------------------------|------------------------------|----------------------------------------|
| Source                    | `key` DirectionalLight       | Single shadow caster                   |
| Shadow map size           | 2048 px                      | Unchanged from current                |
| Algorithm                 | PCF (`usePercentageCloserFiltering = true`) | Unchanged |
| Quality                   | `QUALITY_HIGH`               | Unchanged                              |
| Darkness (dark mode)      | 0.30                         | Unchanged from current                |
| Darkness (light mode)     | 0.60                         | Unchanged from current                |

### 3.2 Shadow Caster Assignment

Only meshes receive shadows; fill and back lights do not have shadow generators. The assignment code (lines 316–323 existing) remains unchanged — only `key`'s shadow generator is used.

---

## 4. System Name Registry

Update `_sysNames` to include all light names so they are excluded from model root node detection:

```javascript
const _sysNames = new Set(['key', 'fill', 'back', 'hemi', 'cam', '__init_light__']);
```

---

## 5. Implementation Changes

### 5.1 Replace Light Creation Block (lines 214–230)

**Before:**
```javascript
// Remove placeholder lights, add proper Z-up lighting
_scene.lights.forEach(l => l.dispose());

const hemi = new BABYLON.HemisphericLight('hemi', new BABYLON.Vector3(0, 0, 1), _scene);
hemi.intensity   = isDark ? 0.45 : 0.65;
hemi.specular    = new BABYLON.Color3(0.1, 0.1, 0.1);
hemi.groundColor = isDark ? new BABYLON.Color3(0.08, 0.08, 0.12)
                          : new BABYLON.Color3(0.5, 0.55, 0.6);

const dir = new BABYLON.DirectionalLight('dir', new BABYLON.Vector3(-0.7, -1.0, -0.8), _scene);
dir.intensity = isDark ? 0.8 : 1.5;

// Add shadow generator for soft shadows
const shadowGenerator = new BABYLON.ShadowGenerator(2048, dir);
shadowGenerator.usePercentageCloserFiltering = true;
shadowGenerator.filteringQuality = BABYLON.ShadowGenerator.QUALITY_HIGH;
shadowGenerator.setDarkness(isDark ? 0.3 : 0.6);
```

**After:**
```javascript
// Remove placeholder lights, add proper Z-up 3-point lighting
_scene.lights.forEach(l => l.dispose());

// ── Ambient ──────────────────────────────────────────────────────────────────
const hemi = new BABYLON.HemisphericLight('hemi', new BABYLON.Vector3(0, 0, 1), _scene);
hemi.intensity   = isDark ? 0.20 : 0.30;
hemi.specular    = new BABYLON.Color3(0.05, 0.05, 0.05);
hemi.groundColor = isDark ? new BABYLON.Color3(0.08, 0.08, 0.12)
                          : new BABYLON.Color3(0.50, 0.55, 0.60);

// ── Key Light (shadows) ──────────────────────────────────────────────────────
const key = new BABYLON.DirectionalLight('key', new BABYLON.Vector3(-0.70, -1.00, -0.80), _scene);
key.intensity = isDark ? 0.80 : 1.50;

const shadowGenerator = new BABYLON.ShadowGenerator(2048, key);
shadowGenerator.usePercentageCloserFiltering = true;
shadowGenerator.filteringQuality = BABYLON.ShadowGenerator.QUALITY_HIGH;
shadowGenerator.setDarkness(isDark ? 0.30 : 0.60);

// ── Fill Light (no shadows) ──────────────────────────────────────────────────
const fill = new BABYLON.DirectionalLight('fill', new BABYLON.Vector3(0.80, -0.40, -0.30), _scene);
fill.intensity = isDark ? 0.30 : 0.50;

// ── Back Light (no shadows) ───────────────────────────────────────────────────
const back = new BABYLON.DirectionalLight('back', new BABYLON.Vector3(0.00, 1.00, -0.50), _scene);
back.intensity = isDark ? 0.40 : 0.65;
```

### 5.2 Update System Names Set

```javascript
const _sysNames = new Set(['key', 'fill', 'back', 'hemi', 'cam', '__init_light__']);
```

### 5.3 No Other Changes

All other code — camera setup, bounding box computation, edge rendering, auto-rotation, pick-point pan, dispose — **remains unchanged**.

---

## 6. Verification

- [ ] Shadows appear on ground-contact surfaces (FDM prints, flat bases)
- [ ] Fill light visibly softens key-side shadows without introducing muddy double shadows
- [ ] Back light creates visible rim/edge separation on parts viewed from front
- [ ] Dark mode: all intensities reduce correctly; no pure-black areas
- [ ] Light mode: scene is brighter but not blown out
- [ ] No regression in edge rendering or bounding box display
- [ ] No regression in dispose() — all 4 lights are cleaned up by `_scene.lights.forEach(l => l.dispose())`
- [ ] `dotnet build` passes (JS file, no C# impact)
