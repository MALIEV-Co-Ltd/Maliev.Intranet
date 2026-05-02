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

function addRing(positions, axisX, centerY, centerZ, radius, count) {
    for (let index = 0; index < count; index += 1) {
        const angle = (Math.PI * 2 * index) / count;
        positions.push(
            axisX,
            centerY + Math.cos(angle) * radius,
            centerZ + Math.sin(angle) * radius);
    }
}

function addRingAboutZ(positions, axisZ, centerX, centerY, radius, count) {
    for (let index = 0; index < count; index += 1) {
        const angle = (Math.PI * 2 * index) / count;
        positions.push(
            centerX + Math.cos(angle) * radius,
            centerY + Math.sin(angle) * radius,
            axisZ);
    }
}

function addBoxVertices(positions, min, max) {
    [
        [min.x, min.y, min.z],
        [min.x, min.y, max.z],
        [min.x, max.y, min.z],
        [min.x, max.y, max.z],
        [max.x, min.y, min.z],
        [max.x, min.y, max.z],
        [max.x, max.y, min.z],
        [max.x, max.y, max.z],
    ].forEach(point => {
        for (let index = 0; index < 16; index += 1) {
            positions.push(point[0], point[1], point[2]);
        }
    });
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
        "resolveTurningAxis(scene, 'viewer', 'AUTO', [0, 0, 0], bb, fallbackCenter)",
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

test('turning axis resolver does not invent an AUTO axis for box geometry', () => {
    const context = loadViewerContext();
    const positions = [];
    addBoxVertices(positions, { x: -10, y: -8, z: -6 }, { x: 10, y: 8, z: 6 });

    context.scene = buildScene(positions);
    context.bb = {
        min: { x: -10, y: -8, z: -6 },
        max: { x: 10, y: 8, z: 6 },
    };
    context.fallbackCenter = new Vector3(0, 0, 0);

    const result = vm.runInContext(
        "resolveTurningAxis(scene, 'viewer', 'AUTO', [0, 0, 0], bb, fallbackCenter)",
        context);

    assert.equal(result, null);
});

test('turning axis resolver prefers long axial ring support over a local circular feature', () => {
    const context = loadViewerContext();
    const positions = [];

    for (let x = -80; x <= 80; x += 16) {
        const radius = Math.abs(x) > 48 ? 3.2 : 4.1;
        addRing(positions, x, 0, 0, radius, 64);
    }

    for (let z = -0.8; z <= 0.8; z += 0.16) {
        addRingAboutZ(positions, z, 0, 0, 1.2, 64);
    }

    context.scene = buildScene(positions);
    context.bb = {
        min: { x: -80, y: -4.1, z: -4.1 },
        max: { x: 80, y: 4.1, z: 4.1 },
    };
    context.fallbackCenter = new Vector3(0, 0, 0);

    const result = vm.runInContext(
        "resolveTurningAxis(scene, 'viewer', 'AUTO', [0, 0, 0], bb, fallbackCenter)",
        context);

    assert.ok(result, 'expected a detected turning axis');
    assert.ok(Math.abs(result.direction.x) > 0.98, `expected X axis, got ${JSON.stringify(result.direction)}`);
});

test('turning axis overlay does not render a CW label', () => {
    assert.equal(viewerSource().includes("createTurningAxisLabel('CW')"), false);
});
