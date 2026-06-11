// Maliev.Intranet.Client/wwwroot/js/geometry/JsInterop/GeometryInterop.js
// Browser-local thumbnail generation for uploaded CAD meshes.
//
// Loaded as a CLASSIC script (no ES modules) from index.html AFTER
// lib/babylonjs/babylon.js and lib/babylonjs/babylonjs.loaders.min.js,
// so the global BABYLON runtime and its OBJ/glTF loaders are available.
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
(function () {
    'use strict';

    const SMALL_SIZE = 256;
    const LARGE_SIZE = 1200;
    const DEFAULT_TIMEOUT_MS = 20000;

    // Extensions the bundled BabylonJS loaders (+ built-in glTF) can parse.
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
            { key: 'frontSmall',     size: SMALL_SIZE, dir: [0, -1, 0],        up: [0, 0, 1],  ortho: true },
            { key: 'backSmall',      size: SMALL_SIZE, dir: [0, 1, 0],         up: [0, 0, 1],  ortho: true },
            { key: 'leftSmall',      size: SMALL_SIZE, dir: [-1, 0, 0],        up: [0, 0, 1],  ortho: true },
            { key: 'rightSmall',     size: SMALL_SIZE, dir: [1, 0, 0],         up: [0, 0, 1],  ortho: true },
            { key: 'topSmall',       size: SMALL_SIZE, dir: [0, 0, 1],         up: [0, 1, 0],  ortho: true },
            { key: 'bottomSmall',    size: SMALL_SIZE, dir: [0, 0, -1],        up: [0, -1, 0], ortho: true },
            { key: 'thumbnailSmall', size: SMALL_SIZE, dir: [iso, -iso, iso],  up: [0, 0, 1],  ortho: false },
            { key: 'thumbnailLarge', size: LARGE_SIZE, dir: [iso, -iso, iso],  up: [0, 0, 1],  ortho: false },
        ];
    }

    function computeWorldBounds(meshes) {
        let min = null;
        let max = null;
        for (const mesh of meshes) {
            if (!mesh.getTotalVertices || mesh.getTotalVertices() === 0) continue;
            mesh.computeWorldMatrix(true);
            const bounds = mesh.getBoundingInfo().boundingBox;
            min = min ? BABYLON.Vector3.Minimize(min, bounds.minimumWorld) : bounds.minimumWorld.clone();
            max = max ? BABYLON.Vector3.Maximize(max, bounds.maximumWorld) : bounds.maximumWorld.clone();
        }
        if (!min || !max) return null;
        return { min, max };
    }

    function applyNeutralMaterial(scene, meshes) {
        const material = new BABYLON.StandardMaterial('thumb_mat', scene);
        material.diffuseColor = new BABYLON.Color3(0.62, 0.66, 0.70);
        material.specularColor = new BABYLON.Color3(0.18, 0.18, 0.18);
        material.backFaceCulling = false;
        for (const mesh of meshes) {
            if (!mesh.material) mesh.material = material;
        }
    }

    function configureCamera(camera, view, center, radius, size) {
        const distance = radius * 3;
        camera.position = new BABYLON.Vector3(
            center.x + view.dir[0] * distance,
            center.y + view.dir[1] * distance,
            center.z + view.dir[2] * distance);
        camera.upVector = new BABYLON.Vector3(view.up[0], view.up[1], view.up[2]);
        camera.setTarget(center.clone());
        camera.minZ = Math.max(0.01, distance - radius * 2);
        camera.maxZ = distance + radius * 4;

        if (view.ortho) {
            camera.mode = BABYLON.Camera.ORTHOGRAPHIC_CAMERA;
            const halfExtent = radius * 1.15; // small margin around the model
            camera.orthoLeft = -halfExtent;
            camera.orthoRight = halfExtent;
            camera.orthoBottom = -halfExtent;
            camera.orthoTop = halfExtent;
        } else {
            camera.mode = BABYLON.Camera.PERSPECTIVE_CAMERA;
            camera.fov = 0.6;
            // Pull back so the bounding sphere fits the perspective frustum.
            const fitDistance = radius / Math.tan(camera.fov / 2) * 1.15;
            camera.position = new BABYLON.Vector3(
                center.x + view.dir[0] * fitDistance,
                center.y + view.dir[1] * fitDistance,
                center.z + view.dir[2] * fitDistance);
            camera.minZ = Math.max(0.01, fitDistance - radius * 2);
            camera.maxZ = fitDistance + radius * 4;
            camera.setTarget(center.clone());
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
    // (proxied via the BFF). Used for formats the bundled BabylonJS loaders cannot
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

            worker.onmessage = event => {
                const message = event.data || {};
                if (message.id !== messageId) return;
                if (!message.ok || !message.result?.meshBuffers) {
                    finish(reject, new Error(message.error || 'Geometry runtime mesh extraction failed'));
                    return;
                }
                finish(resolve, message.result.meshBuffers);
            };
            worker.onerror = event => {
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

    function buildMeshFromRuntimeBuffers(scene, meshBuffers) {
        const positions = Array.from(meshBuffers.positions || [], Number);
        const indices = Array.from(meshBuffers.indices || [], Number);
        if (positions.length < 9 || indices.length < 3) return null;

        const normals = [];
        BABYLON.VertexData.ComputeNormals(positions, indices, normals);

        const mesh = new BABYLON.Mesh('thumb_runtime_mesh', scene);
        const vertexData = new BABYLON.VertexData();
        vertexData.positions = positions;
        vertexData.indices = indices;
        vertexData.normals = normals;
        vertexData.applyToMesh(mesh);
        return mesh;
    }

    async function generateThumbnails(fileUrl, options) {
        options = options || {};
        const timeoutMs = Number(options.timeoutMs) > 0 ? Number(options.timeoutMs) : DEFAULT_TIMEOUT_MS;

        if (typeof BABYLON === 'undefined' || !BABYLON.Engine) {
            throw new Error('BabylonJS runtime is not loaded');
        }
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

        let engine = null;
        let scene = null;
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
            engine = new BABYLON.Engine(canvas, true, { preserveDrawingBuffer: true, premultipliedAlpha: false, alpha: true });
            scene = new BABYLON.Scene(engine);
            // Fully transparent background — thumbnails contain only the part so
            // they composite cleanly over light and dark UI themes.
            scene.clearColor = new BABYLON.Color4(0, 0, 0, 0);

            let meshes;
            if (RUNTIME_EXTRACTION_EXTENSIONS.includes(extension)) {
                // Formats the bundled loaders cannot parse go through the
                // GeometryService browser runtime worker for mesh extraction.
                const fileName = `thumbnail-source${extension}`;
                const meshBuffers = await extractMeshViaGeometryRuntime(
                    await blob.arrayBuffer(), fileName, abortController.signal, timeoutMs);
                if (timedOut) throw new Error('Local thumbnail generation timeout');
                const runtimeMesh = buildMeshFromRuntimeBuffers(scene, meshBuffers);
                if (!runtimeMesh) {
                    throw new Error('No meshes found in file');
                }
                meshes = [runtimeMesh];
            } else {
                blobUrl = URL.createObjectURL(blob);
                const importResult = await BABYLON.SceneLoader.ImportMeshAsync('', '', blobUrl, scene, null, extension);
                if (timedOut) throw new Error('Local thumbnail generation timeout');

                meshes = importResult.meshes.filter(m => m.getTotalVertices && m.getTotalVertices() > 0);
                if (meshes.length === 0) {
                    throw new Error('No meshes found in file');
                }

                // glTF/GLB content is Y-up by spec — rotate roots into the MALIEV
                // Z-up convention (matches the main part-viewer behaviour).
                if (extension === '.glb' || extension === '.gltf') {
                    const zUpQuat = BABYLON.Quaternion.RotationAxis(BABYLON.Axis.X, Math.PI / 2);
                    for (const node of importResult.meshes.filter(m => !m.parent)) {
                        if (node.rotationQuaternion == null) {
                            node.rotationQuaternion = (node.rotation && node.rotation.length())
                                ? BABYLON.Quaternion.FromEulerVector(node.rotation)
                                : BABYLON.Quaternion.Identity();
                        }
                        node.rotationQuaternion = zUpQuat.multiply(node.rotationQuaternion);
                    }
                    for (const mesh of importResult.meshes) mesh.computeWorldMatrix(true);
                }
            }

            applyNeutralMaterial(scene, meshes);

            const bounds = computeWorldBounds(meshes);
            if (!bounds) {
                throw new Error('Mesh has no geometry to render');
            }
            const center = BABYLON.Vector3.Center(bounds.min, bounds.max);
            const radius = Math.max(bounds.max.subtract(bounds.min).length() / 2, 1e-6);

            // Hemispheric key light along +Z (model up) plus a fill from the
            // isometric direction so face views are never fully unlit.
            const keyLight = new BABYLON.HemisphericLight('thumb_key', new BABYLON.Vector3(0, 0, 1), scene);
            keyLight.intensity = 0.9;
            const fillLight = new BABYLON.DirectionalLight('thumb_fill', new BABYLON.Vector3(-1, 1, -1), scene);
            fillLight.intensity = 0.5;

            const camera = new BABYLON.FreeCamera('thumb_cam', BABYLON.Vector3.Zero(), scene);
            scene.activeCamera = camera;

            await scene.whenReadyAsync();

            const result = {
                frontSmall: '', backSmall: '', leftSmall: '', rightSmall: '',
                topSmall: '', bottomSmall: '', thumbnailSmall: '', thumbnailLarge: '',
            };

            for (const view of buildViews()) {
                if (timedOut) throw new Error('Local thumbnail generation timeout');
                engine.setSize(view.size, view.size);
                configureCamera(camera, view, center, radius, view.size);
                scene.render();
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
            if (scene) { try { scene.dispose(); } catch { /* ignore */ } }
            if (engine) { try { engine.dispose(); } catch { /* ignore */ } }
        }
    }

    window.MalievGeometry = {
        generateThumbnails: generateThumbnails,
        isWebGL2Available: isWebGL2Available,
    };
})();
