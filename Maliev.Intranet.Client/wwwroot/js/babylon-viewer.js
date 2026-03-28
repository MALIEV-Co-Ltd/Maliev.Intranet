/**
 * BabylonJS 3D Viewer for Maliev Intranet
 * Lazy-loads BabylonJS from CDN and provides functions for Blazor JS interop.
 *
 * Supported formats (via babylonjs.loaders): .glb, .gltf, .stl, .obj
 * Unsupported formats (.step, .iges, .3mf etc.) should never reach this script —
 * ProjectNew.razor guards with BabylonSupportedExtensions before requesting the viewer URL.
 *
 * Y-UP COORDINATE SYSTEM: Y is vertical (up), Z is depth (into screen).
 * - X: right, Y: up, Z: depth
 * - Camera views (front/back/left/right/top) are relative to Y-up
 */

const engines = {};
const scenes = {};
const darkModes = {};
const modelLoadState = {}; // 'loading' | 'loaded' | 'error'
const originalMaterials = {};  // mesh uniqueId -> original material, for render mode restoration
const sceneBoundingBoxes = {}; // canvasId -> { min, max } in world coords (after offset)
const meshCenters = {};       // canvasId -> { x, y, z } center of mesh after offset
const bboxLines = {};          // canvasId -> BABYLON.Mesh (line mesh)
const bboxLabels = {};          // canvasId -> HTML div elements
const axisGizmoLayer = {};     // canvasId -> { dispose: fn }
const resizeObservers = {};    // canvasId -> ResizeObserver

/**
 * Lazy-loads a script from a URL, resolving immediately if already present.
 */
function loadScript(url) {
    return new Promise((resolve, reject) => {
        if (Array.from(document.scripts).some(s => s.src === url)) {
            resolve();
            return;
        }
        const script = document.createElement('script');
        script.type = 'text/javascript';
        script.src = url;
        script.onload = resolve;
        script.onerror = () => reject(new Error(`Failed to load script: ${url}`));
        document.head.appendChild(script);
    });
}

/**
 * Computes the world bounding box of all non-grid meshes in the scene.
 */
function computeSceneBounds(scene) {
    let minX = Infinity, minY = Infinity, minZ = Infinity;
    let maxX = -Infinity, maxY = -Infinity, maxZ = -Infinity;
    scene.meshes.forEach(mesh => {
        if (mesh.name === '__grid__' || mesh.name.startsWith('__axis')) return;
        mesh.computeWorldMatrix(true);
        const bi = mesh.getBoundingInfo();
        const wmin = bi.boundingBox.minimumWorld;
        const wmax = bi.boundingBox.maximumWorld;
        minX = Math.min(minX, wmin.x); minY = Math.min(minY, wmin.y); minZ = Math.min(minZ, wmin.z);
        maxX = Math.max(maxX, wmax.x); maxY = Math.max(maxY, wmax.y); maxZ = Math.max(maxZ, wmax.z);
    });
    if (!isFinite(minX)) return null;
    return { min: { x: minX, y: minY, z: minZ }, max: { x: maxX, y: maxY, z: maxZ } };
}

/**
 * Maps a file extension to the BabylonJS SceneLoader plugin identifier.
 */
function pluginExtension(ext) {
    const map = { '.glb': '.glb', '.gltf': '.gltf', '.stl': '.stl', '.obj': '.obj' };
    return map[(ext || '').toLowerCase()] ?? '';
}

/**
 * Initializes the BabylonJS viewer on a canvas element.
 * @param {string} canvasId   The id of the <canvas> element.
 * @param {string} fileUrl    Signed GCS URL to the 3D file.
 * @param {string} fileExt    File extension e.g. ".glb".
 * @param {boolean} isDark    Whether the MudBlazor theme is currently in dark mode.
 */
export async function initialize(canvasId, fileUrl, fileExt, isDark) {
    try {
        await loadScript('https://cdn.babylonjs.com/babylon.js');
        await loadScript('https://cdn.babylonjs.com/loaders/babylonjs.loaders.min.js');
        await loadScript('https://cdn.babylonjs.com/materialsLibrary/babylonjs.materials.min.js');

        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error(`[BabylonViewer] Canvas #${canvasId} not found.`);
            return;
        }

        if (engines[canvasId]) {
            await dispose(canvasId);
        }

        darkModes[canvasId] = !!isDark;
        modelLoadState[canvasId] = 'loading';
        originalMaterials[canvasId] = {};
        sceneBoundingBoxes[canvasId] = null;
        meshCenters[canvasId] = null;

        const engine = new BABYLON.Engine(canvas, true);
        const scene = new BABYLON.Scene(engine);

        // Ensure canvas has proper dimensions before model loading
        engine.resize();

        if (isDark) {
            scene.clearColor = new BABYLON.Color4(0.10, 0.12, 0.16, 1);
        } else {
            scene.clearColor = new BABYLON.Color4(0.97, 0.97, 0.98, 1);
        }

        // Create camera with Y-up targeting (Y is up, Z is depth)
        const camera = new BABYLON.ArcRotateCamera(
            'cam', Math.PI / 4, Math.PI / 6, 10,  // beta=PI/6 so camera looks slightly down
            BABYLON.Vector3.Zero(), scene
        );
        camera.attachControl(canvas, true);
        camera.lowerRadiusLimit = 0.1;
        camera.upperRadiusLimit = 2000;
        camera.wheelPrecision = 50;

        // Initial light
        new BABYLON.HemisphericLight('light_init', new BABYLON.Vector3(0, 0, 1), scene);

        const ext = pluginExtension(fileExt);

        BABYLON.SceneLoader.Append('', fileUrl, scene,
            (_scene, _loadedScene) => {
                modelLoadState[canvasId] = 'loaded';
                console.log('[BabylonViewer] Model loaded successfully, meshes:', _scene.meshes.length);

                // ── Keep Y-up (no rotation needed — matches standard 3D printing preview) ──
                _scene.lights.forEach(l => l.dispose());

                // Lighting for Y-up scene (Y is up, Z is depth)
                const hemi = new BABYLON.HemisphericLight('hemi', new BABYLON.Vector3(0, 1, 0), _scene);
                hemi.intensity = isDark ? 0.5 : 0.7;
                hemi.specular = new BABYLON.Color3(0.1, 0.1, 0.1);
                hemi.groundColor = isDark
                    ? new BABYLON.Color3(0.08, 0.08, 0.10)
                    : new BABYLON.Color3(0.5, 0.5, 0.55);

                const dir = new BABYLON.DirectionalLight('dir', new BABYLON.Vector3(-1, -1, -2), _scene);
                dir.intensity = isDark ? 0.7 : 0.9;
                dir.position = new BABYLON.Vector3(20, 20, 40);

                // ── Compute bounding box ──
                const bb = computeSceneBounds(_scene);
                sceneBoundingBoxes[canvasId] = bb;

                if (bb) {
                    // Calculate mesh center
                    const centerX = (bb.min.x + bb.max.x) / 2;
                    const centerY = (bb.min.y + bb.max.y) / 2;
                    const centerZ = (bb.min.z + bb.max.z) / 2;
                    meshCenters[canvasId] = { x: centerX, y: centerY, z: centerZ };

                    // Center mesh at origin (Y-up: Y is vertical)
                    // X and Z centered horizontally, bottom of mesh sits on grid at Y=0
                    _scene.meshes.forEach(m => {
                        if (m.name !== '__grid__' && !m.name.startsWith('__axis')) {
                            m.position.x -= centerX;
                            m.position.y -= (centerY - bb.min.y);  // bottom of mesh at Y=0
                            m.position.z -= centerZ;
                        }
                    });

                    // Recompute bounding box after centering — this is the ground truth
                    const centeredBb = computeSceneBounds(_scene);
                    sceneBoundingBoxes[canvasId] = centeredBb;

                    // Recalculate mesh center AFTER recentering so camera targets correctly
                    if (centeredBb) {
                        meshCenters[canvasId] = {
                            x: (centeredBb.min.x + centeredBb.max.x) / 2,
                            y: (centeredBb.min.y + centeredBb.max.y) / 2,
                            z: (centeredBb.min.z + centeredBb.max.z) / 2
                        };
                    }

                    // ── Create ground grid on XZ plane (Y is up in Y-up) ──
                    const partW = centeredBb.max.x - centeredBb.min.x;
                    const partD = centeredBb.max.z - centeredBb.min.z;
                    const partH = centeredBb.max.y - centeredBb.min.y;
                    const maxDim = Math.max(partW, partD, partH, 1e-6);
                    const rawCell = maxDim / 10;
                    const mag = Math.pow(10, Math.floor(Math.log10(rawCell)));
                    const gridRatio = rawCell >= 5 * mag ? 5 * mag : rawCell >= 2 * mag ? 2 * mag : mag;
                    const majorStep = gridRatio * 5;
                    const halfSpan = Math.max(majorStep * 3, maxDim * 1.5);
                    const gridSize = halfSpan * 2;
                    const subdivisions = Math.max(Math.round(gridSize / gridRatio), 2);

                    const ground = BABYLON.MeshBuilder.CreateGround('__grid__',
                        { width: gridSize, height: gridSize, subdivisions: subdivisions }, _scene);
                    ground.position.y = 0;
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
                        : new BABYLON.Color3(0.65, 0.65, 0.70);
                    gridMat.opacity = 0.9;
                    ground.material = gridMat;
                }

                // ── Save original materials for all meshes ──
                const origMats = originalMaterials[canvasId];
                _scene.meshes.forEach(m => {
                    if (m.name !== '__grid__' && !m.name.startsWith('__axis') && m.material) {
                        origMats[m.uniqueId] = m.material;
                    }
                });

                // ── Configure camera to frame the centered mesh ──
                const cam = _scene.activeCamera;
                if (cam instanceof BABYLON.ArcRotateCamera) {
                    cam.useFramingBehavior = false;
                    const meshCenter = meshCenters[canvasId];
                    const newBb = sceneBoundingBoxes[canvasId];
                    if (newBb && meshCenter) {
                        const w = newBb.max.x - newBb.min.x; // width (X)
                        const d = newBb.max.y - newBb.min.y; // depth (Y)
                        const h = newBb.max.z - newBb.min.z; // height (Z)
                        // Include all 3 dimensions for correct camera distance
                        // 0.7x multiplier keeps camera close enough without clipping
                        const meshRadius = Math.max(Math.sqrt(w * w + d * d + h * h) * 0.7, 5);
                        cam.minZ = meshRadius * 0.001;
                        cam.maxZ = meshRadius * 1000;
                        cam.target = new BABYLON.Vector3(0, meshCenter.y, 0); // look at mesh bounding box center
                        cam.radius = meshRadius;
                        cam.lowerRadiusLimit = meshRadius * 0.1;
                        cam.upperRadiusLimit = meshRadius * 20;
                        // Higher wheelPrecision = slower scroll. Scale with meshRadius for consistency.
                        cam.wheelPrecision = Math.max(50, Math.round(meshRadius * 0.8));
                        cam.pinchPrecision = cam.wheelPrecision * 8;
                    } else {
                        cam.minZ = 0.001;
                        cam.lowerRadiusLimit = 0.001;
                        cam.wheelPrecision = 10;
                    }
                }

                // ── Axis gizmo in top-right corner ──
                const mainCam = _scene.activeCamera;
                createAxisGizmo(canvasId, _scene, mainCam);

                // ── ResizeObserver so expand-button doesn't stretch ──
                if (resizeObservers[canvasId]) {
                    resizeObservers[canvasId].disconnect();
                }
                const ro = new ResizeObserver(() => {
                    if (engines[canvasId]) engines[canvasId].resize();
                });
                ro.observe(canvas);
                resizeObservers[canvasId] = ro;

                setRenderMode(canvasId, 'solid');
            },
            null,
            (_scene, message, exception) => {
                modelLoadState[canvasId] = 'error';
                console.error('[BabylonViewer] Model load error:', message, exception);
            },
            ext
        );

        engine.runRenderLoop(() => scene.render());

        const resizeHandler = () => engine.resize();
        window.addEventListener('resize', resizeHandler);
        engine._resizeHandler = resizeHandler;

        engines[canvasId] = engine;
        scenes[canvasId] = scene;

    } catch (error) {
        modelLoadState[canvasId] = 'error';
        console.error('[BabylonViewer] Initialization failed:', error);
    }
}

/**
 * Creates a fixed XYZ axis indicator pinned to the top-right corner of the canvas.
 * For Y-up coordinate system: X=right, Y=up, Z=depth (into screen)
 */
function createAxisGizmo(canvasId, scene, mainCam) {
    // Dispose previous gizmo if any
    if (axisGizmoLayer[canvasId]) {
        axisGizmoLayer[canvasId].dispose();
        axisGizmoLayer[canvasId] = null;
    }
    ['__axisX__','__axisY__','__axisZ__','__axisXArr__','__axisYArr__','__axisZArr__'].forEach(n => {
        const m = scene.getMeshByName(n);
        if (m) m.dispose();
    });
    const oldAxesCam = scene.getCameraByName('__axesCam__');
    if (oldAxesCam) oldAxesCam.dispose();

    if (!(mainCam instanceof BABYLON.ArcRotateCamera)) return;

    const AXIS_LAYER = 0x10000000;
    const axisLen = 0.65;

    // Y-up: X=right (red), Y=up (green), Z=depth (blue)
    const COL_X = new BABYLON.Color3(0.93, 0.27, 0.27); // Red - X
    const COL_Y = new BABYLON.Color3(0.13, 0.77, 0.27); // Green - Y (up)
    const COL_Z = new BABYLON.Color3(0.22, 0.51, 0.96); // Blue - Z (depth)

    function makeAxis(name, pts, color) {
        const l = BABYLON.MeshBuilder.CreateLines(name, { points: pts }, scene);
        l.color = color;
        l.isPickable = false;
        l.layerMask = AXIS_LAYER;
        return l;
    }

    // Y-up: Y axis points UP, X points RIGHT, Z points DEPTH (into screen)
    makeAxis('__axisX__', [BABYLON.Vector3.Zero(), new BABYLON.Vector3(axisLen, 0, 0)], COL_X);
    makeAxis('__axisY__', [BABYLON.Vector3.Zero(), new BABYLON.Vector3(0, axisLen, 0)], COL_Y);
    makeAxis('__axisZ__', [BABYLON.Vector3.Zero(), new BABYLON.Vector3(0, 0, axisLen)], COL_Z);

    function makeCone(name, dir, color) {
        const d = dir.normalize();
        const cone = BABYLON.MeshBuilder.CreateCylinder(name, {
            diameterTop: 0, diameterBottom: 0.08, height: 0.18, tessellation: 8
        }, scene);
        const mat = new BABYLON.StandardMaterial(name + 'Mat', scene);
        mat.diffuseColor = color;
        mat.emissiveColor = color.scale(0.4);
        mat.backFaceCulling = false;
        cone.material = mat;
        cone.isPickable = false;
        cone.layerMask = AXIS_LAYER;
        // Point cone along direction (Y-up: up vector is Y-axis)
        const up = new BABYLON.Vector3(0, 1, 0); // Y-up
        const cross = BABYLON.Vector3.Cross(up, d);
        if (cross.length() > 0.001) {
            cone.rotationQuaternion = BABYLON.Quaternion.RotationAxis(cross.normalize(), Math.acos(BABYLON.Vector3.Dot(up, d)));
        } else if (BABYLON.Vector3.Dot(up, d) < 0) {
            cone.rotationQuaternion = BABYLON.Quaternion.RotationAxis(new BABYLON.Vector3(1, 0, 0), Math.PI);
        }
        cone.position = d.scale(axisLen + 0.09);
        return cone;
    }

    makeCone('__axisXArr__', new BABYLON.Vector3(1, 0, 0), COL_X);
    makeCone('__axisYArr__', new BABYLON.Vector3(0, 1, 0), COL_Y);
    makeCone('__axisZArr__', new BABYLON.Vector3(0, 0, 1), COL_Z);

    // Axes camera — orthographic, renders only AXIS_LAYER meshes in top-right corner
    const hw = 1.1;
    const axesCam = new BABYLON.ArcRotateCamera('__axesCam__', mainCam.alpha, mainCam.beta, 3.5, BABYLON.Vector3.Zero(), scene);
    axesCam.mode = BABYLON.Camera.ORTHOGRAPHIC_CAMERA;
    axesCam.orthoLeft = -hw; axesCam.orthoRight = hw;
    axesCam.orthoTop = hw; axesCam.orthoBottom = -hw;
    axesCam.viewport = new BABYLON.Viewport(0.78, 0.76, 0.22, 0.24); // top-right corner
    axesCam.layerMask = AXIS_LAYER;
    axesCam.minZ = 0.01;
    axesCam.maxZ = 100;

    // Sync rotation with main camera every frame
    const obs = scene.onBeforeRenderObservable.add(() => {
        axesCam.alpha = mainCam.alpha;
        axesCam.beta = mainCam.beta;
    });

    // Render both cameras — main first, axes composited on top in its corner viewport
    scene.activeCameras = [mainCam, axesCam];

    axisGizmoLayer[canvasId] = {
        dispose: () => {
            scene.onBeforeRenderObservable.remove(obs);
            scene.activeCameras = [mainCam];
            axesCam.dispose();
            ['__axisX__','__axisY__','__axisZ__','__axisXArr__','__axisYArr__','__axisZArr__'].forEach(n => {
                const m = scene.getMeshByName(n);
                if (m) m.dispose();
            });
        }
    };
}

/**
 * Sets solid / wireframe / transparent rendering mode.
 * @param {string} canvasId
 * @param {'solid'|'wireframe'|'transparent'} mode
 */
export function setRenderMode(canvasId, mode) {
    const scene = scenes[canvasId];
    if (!scene) return;
    const origMats = originalMaterials[canvasId] ?? {};

    scene.meshes.forEach(mesh => {
        if (mesh.name === '__grid__' || mesh.name.startsWith('__axis')) return;

        if (mode === 'wireframe') {
            // Save original material if not already saved
            if (!origMats[mesh.uniqueId] && mesh.material) {
                origMats[mesh.uniqueId] = mesh.material;
            }
            const wm = new BABYLON.StandardMaterial('wire_' + mesh.uniqueId, scene);
            wm.wireframe = true;
            // Lighter wireframe color - grayish white
            wm.diffuseColor = new BABYLON.Color3(0.7, 0.7, 0.75);
            wm.emissiveColor = new BABYLON.Color3(0.2, 0.2, 0.22);
            wm.backFaceCulling = false;
            mesh.material = wm;
            try { mesh.disableEdgesRendering(); } catch (_) {}
        } else if (mode === 'transparent') {
            // Save original material if not already saved
            if (!origMats[mesh.uniqueId] && mesh.material) {
                origMats[mesh.uniqueId] = mesh.material;
            }
            const isPBR = mesh.material instanceof BABYLON.PBRMaterial;
            let tm;
            if (isPBR) {
                const pbr = mesh.material;
                tm = new BABYLON.PBRMaterial('xray_' + mesh.uniqueId, scene);
                tm.albedoColor = pbr.albedoColor ? pbr.albedoColor.scale(0.6) : new BABYLON.Color3(0.7, 0.7, 0.7);
                tm.alpha = 0.4;
                tm.transparencyMode = BABYLON.PBRMaterial.PBRMATERIAL_ALPHABLEND;
                tm.backFaceCulling = false;
                tm.twoSidedLighting = true;
                tm.metallic = pbr.metallic ?? 0;
                tm.roughness = pbr.roughness ?? 0.5;
            } else {
                tm = new BABYLON.StandardMaterial('xray_' + mesh.uniqueId, scene);
                if (mesh.material && mesh.material.diffuseColor) {
                    tm.diffuseColor = mesh.material.diffuseColor.scale(0.6);
                } else {
                    tm.diffuseColor = new BABYLON.Color3(0.7, 0.7, 0.7);
                }
                tm.alpha = 0.4;
                tm.transparencyMode = BABYLON.Engine.ALPHA_COMBINE;
                tm.backFaceCulling = false;
                tm.emissiveColor = new BABYLON.Color3(0.15, 0.15, 0.15);
            }
            mesh.material = tm;
            // Don't enable edges in transparent mode - they cause black artifacts
            try { mesh.disableEdgesRendering(); } catch (_) {}
        } else {
            // solid — restore original material
            if (origMats[mesh.uniqueId]) {
                mesh.material = origMats[mesh.uniqueId];
            }
            try { mesh.disableEdgesRendering(); } catch (_) {}
        }
    });
}

/**
 * Resets camera to a bounding-box-fitted default isometric view.
 * @param {string} canvasId
 */
export function resetCamera(canvasId) {
    const scene = scenes[canvasId];
    if (!scene) return;
    const bb = sceneBoundingBoxes[canvasId];
    const meshCenter = meshCenters[canvasId];
    const cam = scene.activeCamera;
    if (cam instanceof BABYLON.ArcRotateCamera) {
        cam.useFramingBehavior = false;
        cam.alpha = Math.PI / 4;
        cam.beta = Math.PI / 3; // 60° from horizontal (looking down)
        if (bb && meshCenter) {
            const w = bb.max.x - bb.min.x; // width (X)
            const d = bb.max.y - bb.min.y; // depth (Y)
            const h = bb.max.z - bb.min.z; // height (Z)
            const meshRadius = Math.max(Math.sqrt(w * w + d * d + h * h) * 1.2, 5);
            cam.minZ = meshRadius * 0.001;
            cam.maxZ = meshRadius * 1000;
            cam.target = new BABYLON.Vector3(0, meshCenter.y, 0); // look at mesh bounding box center
            cam.radius = meshRadius;
            cam.lowerRadiusLimit = meshRadius * 0.01;
            cam.upperRadiusLimit = meshRadius * 20;
            cam.wheelPrecision = Math.max(5, Math.round(meshRadius * 0.3));
            cam.pinchPrecision = cam.wheelPrecision * 10;
        }
        if (!cam.inputs.attached.keyboard) {
            cam.attachControl(document.getElementById(canvasId), true);
        }
    }
}

/**
 * Disposes engine and scene, removes resize listener and overlays.
 * @param {string} canvasId
 */
export function dispose(canvasId) {
    const engine = engines[canvasId];
    const scene = scenes[canvasId];

    if (engine?._resizeHandler) {
        window.removeEventListener('resize', engine._resizeHandler);
    }

    if (resizeObservers[canvasId]) {
        resizeObservers[canvasId].disconnect();
        delete resizeObservers[canvasId];
    }

    if (axisGizmoLayer[canvasId]) {
        try { axisGizmoLayer[canvasId].dispose(); } catch (_) {}
        delete axisGizmoLayer[canvasId];
    }

    const legend = document.getElementById(canvasId + '_axis_legend');
    if (legend) legend.remove();

    clearBoundingBox(canvasId);

    scene?.dispose();
    engine?.dispose();

    delete scenes[canvasId];
    delete engines[canvasId];
    delete darkModes[canvasId];
    delete modelLoadState[canvasId];
    delete originalMaterials[canvasId];
    delete sceneBoundingBoxes[canvasId];
    delete meshCenters[canvasId];
}

/**
 * Sets camera to a predefined view angle (Y-up coordinate system).
 * In Y-up: X=right, Y=up, Z=depth (into screen)
 * @param {string} canvasId
 * @param {'front'|'back'|'left'|'right'|'top'|'bottom'|'iso'} preset
 */
export function setCameraPreset(canvasId, preset) {
    const scene = scenes[canvasId];
    if (!scene) return;
    if (!scene.activeCamera) return;
    const cam = scene.activeCamera;
    if (!(cam instanceof BABYLON.ArcRotateCamera)) return;

    // Z-up camera presets
    // Y-up camera presets: X=right, Y=up, Z=depth
    // beta=PI/2 means camera is horizontal; beta=0 means looking down from above
    const presets = {
        front:  { alpha: 0,            beta: Math.PI / 2 },     // looking at Y+ (front)
        back:   { alpha: Math.PI,      beta: Math.PI / 2 },   // looking at Y- (back)
        right:  { alpha: Math.PI / 2,  beta: Math.PI / 2 },   // looking at X+ (right)
        left:   { alpha: -Math.PI / 2, beta: Math.PI / 2 },  // looking at X- (left)
        top:    { alpha: 0,            beta: 0.01 },           // looking at Y+ from above
        bottom: { alpha: 0,            beta: Math.PI - 0.01 }, // looking at Y- from below
        iso:    { alpha: Math.PI / 4,  beta: Math.PI / 3 },   // isometric 60° elevation (Y-up)
    };
    const p = presets[preset];
    if (p) {
        cam.alpha = p.alpha;
        cam.beta = p.beta;
    }
}

/**
 * Toggles edge rendering on all non-grid meshes.
 * @param {string} canvasId
 * @param {boolean} enabled
 */
export function toggleEdges(canvasId, enabled) {
    const scene = scenes[canvasId];
    if (!scene) return;
    const edgeColor = new BABYLON.Color4(0.5, 0.5, 0.5, 1); // Light gray edges
    scene.meshes.forEach(mesh => {
        if (mesh.name === '__grid__' || mesh.name.startsWith('__axis')) return;
        if (enabled) {
            try {
                // First disable to clear any previous edge state
                mesh.disableEdgesRendering();
                // Epsilon 0.9 = only render edges at >90° angles (sharp exterior edges)
                // This avoids rendering interior mesh lines and creases
                mesh.enableEdgesRendering(0.9);
                mesh.edgesWidth = 4.0;
                mesh.edgesColor = edgeColor;
            } catch (_) {}
        } else {
            try { mesh.disableEdgesRendering(); } catch (_) {}
        }
    });
}

/**
 * Toggles bounding box display with dimension labels.
 * @param {string} canvasId
 * @param {boolean} enabled
 */
export function toggleBoundingBox(canvasId, enabled) {
    if (!enabled) {
        clearBoundingBox(canvasId);
        return;
    }

    const scene = scenes[canvasId];
    const engine = engines[canvasId];
    const canvas = document.getElementById(canvasId);
    if (!scene || !engine || !canvas) return;

    clearBoundingBox(canvasId);

    const bb = sceneBoundingBoxes[canvasId];
    if (!bb) return;

    const v3 = (x, y, z) => new BABYLON.Vector3(x, y, z);

    // 12 edges of the bounding box (centered at origin after offset)
    const edges = [
        // X-axis edges (4)
        [v3(bb.min.x, bb.min.y, bb.min.z), v3(bb.max.x, bb.min.y, bb.min.z)],
        [v3(bb.min.x, bb.max.y, bb.min.z), v3(bb.max.x, bb.max.y, bb.min.z)],
        [v3(bb.min.x, bb.min.y, bb.max.z), v3(bb.max.x, bb.min.y, bb.max.z)],
        [v3(bb.min.x, bb.max.y, bb.max.z), v3(bb.max.x, bb.max.y, bb.max.z)],
        // Y-axis edges (4)
        [v3(bb.min.x, bb.min.y, bb.min.z), v3(bb.min.x, bb.max.y, bb.min.z)],
        [v3(bb.max.x, bb.min.y, bb.min.z), v3(bb.max.x, bb.max.y, bb.min.z)],
        [v3(bb.min.x, bb.min.y, bb.max.z), v3(bb.min.x, bb.max.y, bb.max.z)],
        [v3(bb.max.x, bb.min.y, bb.max.z), v3(bb.max.x, bb.max.y, bb.max.z)],
        // Z-axis edges (4)
        [v3(bb.min.x, bb.min.y, bb.min.z), v3(bb.min.x, bb.min.y, bb.max.z)],
        [v3(bb.max.x, bb.min.y, bb.min.z), v3(bb.max.x, bb.min.y, bb.max.z)],
        [v3(bb.min.x, bb.max.y, bb.min.z), v3(bb.min.x, bb.max.y, bb.max.z)],
        [v3(bb.max.x, bb.max.y, bb.min.z), v3(bb.max.x, bb.max.y, bb.max.z)],
    ];

    // Yellow lines
    const colors = edges.map(() => [
        new BABYLON.Color4(1, 0.85, 0, 1),
        new BABYLON.Color4(1, 0.85, 0, 1)
    ]);

    const lineMesh = BABYLON.MeshBuilder.CreateLineSystem('bbox_lines', {
        lines: edges,
        colors: colors,
        updatable: false
    }, scene);
    lineMesh.isPickable = false;
    bboxLines[canvasId] = lineMesh;

    // Dimension labels
    const container = canvas.parentElement;
    if (!container) return;

    const cam = scene.activeCamera;
    if (!cam) return;

    const rw = engine.getRenderWidth();
    const rh = engine.getRenderHeight();
    const transformMatrix = scene.getTransformMatrix();

    const labels = [];

    // X dimension (width) — bottom front edge, centered on X axis
    const xMid = BABYLON.Vector3.Lerp(v3(bb.min.x, bb.min.y, bb.min.z), v3(bb.max.x, bb.min.y, bb.min.z), 0.5);
    const xDim = (bb.max.x - bb.min.x).toFixed(1);
    labels.push({ worldPos: xMid, label: `X: ${xDim}`, color: '#f0f0f0' });

    // Y dimension (depth) — front face center, clearly visible at front of mesh
    const yMid = BABYLON.Vector3.Lerp(v3(bb.min.x, bb.min.y, bb.min.z), v3(bb.min.x, bb.max.y, bb.min.z), 0.5);
    const yDim = (bb.max.y - bb.min.y).toFixed(1);
    labels.push({ worldPos: yMid, label: `Y: ${yDim}`, color: '#f0f0f0' });

    // Z dimension (height) — top face center, clearly visible above the mesh
    const zMid = new BABYLON.Vector3(0, 0, bb.max.z + 2);
    const zDim = (bb.max.z - bb.min.z).toFixed(1);
    labels.push({ worldPos: zMid, label: `Z: ${zDim}`, color: '#f0f0f0' });

    labels.forEach(({ worldPos, label, color }) => {
        const projected = BABYLON.Vector3.Project(
            worldPos,
            BABYLON.Matrix.Identity(),
            transformMatrix,
            cam.viewport.toGlobal(rw, rh)
        );

        if (projected.z < 0 || projected.z > 1) return;

        const div = document.createElement('div');
        div.textContent = label;
        div.style.cssText = `
            position: absolute;
            transform: translate(-50%, -50%);
            left: ${projected.x}px;
            top: ${projected.y}px;
            background: rgba(0, 0, 0, 0.75);
            color: ${color};
            font-size: 11px;
            font-weight: 600;
            padding: 2px 6px;
            border-radius: 3px;
            pointer-events: none;
            white-space: nowrap;
            z-index: 20;
        `;
        container.appendChild(div);
    });

    bboxLabels[canvasId] = labels;
}

/**
 * Removes bounding box lines and dimension labels.
 */
function clearBoundingBox(canvasId) {
    if (bboxLines[canvasId]) {
        bboxLines[canvasId].dispose();
        delete bboxLines[canvasId];
    }
    if (bboxLabels[canvasId]) {
        bboxLabels[canvasId].forEach(el => {
            if (el.remove) el.remove();
        });
        delete bboxLabels[canvasId];
    }
}

// Debug handle
window.babylonViewer = {
    initialize, setRenderMode, resetCamera, dispose,
    setCameraPreset, toggleEdges, toggleBoundingBox
};
