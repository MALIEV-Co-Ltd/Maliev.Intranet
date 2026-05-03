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

    static TransformCoordinates(point) {
        return new Vector3(point.x, point.y, point.z);
    }

    static TransformNormal(point) {
        return new Vector3(point.x, point.y, point.z);
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
                RotationX: () => ({}),
            },
            VertexBuffer: {
                PositionKind: 'position',
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
        "resolveTurningAxis(scene, 'viewer', 'X', [1, 0, 0], bb, fallbackCenter)",
        context);

    assert.ok(Math.abs(result.direction.x) > 0.98);
    assert.ok(Math.abs(result.center.y) < 0.15, `expected bore center y=0, got ${result.center.y}`);
    assert.ok(Math.abs(result.center.z) < 0.15, `expected bore center z=0, got ${result.center.z}`);
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
        "resolveTurningAxis(scene, 'viewer', 'X', [1, 0, 0], bb, fallbackCenter)",
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

test('viewer avoids WebGL uniform-buffer reuse across Babylon engine swaps', () => {
    const source = viewerSource();

    assert.match(source, /disableUniformBuffers:\s*true/);
});
