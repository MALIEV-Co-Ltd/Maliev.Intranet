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

const createdCanvasContexts = [];

function readLogoSvgPath() {
    const svg = fs.readFileSync('Maliev.Intranet.Client/wwwroot/images/logo.svg', 'utf8');
    return /<path[\s\S]*?\sd="([^"]+)"/.exec(svg)?.[1] ?? '';
}

function normalizeSvgPath(path) {
    return path.replace(/\s+/g, ' ').trim();
}

function createCanvasContext() {
    const calls = [];
    let font = '';
    const context = {
        calls,
        beginPath: () => calls.push(['beginPath']),
        moveTo: (x, y) => calls.push(['moveTo', x, y]),
        lineTo: (x, y) => calls.push(['lineTo', x, y]),
        arcTo: (x1, y1, x2, y2, r) => calls.push(['arcTo', x1, y1, x2, y2, r]),
        closePath: () => calls.push(['closePath']),
        clip: () => calls.push(['clip']),
        fill: (...args) => calls.push(['fill', ...args]),
        stroke: () => calls.push(['stroke']),
        strokeRect: (x, y, w, h) => calls.push(['strokeRect', x, y, w, h]),
        fillText: (text, x, y) => calls.push(['fillText', text, x, y, font]),
        save: () => calls.push(['save']),
        restore: () => calls.push(['restore']),
        translate: (x, y) => calls.push(['translate', x, y]),
        scale: (x, y) => calls.push(['scale', x, y]),
        getImageData: (x, y, width, height) => ({
            data: new Uint8Array(width * height * 4),
            width,
            height,
        }),
    };
    Object.defineProperty(context, 'font', {
        get: () => font,
        set: value => {
            font = value;
            calls.push(['font', value]);
        },
    });
    createdCanvasContexts.push(context);
    return context;
}

function createElement(tagName = '') {
    if (String(tagName).toLowerCase() === 'canvas') {
        const context = createCanvasContext();
        return {
            width: 0,
            height: 0,
            getContext: () => context,
            _context: context,
        };
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
    const rawTextureCalls = [];
    const dynamicTextureCalls = [];

    function createMesh(name, options = {}, scene = null) {
        const mesh = {
            name,
            material: null,
            metadata: {},
            isPickable: true,
            isVisible: true,
            receiveShadows: false,
            position: {},
            rotation: {},
            width: options.width,
            height: options.height,
            depth: options.depth,
            disposed: false,
            dispose() {
                this.disposed = true;
            },
        };
        scene?.meshes?.push(mesh);
        return mesh;
    }

    const context = {
        console,
        document: {
            body: { appendChild: () => {} },
            createElement,
            getElementById: () => null,
        },
        Path2D: class Path2D {
            constructor(path) {
                this.path = path;
            }
        },
        requestAnimationFrame: (callback) => callback(),
        window: {},
        globalThis: {},
        rawTextureCalls,
        dynamicTextureCalls,
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
            ImageProcessingConfiguration: {
                TONEMAPPING_STANDARD: 1,
            },
            GridMaterial: class GridMaterial {
                constructor(name) {
                    this.name = name;
                    this.backFaceCulling = true;
                }
            },
            Matrix: {
                Identity: () => ({}),
                RotationX: () => ({}),
            },
            Mesh: class Mesh {
                constructor(name, scene) {
                    this.name = name;
                    this.material = null;
                    this.isPickable = true;
                    this.isVisible = true;
                    this.receiveShadows = false;
                    this.position = {};
                    this.rotation = {};
                    this.metadata = {};
                    this.disposed = false;
                    this.dispose = () => { this.disposed = true; };
                    scene?.meshes?.push(this);
                }
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
            Material: {
                MATERIAL_OPAQUE: 0,
                MATERIAL_ALPHABLEND: 2,
                MATERIAL_ALPHATEST: 4,
            },
            Constants: {
                TEXTUREFORMAT_RGBA: 5,
                TEXTURETYPE_UNSIGNED_BYTE: 0,
                TEXTURE_TRILINEAR_SAMPLINGMODE: 3,
            },
            DynamicTexture: class DynamicTexture {
                constructor(name, options) {
                    this.name = name;
                    this.options = options;
                    this.hasAlpha = false;
                    this.update = () => {};
                    dynamicTextureCalls.push({ name, options });
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
                CreateGround: (name, options, scene) => createMesh(name, options, scene),
                CreateBox: (name, options, scene) => createMesh(name, options, scene),
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
                CreateRibbon: (name, options) => ({
                    name,
                    pathArray: options.pathArray,
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
            PBRMaterial: class PBRMaterial {
                constructor(name) {
                    this.name = name;
                }
            },
            Quaternion: {
                Identity: () => ({ identity: true }),
                RotationAxis: () => ({}),
            },
            SceneLoader: {
                ImportMeshAsync: async () => ({ meshes: [] }),
            },
            Ray,
            RawTexture: {
                CreateRGBATexture: (data, width, height, scene, generateMipMaps, invertY, samplingMode) => {
                    const texture = {
                        data,
                        width,
                        height,
                        scene,
                        generateMipMaps,
                        invertY,
                        samplingMode,
                        hasAlpha: false,
                    };
                    rawTextureCalls.push(texture);
                    return texture;
                },
            },
            RawCubeTexture: class RawCubeTexture {
                constructor(scene) {
                    this.scene = scene;
                }
            },
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

function makeOverlayNode(name, options = {}) {
    return {
        name,
        parent: options.parent ?? null,
        position: options.position ?? new Vector3(0, 0, 0),
        rotationQuaternion: options.rotationQuaternion ?? null,
        rotation: options.rotation ?? null,
        scaling: {
            value: 1,
            setAll(value) {
                this.value = value;
            },
        },
        isPickable: true,
        isVisible: true,
        getTotalVertices: () => options.totalVertices ?? 24,
        computeWorldMatrix: () => {},
    };
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

test('showGrid uses a low-contrast grid floor in light mode', () => {
    const context = loadViewerContext();
    context.scene = {
        meshes: [],
        getMeshByName(name) {
            return this.meshes.find(mesh => mesh.name === name) ?? null;
        },
    };

    const result = vm.runInContext(`
        scenes.viewer = scene;
        darkModes.viewer = false;
        sceneBoundingBoxes.viewer = {
            min: { x: -25, y: -10, z: 0 },
            max: { x: 25, y: 10, z: 30 }
        };
        showGrid('viewer');
        const grid = scene.getMeshByName('__grid__');
        ({
            gridOpacity: grid.material.opacity,
            minorUnitVisibility: grid.material.minorUnitVisibility,
            lineColor: grid.material.lineColor,
            mainColor: grid.material.mainColor,
            receivesShadows: grid.receiveShadows
        });
    `, context);

    assert.equal(result.receivesShadows, true);
    assert.ok(result.gridOpacity <= 0.45);
    assert.ok(result.minorUnitVisibility <= 0.35);
    assert.ok(result.lineColor.r >= 0.72);
    assert.ok(result.mainColor.r >= 0.93);
});

test('grid floor fades in and fades out before disposal', () => {
    const context = loadViewerContext();
    let now = 1000;
    const renderTicks = [];
    const removedTicks = [];
    context.performance = { now: () => now };
    context.scene = {
        meshes: [],
        getMeshByName(name) {
            return this.meshes.find(mesh => mesh.name === name && !mesh.disposed) ?? null;
        },
        onBeforeRenderObservable: {
            add(callback) {
                renderTicks.push(callback);
                return callback;
            },
            remove(callback) {
                removedTicks.push(callback);
            },
        },
    };

    const start = vm.runInContext(`
        (() => {
            scenes.viewer = scene;
            darkModes.viewer = false;
            sceneBoundingBoxes.viewer = {
                min: { x: -25, y: -10, z: 0 },
                max: { x: 25, y: 10, z: 30 }
            };
            showGrid('viewer');
            const grid = scene.getMeshByName('__grid__');
            return {
                exists: !!grid,
                opacity: grid.material.opacity,
                targetOpacity: grid.metadata.malievGridTargetOpacity
            };
        })();
    `, context);

    assert.equal(start.exists, true);
    assert.equal(start.opacity, 0);
    assert.ok(start.targetOpacity > 0);
    assert.equal(renderTicks.length, 1);

    now += 120;
    renderTicks.at(-1)();
    const duringShow = vm.runInContext(`
        (() => {
            const grid = scene.getMeshByName('__grid__');
            return { opacity: grid.material.opacity, targetOpacity: grid.metadata.malievGridTargetOpacity };
        })();
    `, context);

    assert.ok(duringShow.opacity > 0 && duringShow.opacity < duringShow.targetOpacity);

    now += 500;
    renderTicks.at(-1)();
    const shown = vm.runInContext(`
        (() => {
            const grid = scene.getMeshByName('__grid__');
            return { exists: !!grid, opacity: grid.material.opacity, targetOpacity: grid.metadata.malievGridTargetOpacity };
        })();
    `, context);

    assert.equal(shown.exists, true);
    assert.equal(shown.opacity, shown.targetOpacity);
    assert.equal(removedTicks.length, 1);

    const hideStart = vm.runInContext(`
        (() => {
            hideGrid('viewer');
            const grid = scene.getMeshByName('__grid__');
            return { exists: !!grid, opacity: grid.material.opacity };
        })();
    `, context);

    assert.equal(hideStart.exists, true);
    assert.equal(hideStart.opacity, shown.targetOpacity);
    assert.equal(renderTicks.length, 2);

    now += 120;
    renderTicks.at(-1)();
    const duringHide = vm.runInContext(`
        (() => {
            const grid = scene.getMeshByName('__grid__');
            return { exists: !!grid, opacity: grid.material.opacity };
        })();
    `, context);

    assert.equal(duringHide.exists, true);
    assert.ok(duringHide.opacity > 0 && duringHide.opacity < shown.targetOpacity);

    now += 500;
    renderTicks.at(-1)();
    const hidden = vm.runInContext("!!scene.getMeshByName('__grid__')", context);

    assert.equal(hidden, false);
    assert.equal(removedTicks.length, 2);
});

test('cutting mat creates an RGBA-textured rounded floor at the model base', () => {
    createdCanvasContexts.length = 0;
    const context = loadViewerContext();
    const shadowCatcher = makeMesh('__shadow_catcher__', { totalVertices: 4 });
    shadowCatcher.isVisible = true;
    context.scene = {
        meshes: [shadowCatcher],
        getMeshByName(name) {
            return this.meshes.find(mesh => mesh.name === name) ?? null;
        },
    };

    const result = vm.runInContext(`
        scenes.viewer = scene;
        sceneBoundingBoxes.viewer = {
            min: { x: -30, y: -12, z: 0 },
            max: { x: 30, y: 12, z: 36 }
        };
        showCuttingMat('viewer');
        const top = scene.getMeshByName('__cutting_mat__');
        const slab = scene.getMeshByName('__cutting_mat_slab__');
        ({
            rawTextureCount: rawTextureCalls.length,
            dynamicTextureCount: dynamicTextureCalls.length,
            rawTextureWidth: rawTextureCalls[0]?.width,
            rawTextureDataLength: rawTextureCalls[0]?.data?.length,
            topTextureHasAlpha: top?.material?.diffuseTexture?.hasAlpha === true,
            topTransparencyMode: top?.material?.transparencyMode,
            topReceivesShadows: top?.receiveShadows,
            slabReceivesShadows: slab?.receiveShadows,
            firstOutlineUv: top?.vertexData?.uvs?.slice(2, 4),
            topVertexCount: (top?.vertexData?.positions?.length ?? 0) / 3,
            slabVertexCount: (slab?.vertexData?.positions?.length ?? 0) / 3,
            shadowVisible: scene.getMeshByName('__shadow_catcher__')?.isVisible
        });
    `, context);

    assert.equal(result.rawTextureCount, 1);
    assert.equal(result.dynamicTextureCount, 0);
    assert.equal(result.rawTextureWidth, 2048);
    assert.ok(result.rawTextureDataLength > 2048 * 256 * 4);
    assert.equal(result.topTextureHasAlpha, false);
    assert.equal(result.topTransparencyMode, 0);
    assert.equal(result.topReceivesShadows, false);
    assert.equal(result.slabReceivesShadows, false);
    assert.equal(result.firstOutlineUv[0], 0);
    assert.ok(result.topVertexCount > 12);
    assert.ok(result.slabVertexCount > 24);
    assert.equal(result.shadowVisible, false);

    const drawCalls = createdCanvasContexts.at(-1)?.calls ?? [];
    const mirroredTextCalls = drawCalls.filter(call => call[0] === 'scale' && call[1] < 0);
    const legacyTitleCalls = drawCalls.filter(call => call[0] === 'fillText' && call[1] === 'CUTTING MAT 3022');
    const plainLogoTextCalls = drawCalls.filter(call => call[0] === 'fillText' && call[1] === 'MALIEV');
    const svgLogoCalls = drawCalls.filter(call => call[0] === 'fill' && call[1]?.path?.includes('246.47181'));
    const logoSvgPath = readLogoSvgPath();
    const numberTextCalls = drawCalls.filter(call => call[0] === 'fillText' && /^[0-9]+$/.test(call[1]));
    assert.equal(mirroredTextCalls.length, 0);
    assert.equal(legacyTitleCalls.length, 0);
    assert.equal(plainLogoTextCalls.length, 0);
    assert.equal(svgLogoCalls.length, 1);
    assert.equal(normalizeSvgPath(svgLogoCalls[0][1].path), normalizeSvgPath(logoSvgPath));
    assert.ok(numberTextCalls.length > 0);
    const numericFontSizes = numberTextCalls
        .map(call => /bold\s+(\d+)px/.exec(call[4])?.[1])
        .filter(Boolean)
        .map(Number);
    assert.ok(numericFontSizes.every(size => size <= 32));

    const gridBorderIndex = drawCalls.findIndex(call => call[0] === 'strokeRect');
    assert.notEqual(gridBorderIndex, -1);
    const gridCalls = drawCalls.slice(gridBorderIndex + 1);
    const firstGridStrokeIndex = gridCalls.findIndex(call => call[0] === 'stroke');
    const secondGridStrokeIndex = gridCalls.findIndex((call, index) => index > firstGridStrokeIndex && call[0] === 'stroke');
    assert.notEqual(firstGridStrokeIndex, -1);
    assert.notEqual(secondGridStrokeIndex, -1);

    const minorGridLineCount = gridCalls.slice(0, firstGridStrokeIndex).filter(call => call[0] === 'moveTo').length;
    const majorGridLineCount = gridCalls.slice(firstGridStrokeIndex + 1, secondGridStrokeIndex).filter(call => call[0] === 'moveTo').length;
    assert.equal(minorGridLineCount, 46);
    assert.equal(majorGridLineCount, 5);
});

test('cutting mat expands the active camera fit to include the full mat footprint', () => {
    const context = loadViewerContext();
    const shadowCatcher = makeMesh('__shadow_catcher__', { totalVertices: 4 });
    const camera = {
        fov: 0.8,
        radius: 80,
        minZ: 0.1,
        target: new Vector3(0, 0, 18),
        mode: 0,
        setPosition(position) {
            this.position = position;
        },
    };
    context.scene = {
        meshes: [shadowCatcher],
        activeCamera: camera,
        getMeshByName(name) {
            return this.meshes.find(mesh => mesh.name === name && !mesh.disposed) ?? null;
        },
    };
    context.engine = {
        getAspectRatio: () => 1.5,
    };

    const result = vm.runInContext(`
        scenes.viewer = scene;
        mainCameras.viewer = scene.activeCamera;
        engines.viewer = engine;
        cameraProjection.viewer = 'orthographic';
        fitRadiusMap.viewer = 80;
        orthoZoomFactors.viewer = 1;
        sceneBoundingBoxes.viewer = {
            min: { x: -30, y: -12, z: 0 },
            max: { x: 30, y: 12, z: 36 }
        };

        showCuttingMat('viewer');
        resetCamera('viewer', false);

        ({
            fitRadius: fitRadiusMap.viewer,
            cameraRadius: scene.activeCamera.radius,
            orthoLeft: scene.activeCamera.orthoLeft,
            orthoRight: scene.activeCamera.orthoRight,
            orthoTop: scene.activeCamera.orthoTop,
            orthoBottom: scene.activeCamera.orthoBottom,
            targetZ: scene.activeCamera.target.z
        });
    `, context);

    assert.ok(result.fitRadius > 80);
    assert.equal(result.cameraRadius, result.fitRadius);
    assert.ok(result.orthoRight - result.orthoLeft >= 300);
    assert.ok(result.orthoTop - result.orthoBottom >= 220);
    assert.ok(result.targetZ < 18);
});

test('cutting mat keeps perspective camera near plane high enough to avoid depth tearing', () => {
    const context = loadViewerContext();
    const camera = { minZ: 0.1 };
    context.scene = {
        meshes: [],
        activeCamera: camera,
        getMeshByName(name) {
            return this.meshes.find(mesh => mesh.name === name && !mesh.disposed) ?? null;
        },
    };

    const result = vm.runInContext(`
        scenes.viewer = scene;
        mainCameras.viewer = scene.activeCamera;
        cameraProjection.viewer = 'perspective';
        sceneBoundingBoxes.viewer = {
            min: { x: -30, y: -12, z: 0 },
            max: { x: 30, y: 12, z: 36 }
        };

        showCuttingMat('viewer');
        const activeMinZ = scene.activeCamera.minZ;
        hideCuttingMat('viewer');

        ({
            activeMinZ,
            restoredMinZ: scene.activeCamera.minZ
        });
    `, context);

    assert.equal(result.activeMinZ, 0.01);
    assert.equal(result.restoredMinZ, 0.1);
});

test('cutting mat fades in from below and fades out before disposal', () => {
    const context = loadViewerContext();
    let now = 1000;
    const renderTicks = [];
    const removedTicks = [];
    context.performance = { now: () => now };

    const shadowCatcher = makeMesh('__shadow_catcher__', { totalVertices: 4 });
    shadowCatcher.isVisible = true;
    const camera = { minZ: 0.1 };
    context.scene = {
        meshes: [shadowCatcher],
        activeCamera: camera,
        getMeshByName(name) {
            return this.meshes.find(mesh => mesh.name === name && !mesh.disposed) ?? null;
        },
        onBeforeRenderObservable: {
            add(callback) {
                renderTicks.push(callback);
                return callback;
            },
            remove(callback) {
                removedTicks.push(callback);
            },
        },
    };

    const start = vm.runInContext(`
        (() => {
            scenes.viewer = scene;
            cameraProjection.viewer = 'orthographic';
            sceneBoundingBoxes.viewer = {
                min: { x: -30, y: -12, z: 0 },
                max: { x: 30, y: 12, z: 36 }
            };
            showCuttingMat('viewer');
            const top = scene.getMeshByName('__cutting_mat__');
            const slab = scene.getMeshByName('__cutting_mat_slab__');
            return {
            topZ: top.position.z,
            slabZ: slab.position.z,
            topAlpha: top.material.alpha,
            slabAlpha: slab.material.alpha,
            cameraMinZ: scene.activeCamera.minZ,
            shadowVisible: scene.getMeshByName('__shadow_catcher__').isVisible
            };
        })();
    `, context);

    assert.ok(start.topZ < 0);
    assert.ok(start.slabZ < 0);
    assert.equal(start.topAlpha, 0);
    assert.equal(start.slabAlpha, 0);
    assert.equal(start.cameraMinZ, 0.001);
    assert.equal(start.shadowVisible, false);
    assert.equal(renderTicks.length, 2);

    now += 120;
    renderTicks.at(-1)();
    const duringShow = vm.runInContext(`
        (() => {
            const top = scene.getMeshByName('__cutting_mat__');
            return { topZ: top.position.z, topAlpha: top.material.alpha };
        })();
    `, context);

    assert.ok(duringShow.topZ < 0);
    assert.ok(duringShow.topAlpha > 0 && duringShow.topAlpha < 1);

    now += 500;
    renderTicks.at(-1)();
    const shown = vm.runInContext(`
        (() => {
            const top = scene.getMeshByName('__cutting_mat__');
            return {
                topZ: top.position.z,
                topAlpha: top.material.alpha,
                topTransparencyMode: top.material.transparencyMode,
                cameraMinZ: scene.activeCamera.minZ
            };
        })();
    `, context);

    assert.equal(shown.topZ, 0);
    assert.equal(shown.topAlpha, 1);
    assert.equal(shown.topTransparencyMode, 0);
    assert.equal(shown.cameraMinZ, 0.001);
    assert.equal(removedTicks.length, 1);

    context.scene.activeCamera.minZ = 0.1;
    renderTicks[0]();
    assert.equal(context.scene.activeCamera.minZ, 0.001);

    const hideStart = vm.runInContext(`
        (() => {
            hideCuttingMat('viewer');
            const top = scene.getMeshByName('__cutting_mat__');
            return { topExists: !!top, topZ: top.position.z, topAlpha: top.material.alpha };
        })();
    `, context);

    assert.equal(hideStart.topExists, true);
    assert.equal(hideStart.topZ, 0);
    assert.equal(hideStart.topAlpha, 1);
    assert.equal(renderTicks.length, 3);

    now += 120;
    renderTicks.at(-1)();
    const duringHide = vm.runInContext(`
        (() => {
            const top = scene.getMeshByName('__cutting_mat__');
            return { topExists: !!top, topZ: top.position.z, topAlpha: top.material.alpha };
        })();
    `, context);

    assert.equal(duringHide.topExists, true);
    assert.ok(duringHide.topZ < 0);
    assert.ok(duringHide.topAlpha > 0 && duringHide.topAlpha < 1);

    now += 500;
    renderTicks.at(-1)();
    const hidden = vm.runInContext(`
        (() => ({
            topExists: !!scene.getMeshByName('__cutting_mat__'),
            slabExists: !!scene.getMeshByName('__cutting_mat_slab__'),
            cameraMinZ: scene.activeCamera.minZ,
            shadowVisible: scene.getMeshByName('__shadow_catcher__').isVisible
        }))();
    `, context);

    assert.equal(hidden.topExists, false);
    assert.equal(hidden.slabExists, false);
    assert.equal(hidden.cameraMinZ, 0.1);
    assert.equal(hidden.shadowVisible, true);
    assert.equal(removedTicks.length, 3);
});

test('cutting mat keeps stable textured materials without receiving shadows when realistic render mode is active', () => {
    const context = loadViewerContext();
    const shadowCatcher = makeMesh('__shadow_catcher__', { totalVertices: 4 });
    context.scene = {
        meshes: [shadowCatcher],
        materials: [],
        imageProcessingConfiguration: {},
        getMeshByName(name) {
            return this.meshes.find(mesh => mesh.name === name && !mesh.disposed) ?? null;
        },
    };

    const result = vm.runInContext(`
        scenes.viewer = scene;
        currentRenderModes.viewer = 'realistic';
        sceneBoundingBoxes.viewer = {
            min: { x: -30, y: -12, z: 0 },
            max: { x: 30, y: 12, z: 36 }
        };
        showCuttingMat('viewer');
        const top = scene.getMeshByName('__cutting_mat__');
        const slab = scene.getMeshByName('__cutting_mat_slab__');
        ({
            topIsPbr: top.material instanceof BABYLON.PBRMaterial,
            slabIsPbr: slab.material instanceof BABYLON.PBRMaterial,
            topHasDiffuseTexture: !!top.material.diffuseTexture,
            topReceivesShadows: top.receiveShadows,
            slabReceivesShadows: slab.receiveShadows
        });
    `, context);

    assert.equal(result.topIsPbr, false);
    assert.equal(result.slabIsPbr, false);
    assert.equal(result.topHasDiffuseTexture, true);
    assert.equal(result.topReceivesShadows, false);
    assert.equal(result.slabReceivesShadows, false);
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

    const hatchStripCount = vm.runInContext(`
        tagModelMeshesForAnalysis('viewer', scene);
        _rebuildSectionHatch('viewer', scene, new BABYLON.Vector3(1, 0, 0), 0);
        sectionHatchMeshes.viewer?.pathArray?.length ?? 0;
    `, context);

    assert.ok(hatchStripCount > 0, 'expected visible cross hatch strips');
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
            stripCount: sectionHatchMeshes.viewer?.pathArray?.length ?? 0,
            color: sectionHatchMeshes.viewer?.material?.diffuseColor,
            backFaceCulling: sectionHatchMeshes.viewer?.material?.backFaceCulling,
            disableClipPlanes: sectionHatchMeshes.viewer?.material?.disableClipPlanes,
            disableDepthWrite: sectionHatchMeshes.viewer?.material?.disableDepthWrite,
            forceDepthWrite: sectionHatchMeshes.viewer?.material?.forceDepthWrite,
            alwaysActive: sectionHatchMeshes.viewer?.alwaysSelectAsActiveMesh
        });
    `, context);

    assert.ok(result.stripCount > 0, 'expected cross hatch strips even when the contour is far from the world origin');
    assert.deepEqual(
        { r: result.color.r, g: result.color.g, b: result.color.b },
        { r: 0, g: 0, b: 0 });
    assert.equal(result.backFaceCulling, false);
    assert.equal(result.disableClipPlanes, true);
    assert.equal(result.disableDepthWrite, false);
    assert.equal(result.forceDepthWrite, true);
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
            fillDisableDepthWrite: sectionFillMeshes.viewer?.material?.disableDepthWrite,
            fillForceDepthWrite: sectionFillMeshes.viewer?.material?.forceDepthWrite,
            fillDisableLighting: sectionFillMeshes.viewer?.material?.disableLighting,
            fillNoClip: sectionFillMeshes.viewer?.material?.disableClipPlanes,
            fillMode: sectionFillMeshes.viewer?.metadata?.sectionFillMode,
            hatchStripCount: sectionHatchMeshes.viewer?.pathArray?.length ?? 0
        });
    `, context);

    assert.equal(result.fillName, '__section_fill_viewer__');
    assert.ok(result.fillPositions > 12, 'expected scanline section cap bands, not one fan triangle cap');
    assert.ok(result.fillIndices > 6, 'expected section cap band triangles');
    assert.deepEqual(
        { r: result.fillColor.r, g: result.fillColor.g, b: result.fillColor.b },
        { r: 1, g: 0.78, b: 0.88 });
    assert.deepEqual(
        { r: result.fillEmissiveColor.r, g: result.fillEmissiveColor.g, b: result.fillEmissiveColor.b },
        { r: 1, g: 0.78, b: 0.88 });
    assert.equal(result.fillAlpha, 1);
    assert.equal(result.fillDisableDepthWrite, false);
    assert.equal(result.fillForceDepthWrite, true);
    assert.equal(result.fillDisableLighting, true);
    assert.equal(result.fillNoClip, true);
    assert.equal(result.fillMode, 'scanline-bands');
    assert.ok(result.hatchStripCount > 0, 'expected diagonal hatch strips above the fill');
});

test('section hatch strips have visible width on both sides of the section face', () => {
    const context = loadViewerContext();
    context.scene = {
        clipPlane: {},
        meshes: [
            cubeSectionMesh(() => {}),
        ],
    };

    const result = vm.runInContext(`
        tagModelMeshesForAnalysis('viewer', scene);
        _rebuildSectionHatch('viewer', scene, new BABYLON.Vector3(1, 0, 0), 0);
        const firstStrip = sectionHatchMeshes.viewer?.pathArray?.[0];
        ({
            stripCount: sectionHatchMeshes.viewer?.pathArray?.length ?? 0,
            stripWidth: firstStrip && BABYLON.Vector3.Distance(firstStrip[0], firstStrip[1]),
            materialBackFaceCulling: sectionHatchMeshes.viewer?.material?.backFaceCulling,
            meshAlwaysActive: sectionHatchMeshes.viewer?.alwaysSelectAsActiveMesh
        });
    `, context);

    assert.ok(result.stripCount > 0, 'expected hatch strips');
    assert.ok(result.stripWidth > 0.01, 'expected hatch strip to have real geometric width');
    assert.ok(result.stripWidth < 0.12, 'expected hatch strip to stay visually thin');
    assert.equal(result.materialBackFaceCulling, false);
    assert.equal(result.meshAlwaysActive, true);
});

test('section hatch spacing is dense enough for Fusion-style section lines', () => {
    const context = loadViewerContext();
    const spacing = vm.runInContext('CONFIG.SECTION.hatchSpacingMm;', context);
    assert.ok(spacing <= 1.4);
});

test('DFM overlay transform keeps imported overlay geometry in its authored root space', async () => {
    const context = loadViewerContext();
    const overlayRoot = makeOverlayNode('__root__', {
        totalVertices: 0,
        position: new Vector3(0, 0, 0),
    });
    const overlayMesh = makeOverlayNode('thin_wall_overlay', {
        parent: overlayRoot,
        position: new Vector3(0, 0, 0),
        totalVertices: 16,
    });
    context.BABYLON.Quaternion.RotationAxis = () => ({
        multiply: () => ({ rotated: true }),
    });
    context.BABYLON.SceneLoader.ImportMeshAsync = async () => ({
        meshes: [overlayRoot, overlayMesh],
    });
    context.scene = {
        meshes: [],
    };

    await vm.runInContext(`
        scenes.viewer = scene;
        modelScaleFactors.viewer = 2;
        modelCenterOffsets.viewer = { cx: 10, cy: -4, zLift: 3 };
        toggleDfmOverlay('viewer', 'part-a', 'FDM__thin_wall', 'thin-wall.glb', true);
    `, context);

    assert.equal(overlayMesh.scaling.value, 1);
    assert.equal(overlayRoot.scaling.value, 2);
    assert.deepEqual(
        { x: overlayRoot.position.x, y: overlayRoot.position.y, z: overlayRoot.position.z },
        { x: -10, y: 4, z: 3 });
    assert.deepEqual(
        { x: overlayMesh.position.x, y: overlayMesh.position.y, z: overlayMesh.position.z },
        { x: 0, y: 0, z: 0 });
    assert.deepEqual(overlayRoot.rotationQuaternion, { rotated: true });
    assert.equal(overlayMesh.rotationQuaternion, null);
});

test('section cut edge and hatch lines are dark and depth-aware', () => {
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
            hatchColor: sectionHatchMeshes.viewer?.material?.diffuseColor,
            edgeDepthWrite: sectionEdgeMeshes.viewer?.material?.disableDepthWrite,
            hatchDepthWrite: sectionHatchMeshes.viewer?.material?.disableDepthWrite
        });
    `, context);

    assert.deepEqual(
        { r: result.hatchColor.r, g: result.hatchColor.g, b: result.hatchColor.b },
        { r: 0, g: 0, b: 0 });
    assert.deepEqual(
        { r: result.edgeColor.r, g: result.edgeColor.g, b: result.edgeColor.b },
        { r: 0.1, g: 0.18, b: 0.28 });
    assert.equal(result.edgeDepthWrite, false);
    assert.equal(result.hatchDepthWrite, false);
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

test('section plane defaults invert X and Y normals while preserving Z normal', () => {
    const context = loadViewerContext();
    const model = makeMesh('model', {
        isPickable: true,
        totalVertices: 24,
        material: {},
    });
    model.clone = () => null;
    const scene = {
        clipPlane: null,
        meshes: [model],
        getMaterialByName: () => null,
        onBeforeRenderObservable: {
            remove: () => {},
        },
    };
    context.scene = scene;
    context.model = model;

    const normals = vm.runInContext(`
        scenes.viewer = scene;
        meshCenters.viewer = { x: 0, y: 0, z: 0 };
        modelScaleFactors.viewer = 1;
        setSectionPlane('viewer', true, 'x', 0, false);
        const x = model.material.clipPlane;
        setSectionPlane('viewer', true, 'y', 0, false);
        const y = model.material.clipPlane;
        setSectionPlane('viewer', true, 'z', 0, false);
        const z = model.material.clipPlane;
        ({
            x: { a: x.a, b: x.b, c: x.c },
            y: { a: y.a, b: y.b, c: y.c },
            z: { a: z.a, b: z.b, c: z.c }
        });
    `, context);

    assert.equal(JSON.stringify(normals.x), JSON.stringify({ a: -1, b: 0, c: 0 }));
    assert.equal(JSON.stringify(normals.y), JSON.stringify({ a: 0, b: -1, c: 0 }));
    assert.equal(JSON.stringify(normals.z), JSON.stringify({ a: 0, b: 0, c: 1 }));
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

test('measure hover accepts valid hits with small projection drift', () => {
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
    context.BABYLON.Vector3.Project = () => new Vector3(145, 92, 0.5);
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

    assert.equal(createdSpheres.length, 1);
});

test('analysis tools preserve camera pointer navigation buttons', () => {
    const context = loadViewerContext();
    const pointers = { buttons: [0] };
    context.pointers = pointers;

    const result = vm.runInContext(`
        mainCameras.viewer = { inputs: { attached: { pointers } } };
        setAnalysisNavigationLock('viewer', true);
        const lockedButtons = [...pointers.buttons];
        setAnalysisNavigationLock('viewer', false);
        const unlockedButtons = [...pointers.buttons];
        ({ lockedButtons, unlockedButtons });
    `, context);

    assert.equal(JSON.stringify(result.lockedButtons), JSON.stringify([0]));
    assert.equal(JSON.stringify(result.unlockedButtons), JSON.stringify([0]));
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
