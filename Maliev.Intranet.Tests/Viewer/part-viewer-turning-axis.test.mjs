import assert from 'node:assert/strict';
import fs from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

class Vector3 {
    constructor(x = 0, y = 0, z = 0) {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    clone() {
        return new Vector3(this.x, this.y, this.z);
    }

    add(other) {
        return new Vector3(this.x + other.x, this.y + other.y, this.z + other.z);
    }

    subtract(other) {
        return new Vector3(this.x - other.x, this.y - other.y, this.z - other.z);
    }

    scale(value) {
        return new Vector3(this.x * value, this.y * value, this.z * value);
    }

    length() {
        return Math.hypot(this.x, this.y, this.z);
    }

    normalize() {
        const length = this.length();
        if (length > 1e-12) {
            this.x /= length;
            this.y /= length;
            this.z /= length;
        }

        return this;
    }

    static Dot(a, b) {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    static Cross(a, b) {
        return new Vector3(
            a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x);
    }

    static TransformCoordinates(point, matrix) {
        if (matrix?.type === 'rotationX') {
            const cos = Math.cos(matrix.angle);
            const sin = Math.sin(matrix.angle);
            return new Vector3(
                point.x,
                point.y * cos - point.z * sin,
                point.y * sin + point.z * cos);
        }

        return new Vector3(point.x, point.y, point.z);
    }

    static TransformNormal(point, matrix) {
        return Vector3.TransformCoordinates(point, matrix);
    }
}

function loadViewerContext() {
    const context = {
        console,
        window: {},
        globalThis: {},
        BABYLON: {
            Vector3,
            Axis: {
                X: new Vector3(1, 0, 0),
                Y: new Vector3(0, 1, 0),
                Z: new Vector3(0, 0, 1),
            },
            Matrix: {
                RotationX: angle => ({ type: 'rotationX', angle }),
            },
            VertexBuffer: {
                PositionKind: 'position',
            },
            MaterialPluginBase: class MaterialPluginBase {
                constructor() {
                    this._isEnabled = false;
                }

                set isEnabled(value) {
                    this._isEnabled = value;
                }

                get isEnabled() {
                    return this._isEnabled;
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

function viewerSource() {
    const viewerPath = new URL('../../Maliev.Intranet.Client/wwwroot/js/part-viewer.js', import.meta.url);
    return fs.readFileSync(viewerPath, 'utf8');
}

function partDetailCardSource() {
    const partDetailCardPath = new URL('../../Maliev.Intranet.Client/Components/Project/PartDetailCard.razor', import.meta.url);
    return fs.readFileSync(partDetailCardPath, 'utf8');
}

function addRing(positions, axisX, centerY, centerZ, radius, count) {
    for (let index = 0; index < count; index += 1) {
        const angle = (Math.PI * 2 * index) / count;
        positions.push(
            axisX,
            centerY + Math.cos(angle) * radius,
            centerZ + Math.sin(angle) * radius);
    }
}

function buildScene(positions) {
    const mesh = {
        name: 'part',
        uniqueId: 1,
        getVerticesData: () => positions,
        getTotalVertices: () => positions.length / 3,
        computeWorldMatrix: () => {},
        getWorldMatrix: () => ({}),
    };

    return { meshes: [mesh] };
}

test('turning axis resolver uses concentric bore instead of larger exterior ring center', () => {
    const context = loadViewerContext();
    const positions = [];

    addRing(positions, -6, 0, 0, 1.4, 48);
    addRing(positions, 6, 0, 0, 1.4, 48);

    addRing(positions, -6, 2.0, 0, 9.0, 160);
    addRing(positions, 0, 2.0, 0, 9.0, 160);
    addRing(positions, 6, 2.0, 0, 9.0, 160);

    context.scene = buildScene(positions);
    context.bb = {
        min: { x: -6, y: -7, z: -9 },
        max: { x: 6, y: 11, z: 9 },
    };
    context.fallbackCenter = new Vector3(0, 2, 0);

    const result = vm.runInContext(
        "resolveTurningAxis(scene, 'viewer', 'X', [1, 0, 0], null, bb, fallbackCenter)",
        context);

    assert.ok(Math.abs(result.direction.x) > 0.98);
    assert.ok(Math.abs(result.center.y) < 0.15, `expected bore center y=0, got ${result.center.y}`);
    assert.ok(Math.abs(result.center.z) < 0.15, `expected bore center z=0, got ${result.center.z}`);
});

test('Z-up backend turning axis remains vertical in viewer Z-up coordinates', () => {
    const context = loadViewerContext();
    const positions = [];

    addRing(positions, -1, 0, 0, 7.5, 96);
    addRing(positions, 1, 0, 0, 7.5, 96);

    context.scene = buildScene(positions);
    context.bb = {
        min: { x: -7.5, y: -7.5, z: 0 },
        max: { x: 7.5, y: 7.5, z: 7 },
    };
    context.fallbackCenter = new Vector3(0, 0, 3.5);

    const result = vm.runInContext(
        "resolveTurningAxis(scene, 'viewer', 'Z', [0, 0, 1], null, bb, fallbackCenter)",
        context);

    assert.ok(Math.abs(result.direction.x) < 0.02, `expected direction x=0, got ${result.direction.x}`);
    assert.ok(Math.abs(result.direction.y) < 0.02, `expected direction y=0, got ${result.direction.y}`);
    assert.ok(Math.abs(result.direction.z) > 0.98, `expected direction z=1, got ${result.direction.z}`);
});

test('visible ring center wins over uncentered backend turning axis point', () => {
    const context = loadViewerContext();
    const positions = [];

    addRing(positions, -6, 0, 0, 1.4, 48);
    addRing(positions, 6, 0, 0, 1.4, 48);

    context.scene = buildScene(positions);
    context.bb = {
        min: { x: -6, y: -2, z: -2 },
        max: { x: 6, y: 2, z: 2 },
    };
    context.fallbackCenter = new Vector3(0, 0, 0);

    const result = vm.runInContext(
        "resolveTurningAxis(scene, 'viewer', 'X', [1, 0, 0], [10, -6, 0], bb, fallbackCenter)",
        context);

    assert.ok(Math.abs(result.center.y) < 0.15, `expected visible center y=0, got ${result.center.y}`);
    assert.ok(Math.abs(result.center.z) < 0.15, `expected visible center z=0, got ${result.center.z}`);
});

test('explicit turning axis uses low-resolution bore center instead of bounding-box center', () => {
    const context = loadViewerContext();
    const positions = [];

    addRing(positions, -1, 0, 0, 1.4, 12);
    addRing(positions, 1, 0, 0, 1.4, 12);

    addRing(positions, -1, 2.0, 0, 9.0, 60);
    addRing(positions, 1, 2.0, 0, 9.0, 60);

    context.scene = buildScene(positions);
    context.bb = {
        min: { x: -1, y: -7, z: -9 },
        max: { x: 1, y: 11, z: 9 },
    };
    context.fallbackCenter = new Vector3(0, 2, 0);

    const result = vm.runInContext(
        "resolveTurningAxis(scene, 'viewer', 'X', [1, 0, 0], null, bb, fallbackCenter)",
        context);

    assert.ok(Math.abs(result.direction.x) > 0.98);
    assert.ok(Math.abs(result.center.y) < 0.2, `expected bore center y=0, got ${result.center.y}`);
    assert.ok(Math.abs(result.center.z) < 0.2, `expected bore center z=0, got ${result.center.z}`);
});

test('turning axis is only requested from GeometryService report data', () => {
    const cardSource = partDetailCardSource();
    const source = viewerSource();

    assert.equal(cardSource.includes('BuildProvisionalTurningAxis'), false);
    assert.equal(cardSource.includes('"AUTO"'), false);
    assert.equal(source.includes("axis === 'AUTO'"), false);
});

test('turning axis renderer accepts backend axis point', () => {
    const source = viewerSource();

    assert.match(source, /setTurningAxis\(canvasId,\s*primaryAxis,\s*axisVector,\s*axisPoint/);
    assert.match(source, /buildTurningAxisGeometry\(canvasId,\s*primaryAxis,\s*axisVector,\s*axisPoint/);
    assert.match(source, /transformBackendAxisPoint/);
});

test('viewer initialization applies persisted visual toolbar state', () => {
    const source = viewerSource();

    assert.match(source, /initialize\(canvasId,\s*fileUrl,\s*fileExt,\s*isDark,\s*knownDimsMm,\s*dotNetRef,\s*viewerSettings/);
    assert.match(source, /toggleEdges\(canvasId,\s*!!viewerSettings\.edgesEnabled\)/);
    assert.match(source, /viewerSettings\.gridEnabled\s*\?\s*showGrid\(canvasId\)\s*:\s*hideGrid\(canvasId\)/);
});

test('section view rebuild is throttled and disposes hatch/ghost meshes', () => {
    const source = viewerSource();

    assert.match(source, /scheduleSectionRebuild/);
    assert.match(source, /disposeSectionVisuals/);
    assert.match(source, /sectionHatchMeshes\[canvasId\]/);
    assert.match(source, /sectionGhostMeshes\[canvasId\]/);
});

test('turning axis overlay does not render a CW label', () => {
    assert.equal(viewerSource().includes("createTurningAxisLabel('CW')"), false);
});

test('turning axis overlay is subtle and has no arrowheads', () => {
    const source = viewerSource();

    assert.equal(source.includes("line.setAttribute('marker-start'"), false);
    assert.equal(source.includes("line.setAttribute('marker-end'"), false);
    assert.equal(source.includes("line.setAttribute('opacity', '0.45')"), true);
});

test('viewer disposal invalidates stale render work before releasing Babylon resources', () => {
    const source = viewerSource();

    assert.match(source, /loadGenerations\[canvasId\] = \(loadGenerations\[canvasId\] \|\| 0\) \+ 1;\s+const engine = engines\[canvasId\];/);
    assert.match(source, /engine\?\.stopRenderLoop\(\)/);
    assert.equal(source.includes('delete loadGenerations[canvasId]'), false);
});

test('render loop ignores stale engines from previous part loads', () => {
    const source = viewerSource();

    assert.match(source, /engine\.runRenderLoop\(\(\) => \{\s+if \(loadGenerations\[canvasId\] !== currentGen \|\| engines\[canvasId\] !== engine \|\| scenes\[canvasId\] !== scene\)/);
});

test('viewer registers engines before asynchronous model loading can be superseded', () => {
    const source = viewerSource();
    const sceneCreation = source.indexOf('const scene  = new BABYLON.Scene(engine);');
    const engineRegistration = source.indexOf('engines[canvasId] = engine;');
    const sceneRegistration = source.indexOf('scenes[canvasId]  = scene;');
    const modelPrefetch = source.indexOf('let _resolvedUrl = fileUrl;');

    assert.notEqual(sceneCreation, -1, 'Scene creation was not found.');
    assert.notEqual(engineRegistration, -1, 'Engine registration was not found.');
    assert.notEqual(sceneRegistration, -1, 'Scene registration was not found.');
    assert.notEqual(modelPrefetch, -1, 'GLB prefetch boundary was not found.');
    assert.ok(engineRegistration > sceneCreation, 'Engine must be registered after it is created.');
    assert.ok(sceneRegistration > sceneCreation, 'Scene must be registered after it is created.');
    assert.ok(engineRegistration < modelPrefetch, 'Engine must be registered before awaited GLB prefetch/load work.');
    assert.ok(sceneRegistration < modelPrefetch, 'Scene must be registered before awaited GLB prefetch/load work.');
});

test('viewer reveals a fitted model before expensive render-mode startup work', () => {
    const source = viewerSource();
    const revealHelper = source.indexOf('function revealCanvasAfterInitialFit');
    const revealCall = source.indexOf('revealCanvasAfterInitialFit(canvasId, _scene, canvas, currentGen);');
    const renderModeStartup = source.indexOf('setRenderMode(canvasId, viewerSettings.renderMode);');
    const localAdvisoryStartup = source.indexOf('runLocalAdvisoryGeometry(canvasId');

    assert.notEqual(revealHelper, -1, 'Expected a first-paint helper for canvas reveal.');
    assert.notEqual(revealCall, -1, 'Expected initialize to reveal after initial camera fit.');
    assert.notEqual(renderModeStartup, -1, 'Expected render-mode startup work to still run.');
    assert.notEqual(localAdvisoryStartup, -1, 'Expected advisory startup work to still run.');
    assert.ok(revealCall < renderModeStartup, 'Canvas reveal must happen before realistic material startup.');
    assert.ok(revealCall < localAdvisoryStartup, 'Canvas reveal must happen before advisory startup.');
});

test('viewer avoids WebGL uniform-buffer reuse across Babylon engine swaps', () => {
    const source = viewerSource();

    assert.match(source, /disableUniformBuffers:\s*true/);
});
