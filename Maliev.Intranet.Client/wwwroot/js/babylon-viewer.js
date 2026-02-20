/**
 * BabylonJS 3D Viewer for Maliev Intranet
 * Lazy-loads BabylonJS from CDN and provides functions for Blazor JS interop.
 */

const engines = {};
const scenes = {};

/**
 * Lazy-loads a script from a URL
 * @param {string} url 
 * @returns {Promise}
 */
function loadScript(url) {
    return new Promise((resolve, reject) => {
        const scripts = Array.from(document.getElementsByTagName('script'));
        if (scripts.some(s => s.src === url)) {
            resolve();
            return;
        }
        const script = document.createElement('script');
        script.type = 'text/javascript';
        script.src = url;
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    });
}

/**
 * Initializes the BabylonJS viewer
 * @param {string} canvasId 
 * @param {string} glbUrl 
 */
export async function initialize(canvasId, glbUrl) {
    try {
        // Lazy load BabylonJS and Loaders
        await loadScript('https://cdn.babylonjs.com/babylon.js');
        await loadScript('https://cdn.babylonjs.com/loaders/babylonjs.loaders.min.js');

        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error(`Canvas with ID ${canvasId} not found.`);
            return;
        }

        const engine = new BABYLON.Engine(canvas, true);
        const scene = new BABYLON.Scene(engine);
        scene.clearColor = new BABYLON.Color4(0.95, 0.95, 0.95, 1);

        // Create default camera and light
        const camera = new BABYLON.ArcRotateCamera("camera", Math.PI / 2, Math.PI / 4, 10, BABYLON.Vector3.Zero(), scene);
        camera.attachControl(canvas, true);
        camera.wheelPrecision = 50;

        const light = new BABYLON.HemisphericLight("light", new BABYLON.Vector3(1, 1, 0), scene);

        // Load the GLB model
        BABYLON.SceneLoader.Append("", glbUrl, scene, (loadedScene) => {
            // Adjust camera to fit the model
            scene.createDefaultCameraOrLight(true, true, true);
            if (scene.activeCamera) {
                scene.activeCamera.attachControl(canvas, true);
            }
            
            // Set default rendering
            setRenderMode(canvasId, 'solid');
        }, null, (scene, message, exception) => {
            console.error("Error loading model:", message, exception);
        });

        engine.runRenderLoop(() => {
            scene.render();
        });

        const resizeHandler = () => {
            engine.resize();
        };
        window.addEventListener("resize", resizeHandler);
        engine._resizeHandler = resizeHandler;

        engines[canvasId] = engine;
        scenes[canvasId] = scene;

    } catch (error) {
        console.error("Failed to initialize BabylonJS viewer:", error);
    }
}

/**
 * Sets the rendering mode for the scene
 * @param {string} canvasId 
 * @param {string} mode 'solid' | 'wireframe' | 'xray'
 */
export function setRenderMode(canvasId, mode) {
    const scene = scenes[canvasId];
    if (!scene) return;

    scene.meshes.forEach(mesh => {
        if (!mesh.material) return;

        if (mode === 'wireframe') {
            mesh.material.wireframe = true;
            mesh.material.alpha = 1.0;
        } else if (mode === 'xray') {
            mesh.material.wireframe = false;
            mesh.material.alpha = 0.3;
        } else {
            mesh.material.wireframe = false;
            mesh.material.alpha = 1.0;
        }
    });
}

/**
 * Resets the camera to the default view
 * @param {string} canvasId 
 */
export function resetCamera(canvasId) {
    const scene = scenes[canvasId];
    if (!scene) return;

    const camera = scene.activeCamera;
    if (camera) {
        scene.createDefaultCamera(true, true, true);
        camera.attachControl(document.getElementById(canvasId), true);
    }
}

/**
 * Disposes of the engine and scene
 * @param {string} canvasId 
 */
export function dispose(canvasId) {
    const engine = engines[canvasId];
    const scene = scenes[canvasId];

    if (engine && engine._resizeHandler) {
        window.removeEventListener("resize", engine._resizeHandler);
    }

    if (scene) {
        scene.dispose();
        delete scenes[canvasId];
    }

    if (engine) {
        engine.dispose();
        delete engines[canvasId];
    }
}

// Also expose to window for easier debugging/non-module access if needed
window.babylonViewer = {
    initialize,
    setRenderMode,
    resetCamera,
    dispose
};
