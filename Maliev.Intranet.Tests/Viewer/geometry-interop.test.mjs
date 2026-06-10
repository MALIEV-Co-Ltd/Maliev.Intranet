import assert from 'node:assert/strict';
import fs from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

// ── Minimal BabylonJS fakes ──────────────────────────────────────────────────

class Vector3 {
    constructor(x = 0, y = 0, z = 0) {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    clone() { return new Vector3(this.x, this.y, this.z); }

    subtract(other) { return new Vector3(this.x - other.x, this.y - other.y, this.z - other.z); }

    length() { return Math.hypot(this.x, this.y, this.z); }

    static Zero() { return new Vector3(0, 0, 0); }

    static Center(a, b) { return new Vector3((a.x + b.x) / 2, (a.y + b.y) / 2, (a.z + b.z) / 2); }

    static Minimize(a, b) { return new Vector3(Math.min(a.x, b.x), Math.min(a.y, b.y), Math.min(a.z, b.z)); }

    static Maximize(a, b) { return new Vector3(Math.max(a.x, b.x), Math.max(a.y, b.y), Math.max(a.z, b.z)); }
}

class Color3 { constructor(r, g, b) { this.r = r; this.g = g; this.b = b; } }
class Color4 { constructor(r, g, b, a) { this.r = r; this.g = g; this.b = b; this.a = a; } }

class Quaternion {
    multiply() { return new Quaternion(); }
    static RotationAxis() { return new Quaternion(); }
    static FromEulerVector() { return new Quaternion(); }
    static Identity() { return new Quaternion(); }
}

function makeFakeCanvas(state) {
    return {
        width: 0,
        height: 0,
        getContext: kind => (kind === 'webgl2' && state.webgl2Available ? {} : null),
        toDataURL: (type, quality) => {
            state.encodedFrames.push({ type, quality, size: state.engineSize });
            return `data:image/jpeg;base64,frame${state.encodedFrames.length}`;
        },
    };
}

function makeLoadedMesh() {
    return {
        parent: null,
        material: null,
        rotation: new Vector3(0, 0, 0),
        rotationQuaternion: null,
        getTotalVertices: () => 24,
        computeWorldMatrix: () => {},
        getBoundingInfo: () => ({
            boundingBox: {
                minimumWorld: new Vector3(-5, -10, -2),
                maximumWorld: new Vector3(5, 10, 2),
            },
        }),
    };
}

class FakeWorker {
    static instances = [];

    constructor(url, options) {
        this.url = url;
        this.options = options;
        this.terminated = false;
        this.onmessage = null;
        this.onerror = null;
        FakeWorker.instances.push(this);
    }

    postMessage(message) {
        this.lastMessage = message;
        const respond = FakeWorker.respond ?? (msg => ({
            id: msg.id,
            ok: true,
            result: {
                meshBuffers: {
                    positions: [0, 0, 0, 10, 0, 0, 0, 10, 0, 0, 0, 8],
                    indices: [0, 1, 2, 0, 1, 3],
                },
            },
        }));
        queueMicrotask(() => {
            if (!this.terminated) this.onmessage?.({ data: respond(message) });
        });
    }

    terminate() { this.terminated = true; }
}

function createInteropContext(state) {
    const fakeBabylon = {
        Engine: class {
            constructor(canvas) { this.canvas = canvas; state.engineCreated = true; }
            setSize(w) { state.engineSize = w; }
            dispose() { state.engineDisposed = true; }
        },
        Scene: class {
            constructor() {
                this.clearColor = null;
                this.activeCamera = null;
                state.renderedViews = [];
            }
            render() {
                state.renderedViews.push({
                    size: state.engineSize,
                    mode: this.activeCamera?.mode,
                    position: this.activeCamera?.position?.clone(),
                    up: this.activeCamera?.upVector?.clone(),
                });
            }
            whenReadyAsync() { return Promise.resolve(); }
            dispose() { state.sceneDisposed = true; }
        },
        SceneLoader: {
            ImportMeshAsync: (...args) => {
                state.importArgs = args;
                if (state.importShouldFail) return Promise.reject(new Error('loader exploded'));
                return Promise.resolve({ meshes: [makeLoadedMesh()] });
            },
        },
        FreeCamera: class {
            constructor() {
                this.position = Vector3.Zero();
                this.upVector = new Vector3(0, 1, 0);
                this.mode = null;
            }
            setTarget(target) { this.target = target; }
        },
        Mesh: class {
            constructor(name) {
                this.name = name;
                this.parent = null;
                this.material = null;
                this.vertexData = null;
            }
            getTotalVertices() { return (this.vertexData?.positions?.length ?? 0) / 3; }
            computeWorldMatrix() {}
            getBoundingInfo() {
                const positions = this.vertexData?.positions ?? [];
                const min = new Vector3(Infinity, Infinity, Infinity);
                const max = new Vector3(-Infinity, -Infinity, -Infinity);
                for (let index = 0; index < positions.length; index += 3) {
                    min.x = Math.min(min.x, positions[index]);
                    min.y = Math.min(min.y, positions[index + 1]);
                    min.z = Math.min(min.z, positions[index + 2]);
                    max.x = Math.max(max.x, positions[index]);
                    max.y = Math.max(max.y, positions[index + 1]);
                    max.z = Math.max(max.z, positions[index + 2]);
                }
                return { boundingBox: { minimumWorld: min, maximumWorld: max } };
            }
        },
        VertexData: class {
            applyToMesh(mesh) { mesh.vertexData = this; }
            static ComputeNormals(positions, indices, normals) {
                for (let index = 0; index < positions.length; index += 1) normals.push(0);
            }
        },
        Camera: { ORTHOGRAPHIC_CAMERA: 'ortho', PERSPECTIVE_CAMERA: 'perspective' },
        HemisphericLight: class { constructor() { this.intensity = 0; } },
        DirectionalLight: class { constructor() { this.intensity = 0; } },
        StandardMaterial: class {
            constructor() {
                this.diffuseColor = null;
                this.specularColor = null;
                this.backFaceCulling = true;
            }
        },
        Vector3,
        Color3,
        Color4,
        Quaternion,
        Axis: { X: new Vector3(1, 0, 0) },
    };

    const context = {
        BABYLON: fakeBabylon,
        document: { createElement: () => makeFakeCanvas(state) },
        Worker: FakeWorker,
        fetch: (url, opts) => {
            if (String(url).includes('/geometry/runtime/manifest')) {
                state.manifestFetched = true;
                return Promise.resolve({
                    ok: true,
                    json: () => Promise.resolve({
                        assets: {
                            worker: '/geometry/client-runtime/assets/client-geometry-runtime.abc123.worker.js',
                            wasm: '/geometry/client-runtime/assets/client-geometry-kernel.def456.wasm',
                        },
                    }),
                });
            }
            state.fetchedUrl = url;
            state.fetchSignal = opts?.signal ?? null;
            if (state.fetchShouldFail) {
                return Promise.resolve({ ok: false, status: 503 });
            }
            return Promise.resolve({
                ok: true,
                blob: () => Promise.resolve({
                    kind: 'blob',
                    arrayBuffer: () => Promise.resolve(new ArrayBuffer(16)),
                }),
            });
        },
        URL: class extends URL {
            static createObjectURL() { return 'blob:fake'; }
            static revokeObjectURL() { state.blobRevoked = true; }
        },
        AbortController,
        setTimeout,
        clearTimeout,
        console,
    };
    context.window = context;
    context.window.location = { href: 'https://intranet.local/projects/new' };
    vm.createContext(context);

    const interopPath = new URL(
        '../../Maliev.Intranet.Client/wwwroot/js/geometry/JsInterop/GeometryInterop.js',
        import.meta.url);
    vm.runInContext(fs.readFileSync(interopPath, 'utf8'), context);
    return context;
}

function makeState(overrides = {}) {
    return {
        webgl2Available: true,
        engineSize: 0,
        encodedFrames: [],
        renderedViews: [],
        importShouldFail: false,
        fetchShouldFail: false,
        ...overrides,
    };
}

// ── Tests ────────────────────────────────────────────────────────────────────

test('registers window.MalievGeometry with both interop entry points', () => {
    const state = makeState();
    const context = createInteropContext(state);

    assert.equal(typeof context.window.MalievGeometry.generateThumbnails, 'function');
    assert.equal(typeof context.window.MalievGeometry.isWebGL2Available, 'function');
});

test('isWebGL2Available reflects canvas WebGL2 support', () => {
    const available = createInteropContext(makeState());
    assert.equal(available.window.MalievGeometry.isWebGL2Available(), true);

    const unavailable = createInteropContext(makeState({ webgl2Available: false }));
    assert.equal(unavailable.window.MalievGeometry.isWebGL2Available(), false);
});

test('generateThumbnails renders all 8 ThumbnailSetDto views', async () => {
    const state = makeState();
    const context = createInteropContext(state);

    const result = await context.window.MalievGeometry.generateThumbnails(
        'https://storage.local/parts/bracket.stl?sig=abc',
        { timeoutMs: 5000, jpegQuality: 0.85 });

    const keys = [
        'frontSmall', 'backSmall', 'leftSmall', 'rightSmall',
        'topSmall', 'bottomSmall', 'thumbnailSmall', 'thumbnailLarge',
    ];
    for (const key of keys) {
        assert.ok(
            typeof result[key] === 'string' && result[key].startsWith('data:image/jpeg;base64,'),
            `${key} must be a JPEG DataURL, got: ${result[key]}`);
    }

    // 6 orthographic face views + 2 perspective isometric views.
    assert.equal(state.renderedViews.length, 8);
    assert.equal(state.renderedViews.filter(v => v.mode === 'ortho').length, 6);
    assert.equal(state.renderedViews.filter(v => v.mode === 'perspective').length, 2);

    // The large isometric view renders at 1200px, everything else at 256px.
    assert.equal(state.renderedViews.filter(v => v.size === 1200).length, 1);
    assert.equal(state.renderedViews.filter(v => v.size === 256).length, 7);

    // Z-up convention: the front view looks along +Y with a Z-up camera.
    const front = state.renderedViews[0];
    assert.ok(front.position.y < 0, 'front camera must sit on -Y side');
    assert.deepEqual({ x: front.up.x, y: front.up.y, z: front.up.z }, { x: 0, y: 0, z: 1 });

    // Engine and scene must always be disposed.
    assert.equal(state.engineDisposed, true);
    assert.equal(state.sceneDisposed, true);
    assert.equal(state.blobRevoked, true);
});

test('generateThumbnails resolves loader extension from the signed URL path', async () => {
    const state = makeState();
    const context = createInteropContext(state);

    await context.window.MalievGeometry.generateThumbnails(
        'https://storage.local/parts/widget.GLB?X-Goog-Signature=xyz', {});

    assert.equal(state.importArgs[5], '.glb');
});

test('generateThumbnails renders 3MF files via the GeometryService runtime worker', async () => {
    const state = makeState();
    const context = createInteropContext(state);
    FakeWorker.instances.length = 0;

    const result = await context.window.MalievGeometry.generateThumbnails(
        'https://storage.local/parts/clip.3mf?sig=abc', { timeoutMs: 5000 });

    assert.equal(state.manifestFetched, true, 'must pull the runtime manifest from GeometryService');
    assert.equal(FakeWorker.instances.length, 1, 'must spawn the GeometryService runtime worker');
    const worker = FakeWorker.instances[0];
    assert.equal(worker.url, '/api/v1/geometry/runtime/assets/client-geometry-runtime.abc123.worker.js');
    assert.equal(worker.lastMessage.operation, 'extract_mesh');
    assert.equal(worker.lastMessage.input.fileName, 'thumbnail-source.3mf');
    assert.equal(worker.terminated, true, 'worker must be terminated after extraction');

    assert.equal(state.renderedViews.length, 8);
    assert.ok(result.thumbnailLarge.startsWith('data:image/jpeg;base64,'));
});

test('generateThumbnails rejects unsupported formats so Blazor falls back to the server', async () => {
    const state = makeState();
    const context = createInteropContext(state);

    await assert.rejects(
        () => context.window.MalievGeometry.generateThumbnails('https://storage.local/parts/housing.step', {}),
        /Unsupported format/);
});

test('generateThumbnails rejects when WebGL is unavailable', async () => {
    const state = makeState({ webgl2Available: false });
    const context = createInteropContext(state);

    await assert.rejects(
        () => context.window.MalievGeometry.generateThumbnails('https://storage.local/parts/bracket.stl', {}),
        /WebGL/);
});

test('generateThumbnails surfaces download failures and still cleans up', async () => {
    const state = makeState({ fetchShouldFail: true });
    const context = createInteropContext(state);

    await assert.rejects(
        () => context.window.MalievGeometry.generateThumbnails('https://storage.local/parts/bracket.stl', {}),
        /Download failed: 503/);
});
