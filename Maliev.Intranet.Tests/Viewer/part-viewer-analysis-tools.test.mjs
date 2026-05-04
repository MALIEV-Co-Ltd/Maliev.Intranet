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

    static Project(point) {
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
                Identity: () => ({}),
                RotationX: () => ({}),
            },
            Mesh: class Mesh {
                constructor(name) {
                    this.name = name;
                    this.material = null;
                    this.isPickable = true;
                    this.metadata = {};
                    this.dispose = () => {};
                }
            },
            Plane: class Plane {
                constructor(a, b, c, d) {
                    this.a = a;
                    this.b = b;
                    this.c = c;
                    this.d = d;
                }
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
                    material: {},
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
            VertexData: class VertexData {
                applyToMesh(mesh) {
                    mesh.vertexData = this;
                }
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

function cubeSectionMesh(onRead, zOffset = 0) {
    const positions = [
        -1, -1, -1 + zOffset,  1, -1, -1 + zOffset,  1,  1, -1 + zOffset, -1,  1, -1 + zOffset,
        -1, -1,  1 + zOffset,  1, -1,  1 + zOffset,  1,  1,  1 + zOffset, -1,  1,  1 + zOffset,
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

test('pointer render coordinates scale canvas-local CSS positions into Babylon render pixels', () => {
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

    assert.equal(coords.x, 250);
    assert.equal(coords.y, 150);
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

test('section hatch generation follows translated cut contours and stays visible above the cut', () => {
    const context = loadViewerContext();
    context.scene = {
        clipPlane: {},
        meshes: [
            cubeSectionMesh(() => {}, 1000),
        ],
    };

    const result = vm.runInContext(`
        tagModelMeshesForAnalysis('viewer', scene);
        _rebuildSectionHatch('viewer', scene, new BABYLON.Vector3(1, 0, 0), 0);
        ({
            lineCount: sectionHatchMeshes.viewer?.lines?.length ?? 0,
            color: sectionHatchMeshes.viewer?.color,
            disableClipPlanes: sectionHatchMeshes.viewer?.material?.disableClipPlanes,
            disableDepthWrite: sectionHatchMeshes.viewer?.material?.disableDepthWrite,
            alwaysActive: sectionHatchMeshes.viewer?.alwaysSelectAsActiveMesh
        });
    `, context);

    assert.ok(result.lineCount > 0, 'expected cross hatch lines even when the contour is far from the world origin');
    assert.deepEqual(
        { r: result.color.r, g: result.color.g, b: result.color.b },
        { r: 1, g: 0.22, b: 0.68 });
    assert.equal(result.disableClipPlanes, true);
    assert.equal(result.disableDepthWrite, true);
    assert.equal(result.alwaysActive, true);
});

test('section fill creates a light pink cap below the diagonal hatch lines', () => {
    const context = loadViewerContext();
    context.scene = {
        clipPlane: {},
        meshes: [
            cubeSectionMesh(() => {}),
        ],
    };

    const result = vm.runInContext(`
        tagModelMeshesForAnalysis('viewer', scene);
        _rebuildSectionFill('viewer', scene, new BABYLON.Vector3(1, 0, 0), 0);
        _rebuildSectionHatch('viewer', scene, new BABYLON.Vector3(1, 0, 0), 0);
        ({
            fillName: sectionFillMeshes.viewer?.name,
            fillPositions: sectionFillMeshes.viewer?.vertexData?.positions?.length ?? 0,
            fillIndices: sectionFillMeshes.viewer?.vertexData?.indices?.length ?? 0,
            fillColor: sectionFillMeshes.viewer?.material?.diffuseColor,
            fillEmissiveColor: sectionFillMeshes.viewer?.material?.emissiveColor,
            fillAlpha: sectionFillMeshes.viewer?.material?.alpha,
            fillDisableLighting: sectionFillMeshes.viewer?.material?.disableLighting,
            fillNoClip: sectionFillMeshes.viewer?.material?.disableClipPlanes,
            hatchLineCount: sectionHatchMeshes.viewer?.lines?.length ?? 0
        });
    `, context);

    assert.equal(result.fillName, '__section_fill_viewer__');
    assert.ok(result.fillPositions > 0, 'expected section cap vertices');
    assert.ok(result.fillIndices > 0, 'expected section cap triangles');
    assert.deepEqual(
        { r: result.fillColor.r, g: result.fillColor.g, b: result.fillColor.b },
        { r: 1, g: 0.78, b: 0.88 });
    assert.deepEqual(
        { r: result.fillEmissiveColor.r, g: result.fillEmissiveColor.g, b: result.fillEmissiveColor.b },
        { r: 1, g: 0.78, b: 0.88 });
    assert.equal(result.fillAlpha, 0.96);
    assert.equal(result.fillDisableLighting, true);
    assert.equal(result.fillNoClip, true);
    assert.ok(result.hatchLineCount > 0, 'expected diagonal hatch lines above the fill');
});

test('section cut edge is neutral while hatch fill keeps the pink cross lines', () => {
    const context = loadViewerContext();
    context.scene = {
        clipPlane: {},
        meshes: [
            cubeSectionMesh(() => {}),
        ],
    };

    const result = vm.runInContext(`
        tagModelMeshesForAnalysis('viewer', scene);
        _rebuildSectionEdges('viewer', scene, new BABYLON.Vector3(1, 0, 0), 0);
        _rebuildSectionHatch('viewer', scene, new BABYLON.Vector3(1, 0, 0), 0);
        ({
            edgeColor: sectionEdgeMeshes.viewer?.color,
            hatchColor: sectionHatchMeshes.viewer?.color
        });
    `, context);

    assert.notDeepEqual(
        { r: result.edgeColor.r, g: result.edgeColor.g, b: result.edgeColor.b },
        { r: result.hatchColor.r, g: result.hatchColor.g, b: result.hatchColor.b },
        'the cut border must not be the same pink color as the hatch fill');
    assert.deepEqual(
        { r: result.hatchColor.r, g: result.hatchColor.g, b: result.hatchColor.b },
        { r: 1, g: 0.22, b: 0.68 });
});

test('section plane clips model materials only and leaves grid floor whole', () => {
    const context = loadViewerContext();
    const grid = makeMesh('__grid__', { material: {} });
    const shadow = makeMesh('__shadow_catcher__', { material: {} });
    const model = makeMesh('model', {
        isPickable: true,
        totalVertices: 24,
        material: {},
    });
    model.clone = () => null;
    context.grid = grid;
    context.shadow = shadow;
    context.model = model;
    context.scene = {
        clipPlane: null,
        meshes: [grid, shadow, model],
        getMaterialByName: () => null,
        onBeforeRenderObservable: {
            remove: () => {},
        },
    };

    const result = vm.runInContext(`
        scenes.viewer = scene;
        meshCenters.viewer = { x: 0, y: 0, z: 0 };
        modelScaleFactors.viewer = 1;
        setSectionPlane('viewer', true, 'x', 0, false);
        ({
            sceneClipped: !!scene.clipPlane,
            modelClipped: !!model.material.clipPlane && model.material.disableClipPlanes === false,
            gridClipped: !!grid.material.clipPlane || grid.material.disableClipPlanes !== true,
            shadowClipped: !!shadow.material.clipPlane || shadow.material.disableClipPlanes !== true
        });
    `, context);

    assert.equal(result.sceneClipped, false);
    assert.equal(result.modelClipped, true);
    assert.equal(result.gridClipped, false);
    assert.equal(result.shadowClipped, false);
});

test('cad feature picker keeps flat mesh picks anchored to picked surface point', () => {
    const context = loadViewerContext();
    const model = makeMesh('model', {
        isPickable: true,
        totalVertices: 24,
        boundingInfo: {
            boundingBox: {
                minimumWorld: new Vector3(-50, -10, -10),
                maximumWorld: new Vector3(50, 10, 10),
            },
        },
    });
    context.scene = {
        meshes: [model],
        pick: () => ({
            hit: true,
            pickedMesh: model,
            pickedPoint: new Vector3(22, 5, 3),
            getNormal: () => new Vector3(0, 0, 1),
        }),
    };

    const feature = vm.runInContext(`
        tagModelMeshesForAnalysis('viewer', scene);
        pickCadFeature(scene, 50, 30);
    `, context);

    assert.equal(feature.kind, 'face');
    assert.deepEqual(
        { x: feature.anchor.x, y: feature.anchor.y, z: feature.anchor.z },
        { x: 22, y: 5, z: 3 });
});

test('cad feature picker reports explicit round metadata without using mesh bounding center', () => {
    const context = loadViewerContext();
    const model = makeMesh('model', {
        isPickable: true,
        totalVertices: 24,
        metadata: {
            malievRoundFeature: {
                kind: 'bore',
                center: { x: 12, y: 4, z: 2 },
                axis: { x: 0, y: 0, z: 1 },
                diameter: 6,
            },
        },
        boundingInfo: {
            boundingBox: {
                minimumWorld: new Vector3(-50, -10, -10),
                maximumWorld: new Vector3(50, 10, 10),
            },
        },
    });
    context.scene = {
        meshes: [model],
        pick: () => ({
            hit: true,
            pickedMesh: model,
            pickedPoint: new Vector3(13, 4, 2),
            getNormal: () => new Vector3(0, 0, 1),
        }),
    };

    const feature = vm.runInContext(`
        tagModelMeshesForAnalysis('viewer', scene);
        pickCadFeature(scene, 50, 30);
    `, context);

    assert.equal(feature.kind, 'round');
    assert.equal(feature.diameter, 6);
    assert.deepEqual(
        { x: feature.anchor.x, y: feature.anchor.y, z: feature.anchor.z },
        { x: 12, y: 4, z: 2 });
});

test('measure preview uses surface hit points instead of whole-mesh center anchors', () => {
    const context = loadViewerContext();
    const createdLines = [];
    const model = makeMesh('model', {
        isPickable: true,
        totalVertices: 24,
        boundingInfo: {
            boundingBox: {
                minimumWorld: new Vector3(-50, -10, -10),
                maximumWorld: new Vector3(50, 10, 10),
            },
        },
    });
    const canvas = {
        style: {},
        getBoundingClientRect: () => ({ left: 0, top: 0, width: 1000, height: 500 }),
    };
    context.canvas = canvas;
    context.BABYLON.MeshBuilder.CreateLines = (name, options) => {
        const line = {
            name,
            points: options.points,
            isPickable: true,
            metadata: {},
            dispose: () => {},
        };
        createdLines.push(line);
        return line;
    };

    let pickPoint = new Vector3(22, 5, 3);
    context.scene = {
        meshes: [model],
        onBeforeRenderObservable: { add: () => ({}) },
        onPointerObservable: {
            add(callback) {
                this.callback = callback;
                return callback;
            },
            remove: () => {},
        },
        pick: () => ({
            hit: true,
            pickedMesh: model,
            pickedPoint: pickPoint.clone(),
            getNormal: () => new Vector3(0, 0, 1),
        }),
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
        type: context.BABYLON.PointerEventTypes.POINTERDOWN,
        event: { clientX: 100, clientY: 100 },
    });
    context.scene.onPointerObservable.callback({
        type: context.BABYLON.PointerEventTypes.POINTERUP,
        event: { clientX: 100, clientY: 100 },
    });

    pickPoint = new Vector3(35, 7, 4);
    context.scene.onPointerObservable.callback({
        type: context.BABYLON.PointerEventTypes.POINTERMOVE,
        event: { clientX: 700, clientY: 220 },
    });

    const preview = createdLines.find(line => line.name === 'measure_preview');
    assert.ok(preview, 'expected live preview line after first point selection');
    assert.deepEqual(
        { x: preview.points[0].x, y: preview.points[0].y, z: preview.points[0].z },
        { x: 22, y: 5, z: 3 });
    assert.deepEqual(
        { x: preview.points[1].x, y: preview.points[1].y, z: preview.points[1].z },
        { x: 35, y: 7, z: 4 });
});

test('measure hover uses scaled PointerEvent coordinates instead of stale scene pointer coordinates', () => {
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

    assert.deepEqual(picks.at(-1), [250, 150]);
});

test('thickness hover uses scaled PointerEvent coordinates instead of stale scene pointer coordinates', () => {
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

    assert.deepEqual(picks.at(-1), [250, 150]);
});

test('thickness hover uses render-buffer coordinates so empty canvas positions do not hit the central model ray', () => {
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
            if (x === 900 && y === 180) {
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

    assert.deepEqual(picks.at(-1), [1800, 360]);
    assert.equal(createdSpheres.length, 0);
    assert.equal(labels.at(0).style.display, 'none');
});

test('measure hover rejects mesh hits that project away from the actual pointer', () => {
    const context = loadViewerContext();
    const createdSpheres = [];
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
    context.BABYLON.Vector3.Project = () => new Vector3(460, 260, 0.5);
    context.scene = {
        activeCamera: {
            getViewMatrix: () => ({
                multiply: () => ({}),
            }),
            getProjectionMatrix: () => ({}),
            viewport: {
                toGlobal: () => ({}),
            },
        },
        meshes: [model],
        onBeforeRenderObservable: { add: () => ({}) },
        onPointerObservable: {
            add(callback) {
                this.callback = callback;
                return callback;
            },
            remove: () => {},
        },
        pick: () => ({
            hit: true,
            pickedMesh: model,
            pickedPoint: new Vector3(1, 2, 3),
            getNormal: () => new Vector3(0, 0, 1),
        }),
    };
    vm.runInContext(`
        scenes.viewer = scene;
        mainCameras.viewer = scene.activeCamera;
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
        event: { clientX: 40, clientY: 80 },
    });

    assert.equal(createdSpheres.length, 0);
});

test('thickness hover rejects mesh hits that project away from the actual pointer', () => {
    const context = loadViewerContext();
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
    context.BABYLON.Vector3.Project = () => new Vector3(520, 220, 0.5);
    context.scene = {
        activeCamera: {
            position: new Vector3(0, 0, 10),
            getViewMatrix: () => ({
                multiply: () => ({}),
            }),
            getProjectionMatrix: () => ({}),
            viewport: {
                toGlobal: () => ({}),
            },
        },
        meshes: [model],
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
        pick: () => ({
            faceId: 7,
            hit: true,
            pickedMesh: model,
            pickedPoint: new Vector3(1, 2, 3),
            getNormal: () => new Vector3(0, 0, 1),
        }),
    };
    vm.runInContext(`
        scenes.viewer = scene;
        mainCameras.viewer = scene.activeCamera;
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
        event: { clientX: 50, clientY: 90 },
    });

    assert.equal(createdSpheres.length, 0);
    assert.equal(labels.at(0).style.display, 'none');
});

test('section panel drag clamps the panel inside the viewer container', () => {
    const context = loadViewerContext();
    const handleListeners = {};
    const documentListeners = {};
    const container = {
        getBoundingClientRect: () => ({ left: 0, top: 0, width: 800, height: 600 }),
    };
    const handle = {
        addEventListener: (type, callback) => { handleListeners[type] = callback; },
        removeEventListener: () => {},
    };
    const panel = {
        style: {},
        closest: () => container,
        querySelector: () => handle,
        getBoundingClientRect: () => ({ left: 700, top: 50, width: 260, height: 180 }),
    };
    context.document.getElementById = id => id === 'section-panel' ? panel : null;
    context.document.addEventListener = (type, callback) => { documentListeners[type] = callback; };
    context.document.removeEventListener = () => {};

    vm.runInContext("enableSectionPanelDrag('section-panel')", context);
    handleListeners.pointerdown({
        button: 0,
        clientX: 710,
        clientY: 60,
        preventDefault: () => {},
    });
    documentListeners.pointermove({
        clientX: 1200,
        clientY: 700,
        preventDefault: () => {},
    });

    assert.equal(panel.style.left, '540px');
    assert.equal(panel.style.top, '420px');
    assert.equal(panel.style.right, 'auto');
});
