import assert from 'node:assert/strict';
import fs from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

// ── Minimal viewer context for createAxisGizmo ────────────────────────────────
//
// The axis gizmo must render in its OWN BabylonJS scene: realistic mode attaches
// an SSAO2 prePass renderer to the main scene whose `enabled` flag is getter-only
// in current BabylonJS, so a manual second-camera pass through the main scene is
// silently swallowed into the prePass geometry buffer (blank/black gizmo box).
// These tests pin the dedicated-scene contract.

class Vector3 {
    constructor(x = 0, y = 0, z = 0) {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    normalize() {
        const len = Math.hypot(this.x, this.y, this.z) || 1;
        return new Vector3(this.x / len, this.y / len, this.z / len);
    }

    scale(factor) { return new Vector3(this.x * factor, this.y * factor, this.z * factor); }

    length() { return Math.hypot(this.x, this.y, this.z); }

    subtract(other) { return new Vector3(this.x - other.x, this.y - other.y, this.z - other.z); }

    copyFrom(other) {
        this.x = other.x;
        this.y = other.y;
        this.z = other.z;
        return this;
    }

    static Zero() { return new Vector3(0, 0, 0); }

    static Cross(a, b) {
        return new Vector3(
            a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x);
    }

    static Dot(a, b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
}

class Color3 {
    constructor(r = 0, g = 0, b = 0) { this.r = r; this.g = g; this.b = b; }
    scale(factor) { return new Color3(this.r * factor, this.g * factor, this.b * factor); }
}

class Observable {
    constructor() { this.observers = []; }
    add(callback) {
        const token = { callback };
        this.observers.push(token);
        return token;
    }
    remove(token) {
        const index = this.observers.indexOf(token);
        if (index >= 0) this.observers.splice(index, 1);
        return index >= 0;
    }
}

class FakeScene {
    constructor(engine) {
        this.engine = engine;
        this.meshes = [];
        this.cameras = [];
        this.lights = [];
        this.autoClear = true;
        this.autoClearDepthAndStencil = true;
        this.activeCamera = null;
        this.disposed = false;
        this.renderCount = 0;
        this.onBeforeRenderObservable = new Observable();
        engine?.scenes?.push(this);
    }

    getEngine() { return this.engine; }
    getMeshByName(name) { return this.meshes.find(m => m.name === name) ?? null; }
    getCameraByName(name) { return this.cameras.find(c => c.name === name) ?? null; }
    render() { this.renderCount += 1; }
    dispose() { this.disposed = true; }
}

class FakeArcRotateCamera {
    constructor(name, alpha, beta, radius, target, scene) {
        this.name = name;
        this.alpha = alpha;
        this.beta = beta;
        this.radius = radius;
        this.target = target;
        this.scene = scene;
        this.position = new Vector3(0, 0, radius);
        this.upVector = new Vector3(0, 1, 0);
        this.viewport = null;
        this.mode = null;
        this.disposed = false;
        scene?.cameras?.push(this);
    }

    setPosition(position) { this.position = position; }
    isDisposed() { return this.disposed; }
    dispose() { this.disposed = true; }
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
        lang: '',
        setAttribute: () => {},
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
            ArcRotateCamera: FakeArcRotateCamera,
            Axis: { X: new Vector3(1, 0, 0), Y: new Vector3(0, 1, 0), Z: new Vector3(0, 0, 1) },
            Camera: { ORTHOGRAPHIC_CAMERA: 1, PERSPECTIVE_CAMERA: 0 },
            Color3,
            HemisphericLight: class HemisphericLight {
                constructor(name, direction, scene) {
                    this.name = name;
                    this.direction = direction;
                    this.intensity = 1;
                    scene?.lights?.push(this);
                }
            },
            Mesh: class Mesh {
                constructor(name, scene) {
                    this.name = name;
                    this.disposed = false;
                    this.dispose = () => { this.disposed = true; };
                    scene?.meshes?.push(this);
                }
            },
            MeshBuilder: {
                CreateLines: (name, options, scene) => {
                    const mesh = {
                        name,
                        points: options.points,
                        isPickable: true,
                        dispose: () => {},
                    };
                    scene?.meshes?.push(mesh);
                    return mesh;
                },
                CreateCylinder: (name, options, scene) => {
                    const mesh = {
                        name,
                        isPickable: true,
                        position: null,
                        rotationQuaternion: null,
                        dispose: () => {},
                    };
                    scene?.meshes?.push(mesh);
                    return mesh;
                },
            },
            Quaternion: {
                Identity: () => ({ identity: true }),
                RotationAxis: () => ({}),
            },
            Scene: FakeScene,
            StandardMaterial: class StandardMaterial {
                constructor(name, scene) {
                    this.name = name;
                    this.backFaceCulling = true;
                    this.scene = scene;
                }
            },
            Vector3,
            Viewport: class Viewport {
                constructor(x, y, width, height) {
                    this.x = x;
                    this.y = y;
                    this.width = width;
                    this.height = height;
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

function createGizmo(context) {
    const engine = { scenes: [] };
    const mainScene = new FakeScene(engine);
    const mainCam = new context.BABYLON.ArcRotateCamera('main', 0.5, 1.1, 10, Vector3.Zero(), mainScene);
    const canvas = {
        addEventListener: () => {},
        removeEventListener: () => {},
        getBoundingClientRect: () => ({ left: 0, top: 0, width: 800, height: 600 }),
        offsetWidth: 800,
        offsetHeight: 600,
    };

    context.__mainScene = mainScene;
    context.__mainCam = mainCam;
    context.__canvas = canvas;
    vm.runInContext("createAxisGizmo('c1', __mainScene, __mainCam, __canvas)", context);

    const layer = vm.runInContext("axisGizmoLayer['c1']", context);
    return { engine, mainScene, mainCam, layer };
}

test('createAxisGizmo builds the gizmo in a dedicated scene, not the main scene', () => {
    const context = loadViewerContext();
    const { mainScene, layer } = createGizmo(context);

    assert.ok(layer, 'gizmo layer must be registered');
    assert.ok(layer.gizmoScene, 'gizmo layer must expose its dedicated scene');
    assert.notEqual(layer.gizmoScene, mainScene, 'gizmo scene must not be the main scene');

    // No gizmo meshes or cameras may leak into the main scene — main-scene
    // pipelines (SSAO prePass, tone mapping) must never touch the gizmo.
    assert.equal(mainScene.meshes.length, 0);
    assert.deepEqual(mainScene.cameras.map(c => c.name), ['main']);

    // All six axis meshes (3 lines + 3 cones) live in the gizmo scene.
    const gizmoMeshNames = layer.gizmoScene.meshes.map(m => m.name).sort();
    assert.deepEqual(gizmoMeshNames, [
        '__axisXArr__', '__axisX__',
        '__axisYArr__', '__axisY__',
        '__axisZArr__', '__axisZ__',
    ].sort());

    // The ortho gizmo camera is the gizmo scene's active camera.
    assert.equal(layer.axesCam.scene, layer.gizmoScene);
    assert.equal(layer.gizmoScene.activeCamera, layer.axesCam);

    // The render loop scissor-clears the viewport itself; the gizmo scene must
    // not clear the full framebuffer (it renders after the main scene).
    assert.equal(layer.gizmoScene.autoClear, false);
    assert.equal(layer.gizmoScene.autoClearDepthAndStencil, false);

    // The cones need a light of their own — the main scene's lights are gone.
    assert.ok(layer.gizmoScene.lights.length >= 1, 'gizmo scene must have its own light');
});

test('axis gizmo camera tracks the main camera direction via the main scene render hook', () => {
    const context = loadViewerContext();
    const { mainScene, mainCam, layer } = createGizmo(context);

    assert.equal(mainScene.onBeforeRenderObservable.observers.length, 1,
        'gizmo must sync from the main scene before-render hook');

    mainCam.position = new Vector3(0, -10, 0);
    mainCam.target = Vector3.Zero();
    mainCam.upVector = new Vector3(0, 0, 1);
    mainScene.onBeforeRenderObservable.observers[0].callback();

    // Camera orbits at fixed radius 3.5 along the main camera's view direction.
    assert.equal(layer.axesCam.position.x, 0);
    assert.equal(layer.axesCam.position.y, -3.5);
    assert.equal(layer.axesCam.position.z, 0);
    assert.deepEqual(
        { x: layer.axesCam.upVector.x, y: layer.axesCam.upVector.y, z: layer.axesCam.upVector.z },
        { x: 0, y: 0, z: 1 });
});

test('disposing the gizmo layer disposes the dedicated scene and detaches the sync hook', () => {
    const context = loadViewerContext();
    const { mainScene, layer } = createGizmo(context);

    layer.dispose();

    assert.equal(layer.gizmoScene.disposed, true, 'gizmo scene must be disposed');
    assert.equal(mainScene.onBeforeRenderObservable.observers.length, 0,
        'main scene sync hook must be removed');
});

test('render loop renders the gizmo scene directly and never touches the prePass renderer', () => {
    const viewerPath = new URL('../../Maliev.Intranet.Client/wwwroot/js/part-viewer.js', import.meta.url);
    const source = fs.readFileSync(viewerPath, 'utf8');

    assert.ok(source.includes('gizmo.gizmoScene.render()'),
        'render loop must render the dedicated gizmo scene');
    assert.ok(!source.includes('prePass.enabled'),
        'the gizmo pass must not rely on toggling PrePassRenderer.enabled (getter-only in BabylonJS 8+)');
    assert.ok(!/scene\.activeCamera\s*=\s*axesCam/.test(source),
        'the gizmo camera must not be activated on the main scene');
});
