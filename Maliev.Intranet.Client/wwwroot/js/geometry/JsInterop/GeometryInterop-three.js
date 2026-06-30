// Maliev.Intranet.Client/wwwroot/js/geometry/JsInterop/GeometryInterop-three.js
// Browser-local thumbnail generation for uploaded CAD meshes.
//
// Loaded as an ES module from index.html. Powered by three.js (self-hosted,
// no bundler) — see wwwroot/lib/three/.
//
// Exposes window.MalievGeometry:
//   generateThumbnails(fileUrl, options) → Promise<ThumbnailSet>
//   isWebGL2Available()                  → boolean
//
// ThumbnailSet matches Maliev.Intranet.Shared.Dtos.ThumbnailSetDto
// (camelCase keys, base64 PNG DataURLs with a transparent background):
//   frontSmall, backSmall, leftSmall, rightSmall, topSmall, bottomSmall,
//   thumbnailSmall (256px isometric), thumbnailLarge (1200px isometric).
//
// Models follow the MALIEV Z-up convention: front = looking along +Y,
// top = looking down -Z, isometric = from (+X, -Y, +Z).

import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { OBJLoader } from 'three/addons/loaders/OBJLoader.js';

const SMALL_SIZE = 256;
const LARGE_SIZE = 1200;
const DEFAULT_TIMEOUT_MS = 20000;

// Extensions three.js's vendored loaders can parse directly.
const LOADER_EXTENSIONS = ['.obj', '.glb', '.gltf'];
// Extensions parsed by the GeometryService browser runtime worker instead
// (single source of truth for mesh extraction — same worker the viewer uses).
const RUNTIME_EXTRACTION_EXTENSIONS = ['.stl', '.3mf'];
const SUPPORTED_EXTENSIONS = [...LOADER_EXTENSIONS, ...RUNTIME_EXTRACTION_EXTENSIONS];

const GEOMETRY_RUNTIME_MANIFEST_URL = '/api/v1/geometry/runtime/manifest';
const GEOMETRY_RUNTIME_ASSET_BASE_URL = '/api/v1/geometry/runtime/assets/';

function isWebGL2Available() {
    try {
        const canvas = document.createElement('canvas');
        return !!canvas.getContext('webgl2');
    } catch {
        return false;
    }
}

function resolveFileExtension(fileUrl, explicitExtension) {
    if (typeof explicitExtension === 'string' && explicitExtension.trim()) {
        const ext = explicitExtension.trim().toLowerCase();
        return ext.startsWith('.') ? ext : '.' + ext;
    }
    try {
        const pathname = new URL(fileUrl, window.location.href).pathname;
        const dot = pathname.lastIndexOf('.');
        return dot >= 0 ? pathname.slice(dot).toLowerCase() : '';
    } catch {
        return '';
    }
}

// Z-up view definitions. dir = unit vector from target toward the camera.
function buildViews() {
    const iso = 1 / Math.sqrt(3);
    return [
        { key: 'frontSmall', size: SMALL_SIZE, dir: [0, -1, 0], up: [0, 0, 1], ortho: true },
        { key: 'backSmall', size: SMALL_SIZE, dir: [0, 1, 0], up: [0, 0, 1], ortho: true },
        { key: 'leftSmall', size: SMALL_SIZE, dir: [-1, 0, 0], up: [0, 0, 1], ortho: true },
        { key: 'rightSmall', size: SMALL_SIZE, dir: [1, 0, 0], up: [0, 0, 1], ortho: true },
        { key: 'topSmall', size: SMALL_SIZE, dir: [0, 0, 1], up: [0, 1, 0], ortho: true },
        { key: 'bottomSmall', size: SMALL_SIZE, dir: [0, 0, -1], up: [0, -1, 0], ortho: true },
        { key: 'thumbnailSmall', size: SMALL_SIZE, dir: [iso, -iso, iso], up: [0, 0, 1], ortho: false },
        { key: 'thumbnailLarge', size: LARGE_SIZE, dir: [iso, -iso, iso], up: [0, 0, 1], ortho: false },
    ];
}

function computeWorldBounds(root) {
    root.updateMatrixWorld(true);
    const box = new THREE.Box3().setFromObject(root);
    if (box.isEmpty()) return null;
    return { min: box.min, max: box.max };
}

function makeNeutralMaterial() {
    return new THREE.MeshStandardMaterial({
        color: new THREE.Color(0.78, 0.82, 0.86),
        emissive: new THREE.Color(0.12, 0.13, 0.14),
        roughness: 0.7,
        metalness: 0.05,
        side: THREE.DoubleSide,
    });
}

function configureCamera(camera, view, center, radius) {
    camera.up.set(view.up[0], view.up[1], view.up[2]);

    if (view.ortho) {
        const distance = radius * 3;
        camera.position.set(
            center.x + view.dir[0] * distance,
            center.y + view.dir[1] * distance,
            center.z + view.dir[2] * distance,
        );
        const halfExtent = radius * 1.15;
        camera.left = -halfExtent;
        camera.right = halfExtent;
        camera.bottom = -halfExtent;
        camera.top = halfExtent;
        camera.near = Math.max(0.01, distance - radius * 2);
        camera.far = distance + radius * 4;
        camera.lookAt(center);
        camera.updateProjectionMatrix();
    } else {
        camera.fov = (0.6 * 180) / Math.PI;
        const fitDistance = (radius / Math.tan((camera.fov * Math.PI) / 180 / 2)) * 1.15;
        camera.position.set(
            center.x + view.dir[0] * fitDistance,
            center.y + view.dir[1] * fitDistance,
            center.z + view.dir[2] * fitDistance,
        );
        camera.near = Math.max(0.01, fitDistance - radius * 2);
        camera.far = fitDistance + radius * 4;
        camera.lookAt(center);
        camera.updateProjectionMatrix();
    }
}

function resolveRuntimeAssetUrl(assetPath) {
    const value = String(assetPath || '');
    if (value.includes('..') || value.includes('\\')) return null;
    const name = value.split('?')[0].split('/').pop();
    if (!name || name.includes('..') || name.includes('/') || name.includes('\\')) return null;
    return `${GEOMETRY_RUNTIME_ASSET_BASE_URL}${encodeURIComponent(name)}`;
}

// Extracts mesh buffers using the GeometryService-owned browser runtime worker
// (proxied via the BFF). Used for formats three.js's vendored loaders cannot
// parse, so mesh extraction stays on the single GeometryService runtime.
async function extractMeshViaGeometryRuntime(buffer, fileName, signal, timeoutMs) {
    const manifestResponse = await fetch(GEOMETRY_RUNTIME_MANIFEST_URL, { signal });
    if (!manifestResponse.ok) {
        throw new Error(`Geometry runtime manifest unavailable: ${manifestResponse.status}`);
    }
    const manifest = await manifestResponse.json();
    const workerUrl = resolveRuntimeAssetUrl(manifest.assets && manifest.assets.worker);
    if (!workerUrl) {
        throw new Error('Geometry runtime worker asset unavailable');
    }
    const wasmUrl = resolveRuntimeAssetUrl(manifest.assets && manifest.assets.wasm);

    return await new Promise((resolve, reject) => {
        const worker = new Worker(workerUrl, { name: 'maliev-geometry-thumbnails' });
        const messageId = `thumb:${fileName}:${Math.random().toString(36).slice(2)}`;
        const finish = (callback, value) => {
            clearTimeout(timeoutId);
            try { worker.terminate(); } catch { /* ignore */ }
            callback(value);
        };
        const timeoutId = setTimeout(
            () => finish(reject, new Error('Local thumbnail generation timeout')),
            timeoutMs);

        worker.onmessage = (event) => {
            const message = event.data || {};
            if (message.id !== messageId) return;
            if (!message.ok || !message.result?.meshBuffers) {
                finish(reject, new Error(message.error || 'Geometry runtime mesh extraction failed'));
                return;
            }
            finish(resolve, message.result.meshBuffers);
        };
        worker.onerror = (event) => {
            finish(reject, new Error((event && event.message) || 'Geometry runtime worker failed'));
        };
        worker.postMessage({
            id: messageId,
            operation: 'extract_mesh',
            input: { fileBytes: new Uint8Array(buffer), fileName },
            wasmUrl,
        });
    });
}

function buildMeshFromRuntimeBuffers(meshBuffers) {
    const positions = Float32Array.from(meshBuffers.positions || []);
    const indices = Array.from(meshBuffers.indices || [], Number);
    if (positions.length < 9 || indices.length < 3) return null;

    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
    geometry.setIndex(indices);
    geometry.computeVertexNormals();
    return new THREE.Mesh(geometry, makeNeutralMaterial());
}

async function generateThumbnails(fileUrl, options) {
    options = options || {};
    const timeoutMs = Number(options.timeoutMs) > 0 ? Number(options.timeoutMs) : DEFAULT_TIMEOUT_MS;

    if (!isWebGL2Available()) {
        throw new Error('WebGL unavailable for local thumbnail generation');
    }

    const extension = resolveFileExtension(fileUrl, options.fileExtension);
    if (!SUPPORTED_EXTENSIONS.includes(extension)) {
        throw new Error(`Unsupported format for local thumbnail generation: ${extension || 'unknown'}`);
    }

    const abortController = new AbortController();
    let timedOut = false;
    const timeoutId = setTimeout(() => {
        timedOut = true;
        abortController.abort('timeout');
    }, timeoutMs);

    let renderer = null;
    let blobUrl = null;
    try {
        const response = await fetch(fileUrl, { signal: abortController.signal });
        if (!response.ok) {
            throw new Error(`Download failed: ${response.status}`);
        }
        const blob = await response.blob();

        const canvas = document.createElement('canvas');
        canvas.width = SMALL_SIZE;
        canvas.height = SMALL_SIZE;
        renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true, preserveDrawingBuffer: true });
        renderer.setPixelRatio(1);
        renderer.outputColorSpace = THREE.SRGBColorSpace;

        const scene = new THREE.Scene();
        // Fully transparent background — thumbnails contain only the part so
        // they composite cleanly over light and dark UI themes.
        scene.background = null;

        const root = new THREE.Group();
        scene.add(root);

        let meshes;
        if (RUNTIME_EXTRACTION_EXTENSIONS.includes(extension)) {
            // Formats three.js's vendored loaders cannot parse go through the
            // GeometryService browser runtime worker for mesh extraction.
            const fileName = `thumbnail-source${extension}`;
            const meshBuffers = await extractMeshViaGeometryRuntime(
                await blob.arrayBuffer(), fileName, abortController.signal, timeoutMs);
            if (timedOut) throw new Error('Local thumbnail generation timeout');
            const runtimeMesh = buildMeshFromRuntimeBuffers(meshBuffers);
            if (!runtimeMesh) {
                throw new Error('No meshes found in file');
            }
            root.add(runtimeMesh);
            meshes = [runtimeMesh];
        } else {
            blobUrl = URL.createObjectURL(blob);
            let loaded;
            if (extension === '.obj') {
                loaded = await new OBJLoader().loadAsync(blobUrl);
            } else {
                const gltf = await new GLTFLoader().loadAsync(blobUrl);
                loaded = gltf.scene;
            }
            if (timedOut) throw new Error('Local thumbnail generation timeout');

            // glTF/GLB content is Y-up by spec — rotate roots into the MALIEV
            // Z-up convention (matches the main part-viewer behaviour).
            if (extension === '.glb' || extension === '.gltf') {
                loaded.quaternion.premultiply(new THREE.Quaternion().setFromAxisAngle(new THREE.Vector3(1, 0, 0), Math.PI / 2));
            }
            root.add(loaded);
            root.updateMatrixWorld(true);

            meshes = [];
            root.traverse((n) => { if (n.isMesh && n.geometry?.attributes?.position?.count > 0) meshes.push(n); });
            if (meshes.length === 0) {
                throw new Error('No meshes found in file');
            }
        }

        const bounds = computeWorldBounds(root);
        if (!bounds) {
            throw new Error('Mesh has no geometry to render');
        }
        const center = bounds.min.clone().add(bounds.max).multiplyScalar(0.5);
        const radius = Math.max(bounds.max.clone().sub(bounds.min).length() / 2, 1e-6);

        // Hemisphere key light along +Z (model up) plus a directional fill from
        // the isometric direction so face views are never fully unlit.
        scene.add(new THREE.HemisphereLight(0xffffff, 0x444444, 1.15 * Math.PI));
        const fillLight = new THREE.DirectionalLight(0xffffff, 0.85 * Math.PI);
        fillLight.position.set(1, -1, 1);
        scene.add(fillLight);

        const result = {
            frontSmall: '', backSmall: '', leftSmall: '', rightSmall: '',
            topSmall: '', bottomSmall: '', thumbnailSmall: '', thumbnailLarge: '',
        };

        for (const view of buildViews()) {
            if (timedOut) throw new Error('Local thumbnail generation timeout');
            renderer.setSize(view.size, view.size, false);
            const camera = view.ortho
                ? new THREE.OrthographicCamera(-1, 1, 1, -1, 0.01, 1000)
                : new THREE.PerspectiveCamera(45, 1, 0.01, 1000);
            configureCamera(camera, view, center, radius);
            renderer.render(scene, camera);
            // PNG keeps the alpha channel — JPEG would flatten the
            // transparent background to black.
            result[view.key] = canvas.toDataURL('image/png');
        }

        return result;
    } catch (error) {
        if (timedOut) throw new Error('Local thumbnail generation timeout');
        throw error;
    } finally {
        clearTimeout(timeoutId);
        if (blobUrl) { try { URL.revokeObjectURL(blobUrl); } catch { /* ignore */ } }
        if (renderer) { try { renderer.dispose(); } catch { /* ignore */ } }
    }
}

window.MalievGeometry = {
    generateThumbnails,
    isWebGL2Available,
};
