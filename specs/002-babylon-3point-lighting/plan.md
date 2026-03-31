# Babylon Viewer 3-Point Lighting Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the 2-light setup with a proper 3-point CAD lighting system (Key + Fill + Back + Ambient) in `babylon-viewer.js`.

**Architecture:** Single-file JS change in `Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js`. Three new lights added alongside existing shadow generator. No external dependencies or CDN changes.

**Tech Stack:** Vanilla BabylonJS (existing CDN loads), no new dependencies.

---

## File Map

- **Modify:** `Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js:214-230` (light creation block)
- **Modify:** `Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js:235` (`_sysNames` set)
- **Spec:** `specs/002-babylon-3point-lighting/design.md`

---

## Tasks

### Task 1: Update `_sysNames` to include new light names

**Files:**
- Modify: `Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js:235`

- [ ] **Step 1: Update system names set**

Replace:
```javascript
const _sysNames = new Set(['hemi', 'dir', 'cam', '__init_light__']);
```

With:
```javascript
const _sysNames = new Set(['key', 'fill', 'back', 'hemi', 'cam', '__init_light__']);
```

---

### Task 2: Replace 2-light setup with 3-point lighting

**Files:**
- Modify: `Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js:214-230`

- [ ] **Step 1: Replace light creation block**

Replace the entire block from line 214 to line 230:

```javascript
                // ── Remove placeholder lights, add proper Z-up lighting ──
                _scene.lights.forEach(l => l.dispose());

                // ── Ambient (HemisphericLight — no shadows) ──
                const hemi = new BABYLON.HemisphericLight('hemi', new BABYLON.Vector3(0, 0, 1), _scene);
                hemi.intensity   = isDark ? 0.20 : 0.30;
                hemi.specular    = new BABYLON.Color3(0.05, 0.05, 0.05);
                hemi.groundColor = isDark ? new BABYLON.Color3(0.08, 0.08, 0.12)
                                          : new BABYLON.Color3(0.50, 0.55, 0.60);

                // ── Key Light (shadows) — front-right-above ──
                const key = new BABYLON.DirectionalLight('key', new BABYLON.Vector3(-0.70, -1.00, -0.80), _scene);
                key.intensity = isDark ? 0.80 : 1.50;

                const shadowGenerator = new BABYLON.ShadowGenerator(2048, key);
                shadowGenerator.usePercentageCloserFiltering = true;
                shadowGenerator.filteringQuality = BABYLON.ShadowGenerator.QUALITY_HIGH;
                shadowGenerator.setDarkness(isDark ? 0.30 : 0.60);

                // ── Fill Light (no shadows) — left-front-above ──
                const fill = new BABYLON.DirectionalLight('fill', new BABYLON.Vector3(0.80, -0.40, -0.30), _scene);
                fill.intensity = isDark ? 0.30 : 0.50;

                // ── Back Light (no shadows) — behind-above ──
                const back = new BABYLON.DirectionalLight('back', new BABYLON.Vector3(0.00, 1.00, -0.50), _scene);
                back.intensity = isDark ? 0.40 : 0.65;
```

---

### Task 3: Verify no regressions

**Files:**
- Read: `Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js:214-245` (review changes)
- Read: `Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js:326-333` (shadow caster assignment — unchanged but verify)
- Build: `dotnet build Maliev.Intranet.slnx`

- [ ] **Step 1: Review the modified light block**

Read lines 214–250 of babylon-viewer.js and verify:
- `_scene.lights.forEach(l => l.dispose())` still disposes all 4 lights correctly on reload
- `shadowGenerator` variable name is unchanged (still used at line 321 for `shadowGenerator.addShadowCaster(m)`)
- All 4 new light names (`key`, `fill`, `back`, `hemi`) are in `_sysNames`

- [ ] **Step 2: Verify shadow caster assignment is unchanged**

Read lines 316–324 and confirm `shadowGenerator.addShadowCaster(m)` still uses the same variable (no rename needed).

- [ ] **Step 3: Run dotnet build**

```bash
dotnet build Maliev.Intranet.slnx
```
Expected: BUILD SUCCEEDED (JS change, no C# files affected — build is a sanity check only).

---

### Task 4: Commit

- [ ] **Step 1: Commit the change**

```bash
git add Maliev.Intranet.Client/wwwroot/js/babylon-viewer.js
git commit -m "feat(viewer): upgrade to 3-point CAD lighting (key/fill/back/hemi)

- Key light: directional, casts shadows, 0.80/1.50 (dark/light)
- Fill light: directional, no shadows, softens key-side shadows
- Back light: directional, no shadows, rim separation
- Hemisphere ambient: low-intensity, prevents pure black areas
- ShadowGenerator unchanged: 2048px PCF, QUALITY_HIGH

Spec: specs/002-babylon-3point-lighting/design.md"
```

---

## Verification Checklist

- [ ] Shadows visible on flat surfaces and overhangs
- [ ] Fill light softens key-side shadow darkness
- [ ] Back light creates edge/rim separation when viewing from front
- [ ] Dark mode intensities feel balanced (not too dark, not blown out)
- [ ] Light mode feels bright but not washed out
- [ ] Disposal on model reload works (no ghost lights accumulating)
- [ ] `dotnet build` passes

---

## Spec Coverage Check

| Spec Requirement | Task |
|---|---|
| Key light with shadows | Task 2 |
| Fill light, no shadows | Task 2 |
| Back light, no shadows | Task 2 |
| HemisphericLight ambient | Task 2 |
| Intensity ratios (dark/light) | Task 2 |
| ShadowGenerator on key only | Task 2 |
| PCF 2048px, QUALITY_HIGH | Task 2 |
| _sysNames updated | Task 1 |
| No other code changes | Task 3 |
| Build pass | Task 3 |
| Commit | Task 4 |

**All spec items covered. No gaps.**
