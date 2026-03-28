/**
 * BabylonJS 3D Viewer for Maliev Intranet
 * Lazy-loads BabylonJS from CDN and provides functions for Blazor JS interop.
 *
 * Supported formats (via babylonjs.loaders): .glb, .gltf, .stl, .obj
 * Unsupported formats (.step, .iges, .3mf etc.) must be converted to GLB before reaching this script.
 *
 * Z-UP COORDINATE SYSTEM (visual):
 *   GLB files follow glTF spec (Y-up, meters). After loading:
 *   - Meshes are scaled from meters → mm (or via known dimensions)
 *   - Meshes are rotated +90° around X: GLB-Y → world-Z (up), GLB-Z → world −Y
 *   - Result: X=right, Y=depth/front, Z=up  (standard CAD / 3D-printing convention)
 *   - Grid sits on XY plane at Z=0
 */

// ── Per-canvas state ──────────────────────────────────────────────────────────
const engines          = {};   // canvasId → BABYLON.Engine
const scenes           = {};   // canvasId → BABYLON.Scene
const mainCameras      = {};   // canvasId → main ArcRotateCamera  (FIX: replaces scene.activeCamera)
const darkModes        = {};   // canvasId → boolean
const modelLoadState   = {};   // canvasId → 'loading' | 'loaded' | 'error'
const originalMaterials= {};   // canvasId → { mesh.uniqueId → original material }
const sceneBoundingBoxes={};   // canvasId → { min:{x,y,z}, max:{x,y,z} } world coords (Z-up, mm)
const meshCenters      = {};   // canvasId → { x, y, z } world center after centering
const bboxLines        = {};   // canvasId → BABYLON.LinesMesh
const bboxLabels       = {};   // canvasId → [ { div, worldPos } ]
const bboxObservers    = {};   // canvasId → scene render observable handle
const axisGizmoLayer   = {};   // canvasId → { dispose() }
const axisLabelDivs    = {};   // canvasId → [ { div, localPos } ]
const axisMouseHandlers= {};   // canvasId → mousemove handler function
const resizeObservers  = {};   // canvasId → ResizeObserver
const cameraProjection = {};   // canvasId → 'perspective' | 'orthographic'
const orthoZoomObservers = {}; // canvasId → onViewMatrixChangedObservable handle

// ── Helpers ───────────────────────────────────────────────────────────────────

function loadScript(url) {
    return new Promise((resolve, reject) => {
        if (Array.from(document.scripts).some(s => s.src === url)) { resolve(); return; }
        const s = document.createElement('script');
        s.type = 'text/javascript'; s.src = url;
        s.onload = resolve;
        s.onerror = () => reject(new Error(`Failed to load: ${url}`));
        document.head.appendChild(s);
    });
}

/**
 * World bounding box of all non-system meshes (excludes __grid__, __axis*).
 */
function computeSceneBounds(scene) {
    let minX = Infinity, minY = Infinity, minZ = Infinity;
    let maxX = -Infinity, maxY = -Infinity, maxZ = -Infinity;
    scene.meshes.forEach(m => {
        if (m.name === '__grid__' || m.name.startsWith('__axis')) return;
        m.computeWorldMatrix(true);
        const bi = m.getBoundingInfo();
        const lo = bi.boundingBox.minimumWorld, hi = bi.boundingBox.maximumWorld;
        minX = Math.min(minX, lo.x); minY = Math.min(minY, lo.y); minZ = Math.min(minZ, lo.z);
        maxX = Math.max(maxX, hi.x); maxY = Math.max(maxY, hi.y); maxZ = Math.max(maxZ, hi.z);
    });
    if (!isFinite(minX)) return null;
    return { min: { x: minX, y: minY, z: minZ }, max: { x: maxX, y: maxY, z: maxZ } };
}

/**
 * Resolves the BabylonJS loader extension from the caller-supplied hint or the URL path.
 * Falls back to extracting the extension from the URL before any query string.
 */
function resolveExtension(fileUrl, fileExt) {
    const known = ['.glb', '.gltf', '.stl', '.obj', '.3mf'];
    const lower = (fileExt || '').toLowerCase();
    if (known.includes(lower)) return lower;
    const path = (fileUrl || '').split('?')[0].toLowerCase();
    return known.find(e => path.endsWith(e)) ?? '';
}

/**
 * Camera fitting: set radius so the mesh is comfortably framed by the FOV.
 */
function fitCameraToMesh(cam, bb, meshCenter) {
    const w = bb.max.x - bb.min.x;
    const d = bb.max.y - bb.min.y;
    const h = bb.max.z - bb.min.z;
    const halfDiag = Math.sqrt(w * w + d * d + h * h) / 2;
    const fov = cam.fov || 0.8;
    const meshRadius = Math.max(halfDiag / Math.tan(fov / 2) * 1.3, 1);

    // Z-up target: center horizontally, vertically at half-height
    cam.target = new BABYLON.Vector3(
        (bb.min.x + bb.max.x) / 2,
        (bb.min.y + bb.max.y) / 2,
        (bb.min.z + bb.max.z) / 2
    );
    cam.radius = meshRadius;
    cam.lowerRadiusLimit = meshRadius * 0.02;
    cam.upperRadiusLimit = meshRadius * 50;
    // Adaptive wheel precision: ~3 % change per scroll tick
    cam.wheelPrecision = Math.max(1, Math.round(5000 / meshRadius));
    cam.pinchPrecision = cam.wheelPrecision * 8;
    cam.minZ = meshRadius * 0.001;
    cam.maxZ = meshRadius * 2000;
}

/**
 * Z-up preset definitions.
 * Spherical coords relative to BabylonJS default (Y-up formula):
 *   pos = target + R * (sin(β)cos(α), cos(β), sin(β)sin(α))
 *
 * With our Z-up visual world (BabylonJS X=SW X, BabylonJS Y=SW Y depth, BabylonJS Z=SW Z up):
 * BabylonJS pos formula: target + R*(sin(β)cos(α), cos(β), sin(β)sin(α))
 *
 *   Front  (cam at SW −Y): α=0,      β=π       → pos at (0, −R,  0)  matches Python camera_dir (0,−1,0)
 *   Back   (cam at SW +Y): α=0,      β=0       → pos at (0, +R,  0)
 *   Right  (cam at SW +X): α=0,      β=π/2     → pos at (+R, 0,  0)
 *   Left   (cam at SW −X): α=π,      β=π/2     → pos at (−R, 0,  0)
 *   Top    (cam at SW +Z): α=π/2,    β=π/2     → pos at (0,  0, +R)
 *   Bottom (cam at SW −Z): α=−π/2,   β=π/2     → pos at (0,  0, −R)
 *   ISO    (1,−1,1 dir)   : α=π/4, β=π−acos(1/√3) → standard symmetric isometric, 35.26° elevation, front-right-above
 */
const PRESETS = {
    front:  { alpha: 0,                beta: Math.PI,             up: [0, 0, 1] },
    back:   { alpha: 0,                beta: 0,                   up: [0, 0, 1] },
    right:  { alpha: 0,                beta: Math.PI / 2,         up: [0, 0, 1] },
    left:   { alpha: Math.PI,          beta: Math.PI / 2,         up: [0, 0, 1] },
    top:    { alpha: Math.PI / 2,      beta: Math.PI / 2,         up: [0, 1, 0] },
    bottom: { alpha: -Math.PI / 2,     beta: Math.PI / 2,         up: [0, 1, 0] },
    iso:    { alpha: Math.PI / 4,      beta: Math.PI - Math.acos(1 / Math.sqrt(3)), up: [0, 0, 1] },
};

function applyPreset(cam, presetName) {
    const p = PRESETS[presetName];
    if (!p) return;
    cam.alpha = p.alpha;
    cam.beta  = p.beta;
    cam.upVector = new BABYLON.Vector3(p.up[0], p.up[1], p.up[2]);
}

// ── initialize ────────────────────────────────────────────────────────────────

/**
 * @param {string}  canvasId
 * @param {string}  fileUrl        Signed URL to the 3D file
 * @param {string}  fileExt        e.g. ".glb"
 * @param {boolean} isDark
 * @param {object|null} knownDimsMm  Optional { x, y, z } bounding box in mm from server
 */
export async function initialize(canvasId, fileUrl, fileExt, isDark, knownDimsMm) {
    try {
        await loadScript('https://cdn.babylonjs.com/babylon.js');
        await loadScript('https://cdn.babylonjs.com/loaders/babylonjs.loaders.min.js');
        await loadScript('https://cdn.babylonjs.com/materialsLibrary/babylonjs.materials.min.js');

        const canvas = document.getElementById(canvasId);
        if (!canvas) { console.error(`[BabylonViewer] Canvas #${canvasId} not found.`); return; }

        if (engines[canvasId]) await dispose(canvasId);

        darkModes[canvasId]        = !!isDark;
        modelLoadState[canvasId]   = 'loading';
        originalMaterials[canvasId]= {};
        sceneBoundingBoxes[canvasId] = null;
        meshCenters[canvasId]      = null;

        const engine = new BABYLON.Engine(canvas, true);
        const scene  = new BABYLON.Scene(engine);
        engine.resize();

        scene.clearColor = isDark
            ? new BABYLON.Color4(0.10, 0.12, 0.16, 1)
            : new BABYLON.Color4(0.97, 0.97, 0.98, 1);

        // Camera (upVector updated after model loads)
        const camera = new BABYLON.ArcRotateCamera('cam',
            Math.PI / 4, Math.acos(1 / Math.sqrt(3)), 10,
            BABYLON.Vector3.Zero(), scene);
        camera.attachControl(canvas, true);
        camera.wheelPrecision = 50;
        camera.upVector = new BABYLON.Vector3(0, 0, 1); // Z-up
        mainCameras[canvasId] = camera;

        // Placeholder light (replaced after model loads)
        new BABYLON.HemisphericLight('__init_light__', new BABYLON.Vector3(0, 0, 1), scene);

        const forcedExt = resolveExtension(fileUrl, fileExt);
        BABYLON.SceneLoader.Append('', fileUrl, scene,
            (_scene) => {
                modelLoadState[canvasId] = 'loaded';

                // ── Remove placeholder lights, add proper Z-up lighting ──
                _scene.lights.forEach(l => l.dispose());

                const hemi = new BABYLON.HemisphericLight('hemi', new BABYLON.Vector3(0, 0, 1), _scene);
                hemi.intensity   = isDark ? 0.55 : 0.70;
                hemi.specular    = new BABYLON.Color3(0.08, 0.08, 0.08);
                hemi.groundColor = isDark ? new BABYLON.Color3(0.06, 0.06, 0.08)
                                          : new BABYLON.Color3(0.45, 0.45, 0.50);

                const dir = new BABYLON.DirectionalLight('dir', new BABYLON.Vector3(-0.5, -0.8, -1), _scene);
                dir.intensity = isDark ? 0.65 : 0.85;

                // ── Find root meshes (exclude system meshes) ──
                const rootMeshes = _scene.meshes.filter(m =>
                    m.name !== '__grid__' && !m.name.startsWith('__axis') &&
                    (m.parent == null || !(m.parent instanceof BABYLON.AbstractMesh))
                );

                if (rootMeshes.length === 0) return;

                // ── Compute raw bounding box (GLB units, Y-up) ──
                let rawBb = computeSceneBounds(_scene);
                if (!rawBb) return;

                // ── Determine scale factor (GLB is in meters per glTF spec → scale to mm) ──
                let scaleFactor = 1;
                if (knownDimsMm && knownDimsMm.x > 0 && knownDimsMm.y > 0 && knownDimsMm.z > 0) {
                    const measuredMax = Math.max(
                        rawBb.max.x - rawBb.min.x,
                        rawBb.max.y - rawBb.min.y,
                        rawBb.max.z - rawBb.min.z
                    );
                    const knownMax = Math.max(knownDimsMm.x, knownDimsMm.y, knownDimsMm.z);
                    if (measuredMax > 1e-9) scaleFactor = knownMax / measuredMax;
                } else {
                    // Auto-detect: if bounding box < 1 unit assume meters, scale × 1000 to get mm
                    const maxDim = Math.max(
                        rawBb.max.x - rawBb.min.x,
                        rawBb.max.y - rawBb.min.y,
                        rawBb.max.z - rawBb.min.z
                    );
                    if (maxDim > 0 && maxDim < 1.0) scaleFactor = 1000;
                }

                // ── Apply scaling to root meshes ──
                if (scaleFactor !== 1) {
                    rootMeshes.forEach(m => m.scaling.setAll(scaleFactor));
                }

                // ── Apply Z-up rotation to root meshes: +90° around X ──
                // Transforms: local (x, y, z) → world (x, −z, y)
                // GLB +Y (up) → world +Z (visual up) ✓
                // GLB +Z (depth) → world −Y ✓
                const zUpQuat = BABYLON.Quaternion.RotationAxis(BABYLON.Axis.X, Math.PI / 2);
                rootMeshes.forEach(m => {
                    if (m.rotationQuaternion == null) {
                        m.rotationQuaternion = m.rotation
                            ? BABYLON.Quaternion.FromEulerVector(m.rotation)
                            : BABYLON.Quaternion.Identity();
                    }
                    // Apply Z-up rotation on top of any existing mesh rotation
                    m.rotationQuaternion = zUpQuat.multiply(m.rotationQuaternion);
                });

                // ── Recompute world bounding box (now Z-up, scaled to mm) ──
                let bb = computeSceneBounds(_scene);
                if (!bb) return;

                // ── Center in X and Y (horizontal in Z-up), lift Z_min → 0 ──
                const cx = (bb.min.x + bb.max.x) / 2;
                const cy = (bb.min.y + bb.max.y) / 2;
                const zLift = -bb.min.z;
                rootMeshes.forEach(m => {
                    m.position.x -= cx;
                    m.position.y -= cy;
                    m.position.z += zLift;
                });

                // ── Final bounding box ──
                const finalBb = computeSceneBounds(_scene);
                if (!finalBb) return;
                sceneBoundingBoxes[canvasId] = finalBb;
                meshCenters[canvasId] = {
                    x: (finalBb.min.x + finalBb.max.x) / 2,
                    y: (finalBb.min.y + finalBb.max.y) / 2,
                    z: (finalBb.min.z + finalBb.max.z) / 2,
                };

                // ── Create grid (XY plane at Z = 0) ──
                // Grid cells are always 10 mm. Floor extends 15 % beyond max horizontal dimension.
                const partX = finalBb.max.x - finalBb.min.x;
                const partY = finalBb.max.y - finalBb.min.y;
                const maxHoriz = Math.max(partX, partY, 1e-6);
                const gridRatio = 10; // 10 mm cells
                const rawGridSize = Math.max(maxHoriz * 1.2, 30);
                const gridSize = Math.ceil(rawGridSize / 10) * 10;
                // Cap subdivisions to avoid OOM crash on large/wrongly-scaled meshes
                const subdivs = Math.max(2, Math.min(200, Math.round(gridSize / gridRatio)));

                const ground = BABYLON.MeshBuilder.CreateGround('__grid__',
                    { width: gridSize, height: gridSize, subdivisions: subdivs }, _scene);
                // Rotate XZ plane (Y=0) → XY plane (Z=0)
                ground.rotation.x = Math.PI / 2;
                ground.isPickable = false;

                const gridMat = new BABYLON.GridMaterial('gridMat', _scene);
                gridMat.majorUnitFrequency = 5;
                gridMat.minorUnitVisibility = 0.35;
                gridMat.gridRatio = gridRatio;
                gridMat.backFaceCulling = false;
                gridMat.mainColor = isDark
                    ? new BABYLON.Color3(0.15, 0.15, 0.18)
                    : new BABYLON.Color3(0.88, 0.88, 0.90);
                gridMat.lineColor = isDark
                    ? new BABYLON.Color3(0.30, 0.30, 0.35)
                    : new BABYLON.Color3(0.62, 0.62, 0.68);
                gridMat.opacity = 0.90;
                ground.material = gridMat;

                // ── Save original materials ──
                const origMats = originalMaterials[canvasId];
                _scene.meshes.forEach(m => {
                    if (m.name !== '__grid__' && !m.name.startsWith('__axis') && m.material)
                        origMats[m.uniqueId] = m.material;
                });

                // ── Configure camera ──
                const cam = mainCameras[canvasId];
                if (cam) {
                    cam.upVector = new BABYLON.Vector3(0, 0, 1);
                    fitCameraToMesh(cam, finalBb, meshCenters[canvasId]);
                    applyPreset(cam, 'iso');
                }

                // ── Axis gizmo ──
                createAxisGizmo(canvasId, _scene, mainCameras[canvasId], canvas);

                // ── Resize observer ──
                if (resizeObservers[canvasId]) resizeObservers[canvasId].disconnect();
                const ro = new ResizeObserver(() => { if (engines[canvasId]) engines[canvasId].resize(); });
                ro.observe(canvas);
                resizeObservers[canvasId] = ro;

                setRenderMode(canvasId, 'solid');
            },
            null,
            (_scene, message, exception) => {
                modelLoadState[canvasId] = 'error';
                console.error('[BabylonViewer] Load error:', message, exception);
            },
            forcedExt
        );

        engine.runRenderLoop(() => scene.render());

        const resizeHandler = () => engine.resize();
        window.addEventListener('resize', resizeHandler);
        engine._resizeHandler = resizeHandler;

        engines[canvasId] = engine;
        scenes[canvasId]  = scene;

    } catch (err) {
        modelLoadState[canvasId] = 'error';
        console.error('[BabylonViewer] Init failed:', err);
    }
}

// ── Axis gizmo ────────────────────────────────────────────────────────────────

/**
 * Creates a fixed XYZ axis indicator in the top-right corner.
 * Z-up visual: X=red (right), Y=blue (depth), Z=green (up)
 */
function createAxisGizmo(canvasId, scene, mainCam, canvas) {
    // Dispose previous
    if (axisGizmoLayer[canvasId]) { axisGizmoLayer[canvasId].dispose(); axisGizmoLayer[canvasId] = null; }
    if (axisLabelDivs[canvasId]) {
        axisLabelDivs[canvasId].forEach(({ div }) => div.remove());
        delete axisLabelDivs[canvasId];
    }
    if (axisMouseHandlers[canvasId]) {
        canvas.removeEventListener('mousemove', axisMouseHandlers[canvasId]);
        canvas.removeEventListener('mouseleave', axisMouseHandlers[canvasId + '_leave']);
        delete axisMouseHandlers[canvasId];
    }
    ['__axisX__','__axisY__','__axisZ__','__axisXArr__','__axisYArr__','__axisZArr__']
        .forEach(n => { const m = scene.getMeshByName(n); if (m) m.dispose(); });
    const oldCam = scene.getCameraByName('__axesCam__');
    if (oldCam) oldCam.dispose();

    if (!(mainCam instanceof BABYLON.ArcRotateCamera)) return;

    const LAYER = 0x10000000;
    const LEN   = 0.65;

    // Z-up: X=red, Y=blue (depth/front), Z=green (up)
    const COL_X = new BABYLON.Color3(0.93, 0.27, 0.27); // Red
    const COL_Y = new BABYLON.Color3(0.22, 0.51, 0.96); // Blue
    const COL_Z = new BABYLON.Color3(0.13, 0.77, 0.27); // Green

    function makeAxisLine(name, pts, color) {
        const l = BABYLON.MeshBuilder.CreateLines(name, { points: pts }, scene);
        l.color = color; l.isPickable = false; l.layerMask = LAYER;
        return l;
    }

    makeAxisLine('__axisX__', [BABYLON.Vector3.Zero(), new BABYLON.Vector3(LEN, 0, 0)], COL_X);
    makeAxisLine('__axisY__', [BABYLON.Vector3.Zero(), new BABYLON.Vector3(0, LEN, 0)], COL_Y);
    makeAxisLine('__axisZ__', [BABYLON.Vector3.Zero(), new BABYLON.Vector3(0, 0, LEN)], COL_Z);

    function makeCone(name, dir, color) {
        const d = dir.normalize();
        const cone = BABYLON.MeshBuilder.CreateCylinder(name,
            { diameterTop: 0, diameterBottom: 0.08, height: 0.18, tessellation: 8 }, scene);
        const mat = new BABYLON.StandardMaterial(name + 'Mat', scene);
        mat.diffuseColor = color; mat.emissiveColor = color.scale(0.4);
        mat.backFaceCulling = false;
        cone.material = mat; cone.isPickable = false; cone.layerMask = LAYER;
        const up = new BABYLON.Vector3(0, 1, 0);
        const cross = BABYLON.Vector3.Cross(up, d);
        if (cross.length() > 0.001) {
            cone.rotationQuaternion = BABYLON.Quaternion.RotationAxis(
                cross.normalize(), Math.acos(Math.max(-1, Math.min(1, BABYLON.Vector3.Dot(up, d)))));
        } else if (BABYLON.Vector3.Dot(up, d) < 0) {
            cone.rotationQuaternion = BABYLON.Quaternion.RotationAxis(new BABYLON.Vector3(1, 0, 0), Math.PI);
        }
        cone.position = d.scale(LEN + 0.09);
        return cone;
    }

    makeCone('__axisXArr__', new BABYLON.Vector3(1, 0, 0), COL_X);
    makeCone('__axisYArr__', new BABYLON.Vector3(0, 1, 0), COL_Y);
    makeCone('__axisZArr__', new BABYLON.Vector3(0, 0, 1), COL_Z);

    // Gizmo camera — top-right corner, orthographic, renders only LAYER meshes
    const hw = 1.1;
    const axesCam = new BABYLON.ArcRotateCamera('__axesCam__',
        mainCam.alpha, mainCam.beta, 3.5, BABYLON.Vector3.Zero(), scene);
    axesCam.mode        = BABYLON.Camera.ORTHOGRAPHIC_CAMERA;
    axesCam.orthoLeft   = -hw; axesCam.orthoRight = hw;
    axesCam.orthoTop    = hw;  axesCam.orthoBottom = -hw;
    axesCam.viewport    = new BABYLON.Viewport(0.78, 0.76, 0.22, 0.24); // top-right
    axesCam.layerMask   = LAYER;
    axesCam.minZ = 0.01; axesCam.maxZ = 100;
    axesCam.upVector = new BABYLON.Vector3(0, 0, 1);

    const syncObs = scene.onBeforeRenderObservable.add(() => {
        axesCam.alpha = mainCam.alpha;
        axesCam.beta  = mainCam.beta;
        axesCam.upVector.copyFrom(mainCam.upVector);
        // Update axis hover label positions each frame
        updateAxisLabels(canvasId, scene, axesCam);
    });

    scene.activeCameras = [mainCam, axesCam];

    // ── Hover labels (X, Y, Z text that appear on mouseover) ──
    const labelDefs = [
        { name: 'X', color: '#f04444', pos: new BABYLON.Vector3(LEN + 0.28, 0,       0) },
        { name: 'Y', color: '#3882f5', pos: new BABYLON.Vector3(0,       LEN + 0.28, 0) },
        { name: 'Z', color: '#22c750', pos: new BABYLON.Vector3(0,       0,       LEN + 0.28) },
    ];

    const labelDivs = labelDefs.map(({ name, color, pos }) => {
        const div = document.createElement('div');
        div.textContent = name;
        div.style.cssText = `
            position: fixed;
            transform: translate(-50%, -50%);
            color: ${color};
            font-size: 11px;
            font-weight: 700;
            font-family: monospace;
            pointer-events: none;
            opacity: 0;
            transition: opacity 0.18s ease;
            z-index: 25;
            text-shadow: 0 0 4px rgba(0,0,0,0.8);
        `;
        document.body.appendChild(div);
        return { div, localPos: pos };
    });
    axisLabelDivs[canvasId] = labelDivs;

    // Mousemove: show labels when cursor is over axes viewport region
    // BabylonJS viewport (0.78, 0.76, 0.22, 0.24):
    //   canvas DOM coords: relX ≥ 0.78, relY ≤ 1 − 0.76 = 0.24  (BabylonJS Y is bottom-up)
    function onMouseMove(e) {
        const rect = canvas.getBoundingClientRect();
        const relX = (e.clientX - rect.left) / rect.width;
        const relY = (e.clientY - rect.top) / rect.height;
        const inGizmo = relX >= 0.77 && relY <= 0.25;
        labelDivs.forEach(({ div }) => {
            div.style.opacity = inGizmo ? '1' : '0';
        });
    }
    function onMouseLeave() {
        labelDivs.forEach(({ div }) => { div.style.opacity = '0'; });
    }
    canvas.addEventListener('mousemove', onMouseMove);
    canvas.addEventListener('mouseleave', onMouseLeave);
    axisMouseHandlers[canvasId]              = onMouseMove;
    axisMouseHandlers[canvasId + '_leave']   = onMouseLeave;

    axisGizmoLayer[canvasId] = {
        dispose() {
            scene.onBeforeRenderObservable.remove(syncObs);
            scene.activeCameras = [mainCam];
            axesCam.dispose();
            ['__axisX__','__axisY__','__axisZ__','__axisXArr__','__axisYArr__','__axisZArr__']
                .forEach(n => { const m = scene.getMeshByName(n); if (m) m.dispose(); });
            labelDivs.forEach(({ div }) => div.remove());
            canvas.removeEventListener('mousemove', onMouseMove);
            canvas.removeEventListener('mouseleave', onMouseLeave);
        }
    };
}

/**
 * Reprojects axis label positions to screen space each frame.
 */
function updateAxisLabels(canvasId, scene, axesCam) {
    const labelDivs = axisLabelDivs[canvasId];
    if (!labelDivs) return;
    const engine = engines[canvasId];
    if (!engine) return;
    const canvas = engine.getRenderingCanvas();
    if (!canvas) return;
    const rect = canvas.getBoundingClientRect();
    const rw = engine.getRenderWidth(), rh = engine.getRenderHeight();
    // Use axesCam's own view×projection so labels map to the gizmo viewport.
    const viewProj = axesCam.getViewMatrix().multiply(axesCam.getProjectionMatrix());
    labelDivs.forEach(({ div, localPos }) => {
        const p = BABYLON.Vector3.Project(
            localPos, BABYLON.Matrix.Identity(), viewProj,
            axesCam.viewport.toGlobal(rw, rh)
        );
        if (p.z >= 0 && p.z <= 1) {
            div.style.left = (rect.left + (p.x / rw) * rect.width)  + 'px';
            // BabylonJS viewport.y is in bottom-origin (OpenGL) convention.
            // Subtract it so the gizmo's pixel range [vp.y·rh, rh] maps to CSS [0%, vh%] from top.
            div.style.top  = (rect.top  + (p.y / rh - axesCam.viewport.y) * rect.height) + 'px';
        }
    });
}

// ── setRenderMode ─────────────────────────────────────────────────────────────

export function setRenderMode(canvasId, mode) {
    const scene = scenes[canvasId];
    if (!scene) return;
    const origMats = originalMaterials[canvasId] ?? {};

    scene.meshes.forEach(mesh => {
        if (mesh.name === '__grid__' || mesh.name.startsWith('__axis')) return;

        if (mode === 'wireframe') {
            if (!origMats[mesh.uniqueId] && mesh.material) origMats[mesh.uniqueId] = mesh.material;
            const wm = new BABYLON.StandardMaterial('wire_' + mesh.uniqueId, scene);
            wm.wireframe      = true;
            // Flat emissive-only: no diffuse lighting response → identical colour on every mesh
            wm.emissiveColor  = new BABYLON.Color3(0.75, 0.78, 0.85);
            wm.diffuseColor   = BABYLON.Color3.Black();
            wm.specularColor  = BABYLON.Color3.Black();
            wm.disableLighting = true;
            wm.backFaceCulling = false;
            mesh.material = wm;
            try { mesh.disableEdgesRendering(); } catch (_) {}

        } else if (mode === 'transparent') {
            if (!origMats[mesh.uniqueId] && mesh.material) origMats[mesh.uniqueId] = mesh.material;
            const isPBR = mesh.material instanceof BABYLON.PBRMaterial;
            let tm;
            if (isPBR) {
                const pbr = mesh.material;
                tm = new BABYLON.PBRMaterial('xray_' + mesh.uniqueId, scene);
                tm.albedoColor      = pbr.albedoColor ? pbr.albedoColor.scale(0.6) : new BABYLON.Color3(0.7, 0.7, 0.7);
                tm.alpha            = 0.38;
                tm.transparencyMode = BABYLON.PBRMaterial.PBRMATERIAL_ALPHABLEND;
                tm.metallic         = pbr.metallic ?? 0;
                tm.roughness        = pbr.roughness ?? 0.5;
            } else {
                tm = new BABYLON.StandardMaterial('xray_' + mesh.uniqueId, scene);
                tm.diffuseColor = mesh.material?.diffuseColor
                    ? mesh.material.diffuseColor.scale(0.6)
                    : new BABYLON.Color3(0.7, 0.7, 0.7);
                tm.alpha         = 0.38;
                tm.emissiveColor = new BABYLON.Color3(0.12, 0.12, 0.12);
            }
            tm.backFaceCulling  = false;
            tm.twoSidedLighting = true;
            mesh.material = tm;
            try { mesh.disableEdgesRendering(); } catch (_) {}

        } else {
            // Solid — restore original material
            if (origMats[mesh.uniqueId]) mesh.material = origMats[mesh.uniqueId];
            try { mesh.disableEdgesRendering(); } catch (_) {}
        }
    });
}

// ── setCameraPreset ───────────────────────────────────────────────────────────

export function setCameraPreset(canvasId, preset) {
    const cam = mainCameras[canvasId];
    const bb  = sceneBoundingBoxes[canvasId];
    if (!cam) return;

    // Re-fit camera distance so model stays in frame
    if (bb) fitCameraToMesh(cam, bb, meshCenters[canvasId]);

    applyPreset(cam, preset);
}

// ── resetCamera ───────────────────────────────────────────────────────────────

export function resetCamera(canvasId) {
    const cam = mainCameras[canvasId];
    const bb  = sceneBoundingBoxes[canvasId];
    if (!cam) return;

    cam.upVector = new BABYLON.Vector3(0, 0, 1);
    if (bb) fitCameraToMesh(cam, bb, meshCenters[canvasId]);
    applyPreset(cam, 'iso');

    const canvas = document.getElementById(canvasId);
    if (canvas && !cam.inputs.attached.keyboard) cam.attachControl(canvas, true);
}

// ── toggleEdges ───────────────────────────────────────────────────────────────

/**
 * Reads the --mud-palette-text-primary CSS variable and converts it to a BABYLON.Color4.
 * Falls back to mode-aware hardcoded colors if the variable is missing or unparseable.
 */
function getEdgeColorFromCss(isDark) {
    try {
        const raw = getComputedStyle(document.documentElement)
            .getPropertyValue('--mud-palette-text-primary').trim();
        if (raw) {
            // Use a temporary canvas context to normalize any CSS color format → hex
            const ctx = document.createElement('canvas').getContext('2d');
            ctx.fillStyle = raw;
            const hex = ctx.fillStyle; // browser normalizes to #rrggbb
            const m = hex.match(/^#([0-9a-f]{2})([0-9a-f]{2})([0-9a-f]{2})$/i);
            if (m) return new BABYLON.Color4(
                parseInt(m[1], 16) / 255,
                parseInt(m[2], 16) / 255,
                parseInt(m[3], 16) / 255,
                0.9
            );
        }
    } catch (_) {}
    return isDark
        ? new BABYLON.Color4(0.80, 0.82, 0.88, 1.0)
        : new BABYLON.Color4(0.12, 0.14, 0.18, 1.0);
}

export function toggleEdges(canvasId, enabled) {
    const scene = scenes[canvasId];
    if (!scene) return;
    const edgeColor = getEdgeColorFromCss(darkModes[canvasId]);
    scene.meshes.forEach(mesh => {
        if (mesh.name === '__grid__' || mesh.name.startsWith('__axis')) return;
        if (enabled) {
            try {
                mesh.disableEdgesRendering();
                // epsilon=0.95, checkVerticesInsteadOfIndices=true fixes non-welded STL meshes
                mesh.enableEdgesRendering(0.95, true);
                mesh.edgesWidth = 8;
                mesh.edgesColor = edgeColor;
            } catch (_) {}
        } else {
            try { mesh.disableEdgesRendering(); } catch (_) {}
        }
    });
}

// ── toggleBoundingBox ─────────────────────────────────────────────────────────

export function toggleBoundingBox(canvasId, enabled) {
    if (!enabled) { clearBoundingBox(canvasId); return; }

    const scene  = scenes[canvasId];
    const engine = engines[canvasId];
    const canvas = document.getElementById(canvasId);
    if (!scene || !engine || !canvas) return;

    clearBoundingBox(canvasId);

    const bb = sceneBoundingBoxes[canvasId];
    if (!bb) return;

    const v3 = (x, y, z) => new BABYLON.Vector3(x, y, z);

    // 12 edges of the bounding box (Z-up world: X=width, Y=depth, Z=height)
    const edges = [
        // 4 edges along X (width)
        [v3(bb.min.x, bb.min.y, bb.min.z), v3(bb.max.x, bb.min.y, bb.min.z)],
        [v3(bb.min.x, bb.max.y, bb.min.z), v3(bb.max.x, bb.max.y, bb.min.z)],
        [v3(bb.min.x, bb.min.y, bb.max.z), v3(bb.max.x, bb.min.y, bb.max.z)],
        [v3(bb.min.x, bb.max.y, bb.max.z), v3(bb.max.x, bb.max.y, bb.max.z)],
        // 4 edges along Y (depth)
        [v3(bb.min.x, bb.min.y, bb.min.z), v3(bb.min.x, bb.max.y, bb.min.z)],
        [v3(bb.max.x, bb.min.y, bb.min.z), v3(bb.max.x, bb.max.y, bb.min.z)],
        [v3(bb.min.x, bb.min.y, bb.max.z), v3(bb.min.x, bb.max.y, bb.max.z)],
        [v3(bb.max.x, bb.min.y, bb.max.z), v3(bb.max.x, bb.max.y, bb.max.z)],
        // 4 edges along Z (height)
        [v3(bb.min.x, bb.min.y, bb.min.z), v3(bb.min.x, bb.min.y, bb.max.z)],
        [v3(bb.max.x, bb.min.y, bb.min.z), v3(bb.max.x, bb.min.y, bb.max.z)],
        [v3(bb.min.x, bb.max.y, bb.min.z), v3(bb.min.x, bb.max.y, bb.max.z)],
        [v3(bb.max.x, bb.max.y, bb.min.z), v3(bb.max.x, bb.max.y, bb.max.z)],
    ];

    const gold = new BABYLON.Color4(1, 0.85, 0, 1);
    const lineMesh = BABYLON.MeshBuilder.CreateLineSystem('bbox_lines', {
        lines: edges,
        colors: edges.map(() => [gold, gold]),
        updatable: false
    }, scene);
    lineMesh.isPickable = false;
    bboxLines[canvasId] = lineMesh;

    // Dimension labels — midpoint of each dimension, worldPos is a mutable Vector3
    // so the render observable can reposition them to the camera-facing side each frame.
    const midX = (bb.min.x + bb.max.x) / 2;
    const midY = (bb.min.y + bb.max.y) / 2;
    const midZ = (bb.min.z + bb.max.z) / 2;
    const gap  = Math.max((bb.max.x - bb.min.x) * 0.04, 2);

    const labelDefs = [
        { text: `X: ${(bb.max.x - bb.min.x).toFixed(1)} mm`, borderColor: '#f04444' },
        { text: `Y: ${(bb.max.y - bb.min.y).toFixed(1)} mm`, borderColor: '#3882f5' },
        { text: `Z: ${(bb.max.z - bb.min.z).toFixed(1)} mm`, borderColor: '#22c750' },
    ];

    const labelEntries = labelDefs.map(({ text, borderColor }) => {
        const div = document.createElement('div');
        div.textContent = text;
        div.style.cssText = `
            position: fixed;
            transform: translate(-50%, -50%);
            background: rgba(10, 10, 14, 0.82);
            color: #f0f0f0;
            font-size: 11px;
            font-weight: 600;
            font-family: monospace;
            padding: 2px 7px;
            border-radius: 4px;
            border-left: 2px solid ${borderColor};
            pointer-events: none;
            white-space: nowrap;
            z-index: 20;
        `;
        document.body.appendChild(div);
        // Use mutable Vector3 so each frame we can call .set() with the camera-facing anchor
        return { div, worldPos: new BABYLON.Vector3(midX, bb.min.y - gap, bb.min.z - gap) };
    });

    // Store entries + gap for use in the render observable
    bboxLabels[canvasId] = { entries: labelEntries, gap };

    // Update label world-positions and screen positions every frame
    const cam = mainCameras[canvasId];
    if (cam) {
        const obs = scene.onAfterRenderObservable.add(() => {
            const cvs  = engine.getRenderingCanvas();
            const rect = cvs ? cvs.getBoundingClientRect() : { left: 0, top: 0, width: 1, height: 1 };
            const rw = engine.getRenderWidth(), rh = engine.getRenderHeight();
            // Build view×projection from the main camera explicitly — scene.getTransformMatrix()
            // is unreliable when activeCameras has multiple entries (returns last-rendered camera).
            const viewProj = cam.getViewMatrix().multiply(cam.getProjectionMatrix());

            // Reposition anchors to the camera-facing side of the bounding box each frame
            // so labels are never hidden behind the mesh when the camera orbits.
            const camPos = cam.position;
            const bbLive = sceneBoundingBoxes[canvasId];
            const g = bboxLabels[canvasId]?.gap ?? gap;
            if (bbLive) {
                const anchorY = camPos.y < (bbLive.min.y + bbLive.max.y) / 2 ? bbLive.min.y - g : bbLive.max.y + g;
                const anchorZ = camPos.z > (bbLive.min.z + bbLive.max.z) / 2 ? bbLive.max.z + g : bbLive.min.z - g;
                const anchorX = camPos.x > (bbLive.min.x + bbLive.max.x) / 2 ? bbLive.max.x + g : bbLive.min.x - g;
                const mx = (bbLive.min.x + bbLive.max.x) / 2;
                const my = (bbLive.min.y + bbLive.max.y) / 2;
                const mz = (bbLive.min.z + bbLive.max.z) / 2;
                labelEntries[0].worldPos.set(mx,      anchorY, anchorZ); // X dim label
                labelEntries[1].worldPos.set(anchorX, my,      anchorZ); // Y dim label
                labelEntries[2].worldPos.set(anchorX, anchorY, mz);      // Z dim label
            }

            labelEntries.forEach(({ div, worldPos }) => {
                const p = BABYLON.Vector3.Project(
                    worldPos, BABYLON.Matrix.Identity(), viewProj,
                    cam.viewport.toGlobal(rw, rh)
                );
                if (p.z >= 0 && p.z <= 1) {
                    div.style.left    = (rect.left + (p.x / rw) * rect.width)  + 'px';
                    div.style.top     = (rect.top  + (p.y / rh) * rect.height) + 'px';
                    div.style.display = '';
                } else {
                    div.style.display = 'none';
                }
            });
        });
        bboxObservers[canvasId] = obs;
    }
}

function clearBoundingBox(canvasId) {
    if (bboxLines[canvasId]) { bboxLines[canvasId].dispose(); delete bboxLines[canvasId]; }
    if (bboxLabels[canvasId]) {
        bboxLabels[canvasId].entries.forEach(({ div }) => div.remove());
        delete bboxLabels[canvasId];
    }
    if (bboxObservers[canvasId]) {
        const scene = scenes[canvasId];
        if (scene) scene.onAfterRenderObservable.remove(bboxObservers[canvasId]);
        delete bboxObservers[canvasId];
    }
}

// ── setCameraProjection ───────────────────────────────────────────────────────

/**
 * Toggle between perspective and orthographic projection for the main camera.
 * In ortho mode the ortho bounds are kept in sync with camera radius so
 * scroll-to-zoom continues to work.
 */
export function setCameraProjection(canvasId, mode) {
    const cam    = mainCameras[canvasId];
    const engine = engines[canvasId];
    if (!cam || !engine) return;

    cameraProjection[canvasId] = mode;

    if (mode === 'orthographic') {
        const aspect = engine.getAspectRatio(cam);
        const half   = cam.radius * 0.5;
        cam.mode        = BABYLON.Camera.ORTHOGRAPHIC_CAMERA;
        cam.orthoLeft   = -half * aspect;
        cam.orthoRight  =  half * aspect;
        cam.orthoTop    =  half;
        cam.orthoBottom = -half;

        // Keep ortho bounds in sync when the user zooms (radius changes)
        if (!orthoZoomObservers[canvasId]) {
            orthoZoomObservers[canvasId] = cam.onViewMatrixChangedObservable.add(() => {
                if (cameraProjection[canvasId] !== 'orthographic') return;
                const asp = engine.getAspectRatio(cam);
                const h   = cam.radius * 0.5;
                cam.orthoLeft   = -h * asp;
                cam.orthoRight  =  h * asp;
                cam.orthoTop    =  h;
                cam.orthoBottom = -h;
            });
        }
    } else {
        cam.mode = BABYLON.Camera.PERSPECTIVE_CAMERA;
    }
}

// ── dispose ───────────────────────────────────────────────────────────────────

export function dispose(canvasId) {
    const engine = engines[canvasId];
    if (engine?._resizeHandler) window.removeEventListener('resize', engine._resizeHandler);

    if (resizeObservers[canvasId]) { resizeObservers[canvasId].disconnect(); delete resizeObservers[canvasId]; }
    if (axisGizmoLayer[canvasId])  { try { axisGizmoLayer[canvasId].dispose(); } catch (_) {} delete axisGizmoLayer[canvasId]; }
    if (axisLabelDivs[canvasId])   { axisLabelDivs[canvasId].forEach(({ div }) => div.remove()); delete axisLabelDivs[canvasId]; }

    const canvas = document.getElementById(canvasId);
    if (canvas) {
        if (axisMouseHandlers[canvasId])              canvas.removeEventListener('mousemove',  axisMouseHandlers[canvasId]);
        if (axisMouseHandlers[canvasId + '_leave'])   canvas.removeEventListener('mouseleave', axisMouseHandlers[canvasId + '_leave']);
    }
    delete axisMouseHandlers[canvasId];
    delete axisMouseHandlers[canvasId + '_leave'];

    if (orthoZoomObservers[canvasId]) {
        const cam = mainCameras[canvasId];
        if (cam) cam.onViewMatrixChangedObservable.remove(orthoZoomObservers[canvasId]);
        delete orthoZoomObservers[canvasId];
    }

    clearBoundingBox(canvasId);

    scenes[canvasId]?.dispose();
    engine?.dispose();

    delete engines[canvasId];
    delete scenes[canvasId];
    delete mainCameras[canvasId];
    delete darkModes[canvasId];
    delete modelLoadState[canvasId];
    delete originalMaterials[canvasId];
    delete sceneBoundingBoxes[canvasId];
    delete meshCenters[canvasId];
    delete cameraProjection[canvasId];
}

// Debug handle
window.babylonViewer = { initialize, setRenderMode, resetCamera, dispose, setCameraPreset, toggleEdges, toggleBoundingBox, setCameraProjection };
