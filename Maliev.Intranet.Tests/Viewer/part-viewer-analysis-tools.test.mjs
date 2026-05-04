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

    addInPlace(other) {
        this.x += other.x;
        this.y += other.y;
        this.z += other.z;
        return this;
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

    static Center(a, b) {
        return new Vector3((a.x + b.x) / 2, (a.y + b.y) / 2, (a.z + b.z) / 2);
    }

    static Cross(a, b) {
        return new Vector3(
            a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x);
    }

    static Distance(a, b) {
        return a.subtract(b).length();
    }

    static Dot(a, b) {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    static Lerp(a, b, t) {
        return new Vector3(
            a.x + (b.x - a.x) * t,
            a.y + (b.y - a.y) * t,
            a.z + (b.z - a.z) * t);
    }

    static TransformCoordinates(point) {
        return new Vector3(point.x, point.y, point.z);
    }

    static TransformNormal(point) {
        return new Vector3(point.x, point.y, point.z);
    }
}

class Ray {
    constructor(origin, direction, length) {
        this.origin = origin;
        this.direction = direction;
        this.length = length;
    }
}

function createElement() {
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
        requestAnimationFrame: (callback) => callback(),
        window: {},
        globalThis: {},
        BABYLON: {
            Axis: {
                X: new Vector3(1, 0, 0),
                Y: new Vector3(0, 1, 0),
                Z: new Vector3(0, 0, 1),
            },
            Color3: class Color3 {
                constructor(r = 0, g = 0, b = 0) {
                    this.r = r;
                    this.g = g;
                    this.b = b;
                }
            },
            Matrix: {
                RotationX: () => ({}),
            },
            MeshBuilder: {
                CreateLines: (name, options) => ({
                    name,
                    points: options.points,
                    isPickable: true,
                    dispose: () => {},
                }),
                CreateLineSystem: (name, options) => ({
                    name,
                    lines: options.lines,
                    isPickable: true,
                    dispose: () => {},
                }),
                CreateSphere: (name) => ({
                    name,
                    isPickable: true,
                    position: null,
                    dispose: () => {},
                }),
                CreateCylinder: (name) => ({
                    name,
                    isPickable: true,
                    position: null,
                    dispose: () => {},
                }),
            },
            PointerEventTypes: {
                POINTERDOWN: 1,
                POINTERUP: 2,
                POINTERMOVE: 3,
            },
            Quaternion: {
                RotationAxis: () => ({}),
            },
            Ray,
            StandardMaterial: class StandardMaterial {
                constructor(name) {
                    this.name = name;
                    this.backFaceCulling = true;
                }
            },
            Vector3,
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

function makeMesh(name, options = {}) {
    return {
        name,
        uniqueId: options.uniqueId ?? Math.floor(Math.random() * 100000),
        isPickable: options.isPickable ?? false,
        isVisible: options.isVisible ?? true,
        material: options.material ?? { backFaceCulling: true },
        metadata: options.metadata ?? {},
        getBoundingInfo: () => options.boundingInfo ?? {
            boundingBox: {
                minimumWorld: new Vector3(-1, -1, -1),
                maximumWorld: new Vector3(1, 1, 1),
            },
        },
        getIndices: () => options.indices ?? [],
        getTotalIndices: () => (options.indices ?? []).length,
        getTotalVertices: () => options.totalVertices ?? ((options.positions?.length ?? 0) / 3 || 12),
        getVerticesData: () => {
            options.onRead?.();
            return options.positions ?? [];
        },
        getWorldMatrix: () => ({}),
        isEnabled: () => options.enabled ?? true,
    };
}

function cubeSectionMesh(onRead) {
    const positions = [
        -1, -1, -1,  1, -1, -1,  1,  1, -1, -1,  1, -1,
        -1, -1,  1,  1, -1,  1,  1,  1,  1, -1,  1,  1,
    ];
    const indices = [
        0, 1, 2, 0, 2, 3,
        4, 6, 5, 4, 7, 6,
        0, 4, 5, 0, 5, 1,
        1, 5, 6, 1, 6, 2,
        2, 6, 7, 2, 7, 3,
        3, 7, 4, 3, 4, 0,
    ];
    return makeMesh('model', {
        isPickable: false,
        positions,
        indices,
        totalVertices: positions.length / 3,
        onRead,
    });
}

test('pointer render coordinates use canvas-local CSS coordinates for Babylon scene picks', () => {
    const context = loadViewerContext();
    const canvas = {
        getBoundingClientRect: () => ({ left: 100, top: 50, width: 200, height: 100 }),
    };
    context.canvas = canvas;
    vm.runInContext(`
        engines.viewer = {
            getRenderWidth: () => 1000,
            getRenderHeight: () => 500,
            getRenderingCanvas: () => canvas
        };
    `, context);

    const coords = vm.runInContext(
        "getPointerRenderCoordinates('viewer', { clientX: 150, clientY: 80 })",
        context);

    assert.equal(coords.x, 50);
    assert.equal(coords.y, 30);
});

test('model mesh registry marks only real model geometry pickable for analysis', () => {
    const context = loadViewerContext();
    context.scene = {
        meshes: [
            makeMesh('model', { totalVertices: 24 }),
            makeMesh('__grid__', { totalVertices: 4 }),
            makeMesh('__shadow_catcher__', { totalVertices: 4 }),
            makeMesh('__section_ghost_1', { totalVertices: 24 }),
            makeMesh('__section_hatch_viewer__', { totalVertices: 2 }),
            makeMesh('measure_hover_dot', { totalVertices: 8 }),
            makeMesh('thickness_line', { totalVertices: 2 }),
            makeMesh('__flipped_overlay__1', { totalVertices: 24 }),
            makeMesh('bbox_viewer', { totalVertices: 2 }),
        ],
    };

    const result = vm.runInContext(`
        tagModelMeshesForAnalysis('viewer', scene);
        scene.meshes.map(m => ({ name: m.name, pickable: m.isPickable, model: isModelMeshForAnalysis(m) }));
    `, context);

    assert.deepEqual(result.filter(m => m.model).map(m => m.name), ['model']);
    assert.equal(result.find(m => m.name === 'model').pickable, true);
    assert.equal(result.find(m => m.name === '__section_ghost_1').pickable, false);
});

test('section hatch generation uses model meshes and excludes section ghost meshes', () => {
    const context = loadViewerContext();
    let modelReads = 0;
    let ghostReads = 0;
    context.scene = {
        clipPlane: {},
        meshes: [
            cubeSectionMesh(() => { modelReads += 1; }),
            cubeSectionMesh(() => { ghostReads += 1; }),
        ],
    };
    context.scene.meshes[1].name = '__section_ghost_1';

    const hatchLineCount = vm.runInContext(`
        tagModelMeshesForAnalysis('viewer', scene);
        _rebuildSectionHatch('viewer', scene, new BABYLON.Vector3(1, 0, 0), 0);
        sectionHatchMeshes.viewer?.lines?.length ?? 0;
    `, context);

    assert.ok(hatchLineCount > 0, 'expected visible cross hatch lines');
    assert.ok(modelReads > 0, 'expected section to read model geometry');
    assert.equal(ghostReads, 0, 'section ghosts must not feed future hatch rebuilds');
});

test('measure hover uses PointerEvent coordinates instead of stale scene pointer coordinates', () => {
    const context = loadViewerContext();
    const picks = [];
    const model = makeMesh('model', {
        isPickable: true,
        totalVertices: 24,
    });
    const canvas = {
        style: {},
        getBoundingClientRect: () => ({ left: 100, top: 50, width: 200, height: 100 }),
    };
    context.canvas = canvas;
    context.scene = {
        meshes: [model],
        pointerX: 999,
        pointerY: 999,
        onBeforeRenderObservable: { add: () => ({}) },
        onPointerObservable: {
            add(callback) {
                this.callback = callback;
                return callback;
            },
            remove: () => {},
        },
        pick(x, y) {
            picks.push([x, y]);
            return {
                hit: true,
                pickedMesh: model,
                pickedPoint: new Vector3(1, 2, 3),
                getNormal: () => new Vector3(0, 0, 1),
            };
        },
    };
    vm.runInContext(`
        scenes.viewer = scene;
        engines.viewer = {
            getRenderWidth: () => 1000,
            getRenderHeight: () => 500,
            getRenderingCanvas: () => canvas
        };
    `, context);
    context.document.getElementById = () => canvas;

    vm.runInContext("enableMeasureTool('viewer', null)", context);
    context.scene.onPointerObservable.callback({
        type: context.BABYLON.PointerEventTypes.POINTERMOVE,
        event: { clientX: 150, clientY: 80 },
    });

    assert.deepEqual(picks.at(-1), [50, 30]);
});

test('thickness hover uses PointerEvent coordinates instead of stale scene pointer coordinates', () => {
    const context = loadViewerContext();
    const picks = [];
    const model = makeMesh('model', {
        isPickable: true,
        totalVertices: 24,
    });
    const canvas = {
        style: {},
        getBoundingClientRect: () => ({ left: 100, top: 50, width: 200, height: 100 }),
    };
    context.canvas = canvas;
    context.scene = {
        activeCamera: { position: new Vector3(0, 0, 10) },
        meshes: [model],
        pointerX: 999,
        pointerY: 999,
        multiPickWithRay: () => [],
        onBeforeRenderObservable: { add: () => ({}) },
        onPointerObservable: {
            add(callback) {
                this.callback = callback;
                return callback;
            },
            remove: () => {},
        },
        pick(x, y) {
            picks.push([x, y]);
            return {
                faceId: 7,
                hit: true,
                pickedMesh: model,
                pickedPoint: new Vector3(1, 2, 3),
                getNormal: () => new Vector3(0, 0, 1),
            };
        },
    };
    vm.runInContext(`
        scenes.viewer = scene;
        engines.viewer = {
            getRenderWidth: () => 1000,
            getRenderHeight: () => 500,
            getRenderingCanvas: () => canvas
        };
    `, context);
    context.document.getElementById = () => canvas;

    vm.runInContext("enableThicknessAnalysis('viewer')", context);
    context.scene.onPointerObservable.callback({
        type: context.BABYLON.PointerEventTypes.POINTERMOVE,
        event: { clientX: 150, clientY: 80 },
    });

    assert.deepEqual(picks.at(-1), [50, 30]);
});

test('thickness hover ignores empty canvas positions instead of scaled false model hits', () => {
    const context = loadViewerContext();
    const picks = [];
    const createdSpheres = [];
    const labels = [];
    const model = makeMesh('model', {
        isPickable: true,
        totalVertices: 24,
    });
    const canvas = {
        style: {},
        getBoundingClientRect: () => ({ left: 0, top: 0, width: 1000, height: 500 }),
    };

    context.canvas = canvas;
    context.BABYLON.MeshBuilder.CreateSphere = (name) => {
        const sphere = {
            name,
            isPickable: true,
            metadata: {},
            position: null,
            dispose: () => {},
        };
        createdSpheres.push(sphere);
        return sphere;
    };
    context.document.createElement = () => {
        const element = createElement();
        labels.push(element);
        return element;
    };
    context.scene = {
        activeCamera: { position: new Vector3(0, 0, 10) },
        meshes: [model],
        pointerX: 12,
        pointerY: 12,
        multiPickWithRay: () => [{
            hit: true,
            pickedMesh: model,
            pickedPoint: new Vector3(0, 0, -2),
            getNormal: () => new Vector3(0, 0, -1),
        }],
        onBeforeRenderObservable: { add: () => ({}) },
        onPointerObservable: {
            add(callback) {
                this.callback = callback;
                return callback;
            },
            remove: () => {},
        },
        pick(x, y) {
            picks.push([x, y]);
            if (x === 1800 && y === 360) {
                return {
                    faceId: 7,
                    hit: true,
                    pickedMesh: model,
                    pickedPoint: new Vector3(1, 2, 3),
                    getNormal: () => new Vector3(0, 0, 1),
                };
            }

            return { hit: false };
        },
    };
    vm.runInContext(`
        scenes.viewer = scene;
        engines.viewer = {
            getRenderWidth: () => 2000,
            getRenderHeight: () => 1000,
            getRenderingCanvas: () => canvas
        };
    `, context);
    context.document.getElementById = () => canvas;

    vm.runInContext("enableThicknessAnalysis('viewer')", context);
    context.scene.onPointerObservable.callback({
        type: context.BABYLON.PointerEventTypes.POINTERMOVE,
        event: { clientX: 900, clientY: 180 },
    });

    assert.deepEqual(picks.at(-1), [900, 180]);
    assert.equal(createdSpheres.length, 0);
    assert.equal(labels.at(0).style.display, 'none');
});
