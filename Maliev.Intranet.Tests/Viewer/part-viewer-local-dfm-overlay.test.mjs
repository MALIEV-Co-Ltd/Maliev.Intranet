import assert from 'node:assert/strict';
import fs from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

// ── Minimal viewer context for toggleLocalDfmOverlay ─────────────────────────

class Vector3 {
    constructor(x = 0, y = 0, z = 0) {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}

function createElement(tagName = '') {
    if (String(tagName).toLowerCase() === 'canvas') {
        return { width: 0, height: 0, getContext: () => null };
    }
    return {
        className: '',
        innerHTML: '',
        style: {},
        textContent: '',
        appendChild: () => {},
        remove: () => {},
    };
}

function loadViewerContext() {
    const context = {
        console,
        document: {
            body: { appendChild: () => {} },
            createElement,
            getElementById: () => null,
        },
        Path2D: class Path2D {},
        requestAnimationFrame: callback => callback(),
        window: {},
        globalThis: {},
        BABYLON: {
            Axis: { X: new Vector3(1, 0, 0), Y: new Vector3(0, 1, 0), Z: new Vector3(0, 0, 1) },
            Color3: class Color3 {
                constructor(r = 0, g = 0, b = 0) { this.r = r; this.g = g; this.b = b; }
            },
            Material: { MATERIAL_OPAQUE: 0, MATERIAL_ALPHABLEND: 2, MATERIAL_ALPHATEST: 4 },
            Mesh: class Mesh {
                constructor(name, scene) {
                    this.name = name;
                    this.material = null;
                    this.isPickable = true;
                    this.isVisible = true;
                    this.metadata = {};
                    this.disposed = false;
                    this.dispose = () => { this.disposed = true; };
                    scene?.meshes?.push(this);
                }
            },
            PBRMaterial: class PBRMaterial {
                constructor(name) { this.name = name; }
            },
            Vector3,
            VertexBuffer: { PositionKind: 'position' },
            VertexData: class VertexData {
                applyToMesh(mesh) { mesh.vertexData = this; }
                static ComputeNormals(positions, indices, normals) {
                    for (let index = 0; index < positions.length; index += 1) normals.push(0);
                }
            },
        },
    };
    context.globalThis = context;
    vm.createContext(context);

    const viewerPath = new URL('../../Maliev.Intranet.Client/wwwroot/js/part-viewer.js', import.meta.url);
    const code = fs.readFileSync(viewerPath, 'utf8').replaceAll(/\bexport\s+/g, '');
    vm.runInContext(code, context);
    return context;
}

function makeModelMesh(name, positions, indices, uniqueId) {
    return {
        name,
        uniqueId,
        isPickable: true,
        isVisible: true,
        metadata: { malievModelMesh: true },
        getTotalVertices: () => positions.length / 3,
        getVerticesData: kind => (kind === 'position' ? positions : null),
        getIndices: () => indices,
        isEnabled: () => true,
        // No getWorldMatrix → collectAdvisoryMeshBuffers falls back to raw positions.
    };
}

function setupScene(context, canvasId, meshes) {
    context.__meshes = meshes;
    vm.runInContext(
        `scenes[${JSON.stringify(canvasId)}] = { meshes: __meshes };`,
        context);
}

// Two triangles: t0 = unit triangle at z=0, t1 = triangle at z=5.
const MESH_A_POSITIONS = [
    0, 0, 0,  1, 0, 0,  0, 1, 0,
    0, 0, 5,  1, 0, 5,  0, 1, 5,
];
const MESH_A_INDICES = [0, 1, 2, 3, 4, 5];

// One triangle at x=100 — global face index 2 when collected after mesh A.
const MESH_B_POSITIONS = [100, 0, 0, 101, 0, 0, 100, 1, 0];
const MESH_B_INDICES = [0, 1, 2];

test('toggleLocalDfmOverlay builds a world-space overlay mesh from face indices', () => {
    const context = loadViewerContext();
    setupScene(context, 'c1', [makeModelMesh('part', MESH_A_POSITIONS, MESH_A_INDICES, 11)]);

    vm.runInContext("toggleLocalDfmOverlay('c1', 'file1', 'FDM__overhang', [1], true)", context);

    const meshes = context.__meshes;
    const overlay = meshes.find(m => m.name === '__local_dfm_overlay__FDM__overhang');
    assert.ok(overlay, 'overlay mesh must be added to the scene');
    // Face index 1 = mesh A's second triangle (the z=5 one).
    assert.deepEqual(Array.from(overlay.vertexData.positions), [0, 0, 5, 1, 0, 5, 0, 1, 5]);
    assert.deepEqual(Array.from(overlay.vertexData.indices), [0, 1, 2]);
    assert.equal(overlay.isPickable, false);
    assert.equal(overlay.alwaysSelectAsActiveMesh, true);
    assert.equal(overlay.metadata.malievAnalysisHelper, true);
    // Default red DFM style (overhang has no per-category override).
    assert.deepEqual(
        {
            r: overlay.material.albedoColor.r,
            g: overlay.material.albedoColor.g,
            b: overlay.material.albedoColor.b,
        },
        { r: 0.95, g: 0.10, b: 0.05 });
    assert.equal(overlay.material.alpha, 0.55);
    assert.equal(overlay.material.transparencyMode, context.BABYLON.Material.MATERIAL_ALPHABLEND);
    assert.equal(overlay.material.needDepthPrePass, false);
    assert.equal(overlay.material.forceDepthWrite, false);
    assert.equal(overlay.material.zOffset, -2);
});

test('toggleLocalDfmOverlay maps face indices across concatenated mesh buffers', () => {
    const context = loadViewerContext();
    setupScene(context, 'c2', [
        makeModelMesh('bodyA', MESH_A_POSITIONS, MESH_A_INDICES, 21),
        makeModelMesh('bodyB', MESH_B_POSITIONS, MESH_B_INDICES, 22),
    ]);

    vm.runInContext("toggleLocalDfmOverlay('c2', 'file2', 'FDM__overhang', [2], true)", context);

    const overlay = context.__meshes.find(m => m.name === '__local_dfm_overlay__FDM__overhang');
    assert.ok(overlay, 'overlay mesh must be added to the scene');
    // Global face index 2 lands on bodyB's only triangle.
    assert.deepEqual(Array.from(overlay.vertexData.positions), MESH_B_POSITIONS);
});

test('toggleLocalDfmOverlay reuses the built mesh when toggling visibility', () => {
    const context = loadViewerContext();
    setupScene(context, 'c3', [makeModelMesh('part', MESH_A_POSITIONS, MESH_A_INDICES, 31)]);

    vm.runInContext("toggleLocalDfmOverlay('c3', 'file3', 'FDM__overhang', [0], true)", context);
    vm.runInContext("toggleLocalDfmOverlay('c3', 'file3', 'FDM__overhang', [0], false)", context);

    const overlays = context.__meshes.filter(m => m.name === '__local_dfm_overlay__FDM__overhang');
    assert.equal(overlays.length, 1, 'hide must reuse the existing overlay mesh');
    assert.equal(overlays[0].isVisible, false);

    vm.runInContext("toggleLocalDfmOverlay('c3', 'file3', 'FDM__overhang', [0], true)", context);
    assert.equal(overlays[0].isVisible, true);
    assert.equal(
        context.__meshes.filter(m => m.name === '__local_dfm_overlay__FDM__overhang').length,
        1,
        're-show must not build a second overlay mesh');
});

test('toggleLocalDfmOverlay builds nothing for a hide request or empty face indices', () => {
    const context = loadViewerContext();
    setupScene(context, 'c4', [makeModelMesh('part', MESH_A_POSITIONS, MESH_A_INDICES, 41)]);

    vm.runInContext("toggleLocalDfmOverlay('c4', 'file4', 'FDM__overhang', [0], false)", context);
    vm.runInContext("toggleLocalDfmOverlay('c4', 'file4', 'FDM__thin_wall', [], true)", context);
    vm.runInContext("toggleLocalDfmOverlay('c4', 'file4', 'FDM__bogus', [999], true)", context);

    assert.equal(
        context.__meshes.filter(m => m.name.startsWith('__local_dfm_overlay__')).length,
        0,
        'no overlay mesh may be created for hide requests, empty or out-of-range indices');
});

test('clearDfmOverlays disposes locally generated overlay meshes', () => {
    const context = loadViewerContext();
    setupScene(context, 'c5', [makeModelMesh('part', MESH_A_POSITIONS, MESH_A_INDICES, 51)]);

    vm.runInContext("toggleLocalDfmOverlay('c5', 'file5', 'FDM__overhang', [0], true)", context);
    const overlay = context.__meshes.find(m => m.name === '__local_dfm_overlay__FDM__overhang');
    assert.ok(overlay);

    vm.runInContext("clearDfmOverlays('c5', 'file5')", context);
    assert.equal(overlay.disposed, true);
});

test('collectAdvisoryMeshBuffers swaps triangle winding under mirroring world matrices', () => {
    const context = loadViewerContext();

    // GLB roots get a negative-determinant (mirroring) world matrix from
    // BabylonJS handedness conversion. Once positions are baked to world
    // space, the stored winding is inverted — cross-product normals in the
    // DFM worker would point inward and upward faces would be flagged as
    // overhangs. The collector must re-orient triangles.
    const mirrored = makeModelMesh('part', MESH_A_POSITIONS, MESH_A_INDICES, 21);
    mirrored.computeWorldMatrix = () => {};
    mirrored.getWorldMatrix = () => ({ determinant: () => -1 });
    setupScene(context, 'cw', [mirrored]);

    const buffers = vm.runInContext("collectAdvisoryMeshBuffers('cw')", context);
    assert.equal(buffers.length, 1);
    assert.deepEqual(Array.from(buffers[0].indices), [0, 2, 1, 3, 5, 4]);

    // A regular (positive determinant) matrix keeps the original winding.
    const regular = makeModelMesh('part2', MESH_A_POSITIONS, MESH_A_INDICES, 22);
    regular.computeWorldMatrix = () => {};
    regular.getWorldMatrix = () => ({ determinant: () => 1 });
    setupScene(context, 'cw2', [regular]);

    const regularBuffers = vm.runInContext("collectAdvisoryMeshBuffers('cw2')", context);
    assert.deepEqual(Array.from(regularBuffers[0].indices), [0, 1, 2, 3, 4, 5]);
});
