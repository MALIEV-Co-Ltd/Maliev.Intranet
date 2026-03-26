/**
 * BlocksCubesIcon - Animated 3D cubes icon using BabylonJS.
 * Used in the "View in 3D" button on ProjectNew.razor.
 *
 * Three isometric cubes in MALIEV brand colors:
 * - Top cube:  primary blue (#1b6ec2)
 * - Bottom two cubes: dark navy (#1a1a1a)
 *
 * Animation: on hover, cubes animate from scattered → assembled positions.
 * Uses BabylonJS loaded lazily from CDN (same CDN as babylon-viewer.js).
 */

const iconEngines = {};

function loadScript(url) {
    return new Promise((resolve, reject) => {
        if (Array.from(document.scripts).some(s => s.src === url)) {
            resolve();
            return;
        }
        const script = document.createElement('script');
        script.type = 'text/javascript';
        script.src = url;
        script.onload = resolve;
        script.onerror = () => reject(new Error(`Failed to load script: ${url}`));
        document.head.appendChild(script);
    });
}

/**
 * Creates the ISO camera for the icon scene.
 * True isometric: alpha = -π/4, beta = π/3, radius irrelevant (orthographic).
 */
function createIsoCamera(scene, canvas) {
    // Orthographic camera for true isometric projection
    const camera = new BABYLON.UniversalCamera(
        'iconCam',
        new BABYLON.Vector3(0, 0, -10),
        scene
    );
    camera.setTarget(BABYLON.Vector3.Zero());

    // True isometric angles
    camera.alpha = -Math.PI / 4;        // 45° from front
    camera.beta = Math.PI / 3;          // ~60° from top (true isometric)

    // Orthographic projection — scale determines how "zoommed in" the cubes look
    camera.mode = BABYLON.Camera.ORTHOGRAPHIC_CAMERA;
    const aspect = canvas.height / canvas.width;
    const halfSize = 1.4;
    camera.orthoTop    =  halfSize;
    camera.orthoBottom = -halfSize;
    camera.orthoLeft   = -halfSize / aspect;
    camera.orthoRight  =  halfSize / aspect;

    // No user interaction — camera is locked
    camera.inputs.clear();

    return camera;
}

/**
 * Creates three box meshes: top cube + two bottom cubes.
 * Returns { topCube, bottomLeft, bottomRight }.
 */
function createCubes(scene) {
    // Top cube — primary blue
    const topCube = BABYLON.MeshBuilder.CreateBox('topCube', { size: 0.9 }, scene);
    const topMat = new BABYLON.StandardMaterial('topMat', scene);
    topMat.diffuseColor = new BABYLON.Color3.FromHexString('#1b6ec2');
    topMat.specularColor = new BABYLON.Color3(0.15, 0.15, 0.15);
    topMat.emissiveColor = new BABYLON.Color3(0.04, 0.04, 0.07);
    topCube.material = topMat;
    topCube.position.y = 0.52;

    // Bottom-left cube — dark navy
    const blCube = BABYLON.MeshBuilder.CreateBox('blCube', { size: 0.9 }, scene);
    const blMat = new BABYLON.StandardMaterial('blMat', scene);
    blMat.diffuseColor = new BABYLON.Color3.FromHexString('#1a1a1a');
    blMat.specularColor = new BABYLON.Color3(0.1, 0.1, 0.1);
    blMat.emissiveColor = new BABYLON.Color3(0.02, 0.02, 0.02);
    blCube.material = blMat;
    blCube.position.set(-0.48, -0.4, 0);

    // Bottom-right cube — dark navy
    const brCube = BABYLON.MeshBuilder.CreateBox('brCube', { size: 0.9 }, scene);
    const brMat = new BABYLON.StandardMaterial('brMat', scene);
    brMat.diffuseColor = new BABYLON.Color3.FromHexString('#1a1a1a');
    brMat.specularColor = new BABYLON.Color3(0.1, 0.1, 0.1);
    brMat.emissiveColor = new BABYLON.Color3(0.02, 0.02, 0.02);
    brCube.material = brMat;
    brCube.position.set(0.48, -0.4, 0);

    return { topCube, bottomLeft: blCube, bottomRight: brCube };
}

/**
 * Creates looping position animations for each cube.
 * Animates from scattered (hover-in) to assembled (hover-out) positions.
 * Returns { topAnim, blAnim, brAnim }.
 */
function createAnimations(scene) {
    const fps = 30;
    const frameCount = 40; // ~1.3s per direction at 30fps

    function makeAnim(name, mesh, fromPos, toPos) {
        const anim = new BABYLON.Animation(
            name, 'position', fps,
            BABYLON.Animation.ANIMATIONTYPE_VECTOR3,
            BABYLON.Animation.ANIMATIONLOOPMODE_CYCLE
        );
        const ease = new BABYLON.SineEase();
        ease.setEasingMode(BABYLON.EasingFunction.EASINGMODE_EASEINOUT);
        anim.setEasingFunction(ease);
        anim.setKeys([
            { frame: 0,      value: fromPos.clone() },
            { frame: frameCount, value: toPos.clone() },
        ]);
        mesh.animations = [anim];
        return anim;
    }

    const assembled = {
        top:        new BABYLON.Vector3(0,     0.52, 0),
        bottomLeft: new BABYLON.Vector3(-0.48, -0.4, 0),
        bottomRight:new BABYLON.Vector3(0.48,  -0.4, 0),
    };

    const scattered = {
        top:        new BABYLON.Vector3(0,     1.6,  0),   // flies up
        bottomLeft: new BABYLON.Vector3(-1.4, -1.0, 0),  // flies left
        bottomRight:new BABYLON.Vector3(1.4,  -1.0, 0),  // flies right
    };

    const cubes = scene.meshes.filter(m => m.name !== 'iconCam');
    const [topCube, blCube, brCube] = cubes;

    const topAnim = makeAnim('topAnim', topCube, assembled.top, scattered.top);
    const blAnim  = makeAnim('blAnim',  blCube,  assembled.bottomLeft,  scattered.bottomLeft);
    const brAnim  = makeAnim('brAnim',  brCube,  assembled.bottomRight, scattered.bottomRight);

    return { topAnim, blAnim, brAnim, frameCount };
}

/**
 * Initializes the animated cubes icon on a canvas element.
 * @param {string} canvasId  The id of the <canvas> element.
 * @param {number} size      Pixel size (canvas rendered square, CSS controls display size).
 */
export async function initBlocksCubesIcon(canvasId, size) {
    await loadScript('https://cdn.babylonjs.com/babylon.js');

    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    // If already initialized, dispose first
    if (iconEngines[canvasId]) {
        iconEngines[canvasId].dispose();
        delete iconEngines[canvasId];
    }

    const engine = new BABYLON.Engine(canvas, false, {
        preserveDrawingBuffer: false,
        stencil: false,
        disableWebGL2Support: false,
    });
    iconEngines[canvasId] = engine;

    const scene = new BABYLON.Scene(engine);
    scene.clearColor = new BABYLON.Color4(0, 0, 0, 0); // transparent

    // Camera
    createIsoCamera(scene, canvas);

    // Lighting — soft ambient + directional
    const hemi = new BABYLON.HemisphericLight('hemi', new BABYLON.Vector3(0, 1, 0), scene);
    hemi.intensity = 0.7;
    hemi.groundColor = new BABYLON.Color3(0.2, 0.2, 0.25);

    const dir = new BABYLON.DirectionalLight('dir', new BABYLON.Vector3(-1, -2, 1), scene);
    dir.intensity = 0.5;

    // Cubes
    const { topAnim, blAnim, brAnim, frameCount } = createAnimations(scene);
    const cubes = scene.meshes.filter(m => m.name !== 'iconCam');

    let isHovered = false;
    let activeAnimatables = [];

    function startAnimation() {
        // Reset to scattered positions first so animation is visible
        cubes[0].position = new BABYLON.Vector3(0, 1.6, 0);
        cubes[1].position = new BABYLON.Vector3(-1.4, -1.0, 0);
        cubes[2].position = new BABYLON.Vector3(1.4, -1.0, 0);

        scene.stopAllAnimations();
        activeAnimatables = [];

        const topInst = scene.beginAnimation(cubes[0], 0, frameCount, true);
        const blInst  = scene.beginAnimation(cubes[1], 0, frameCount, true);
        const brInst  = scene.beginAnimation(cubes[2], 0, frameCount, true);
        activeAnimatables = [topInst, blInst, brInst];
    }

    function stopAnimation() {
        scene.stopAllAnimations();
        activeAnimatables = [];

        // Snap back to scattered positions
        cubes[0].position = new BABYLON.Vector3(0, 1.6, 0);
        cubes[1].position = new BABYLON.Vector3(-1.4, -1.0, 0);
        cubes[2].position = new BABYLON.Vector3(1.4, -1.0, 0);
    }

    // Start in scattered (disassembled) state
    stopAnimation();

    // Hover events
    const wrapper = canvas.parentElement;
    wrapper.addEventListener('pointerenter', () => {
        isHovered = true;
        startAnimation();
    });
    wrapper.addEventListener('pointerleave', () => {
        isHovered = false;
        stopAnimation();
    });

    // Initial render then stop
    engine.runRenderLoop(() => { scene.render(); });
    scene.stopAllAnimations();

    // Handle resize
    canvas._iconResizeObserver = new ResizeObserver(() => {
        engine.resize();
    });
    canvas._iconResizeObserver.observe(canvas);
}

/**
 * Disposes the BabylonJS engine for a given canvas.
 * Called automatically when the Blazor component is disposed.
 */
export function disposeBlocksCubesIcon(canvasId) {
    if (iconEngines[canvasId]) {
        iconEngines[canvasId].dispose();
        delete iconEngines[canvasId];
    }
    const canvas = document.getElementById(canvasId);
    if (canvas && canvas._iconResizeObserver) {
        canvas._iconResizeObserver.disconnect();
        delete canvas._iconResizeObserver;
    }
}

// Expose for Blazor JS interop
window.blocksCubesIcon = { init: initBlocksCubesIcon, dispose: disposeBlocksCubesIcon };

// ES module export — named export "init" matches Blazor's InvokeVoidAsync("init", ...)
export const init = initBlocksCubesIcon;
export const dispose = disposeBlocksCubesIcon;
