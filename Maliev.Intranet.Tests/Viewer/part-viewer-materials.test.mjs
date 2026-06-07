import assert from 'node:assert/strict';
import fs from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

class Color3 {
    constructor(r = 0, g = 0, b = 0) {
        this.r = r;
        this.g = g;
        this.b = b;
    }

    set(r, g, b) {
        this.r = r;
        this.g = g;
        this.b = b;
    }
}

class Vector3 {
    constructor(x = 0, y = 0, z = 0) {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}

function loadViewerContext() {
    const rawCubeTextures = [];
    const scheduledIdleCallbacks = [];
    class NodeMaterialConnectionPoint {
        connectTo() {}
    }

    class NodeMaterialBlockStub {
        constructor(name) {
            this.name = name;
            this.output = new NodeMaterialConnectionPoint();
            this.xyz = new NodeMaterialConnectionPoint();
            this.rgb = new NodeMaterialConnectionPoint();
            this.rgba = new NodeMaterialConnectionPoint();
            this.lighting = new NodeMaterialConnectionPoint();
            this.reflection = new NodeMaterialConnectionPoint();
            this.vector = new NodeMaterialConnectionPoint();
            this.transform = new NodeMaterialConnectionPoint();
            this.worldPosition = new NodeMaterialConnectionPoint();
            this.worldNormal = new NodeMaterialConnectionPoint();
            this.view = new NodeMaterialConnectionPoint();
            this.cameraPosition = new NodeMaterialConnectionPoint();
            this.perturbedNormal = new NodeMaterialConnectionPoint();
            this.baseColor = new NodeMaterialConnectionPoint();
            this.metallic = new NodeMaterialConnectionPoint();
            this.roughness = new NodeMaterialConnectionPoint();
            this.position = new NodeMaterialConnectionPoint();
            this.world = new NodeMaterialConnectionPoint();
            this.input = new NodeMaterialConnectionPoint();
            this.factor = new NodeMaterialConnectionPoint();
            this.left = new NodeMaterialConnectionPoint();
            this.right = new NodeMaterialConnectionPoint();
            this.seed = new NodeMaterialConnectionPoint();
            this.xyzw = new NodeMaterialConnectionPoint();
            this.x = new NodeMaterialConnectionPoint();
            this.y = new NodeMaterialConnectionPoint();
            this.z = new NodeMaterialConnectionPoint();
        }

        setAsAttribute(attributeName) {
            this.attributeName = attributeName;
        }

        setAsSystemValue(systemValue) {
            this.systemValue = systemValue;
        }
    }

    const context = {
        console,
        document: {
            getElementById: () => null,
        },
        window: {},
        globalThis: {},
        rawCubeTextures,
        scheduledIdleCallbacks,
        requestIdleCallback: callback => {
            scheduledIdleCallbacks.push(callback);
            return scheduledIdleCallbacks.length;
        },
        cancelIdleCallback: () => {},
        BABYLON: {
            Axis: {
                X: new Vector3(1, 0, 0),
                Y: new Vector3(0, 1, 0),
                Z: new Vector3(0, 0, 1),
            },
            Color3,
            ImageProcessingConfiguration: {
                TONEMAPPING_STANDARD: 1,
            },
            Material: {
                MATERIAL_OPAQUE: 0,
                MATERIAL_ALPHABLEND: 2,
            },
            MaterialPluginBase: class MaterialPluginBase {
                constructor(material, name, priority, defines) {
                    this.material = material;
                    this.name = name;
                    this.priority = priority;
                    this.defines = defines;
                    this._isEnabled = false;
                    material._pluginInstances ??= [];
                    material._pluginInstances.push(this);
                }

                _enable(enabled) {
                    if (enabled === true) {
                        this.material._activatedPlugins ??= [];
                        this.material._activatedPlugins.push(this.name);
                    }

                    this._enableCalls = (this._enableCalls ?? 0) + 1;
                }
            },
            Matrix: {
                Identity: () => ({}),
                RotationX: () => ({}),
            },
            Mesh: class Mesh {
                constructor(name, scene) {
                    this.name = name;
                    this.metadata = {};
                    scene?.meshes?.push(this);
                }
            },
            PBRMaterial: class PBRMaterial {
                constructor(name, scene) {
                    this.name = name;
                    this._scene = scene;
                    this.albedoColor = new Color3();
                    this.metallic = 0;
                    this.roughness = 0;
                    this.subSurface = {};
                    scene?.materials?.push(this);
                }

                getScene() {
                    return this._scene;
                }
            },
            NodeMaterial: class NodeMaterial {
                constructor(name, scene) {
                    this.name = name;
                    this.metadata = {};
                    this.outputNodes = [];
                    scene?.materials?.push(this);
                }

                addOutputNode(node) {
                    this.outputNodes.push(node);
                }

                build() {
                    this.wasBuilt = true;
                }
            },
            NodeMaterialBlockTargets: {
                Vertex: 1,
                Fragment: 2,
                VertexAndFragment: 3,
                Neutral: 4,
            },
            NodeMaterialModes: {
                Material: 0,
            },
            NodeMaterialSystemValues: {
                World: 1,
                View: 2,
                ViewProjection: 4,
                CameraPosition: 7,
            },
            WaveBlockKind: {
                SawTooth: 0,
                Square: 1,
                Triangle: 2,
            },
            InputBlock: NodeMaterialBlockStub,
            TransformBlock: NodeMaterialBlockStub,
            VertexOutputBlock: NodeMaterialBlockStub,
            FragmentOutputBlock: NodeMaterialBlockStub,
            PBRMetallicRoughnessBlock: NodeMaterialBlockStub,
            ReflectionBlock: NodeMaterialBlockStub,
            HeightToNormalBlock: NodeMaterialBlockStub,
            SimplexPerlin3DBlock: NodeMaterialBlockStub,
            ScaleBlock: NodeMaterialBlockStub,
            AddBlock: NodeMaterialBlockStub,
            MultiplyBlock: NodeMaterialBlockStub,
            TrigonometryBlock: NodeMaterialBlockStub,
            TrigonometryBlockOperations: {
                Cos: 0,
                Sin: 1,
            },
            WaveBlock: NodeMaterialBlockStub,
            VectorSplitterBlock: NodeMaterialBlockStub,
            RawCubeTexture: class RawCubeTexture {
                constructor(scene, faces, size, format, type, generateMipMaps, invertY, samplingMode) {
                    this.scene = scene;
                    this.faces = faces;
                    this.size = size;
                    this.format = format;
                    this.type = type;
                    this.generateMipMaps = generateMipMaps;
                    this.invertY = invertY;
                    this.samplingMode = samplingMode;
                    rawCubeTextures.push(this);
                }
            },
            ShadowGenerator: Object.assign(class ShadowGenerator {
                constructor(size, light) {
                    this.size = size;
                    this.light = light;
                    this.casters = [];
                }

                addShadowCaster(mesh) {
                    this.casters.push(mesh);
                }

                setDarkness(value) {
                    this.darkness = value;
                    this._darkness = value;
                }

                dispose() {
                    this.disposed = true;
                }
            }, {
                QUALITY_HIGH: 2,
            }),
            SSAO2RenderingPipeline: class SSAO2RenderingPipeline {
                constructor(name, scene, ratio, cameras) {
                    this.name = name;
                    this.scene = scene;
                    this.ratio = ratio;
                    this.cameras = cameras;
                }

                dispose() {
                    this.disposed = true;
                }
            },
            StandardMaterial: class StandardMaterial {
                constructor(name, scene) {
                    this.name = name;
                    scene?.materials?.push(this);
                }
            },
            Constants: {
                TEXTUREFORMAT_RGBA: 5,
                TEXTURETYPE_UNSIGNED_BYTE: 0,
                TEXTURE_TRILINEAR_SAMPLINGMODE: 3,
            },
            Vector3,
            VertexData: class VertexData {
                applyToMesh(mesh) {
                    mesh.vertexData = {
                        positions: this.positions,
                        indices: this.indices,
                        normals: this.normals,
                        uvs: this.uvs,
                    };
                }

                static ComputeNormals(_positions, _indices, normals) {
                    normals.length = 0;
                }
            },
            VertexBuffer: {
                NormalKind: 'normal',
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

function flushScheduledIdleCallbacks(context) {
    while (context.scheduledIdleCallbacks.length > 0) {
        const callback = context.scheduledIdleCallbacks.shift();
        callback({ didTimeout: false, timeRemaining: () => 16 });
    }
}

function makeScene(mesh) {
    return {
        meshes: [mesh],
        materials: [],
        environmentIntensity: 0,
        environmentTexture: null,
        imageProcessingConfiguration: {},
        activeCamera: { name: 'active-camera' },
        getMaterialByName(name) {
            return this.materials.find(material => material.name === name) ?? null;
        },
    };
}

function sampleRgb(face, size, x, y) {
    const offset = (y * size + x) * 4;
    return {
        r: face[offset],
        g: face[offset + 1],
        b: face[offset + 2],
    };
}

function colorDistance(a, b) {
    return Math.sqrt(
        ((a.r - b.r) * (a.r - b.r))
        + ((a.g - b.g) * (a.g - b.g))
        + ((a.b - b.b) * (a.b - b.b)));
}

test('solid CAD render mode prepares PBR environment lighting on first material assignment', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        setRenderMode('viewer', 'solid');
        ({
            environmentCreated: !!scene.environmentTexture,
            rawCubeTextureCount: rawCubeTextures.length,
            environmentIntensity: scene.environmentIntensity,
            toneMappingEnabled: scene.imageProcessingConfiguration.toneMappingEnabled === true,
            materialName: scene.meshes[0].material?.name ?? null
        });
    `, context);

    assert.equal(result.materialName, '__cad_gray__');
    assert.equal(result.environmentCreated, true);
    assert.equal(result.rawCubeTextureCount, 1);
    assert.equal(result.environmentIntensity, 1);
    assert.equal(result.toneMappingEnabled, true);
});

test('procedural studio reflection map avoids flat grey cube-room side panels', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        setRenderMode('viewer', 'solid');
        const cube = rawCubeTextures[0];
        ({
            size: cube.size,
            posX: Array.from(cube.faces[0])
        });
    `, context);

    const sideFace = result.posX;
    const size = result.size;
    const middleY = Math.floor(size * 0.50);
    const left = sampleRgb(sideFace, size, Math.floor(size * 0.18), middleY);
    const right = sampleRgb(sideFace, size, Math.floor(size * 0.82), middleY);
    const horizontalVariation = colorDistance(left, right);

    assert.ok(size <= 256, `expected lightweight synthetic environment, got ${size}px cubemap`);
    assert.ok(
        horizontalVariation >= 18,
        `expected non-flat studio side reflection, got horizontal color distance ${horizontalVariation}`);
});

test('realistic render mode assigns a PBRMaterial that uses the scene environment (regression: metals rendered black via broken NodeMaterial reflection)', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        setMaterialType('viewer', 'aluminum');
        setRenderMode('viewer', 'realistic');
        const m = scene.meshes[0].material;
        ({
            isPbr: m instanceof BABYLON.PBRMaterial,
            isNodeMaterial: m instanceof BABYLON.NodeMaterial,
            metallic: m?.metallic ?? null,
            materialName: m?.name ?? null,
            environmentCreated: !!scene.environmentTexture,
            forceIrradianceInFragment: m?.forceIrradianceInFragment ?? null,
            realTimeFiltering: m?.realTimeFiltering ?? null,
        });
    `, context);

    // Metallic aluminum (metallic 0.95) derives its whole appearance from the environment
    // reflection. The old hand-wired NodeMaterial left its ReflectionBlock untextured, so
    // metals rendered pure black. A PBRMaterial + a prepared scene environment fixes it.
    assert.equal(result.isPbr, true);
    assert.equal(result.isNodeMaterial, false);
    assert.equal(result.materialName, '__realistic_aluminum__');
    assert.ok(result.metallic > 0.5, 'aluminum realistic preset should stay metallic');
    assert.equal(result.environmentCreated, true);
    assert.equal(result.forceIrradianceInFragment, true);
    // realTimeFiltering must NOT be enabled (expensive + driver-dependent flicker risk).
    assert.notEqual(result.realTimeFiltering, true);
});

test('transparent manufacturing presets use alpha-blended PBR material depth handling', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        const snapshot = (materialKey, colorHex, processCode) => {
            configureMaterialFromConfigurator('viewer', materialKey, colorHex, 'AS_PRINTED', null, processCode);
            setRenderMode('viewer', 'realistic');
            const material = scene.meshes[0].material;
            return {
                materialName: material?.name ?? null,
                isPbr: material instanceof BABYLON.PBRMaterial,
                alpha: material?.alpha ?? null,
                transparencyMode: material?.transparencyMode ?? null,
                needDepthPrePass: material?.needDepthPrePass ?? null,
                separateCullingPass: material?.separateCullingPass ?? null,
                backFaceCulling: material?.backFaceCulling ?? null,
                metallic: material?.metallic ?? null,
                roughness: material?.roughness ?? null,
                albedoB: material?.albedoColor?.b ?? null,
                indexOfRefraction: material?.indexOfRefraction ?? null,
                linkRefractionWithTransparency: material?.linkRefractionWithTransparency ?? null,
                useRadianceOverAlpha: material?.useRadianceOverAlpha ?? null,
                useSpecularOverAlpha: material?.useSpecularOverAlpha ?? null,
                refractionTextureUsesEnvironment: material?.subSurface?.refractionTexture === scene.environmentTexture,
                refractionEnabled: material?.subSurface?.isRefractionEnabled ?? null,
                translucencyEnabled: material?.subSurface?.isTranslucencyEnabled ?? null,
                refractionIntensity: material?.subSurface?.refractionIntensity ?? null,
                translucencyIntensity: material?.subSurface?.translucencyIntensity ?? null
            };
        };
        ({
            petg: snapshot('petg-clear', '#f6fbff', 'FDM'),
            resin: snapshot('resin-clear', null, 'SLA_DLP'),
            acrylic: snapshot('acrylic-clear', null, 'CNC_MILL')
        });
    `, context);

    for (const [name, material] of Object.entries(result)) {
        assert.equal(material.isPbr, true, `${name} should render through the PBR pipeline`);
        assert.equal(material.transparencyMode, context.BABYLON.Material.MATERIAL_ALPHABLEND, `${name} should use alpha blend`);
        assert.equal(material.needDepthPrePass, true, `${name} should write a depth pre-pass for stable transparent sorting`);
        assert.equal(material.separateCullingPass, true, `${name} should draw back/front faces separately`);
        assert.equal(material.backFaceCulling, false, `${name} should keep interior transparent surfaces visible`);
        assert.equal(material.metallic, 0, `${name} should be dielectric, not metal`);
        assert.ok(material.alpha > 0.30 && material.alpha < 0.70, `${name} should be translucent, got alpha ${material.alpha}`);
        assert.ok(material.roughness <= 0.16, `${name} should stay clear/glossy instead of frosted matte, got ${material.roughness}`);
        assert.ok(material.albedoB >= 0.93, `${name} should keep a bright clear-material albedo, got blue channel ${material.albedoB}`);
        assert.ok(material.indexOfRefraction >= 1.45 && material.indexOfRefraction <= 1.55, `${name} should use plastic/resin IOR, got ${material.indexOfRefraction}`);
        assert.equal(material.linkRefractionWithTransparency, true, `${name} should link alpha to PBR refraction instead of rendering as a dark alpha shell`);
        assert.equal(material.useRadianceOverAlpha, true, `${name} should keep environment radiance visible through transparent pixels`);
        assert.equal(material.useSpecularOverAlpha, true, `${name} should keep clear-material highlights visible through transparent pixels`);
        assert.equal(material.refractionTextureUsesEnvironment, true, `${name} should refract the studio environment instead of sampling an empty black refraction texture`);
        assert.equal(material.refractionEnabled, true, `${name} should enable PBR refraction`);
        assert.equal(material.translucencyEnabled, true, `${name} should enable PBR translucency`);
        assert.ok(material.refractionIntensity > 0 && material.refractionIntensity <= 1, `${name} should carry bounded refraction intensity`);
        assert.ok(material.translucencyIntensity > 0 && material.translucencyIntensity <= 1, `${name} should carry bounded translucency intensity`);
    }

    assert.equal(result.petg.materialName, '__realistic_petg-clear__');
    assert.equal(result.resin.materialName, '__realistic_resin-clear__');
    assert.equal(result.acrylic.materialName, '__realistic_acrylic-clear__');
});

test('transparent realistic material refraction tracks the promoted studio environment texture', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const firstPaint = vm.runInContext(`
        (() => {
            scenes.viewer = scene;
            configureMaterialFromConfigurator('viewer', 'acrylic-clear', null, 'AS_MACHINED', null, 'CNC_MILL');
            setRenderMode('viewer', 'realistic');
            const material = scene.meshes[0].material;
            const fastTexture = scene.environmentTexture;
            return {
                refractionTextureUsesFastEnvironment: material?.subSurface?.refractionTexture === fastTexture,
                environmentQuality: fastTexture?._malievEnvironmentQuality ?? null
            };
        })();
    `, context);

    assert.equal(firstPaint.environmentQuality, 'fast');
    assert.equal(
        firstPaint.refractionTextureUsesFastEnvironment,
        true,
        'transparent first paint should refract the fast studio environment instead of an empty texture');

    flushScheduledIdleCallbacks(context);

    const promoted = vm.runInContext(`
        (() => {
            const material = scene.meshes[0].material;
            return {
                refractionTextureUsesPromotedEnvironment: material?.subSurface?.refractionTexture === scene.environmentTexture,
                environmentQuality: scene.environmentTexture?._malievEnvironmentQuality ?? null,
                refractionQuality: material?.subSurface?.refractionTexture?._malievEnvironmentQuality ?? null
            };
        })();
    `, context);

    assert.equal(promoted.environmentQuality, 'high');
    assert.equal(promoted.refractionQuality, 'high');
    assert.equal(
        promoted.refractionTextureUsesPromotedEnvironment,
        true,
        'transparent material refraction should follow the promoted high-quality studio environment');
});

test('transparent realistic presets stay translucent when opaque CNC finishes are selected', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'clear-part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        const snapshot = (materialKey, finishCode, processCode) => {
            configureMaterialFromConfigurator('viewer', materialKey, null, finishCode, 'RA_3_2', processCode);
            setRenderMode('viewer', 'realistic');
            const material = scene.meshes[0].material;
            return {
                materialName: material?.name ?? null,
                alpha: material?.alpha ?? null,
                transparencyMode: material?.transparencyMode ?? null,
                metallic: material?.metallic ?? null,
                roughness: material?.roughness ?? null,
                effectKey: material?._malievSurfaceEffect?.key ?? null,
                linkRefractionWithTransparency: material?.linkRefractionWithTransparency ?? null,
                useRadianceOverAlpha: material?.useRadianceOverAlpha ?? null,
                refractionEnabled: material?.subSurface?.isRefractionEnabled ?? null
            };
        };
        ({
            brushedAcrylic: snapshot('acrylic-clear', 'BRUSHED', 'CNC_MILL'),
            beadBlastAcrylic: snapshot('acrylic-clear', 'BEAD_BLAST', 'CNC_MILL'),
            polishedResin: snapshot('resin-clear', 'MIRROR_POLISH', 'SLA_DLP')
        });
    `, context);

    for (const [name, material] of Object.entries(result)) {
        assert.equal(material.transparencyMode, context.BABYLON.Material.MATERIAL_ALPHABLEND, `${name} should stay alpha blended`);
        assert.ok(material.alpha > 0.30 && material.alpha < 0.70, `${name} should stay translucent, got alpha ${material.alpha}`);
        assert.equal(material.metallic, 0, `${name} should stay dielectric, got metallic ${material.metallic}`);
        assert.equal(material.effectKey, null, `${name} should not use opaque surface darkening plugins`);
        assert.equal(material.linkRefractionWithTransparency, true, `${name} should keep refraction linked to alpha`);
        assert.equal(material.useRadianceOverAlpha, true, `${name} should keep environment radiance through alpha`);
        assert.equal(material.refractionEnabled, true, `${name} should keep PBR refraction enabled`);
        assert.ok(material.roughness <= 0.28, `${name} should stay translucent instead of heavily frosted/black, got roughness ${material.roughness}`);
    }

    assert.equal(result.brushedAcrylic.materialName, '__realistic_acrylic-clear__');
    assert.equal(result.beadBlastAcrylic.materialName, '__realistic_acrylic-clear__');
    assert.equal(result.polishedResin.materialName, '__realistic_resin-clear__');
});

test('viewer module imports before Babylon globals are loaded', async () => {
    const previousWindow = globalThis.window;
    delete globalThis.BABYLON;
    globalThis.window = {};

    try {
        const viewerPath = new URL(
            `../../Maliev.Intranet.Client/wwwroot/js/part-viewer.js?import-before-babylon=${Date.now()}`,
            import.meta.url);

        await import(viewerPath.href);
    } finally {
        if (previousWindow === undefined) {
            delete globalThis.window;
        } else {
            globalThis.window = previousWindow;
        }
    }
});

test('studio lighting keeps shadows soft enough for dark studio mode', () => {
    const context = loadViewerContext();

    const result = vm.runInContext(`({
        light: CONFIG.STUDIO_LIGHT.key.shadowDarkness,
        dark: CONFIG.STUDIO_DARK.key.shadowDarkness,
        generator: (() => {
            const shadow = new BABYLON.ShadowGenerator(1024, {});
            configureSoftShadowGenerator(shadow, CONFIG.STUDIO_LIGHT.key);
            return {
                usesPcf: shadow.usePercentageCloserFiltering,
                filteringQuality: shadow.filteringQuality,
                transparencyShadow: shadow.transparencyShadow,
                darkness: shadow._darkness,
                bias: shadow.bias,
                normalBias: shadow.normalBias,
                contactHardening: shadow.useContactHardeningShadow,
                contactSize: shadow.contactHardeningLightSizeUVRatio
            };
        })()
    })`, context);

    assert.ok(result.light <= 0.12);
    assert.ok(result.dark <= 0.10);
    assert.ok(result.dark <= result.light);
    assert.equal(result.generator.usesPcf, true);
    assert.equal(result.generator.filteringQuality, context.BABYLON.ShadowGenerator.QUALITY_HIGH);
    assert.equal(result.generator.transparencyShadow, true);
    assert.equal(result.generator.darkness, result.light);
    assert.ok(result.generator.bias > 0 && result.generator.bias < 0.001);
    assert.ok(result.generator.normalBias > 0 && result.generator.normalBias < 0.1);
    assert.equal(result.generator.contactHardening, false);
    assert.ok(result.generator.contactSize > 0 && result.generator.contactSize < 0.2);
});

test('realistic render mode preserves stable cutting mat texture materials and receives soft floor shadows', () => {
    const context = loadViewerContext();
    const matTexture = { name: 'cutting-mat-texture' };
    const model = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const top = {
        name: '__cutting_mat__',
        uniqueId: 201,
        material: {
            diffuseTexture: matTexture,
            alpha: 1,
            transparencyMode: 0,
        },
        metadata: {},
        receiveShadows: false,
        disableEdgesRendering: () => {},
    };
    const slab = {
        name: '__cutting_mat_slab__',
        uniqueId: 202,
        material: {
            alpha: 1,
            transparencyMode: 0,
        },
        metadata: {},
        receiveShadows: false,
        disableEdgesRendering: () => {},
    };
    const scene = makeScene(model);
    scene.meshes.push(top, slab);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        shadowGenerators.viewer = new BABYLON.ShadowGenerator(CONFIG.STUDIO_LIGHT.key.shadowMapSize, {});
        setRenderMode('viewer', 'realistic');
        ({
            topIsPbr: scene.meshes[1].material instanceof BABYLON.PBRMaterial,
            slabIsPbr: scene.meshes[2].material instanceof BABYLON.PBRMaterial,
            actualTexturePreserved: scene.meshes[1].material.diffuseTexture?.name === 'cutting-mat-texture',
            modelReceivesShadows: scene.meshes[0].receiveShadows,
            topReceivesShadows: scene.meshes[1].receiveShadows,
            slabReceivesShadows: scene.meshes[2].receiveShadows,
            shadowCasterNames: shadowGenerators.viewer.casters.map(mesh => mesh.name)
        });
    `, context);

    assert.equal(result.topIsPbr, false);
    assert.equal(result.slabIsPbr, false);
    assert.equal(result.actualTexturePreserved, true);
    assert.equal(result.modelReceivesShadows, true);
    assert.equal(result.topReceivesShadows, false);
    assert.equal(result.slabReceivesShadows, false);
    assert.deepEqual(result.shadowCasterNames, ['part']);
});

test('realistic render mode enables subtle SSAO and disposes it outside realistic mode', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        mainCameras.viewer = scene.activeCamera;
        setRenderMode('viewer', 'realistic');
        const pipeline = ssaoPipelines.viewer;
        setRenderMode('viewer', 'solid');
        ({
            enabled: !!pipeline,
            name: pipeline?.name ?? null,
            ssaoRatio: pipeline?.ratio?.ssaoRatio ?? null,
            blurRatio: pipeline?.ratio?.blurRatio ?? null,
            radius: pipeline?.radius ?? null,
            totalStrength: pipeline?.totalStrength ?? null,
            base: pipeline?.base ?? null,
            disposedAfterSolid: pipeline?.disposed === true,
            stillRegistered: !!ssaoPipelines.viewer
        });
    `, context);

    assert.equal(result.enabled, true);
    assert.equal(result.name, '__maliev_ssao_viewer__');
    assert.ok(result.ssaoRatio > 0 && result.ssaoRatio <= 0.75);
    assert.ok(result.blurRatio > 0 && result.blurRatio <= 0.75);
    assert.ok(result.radius > 0 && result.radius <= 6);
    assert.ok(result.totalStrength > 0 && result.totalStrength <= 0.8);
    assert.ok(result.base >= 0 && result.base < result.totalStrength);
    assert.equal(result.disposedAfterSolid, true);
    assert.equal(result.stillRegistered, false);
});

test('cutting mat slab meets textured top surface without perspective edge cracks', () => {
    const context = loadViewerContext();
    const scene = makeScene({
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
    });
    context.scene = scene;

    const result = vm.runInContext(`
        const outline = _roundedRectPoints(300, 220, 8, 4);
        const top = _createRoundedMatTopMesh(scene, outline, 300, 220);
        const slab = _createRoundedMatSlabMesh(scene, outline, 3);
        const topRingZ = top.vertexData.positions.slice(5).filter((_, index) => index % 3 === 0);
        const slabTopRingZ = slab.vertexData.positions.slice(2, outline.length * 3).filter((_, index) => index % 3 === 0);
        ({
            topUniqueZ: [...new Set(topRingZ)].join(','),
            slabTopUniqueZ: [...new Set(slabTopRingZ)].join(',')
        });
    `, context);

    assert.equal(result.topUniqueZ, '0');
    assert.equal(result.slabTopUniqueZ, '0');
});

test('cutting mat textured top cap overlaps slab edge to hide perspective raster cracks', () => {
    const context = loadViewerContext();

    const result = vm.runInContext(`
        const slabOutline = _cuttingMatSlabOutline(300, 220, 8);
        const topOutline = _cuttingMatTopOutline(300, 220, 8);
        const bounds = pts => ({
            minX: Math.min(...pts.map(p => p.x)),
            maxX: Math.max(...pts.map(p => p.x)),
            minY: Math.min(...pts.map(p => p.y)),
            maxY: Math.max(...pts.map(p => p.y)),
        });
        ({
            slab: bounds(slabOutline),
            top: bounds(topOutline)
        });
    `, context);

    assert.ok(result.top.minX < result.slab.minX, `expected top cap to extend past slab minX, got ${result.top.minX} >= ${result.slab.minX}`);
    assert.ok(result.top.maxX > result.slab.maxX, `expected top cap to extend past slab maxX, got ${result.top.maxX} <= ${result.slab.maxX}`);
    assert.ok(result.top.minY < result.slab.minY, `expected top cap to extend past slab minY, got ${result.top.minY} >= ${result.slab.minY}`);
    assert.ok(result.top.maxY > result.slab.maxY, `expected top cap to extend past slab maxY, got ${result.top.maxY} <= ${result.slab.maxY}`);
});

test('cutting mat texture pads outside rounded rect with green to prevent filtered black edge bleed', () => {
    const context = loadViewerContext();

    const result = vm.runInContext(`
        const calls = [];
        const ctx = {
            set fillStyle(value) { calls.push({ method: 'fillStyle', value }); },
            fillRect(x, y, w, h) { calls.push({ method: 'fillRect', x, y, w, h }); },
        };
        _primeCuttingMatTextureBleedGuard(ctx, 2048, 1024);
        calls;
    `, context);

    assert.equal(result.map(call => call.method).join(','), 'fillStyle,fillRect');
    assert.equal(result[0].value, '#2d7a4f');
    assert.equal(JSON.stringify({ x: result[1].x, y: result[1].y, w: result[1].w, h: result[1].h }), '{"x":0,"y":0,"w":2048,"h":1024}');
});

test('realistic render mode smooths near-coincident CAD vertices across conversion tolerance', () => {
    const context = loadViewerContext();
    const positions = new Float32Array([
        0, 0, 0,
        -1, 0, 0,
        0, 0, 1,
        0.024, 0, 0,
        0.024, 0, 1,
        1, 0.2, 0,
    ]);
    const indices = [0, 1, 2, 3, 4, 5];
    const normals = new Float32Array([
        0, 1, 0,
        0, 1, 0,
        0, 1, 0,
        -0.2009, 0.9796, 0,
        -0.2009, 0.9796, 0,
        -0.2009, 0.9796, 0,
    ]);
    let smoothedNormals = null;
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: kind => kind === 'position' ? positions : normals,
        getIndices: () => indices,
        setVerticesData: (kind, data) => {
            if (kind === 'normal') {
                smoothedNormals = data;
            }
        },
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    vm.runInContext(`
        scenes.viewer = scene;
        setRenderMode('viewer', 'realistic');
    `, context);
    assert.equal(smoothedNormals, null, 'realistic first paint should not smooth normals synchronously');

    flushScheduledIdleCallbacks(context);

    const result = {
        firstNormalX: smoothedNormals?.[0] ?? null,
        firstNormalY: smoothedNormals?.[1] ?? null,
        fourthNormalX: smoothedNormals?.[9] ?? null,
        fourthNormalY: smoothedNormals?.[10] ?? null,
    };

    assert.ok(result.firstNormalX < -0.05, `expected seam normal to blend with adjacent face, got ${result.firstNormalX}`);
    assert.ok(result.firstNormalY < 1, `expected seam normal Y to change from hard face normal, got ${result.firstNormalY}`);
    assert.ok(Math.abs(result.firstNormalX - result.fourthNormalX) < 0.001);
    assert.ok(Math.abs(result.firstNormalY - result.fourthNormalY) < 0.001);
});

test('realistic render mode uses fast first paint before queued quality upgrades', () => {
    const context = loadViewerContext();
    const positions = new Float32Array([
        0, 0, 0,
        -1, 0, 0,
        0, 0, 1,
        0.024, 0, 0,
        0.024, 0, 1,
        1, 0.2, 0,
    ]);
    const indices = [0, 1, 2, 3, 4, 5];
    const normals = new Float32Array([
        0, 1, 0,
        0, 1, 0,
        0, 1, 0,
        -0.2009, 0.9796, 0,
        -0.2009, 0.9796, 0,
        -0.2009, 0.9796, 0,
    ]);
    let smoothedNormals = null;
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: kind => kind === 'position' ? positions : normals,
        getIndices: () => indices,
        setVerticesData: (kind, data) => {
            if (kind === 'normal') {
                smoothedNormals = data;
            }
        },
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const firstPaint = vm.runInContext(`
        scenes.viewer = scene;
        setRenderMode('viewer', 'realistic');
        ({
            firstEnvironmentSize: rawCubeTextures[0]?.size ?? null,
            rawCubeTextureCount: rawCubeTextures.length,
            materialName: scene.meshes[0].material?.name ?? null
        });
    `, context);

    assert.equal(firstPaint.materialName, '__realistic_aluminum__');
    assert.equal(firstPaint.firstEnvironmentSize, 128);
    assert.equal(firstPaint.rawCubeTextureCount, 1);
    assert.equal(smoothedNormals, null, 'normal smoothing should be queued after first realistic paint');
    assert.ok(context.scheduledIdleCallbacks.length > 0, 'quality upgrade work should be scheduled after first paint');

    flushScheduledIdleCallbacks(context);

    const upgraded = vm.runInContext(`
        ({
            currentEnvironmentSize: scene.environmentTexture?.size ?? null,
            rawCubeTextureCount: rawCubeTextures.length
        });
    `, context);

    assert.equal(upgraded.currentEnvironmentSize, 256);
    assert.equal(upgraded.rawCubeTextureCount, 2);
    assert.ok(smoothedNormals?.[0] < -0.05, 'queued quality pass should still apply smoothed normals');
});

test('realistic render mode syncs one shared material once for multi-body first paint', () => {
    const context = loadViewerContext();
    const makeMesh = uniqueId => ({
        name: `part-${uniqueId}`,
        uniqueId,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    });
    const scene = makeScene(makeMesh(101));
    scene.meshes.push(makeMesh(102), makeMesh(103));
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        const originalSync = syncRealisticMaterialProperties;
        let syncCalls = 0;
        syncRealisticMaterialProperties = (...args) => {
            syncCalls++;
            return originalSync(...args);
        };
        setRenderMode('viewer', 'realistic');
        ({
            syncCalls,
            uniqueMaterialCount: new Set(scene.meshes.map(mesh => mesh.material)).size,
            materialName: scene.meshes[0].material?.name ?? null
        });
    `, context);

    assert.equal(result.materialName, '__realistic_aluminum__');
    assert.equal(result.uniqueMaterialCount, 1);
    assert.ok(result.syncCalls <= 2, `expected one create sync plus one apply sync, got ${result.syncCalls}`);
});

test('polished finish refreshes glossy normal smoothing for low-tessellation reflections', () => {
    const context = loadViewerContext();
    const positions = new Float32Array([
        0, 0, 0,
        -1, 0, 0,
        0, 0, 1,
        0.024, 0, 0,
        0.024, 0, 1,
        1, 5.6, 0,
    ]);
    const indices = [0, 1, 2, 3, 4, 5];
    const normals = new Float32Array([
        0, 1, 0,
        0, 1, 0,
        0, 1, 0,
        -0.985, 0.172, 0,
        -0.985, 0.172, 0,
        -0.985, 0.172, 0,
    ]);
    let currentNormals = normals;
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: kind => {
            if (kind === 'position') return positions;
            if (kind === 'normal') return currentNormals;
            return null;
        },
        getIndices: () => indices,
        setVerticesData: (kind, data) => {
            if (kind === 'normal') {
                currentNormals = data;
            }
        },
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        configureMaterialFromConfigurator('viewer', 'aluminum', null, 'AS_MACHINED', null, 'CNC_MILL');
        setRenderMode('viewer', 'realistic');
        while (scheduledIdleCallbacks.length > 0) { scheduledIdleCallbacks.shift()({ didTimeout: false, timeRemaining: () => 16 }); }
        const defaultNormals = scene.meshes[0].getVerticesData(BABYLON.VertexBuffer.NormalKind);
        const defaultX = defaultNormals[0];
        const defaultY = defaultNormals[1];
        configureMaterialFromConfigurator('viewer', 'aluminum', null, 'POLISHED', 'RA_1_6', 'CNC_MILL');
        while (scheduledIdleCallbacks.length > 0) { scheduledIdleCallbacks.shift()({ didTimeout: false, timeRemaining: () => 16 }); }
        const material = scene.meshes[0].material;
        const polishedNormals = scene.meshes[0].getVerticesData(BABYLON.VertexBuffer.NormalKind);
        ({
            defaultX,
            defaultY,
            polishedX: polishedNormals[0],
            polishedY: polishedNormals[1],
            roughness: material?.roughness ?? null,
            smoothingEnabled: perCanvasFinishModifiers.viewer?.polishedReflectionSmoothing ?? false
        });
    `, context);

    assert.ok(Math.abs(result.defaultX) < 0.01, `expected default smoothing to preserve the coarse bevel normal, got ${result.defaultX}`);
    assert.ok(result.defaultY > 0.99, `expected default smoothing to preserve the coarse bevel normal, got ${result.defaultY}`);
    assert.ok(result.polishedX < -0.6, `expected polished smoothing to blend low-poly bevel normals, got ${result.polishedX}`);
    assert.ok(result.polishedY < 0.6, `expected polished smoothing to soften polygonal reflection bands, got ${result.polishedY}`);
    assert.ok(result.roughness >= 0.16 && result.roughness <= 0.20, `expected polished roughness floor to blur jagged reflections, got ${result.roughness}`);
    assert.equal(result.smoothingEnabled, true);
});

test('realistic configurator applies bead blasted procedural surface effect', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        configureMaterialFromConfigurator('viewer', 'aluminum', null, 'BEAD_BLAST', 'RA_3_2', 'CNC_MILL');
        setRenderMode('viewer', 'realistic');
        ({
            materialType: materialTypes.viewer,
            roughness: scene.meshes[0].material?.roughness ?? null,
            metallic: scene.meshes[0].material?.metallic ?? null,
            effectKey: scene.meshes[0].material?._malievSurfaceEffect?.key ?? null,
            effectKind: scene.meshes[0].material?._malievSurfaceEffect?.kind ?? null,
            effectScale: scene.meshes[0].material?._malievSurfaceEffect?.scale ?? null,
            effectStrength: scene.meshes[0].material?._malievSurfaceEffect?.strength ?? null,
            effectBump: scene.meshes[0].material?._malievSurfaceEffect?.bump ?? null
        });
    `, context);

    assert.equal(result.materialType, 'aluminum');
    assert.ok(result.roughness >= 0.86 && result.roughness <= 0.94, `expected zoom-stable matte bead-blast roughness, got ${result.roughness}`);
    assert.ok(result.metallic >= 0.72 && result.metallic <= 0.82, `expected bead-blasted aluminum to keep muted metal response without zoom-out reflections, got ${result.metallic}`);
    assert.equal(result.effectKey, 'bead-blast');
    assert.equal(result.effectKind, 1);
    assert.ok(result.effectScale >= 8.8 && result.effectScale <= 10.5, `expected larger visible fine bead-blast grain scale, got ${result.effectScale}`);
    assert.ok(result.effectStrength >= 0.058 && result.effectStrength <= 0.070, `expected readable bead-blast roughness variation, got ${result.effectStrength}`);
    assert.ok(result.effectBump >= 0.055 && result.effectBump <= 0.070, `expected visible but realistic bead-blast relief amplitude, got ${result.effectBump}`);
});

test('realistic material sync updates surface plugin stored on Babylon plugin manager', () => {
    const context = loadViewerContext();

    const result = vm.runInContext(`
        const surfacePlugin = {
            name: 'MalievSurfaceEffect',
            setEffect(effect) {
                this.lastEffect = effect;
                material._malievSurfaceEffect = effect?.kind > 0 ? effect : null;
            }
        };
        const material = {
            albedoColor: new BABYLON.Color3(),
            pluginManager: { _plugins: [surfacePlugin] }
        };

        syncRealisticMaterialProperties(
            material,
            CONFIG.MATERIAL_REALISTIC.aluminum,
            null,
            { roughnessOffset: 0.48, metallicOffset: -0.18, absoluteRoughness: 0.90 },
            { surfaceEffectKey: 'bead-blast' });

        ({
            effectKey: surfacePlugin.lastEffect?.key ?? null,
            materialEffectKey: material._malievSurfaceEffect?.key ?? null,
            roughness: material.roughness,
            metallic: material.metallic
        });
    `, context);

    assert.equal(result.effectKey, 'bead-blast');
    assert.equal(result.materialEffectKey, 'bead-blast');
    assert.ok(result.roughness >= 0.86 && result.roughness <= 0.94);
    assert.ok(result.metallic >= 0.72 && result.metallic <= 0.82);
});

test('realistic configurator keeps intrinsic blue POM darker than the UI swatch override', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        configureMaterialFromConfigurator('viewer', 'blue-pom', '#2f6fd6', 'AS_MACHINED', 'RA_1_6', 'CNC_MILL');
        setRenderMode('viewer', 'realistic');
        const material = scene.meshes[0].material;
        ({
            r: material?.albedoColor?.r ?? null,
            g: material?.albedoColor?.g ?? null,
            b: material?.albedoColor?.b ?? null,
            materialName: material?.name ?? null
        });
    `, context);

    assert.equal(result.materialName, '__realistic_blue-pom__');
    assert.equal(result.r, 0.08);
    assert.equal(result.g, 0.28);
    assert.equal(result.b, 0.62);
});

test('realistic configurator renders natural PEEK as a warmer brown tan', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        configureMaterialFromConfigurator('viewer', 'peek', '#B3AA9E', 'AS_MACHINED', 'RA_1_6', 'CNC_MILL');
        setRenderMode('viewer', 'realistic');
        const material = scene.meshes[0].material;
        const luma = material.albedoColor.r * 0.2126
            + material.albedoColor.g * 0.7152
            + material.albedoColor.b * 0.0722;
        ({
            r: material?.albedoColor?.r ?? null,
            g: material?.albedoColor?.g ?? null,
            b: material?.albedoColor?.b ?? null,
            luma,
            materialName: material?.name ?? null
        });
    `, context);

    assert.equal(result.materialName, '__realistic_peek__');
    assert.ok(result.r > result.g, `expected natural PEEK to have a warm red bias, got r=${result.r} g=${result.g}`);
    assert.ok(result.g > result.b, `expected natural PEEK to suppress blue for a brown tan, got g=${result.g} b=${result.b}`);
    assert.ok(result.r - result.b >= 0.20, `expected a stronger brown tan than the UI swatch, got r=${result.r} b=${result.b}`);
    assert.ok(result.b <= 0.42, `expected natural PEEK to be less pale/grey, got blue channel ${result.b}`);
    assert.ok(result.luma <= 0.58, `expected natural PEEK to render darker than the old pale beige, got luma ${result.luma}`);
});

test('realistic configurator makes CNC surface finishes visibly distinct even when Ra is present', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        const snapshotFinish = (finishCode) => {
            configureMaterialFromConfigurator('viewer', 'brass', null, finishCode, 'RA_1_6', 'CNC_MILL');
            setRenderMode('viewer', 'realistic');
            const material = scene.meshes[0].material;
            return {
                roughness: material?.roughness ?? null,
                metallic: material?.metallic ?? null,
                effectKey: material?._malievSurfaceEffect?.key ?? null,
                effectKind: material?._malievSurfaceEffect?.kind ?? null,
                stripeScale: material?._malievSurfaceEffect?.stripeScale ?? 0,
                stripeStrength: material?._malievSurfaceEffect?.stripeStrength ?? 0,
                bump: material?._malievSurfaceEffect?.bump ?? 0
            };
        };
        ({
            beadBlast: snapshotFinish('BEAD_BLAST'),
            brushed: snapshotFinish('BRUSHED'),
            mirrorPolish: snapshotFinish('MIRROR_POLISH')
        });
    `, context);

    assert.ok(
        result.beadBlast.roughness > result.brushed.roughness,
        `expected bead blasted to be rougher than brushed, got ${result.beadBlast.roughness} <= ${result.brushed.roughness}`
    );
    assert.ok(
        result.brushed.roughness > result.mirrorPolish.roughness,
        `expected brushed to be rougher than mirror polish, got ${result.brushed.roughness} <= ${result.mirrorPolish.roughness}`
    );
    assert.ok(
        result.brushed.roughness >= 0.42 && result.brushed.roughness <= 0.56,
        `expected brushed finish to be a restrained satin rather than dark matte, got ${result.brushed.roughness}`
    );
    assert.equal(result.beadBlast.effectKey, 'bead-blast');
    assert.equal(result.brushed.effectKey, 'brushed');
    assert.equal(result.brushed.effectKind, 2);
    assert.ok(result.brushed.stripeScale >= 18, `expected fine brushed strokes, got stripe scale ${result.brushed.stripeScale}`);
    assert.ok(result.brushed.stripeStrength <= 0.04, `expected subtle brushed strokes, got stripe strength ${result.brushed.stripeStrength}`);
    assert.ok(result.brushed.bump <= 0.04, `expected low brushed relief, got bump ${result.brushed.bump}`);
    assert.equal(result.mirrorPolish.effectKey, null);
    assert.ok(result.mirrorPolish.metallic >= result.brushed.metallic);
});

test('realistic configurator maps coating anodize and polishing finishes to distinct PBR semantics', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        const snapshot = (materialKey, finishCode, roughnessCode, colorHex) => {
            configureMaterialFromConfigurator('viewer', materialKey, colorHex, finishCode, roughnessCode, 'CNC_MILL');
            setRenderMode('viewer', 'realistic');
            const material = scene.meshes[0].material;
            return {
                roughness: material?.roughness ?? null,
                metallic: material?.metallic ?? null,
                albedoR: material?.albedoColor?.r ?? null,
                albedoG: material?.albedoColor?.g ?? null,
                albedoB: material?.albedoColor?.b ?? null,
                effectKey: material?._malievSurfaceEffect?.key ?? null,
                polishedReflectionSmoothing: perCanvasFinishModifiers.viewer?.polishedReflectionSmoothing ?? false
            };
        };
        ({
            mirror: snapshot('aluminum', 'MIRROR_POLISH', 'RA_0_4', null),
            electropolished: snapshot('stainless-steel', 'ELECTROPOLISHED', 'RA_0_8', null),
            anodizeTypeII: snapshot('aluminum', 'ANODIZE_TYPE_II', 'RA_1_6', '#2f6fd6'),
            anodizeTypeIII: snapshot('aluminum', 'ANODIZE_TYPE_III', 'RA_3_2', '#111111'),
            powderCoated: snapshot('aluminum', 'POWDER_COATED', 'RA_3_2', '#2f6fd6')
        });
    `, context);

    assert.ok(result.mirror.roughness <= 0.08, `expected mirror polish to render glossy, got ${result.mirror.roughness}`);
    assert.equal(result.mirror.effectKey, null);
    assert.equal(result.mirror.polishedReflectionSmoothing, true);

    assert.ok(
        result.electropolished.roughness >= 0.09 && result.electropolished.roughness <= 0.14,
        `expected electropolished finish to stay bright and glossy without color selection, got ${result.electropolished.roughness}`
    );
    assert.equal(result.electropolished.effectKey, null);
    assert.equal(result.electropolished.polishedReflectionSmoothing, true);
    assert.ok(result.electropolished.albedoR >= 0.68, `expected bright stainless albedo, got ${result.electropolished.albedoR}`);

    assert.ok(result.anodizeTypeII.albedoB > result.anodizeTypeII.albedoR, 'expected anodize Type II color to tint the oxide layer');
    assert.ok(result.anodizeTypeII.metallic < 0.85, `expected anodize Type II to reduce bare-metal metallic response, got ${result.anodizeTypeII.metallic}`);
    assert.ok(result.anodizeTypeII.roughness >= 0.26 && result.anodizeTypeII.roughness <= 0.36);

    assert.ok(result.anodizeTypeIII.roughness > result.anodizeTypeII.roughness);
    assert.ok(result.anodizeTypeIII.metallic <= result.anodizeTypeII.metallic);
    assert.ok(result.anodizeTypeIII.albedoR < 0.08, `expected black hard anodize to render dark, got ${result.anodizeTypeIII.albedoR}`);

    assert.equal(result.powderCoated.metallic, 0);
    assert.equal(result.powderCoated.effectKey, 'powder-coat');
    assert.ok(result.powderCoated.albedoB > result.powderCoated.albedoR, 'expected powder coat color to drive coating albedo');
    assert.ok(
        result.powderCoated.roughness >= 0.50 && result.powderCoated.roughness <= 0.68,
        `expected powder coat to render as satin textured coating, got ${result.powderCoated.roughness}`
    );
});

test('Ra selection modulates brushed finish without removing directional surface effect', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        const snapshotRa = (roughnessCode) => {
            configureMaterialFromConfigurator('viewer', 'aluminum', null, 'BRUSHED', roughnessCode, 'CNC_MILL');
            setRenderMode('viewer', 'realistic');
            const material = scene.meshes[0].material;
            return {
                roughness: material?.roughness ?? null,
                effectKey: material?._malievSurfaceEffect?.key ?? null
            };
        };
        ({
            fine: snapshotRa('RA_0_4'),
            coarse: snapshotRa('RA_3_2')
        });
    `, context);

    assert.equal(result.fine.effectKey, 'brushed');
    assert.equal(result.coarse.effectKey, 'brushed');
    assert.ok(
        result.coarse.roughness > result.fine.roughness + 0.05,
        `expected Ra to visibly modulate brushed roughness, got fine=${result.fine.roughness} coarse=${result.coarse.roughness}`
    );
    assert.ok(result.fine.roughness < 0.50, `expected fine Ra brushed finish to stay below heavy matte, got ${result.fine.roughness}`);
});

test('brushed surface shader projects strokes along each face tangent instead of stacking on side faces', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        configureMaterialFromConfigurator('viewer', 'aluminum', null, 'BRUSHED', 'RA_1_6', 'CNC_MILL');
        setRenderMode('viewer', 'realistic');
        const material = scene.meshes[0].material;
        const surfacePlugin = material?._pluginInstances?.find(plugin => plugin.name === 'MalievSurfaceEffect');
        const customCode = surfacePlugin?.getCustomCode('fragment') ?? {};
        ({
            effectKey: material?._malievSurfaceEffect?.key ?? null,
            effectKind: material?._malievSurfaceEffect?.kind ?? null,
            profile: material?._malievNodeMaterialProfile ?? null,
            definitions: customCode.CUSTOM_FRAGMENT_DEFINITIONS ?? '',
            beforeLights: customCode.CUSTOM_FRAGMENT_BEFORE_LIGHTS ?? '',
            updateAlbedo: customCode.CUSTOM_FRAGMENT_UPDATE_ALBEDO ?? '',
            beforeFragColor: customCode.CUSTOM_FRAGMENT_BEFORE_FRAGCOLOR ?? ''
        });
    `, context);

    assert.equal(result.effectKey, 'brushed');
    assert.equal(result.effectKind, 2);
    assert.equal(result.profile?.surfaceEffectKey, 'brushed');
    assert.equal(result.profile?.stripeAxis, 'x');
    assert.match(result.definitions, /malievBrushedLayAxis/);
    assert.match(result.definitions, /malievBrushedSurfaceUv/);
    assert.match(result.definitions, /malievBrushedHeight/);
    assert.doesNotMatch(result.definitions, /float\s+lines\s*=\s*sin\(p\.y\s*\*\s*sScl\)/);
    assert.match(result.updateAlbedo, /malievMseH/);
    assert.match(result.beforeFragColor, /malievMseRelief/);
});

test('realistic configurator applies CNC machining surface effect when no finish hides tool marks', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        configureMaterialFromConfigurator('viewer', 'aluminum', null, 'AS_MACHINED', 'RA_1_6', 'CNC_MILL');
        setRenderMode('viewer', 'realistic');
        ({
            roughness: scene.meshes[0].material?.roughness ?? null,
            effectKey: scene.meshes[0].material?._malievSurfaceEffect?.key ?? null,
            effectKind: scene.meshes[0].material?._malievSurfaceEffect?.kind ?? null
        });
    `, context);

    assert.equal(result.roughness, 0.34);
    assert.equal(result.effectKey, 'machining');
    // kind 4 = orientation-aware CNC tool marks (face vs side milling), distinct from brushed (kind 2).
    assert.equal(result.effectKind, 4);
});

test('as-machined shader uses fine side-wall milling lines and controlled face-milling passes', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        configureMaterialFromConfigurator('viewer', 'aluminum', null, 'AS_MACHINED', 'RA_1_6', 'CNC_MILL');
        setRenderMode('viewer', 'realistic');
        const material = scene.meshes[0].material;
        const surfacePlugin = material?._pluginInstances?.find(plugin => plugin.name === 'MalievSurfaceEffect');
        const customCode = surfacePlugin?.getCustomCode('fragment') ?? {};
        ({
            effectKey: material?._malievSurfaceEffect?.key ?? null,
            effectStripeScale: material?._malievSurfaceEffect?.stripeScale ?? null,
            effectStripeStrength: material?._malievSurfaceEffect?.stripeStrength ?? null,
            effectBump: material?._malievSurfaceEffect?.bump ?? null,
            definitions: customCode.CUSTOM_FRAGMENT_DEFINITIONS ?? '',
            beforeLights: customCode.CUSTOM_FRAGMENT_BEFORE_LIGHTS ?? ''
        });
    `, context);

    assert.equal(result.effectKey, 'machining');
    assert.ok(result.effectStripeScale >= 12, `expected fine as-machined line frequency, got ${result.effectStripeScale}`);
    assert.ok(result.effectStripeStrength <= 0.024, `expected subtle as-machined stripe strength, got ${result.effectStripeStrength}`);
    assert.ok(result.effectBump <= 0.022, `expected subtle as-machined bump, got ${result.effectBump}`);
    assert.match(result.definitions, /malievMachinedFaceUv/);
    assert.match(result.definitions, /malievMachinedSideUv/);
    assert.match(result.definitions, /malievFaceMillingHeight/);
    assert.match(result.definitions, /malievSideMillingHeight/);
    assert.match(result.definitions, /malievMachinedHeight/);
    assert.match(result.definitions, /sideFineLines/);
    assert.match(result.definitions, /faceFeedLines/);
    assert.match(result.definitions, /mix\(sideMarks,\s*faceMarks,\s*faceBlend\)/);
    assert.doesNotMatch(result.definitions, /float\s+fa\s*=\s*sin\(p\.x\s*\*\s*sScl\)/);
    assert.doesNotMatch(result.definitions, /stepDownScallop/);
    assert.doesNotMatch(result.definitions, /ridge\s*=\s*smoothstep/);
    assert.doesNotMatch(result.definitions, /fineFeed\s*=\s*malievVNoise/);
    assert.match(result.beforeLights, /malievHeightGradient/);
    assert.match(result.beforeLights, /normalW\s*=\s*normalize/);
});

test('realistic configurator renders raw steel and stainless as bright machined metal', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        const snapshot = (materialKey, finishCode) => {
            configureMaterialFromConfigurator('viewer', materialKey, null, finishCode, 'RA_1_6', 'CNC_MILL');
            setRenderMode('viewer', 'realistic');
            const material = scene.meshes[0].material;
            const surfacePlugin = material?._pluginInstances?.find(plugin => plugin.name === 'MalievSurfaceEffect');
            const customCode = surfacePlugin?.getCustomCode('fragment') ?? {};
            return {
                materialName: material?.name ?? null,
                albedoR: material?.albedoColor?.r ?? null,
                albedoG: material?.albedoColor?.g ?? null,
                albedoB: material?.albedoColor?.b ?? null,
                metallic: material?.metallic ?? null,
                roughness: material?.roughness ?? null,
                anisotropyEnabled: material?.anisotropy?.isEnabled ?? false,
                effectKey: material?._malievSurfaceEffect?.key ?? null,
                effectKind: material?._malievSurfaceEffect?.kind ?? null,
                effectStripeStrength: material?._malievSurfaceEffect?.stripeStrength ?? null,
                effectBump: material?._malievSurfaceEffect?.bump ?? null,
                profile: material?._malievNodeMaterialProfile ?? null,
                beforeLights: customCode.CUSTOM_FRAGMENT_BEFORE_LIGHTS ?? ''
            };
        };
        ({
            steelRaw: snapshot('steel', 'RAW'),
            stainlessRaw: snapshot('stainless-steel', 'RAW'),
            stainlessMachined: snapshot('stainless-steel', 'AS_MACHINED')
        });
    `, context);

    for (const [name, material] of Object.entries(result)) {
        assert.equal(material.effectKey, 'machining', `${name} should show raw CNC machining marks`);
        assert.equal(material.effectKind, 4, `${name} should use the machining surface effect`);
        assert.ok(material.metallic >= 0.97, `${name} should stay highly metallic, got ${material.metallic}`);
        assert.ok(material.roughness >= 0.20 && material.roughness <= 0.30, `${name} should be smooth raw metal, got ${material.roughness}`);
        assert.equal(material.anisotropyEnabled, false, `${name} should not enable tangent-dependent anisotropy on CAD meshes`);
        assert.ok(material.effectStripeStrength >= 0.018 && material.effectStripeStrength <= 0.024, `${name} machining marks should be readable but subtle, got ${material.effectStripeStrength}`);
        assert.ok(material.effectBump >= 0.016 && material.effectBump <= 0.022, `${name} machining bump should be visible without gouging, got ${material.effectBump}`);
        assert.equal(material.profile?.surfaceEffectKey, 'machining', `${name} profile should carry machining`);
        assert.ok(material.profile?.stripeStrength >= 0.012, `${name} should keep visible directional tool marks`);
        assert.match(material.beforeLights, /_isMach/);
        assert.match(material.beforeLights, /malievHeightGradient/);
        assert.match(material.beforeLights, /normalW\s*=\s*normalize/);
    }

    assert.ok(result.steelRaw.albedoR >= 0.55, `expected raw steel to be bright silver-grey, got ${result.steelRaw.albedoR}`);
    assert.ok(result.steelRaw.albedoB >= 0.52, `expected raw steel to avoid charcoal rendering, got ${result.steelRaw.albedoB}`);
    assert.ok(result.stainlessRaw.albedoR >= 0.68, `expected raw stainless to be bright, got ${result.stainlessRaw.albedoR}`);
    assert.ok(result.stainlessRaw.albedoB >= 0.66, `expected raw stainless to be bright neutral metal, got ${result.stainlessRaw.albedoB}`);
    assert.equal(result.stainlessMachined.materialName, '__realistic_stainless-steel__');
});

test('realistic configurator applies powder-grain effect for MJF and SLS nylon powder', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        ({
            powderPresetExists: !!CONFIG.MATERIAL_REALISTIC['nylon-powder'],
            mjf: (() => {
                configureMaterialFromConfigurator('viewer', 'nylon-powder', null, 'AS_PRINTED', null, 'MJF');
                setRenderMode('viewer', 'realistic');
                const material = scene.meshes[0].material;
                const powderPlugin = material?._pluginInstances?.find(plugin => plugin.name === 'MalievSurfaceEffect');
                const layerPlugin = material?._pluginInstances?.find(plugin => plugin.name === 'FdmLayer');
                const powderCustomCode = powderPlugin?.getCustomCode('fragment') ?? {};
                const layerCustomCode = layerPlugin?.getCustomCode('fragment') ?? {};
                return {
                    materialType: materialTypes.viewer,
                    roughness: material?.roughness ?? null,
                    albedoR: material?.albedoColor?.r ?? null,
                    albedoB: material?.albedoColor?.b ?? null,
                    effectKey: material?._malievSurfaceEffect?.key ?? null,
                    effectKind: material?._malievSurfaceEffect?.kind ?? null,
                    effectScale: material?._malievSurfaceEffect?.scale ?? null,
                    effectStrength: material?._malievSurfaceEffect?.strength ?? null,
                    effectBump: material?._malievSurfaceEffect?.bump ?? null,
                    layerHeightMm: layerPlugin?._layerHeightMm ?? null,
                    layerLineStrength: material?._malievNodeMaterialProfile?.layerLineStrength ?? null,
                    layerBump: layerPlugin?._layerBump ?? null,
                    pluginNames: material?._pluginInstances?.map(plugin => plugin.name) ?? [],
                    definitions: powderCustomCode.CUSTOM_FRAGMENT_DEFINITIONS ?? '',
                    beforeLights: powderCustomCode.CUSTOM_FRAGMENT_BEFORE_LIGHTS ?? '',
                    updateAlbedo: powderCustomCode.CUSTOM_FRAGMENT_UPDATE_ALBEDO ?? '',
                    updateMetallicRoughness: powderCustomCode.CUSTOM_FRAGMENT_UPDATE_METALLICROUGHNESS ?? '',
                    beforeFragColor: powderCustomCode.CUSTOM_FRAGMENT_BEFORE_FRAGCOLOR ?? '',
                    layerDefinitions: layerCustomCode.CUSTOM_FRAGMENT_DEFINITIONS ?? '',
                    layerBeforeFragColor: layerCustomCode.CUSTOM_FRAGMENT_BEFORE_FRAGCOLOR ?? ''
                };
            })(),
            slsEffect: (() => {
                configureMaterialFromConfigurator('viewer', 'nylon-powder', null, 'DYED_BLACK', null, 'SLS');
                setRenderMode('viewer', 'realistic');
                const material = scene.meshes[0].material;
                const layerPlugin = material?._pluginInstances?.find(plugin => plugin.name === 'FdmLayer');
                return {
                    effectKey: material?._malievSurfaceEffect?.key ?? null,
                    layerHeightMm: layerPlugin?._layerHeightMm ?? null
                };
            })()
        });
    `, context);

    assert.equal(result.powderPresetExists, true);
    assert.equal(result.mjf.materialType, 'nylon-powder');
    assert.ok(result.mjf.roughness >= 0.80, `expected matte raw powder-bed nylon roughness, got ${result.mjf.roughness}`);
    assert.ok(result.mjf.albedoR <= 0.62 && result.mjf.albedoB <= 0.62, `expected cool grey raw powder nylon, got r=${result.mjf.albedoR} b=${result.mjf.albedoB}`);
    assert.equal(result.mjf.effectKey, 'powder-grain');
    assert.equal(result.mjf.effectKind, 3);
    assert.ok(result.mjf.effectScale >= 3.5 && result.mjf.effectScale <= 6.5, `expected fine powder grain scale, got ${result.mjf.effectScale}`);
    assert.ok(result.mjf.effectStrength >= 0.18, `expected visible powder speckle strength, got ${result.mjf.effectStrength}`);
    assert.ok(result.mjf.effectBump >= 0.20, `expected tactile powder bump, got ${result.mjf.effectBump}`);
    assert.ok(result.mjf.layerLineStrength >= 0.016 && result.mjf.layerLineStrength <= 0.022, `expected visible powder-bed layer strength, got ${result.mjf.layerLineStrength}`);
    assert.ok(Math.abs(result.mjf.layerHeightMm - 0.3) < 0.001, `expected 0.3 mm MJF/SLS layer height, got ${result.mjf.layerHeightMm}`);
    assert.ok(result.mjf.layerBump >= 0.10 && result.mjf.layerBump <= 0.14, `expected visible powder-bed layer bump mixed with grain, got ${result.mjf.layerBump}`);
    assert.ok(result.mjf.pluginNames.includes('FdmLayer'), 'MJF/SLS powder texture should include additive layer-line relief');
    assert.match(result.mjf.definitions, /malievPowderFineSpeckle/);
    assert.match(result.mjf.definitions, /malievPowderBedPores/);
    assert.match(result.mjf.definitions, /malievPowderBedHeight/);
    assert.match(result.mjf.definitions, /malievPowderBedAa/);
    assert.match(result.mjf.layerDefinitions, /malievFdmLayerStepRelief/);
    assert.match(result.mjf.layerBeforeFragColor, /malievFdmLayerRelief/);
    assert.match(result.mjf.beforeLights, /_isPowder/);
    assert.match(result.mjf.beforeLights, /normalW\s*=\s*normalize/);
    assert.match(result.mjf.updateAlbedo, /malievMsePowFine/);
    assert.match(result.mjf.updateAlbedo, /malievMsePowPores/);
    assert.match(result.mjf.updateMetallicRoughness, /malievMsePowPores/);
    assert.match(result.mjf.beforeFragColor, /_powPoreShadow/);
    assert.match(result.mjf.beforeFragColor, /malievMsePowFine/);
    assert.equal(result.slsEffect.effectKey, 'powder-grain');
    assert.ok(Math.abs(result.slsEffect.layerHeightMm - 0.3) < 0.001, `expected 0.3 mm SLS layer height, got ${result.slsEffect.layerHeightMm}`);
});

test('realistic configurator uses PBRMaterial + plugins carrying procedural surface profiles for textured finishes', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        const snapshot = (materialKey, finishCode, roughnessCode, processCode) => {
            configureMaterialFromConfigurator('viewer', materialKey, null, finishCode, roughnessCode, processCode);
            setRenderMode('viewer', 'realistic');
            const material = scene.meshes[0].material;
            return {
                isPbr: material instanceof BABYLON.PBRMaterial,
                isNodeMaterial: material instanceof BABYLON.NodeMaterial,
                pipeline: material?._malievMaterialPipeline ?? null,
                effectKey: material?._malievSurfaceEffect?.key ?? null,
                nodeEffectKey: material?._malievNodeMaterialProfile?.surfaceEffectKey ?? null,
                normalStrength: material?._malievNodeMaterialProfile?.normalStrength ?? 0,
                stripeStrength: material?._malievNodeMaterialProfile?.stripeStrength ?? 0,
                layerLineStrength: material?._malievNodeMaterialProfile?.layerLineStrength ?? 0,
                layerHeightMm: material?._malievNodeMaterialProfile?.layerHeightMm ?? 0,
                layerBump: material?._malievNodeMaterialProfile?.layerBump ?? 0
            };
        };
        ({
            beadBlast: snapshot('aluminum', 'BEAD_BLAST', 'RA_3_2', 'CNC_MILL'),
            brushed: snapshot('aluminum', 'BRUSHED', 'RA_1_6', 'CNC_MILL'),
            fdm: snapshot('pla', 'AS_PRINTED', null, 'FDM'),
            sla: snapshot('resin', 'AS_PRINTED', null, 'SLA'),
            powder: snapshot('nylon-powder', 'AS_PRINTED', null, 'MJF')
        });
    `, context);

    // Materials are now PBRMaterial (render metals correctly) carrying the
    // procedural surface profile + plugins, not the old broken NodeMaterial graph.
    assert.equal(result.beadBlast.isPbr, true);
    assert.equal(result.beadBlast.isNodeMaterial, false);
    assert.equal(result.beadBlast.pipeline, 'pbr-plugin-fallback');
    assert.equal(result.beadBlast.effectKey, 'bead-blast');
    assert.equal(result.beadBlast.nodeEffectKey, 'bead-blast');
    assert.ok(result.beadBlast.normalStrength > 0, 'bead blast should carry a grain profile');

    assert.equal(result.brushed.isPbr, true);
    assert.equal(result.brushed.nodeEffectKey, 'brushed');
    assert.ok(result.brushed.stripeStrength > 0, 'brushed finish should include directional stripe detail');

    assert.equal(result.fdm.isPbr, true);
    assert.equal(result.fdm.nodeEffectKey, 'fdm-layer-lines');
    assert.ok(result.fdm.layerLineStrength > 0, 'FDM printing should include visible layer-line detail');
    assert.ok(Math.abs(result.fdm.layerHeightMm - 0.2) < 0.001, `expected 0.2 mm FDM layer height, got ${result.fdm.layerHeightMm}`);
    assert.ok(result.fdm.layerBump >= 0.20, `expected FDM profile to carry protruding extrusion-ridge bump, got ${result.fdm.layerBump}`);

    assert.equal(result.sla.isPbr, true);
    assert.equal(result.sla.nodeEffectKey, 'fdm-layer-lines');
    assert.ok(result.sla.layerLineStrength > 0, 'SLA printing should include visible layer-line detail');

    assert.equal(result.powder.isPbr, true);
    assert.equal(result.powder.effectKey, 'powder-grain');
    assert.equal(result.powder.nodeEffectKey, 'powder-grain');
    assert.ok(result.powder.layerLineStrength > 0, 'MJF/SLS printing should include visible 0.3 mm layer stepping');
    assert.ok(Math.abs(result.powder.layerHeightMm - 0.3) < 0.001, `expected 0.3 mm MJF/SLS layer height, got ${result.powder.layerHeightMm}`);
});

test('realistic material plugins enable Babylon shader defines through plugin API', () => {
    const context = loadViewerContext();
    const makeMesh = () => ({
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    });

    const scene = makeScene(makeMesh());
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;

        configureMaterialFromConfigurator('viewer', 'aluminum', null, 'BEAD_BLAST', 'RA_3_2', 'CNC_MILL');
        setRenderMode('viewer', 'realistic');
        const beadBlastMaterial = scene.meshes[0].material;
        const surfacePlugin = beadBlastMaterial._pluginInstances?.find(plugin => plugin.name === 'MalievSurfaceEffect');
        const surfaceDefines = {};
        surfacePlugin?.prepareDefines(surfaceDefines);

        scene.meshes = [${makeMesh.toString()}()];
        realisticMaterialCache.viewer = {};
        configureMaterialFromConfigurator('viewer', 'pla', null, 'AS_PRINTED', null, 'FDM');
        setRenderMode('viewer', 'realistic');
        const fdmMaterial = scene.meshes[0].material;
        const fdmPlugin = fdmMaterial._pluginInstances?.find(plugin => plugin.name === 'FdmLayer');
        const fdmDefines = {};
        fdmPlugin?.prepareDefines(fdmDefines);
        const fdmCustomCode = fdmPlugin?.getCustomCode('fragment') ?? {};
        const fdmProfile = fdmMaterial?._malievNodeMaterialProfile ?? {};

        ({
            surfaceEnabled: surfacePlugin?._isEnabled ?? null,
            surfaceEnableCalls: surfacePlugin?._enableCalls ?? 0,
            surfaceDefine: surfaceDefines.MALIEV_SURFACE_EFFECT ?? null,
            fdmEnabled: fdmPlugin?._isEnabled ?? null,
            fdmEnableCalls: fdmPlugin?._enableCalls ?? 0,
            fdmDefine: fdmDefines.FDMLAYER ?? null,
            fdmLayerHeight: fdmPlugin?._layerHeightMm ?? null,
            fdmProfileLayerHeight: fdmProfile.layerHeightMm ?? null,
            fdmLayerStrength: fdmPlugin?._layerStrength ?? null,
            fdmLayerBump: fdmPlugin?._layerBump ?? null,
            fdmProfileLayerStrength: fdmProfile.layerLineStrength ?? null,
            fdmProfileLayerBump: fdmProfile.layerBump ?? null,
            fdmDefinitions: fdmCustomCode.CUSTOM_FRAGMENT_DEFINITIONS ?? '',
            fdmBeforeLights: fdmCustomCode.CUSTOM_FRAGMENT_BEFORE_LIGHTS ?? '',
            fdmUpdateMetallicRoughness: fdmCustomCode.CUSTOM_FRAGMENT_UPDATE_METALLICROUGHNESS ?? '',
            fdmBeforeFragColor: fdmCustomCode.CUSTOM_FRAGMENT_BEFORE_FRAGCOLOR ?? ''
        });
    `, context);

    assert.equal(result.surfaceEnabled, true);
    assert.equal(result.surfaceEnableCalls, 1);
    assert.equal(result.surfaceDefine, true);
    assert.equal(result.fdmEnabled, true);
    assert.equal(result.fdmEnableCalls, 1);
    assert.equal(result.fdmDefine, true);
    assert.equal(result.fdmLayerHeight, result.fdmProfileLayerHeight);
    assert.equal(result.fdmLayerStrength, result.fdmProfileLayerStrength);
    assert.equal(result.fdmLayerBump, result.fdmProfileLayerBump);
    assert.ok(Math.abs(result.fdmLayerHeight - 0.2) < 0.001, `expected 0.2 mm FDM layer height, got ${result.fdmLayerHeight}`);
    assert.ok(result.fdmLayerBump >= 0.20 && result.fdmLayerBump <= 0.26, `expected pronounced FDM extrusion-ridge bump, got ${result.fdmLayerBump}`);
    assert.match(result.fdmDefinitions, /malievFdmLayerAa/);
    assert.match(result.fdmDefinitions, /malievFdmLayerStepRelief/);
    assert.match(result.fdmDefinitions, /malievFdmLayerRelief/);
    assert.match(result.fdmDefinitions, /malievFdmLayerRidge/);
    assert.match(result.fdmDefinitions, /dFdx/);
    assert.match(result.fdmDefinitions, /dFdy/);
    assert.match(result.fdmBeforeLights, /#ifdef NORMAL/);
    assert.match(result.fdmBeforeLights, /normalW\s*=\s*normalize/);
    assert.doesNotMatch(result.fdmUpdateMetallicRoughness, /\bnormalW\b/);
    assert.match(result.fdmUpdateMetallicRoughness, /metallicRoughness\.g/);
    assert.doesNotMatch(result.fdmBeforeFragColor, /#ifndef NORMAL/);
    assert.match(result.fdmBeforeFragColor, /malievFdmLayerRelief/);
    assert.match(result.fdmBeforeFragColor, /_fdmRidgeHighlight/);
    assert.match(result.fdmBeforeFragColor, /_fdmGrooveShadow/);
    assert.match(result.fdmBeforeFragColor, /finalColor\.rgb\s*\*=/);
    assert.doesNotMatch(result.fdmBeforeFragColor, /1\.0\s*-\s*_groove/);
    assert.doesNotMatch(result.fdmBeforeFragColor, /\bcolor\.rgb\b/);
});

test('FDM layer shader keeps close-up line contrast while dampening moire', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        configureMaterialFromConfigurator('viewer', 'pla', null, 'AS_PRINTED', null, 'FDM');
        setRenderMode('viewer', 'realistic');
        const material = scene.meshes[0].material;
        const fdmPlugin = material?._pluginInstances?.find(plugin => plugin.name === 'FdmLayer');
        const customCode = fdmPlugin?.getCustomCode('fragment') ?? {};
        ({
            definitions: customCode.CUSTOM_FRAGMENT_DEFINITIONS ?? '',
            beforeLights: customCode.CUSTOM_FRAGMENT_BEFORE_LIGHTS ?? '',
            updateMetallicRoughness: customCode.CUSTOM_FRAGMENT_UPDATE_METALLICROUGHNESS ?? '',
            beforeFragColor: customCode.CUSTOM_FRAGMENT_BEFORE_FRAGCOLOR ?? ''
        });
    `, context);

    assert.match(result.definitions, /malievFdmLayerFootprint/);
    assert.match(result.definitions, /malievFdmLayerVisibility/);
    assert.match(result.definitions, /malievFdmLayerMoireDampening/);
    assert.match(result.definitions, /mix\(\s*0\.[4-6][0-9]*\s*,\s*1\.0\s*,\s*_fdmCloseDetail\s*\)/);
    assert.match(result.definitions, /mix\(\s*1\.0\s*,\s*0\.[3-5][0-9]*\s*,\s*_fdmAliasRisk\s*\)/);
    assert.match(result.definitions, /malievFdmLayerFilteredGroove/);
    assert.match(result.beforeLights, /malievFdmVis/);
    assert.match(result.beforeLights, /malievFdmMoire/);
    assert.match(result.updateMetallicRoughness, /malievFdmVis/);
    assert.match(result.beforeFragColor, /malievFdmVis/);
    assert.match(result.beforeFragColor, /malievFdmMoire/);
    assert.doesNotMatch(result.beforeFragColor, /_fdmFinalAa/);
});

test('realistic smoothing does not synthesize normals for no-normal meshes', () => {
    const context = loadViewerContext();
    const setDataCalls = [];
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: kind => kind === 'position'
            ? new Float32Array([
                0, 0, 0,
                1, 0, 0,
                0, 1, 0,
            ])
            : null,
        getIndices: () => [0, 1, 2],
        setVerticesData: (kind, data) => setDataCalls.push({ kind, length: data?.length ?? 0 }),
    };
    const scene = makeScene(mesh);
    context.scene = scene;
    context.setDataCalls = setDataCalls;

    const result = vm.runInContext(`
        let computeNormalsCalls = 0;
        BABYLON.VertexData.ComputeNormals = (_positions, _indices, normals) => {
            computeNormalsCalls++;
            normals.push(0, 0, 1, 0, 0, 1, 0, 0, 1);
        };
        scenes.viewer = scene;
        setRenderMode('viewer', 'realistic');
        ({
            computeNormalsCalls,
            setDataCalls,
            savedNormalCount: originalNormalData.viewer?.size ?? 0
        });
    `, context);

    assert.equal(result.computeNormalsCalls, 0);
    assert.deepEqual(result.setDataCalls, []);
    assert.equal(result.savedNormalCount, 0);
});

test('bead blasted surface shader renders visible satin aluminum micrograin without coarse relief', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        configureMaterialFromConfigurator('viewer', 'aluminum', null, 'BEAD_BLAST', 'RA_3_2', 'CNC_MILL');
        setRenderMode('viewer', 'realistic');
        const material = scene.meshes[0].material;
        const surfacePlugin = material._pluginInstances?.find(plugin => plugin.name === 'MalievSurfaceEffect');
        const customCode = surfacePlugin?.getCustomCode('fragment') ?? {};
        ({
            bump: material?._malievSurfaceEffect?.bump ?? 0,
            scale: material?._malievSurfaceEffect?.scale ?? 0,
            strength: material?._malievSurfaceEffect?.strength ?? 0,
            roughness: material?.roughness ?? null,
            metallic: material?.metallic ?? null,
            definitions: customCode.CUSTOM_FRAGMENT_DEFINITIONS ?? '',
            updateAlbedo: customCode.CUSTOM_FRAGMENT_UPDATE_ALBEDO ?? '',
            updateMetallicRoughness: customCode.CUSTOM_FRAGMENT_UPDATE_METALLICROUGHNESS ?? '',
            beforeLights: customCode.CUSTOM_FRAGMENT_BEFORE_LIGHTS ?? '',
            beforeFragColor: customCode.CUSTOM_FRAGMENT_BEFORE_FRAGCOLOR ?? ''
        });
    `, context);

    assert.ok(result.roughness >= 0.86 && result.roughness <= 0.94, `expected zoom-stable matte bead-blast roughness, got ${result.roughness}`);
    assert.ok(result.metallic >= 0.72 && result.metallic <= 0.82, `expected bead-blasted aluminum to keep muted metal response without zoom-out reflections, got ${result.metallic}`);
    assert.ok(result.scale >= 8.8 && result.scale <= 10.5, `expected larger visible fine bead-blast grain scale, got ${result.scale}`);
    assert.ok(result.strength >= 0.058 && result.strength <= 0.070, `expected readable bead-blast roughness variation, got ${result.strength}`);
    assert.ok(result.bump >= 0.055 && result.bump <= 0.070, `expected visible but realistic bead-blast relief amplitude, got ${result.bump}`);
    assert.match(result.definitions, /malievHeightGradient/);
    assert.match(result.definitions, /malievSurfaceSpeckle/);
    assert.match(result.definitions, /malievBeadCraterHeight/);
    assert.match(result.definitions, /malievBeadCraterFootprint/);
    assert.match(result.beforeLights, /normalW\s*=\s*normalize/);
    assert.match(result.beforeLights, /malievBeadCraterGradient/);
    assert.match(result.beforeLights, /_beadAa/);
    assert.match(result.beforeLights, /smoothstep/);
    assert.match(result.beforeLights, /surfaceEffectBump/);
    assert.match(result.updateAlbedo, /malievMseIsBead/);
    assert.match(result.updateAlbedo, /malievMseRelief/);
    assert.match(result.updateMetallicRoughness, /surfaceEffectBump\s*\*\s*0\.35/);
    assert.match(result.updateMetallicRoughness, /max\(metallicRoughness\.g,\s*mix\(0\.0,\s*0\.88,\s*malievMseIsBead\)\)/);
    assert.match(result.beforeFragColor, /malievMseIsBead/);
    assert.match(result.beforeFragColor, /malievMseSpeckle/);
    assert.match(result.beforeFragColor, /malievMseRelief/);
    assert.match(result.beforeFragColor, /mix\(0\.18,\s*0\.05,\s*malievMseIsBead\)/);
    assert.match(result.beforeFragColor, /finalColor\.rgb/);
    assert.doesNotMatch(result.beforeFragColor, /\bcolor\.rgb\b/);
});

test('realistic material profiles use smooth low-amplitude finish detail to avoid temporal flicker', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        const profile = (materialKey, finishCode, roughnessCode, processCode) => {
            configureMaterialFromConfigurator('viewer', materialKey, null, finishCode, roughnessCode, processCode);
            setRenderMode('viewer', 'realistic');
            return scene.meshes[0].material?._malievNodeMaterialProfile;
        };
        ({
            brushed: profile('aluminum', 'BRUSHED', 'RA_1_6', 'CNC_MILL'),
            machining: profile('aluminum', 'AS_MACHINED', 'RA_1_6', 'CNC_MILL'),
            beadBlast: profile('aluminum', 'BEAD_BLAST', 'RA_3_2', 'CNC_MILL'),
            fdm: profile('pla', 'AS_PRINTED', null, 'FDM'),
            powder: profile('nylon-powder', 'AS_PRINTED', null, 'MJF')
        });
    `, context);

    assert.equal(result.brushed.stripeWaveform, 'sine');
    assert.ok(result.brushed.stripeScale <= 0.9);
    assert.ok(result.brushed.stripeStrength <= 0.02);
    assert.ok(result.brushed.normalStrength <= 0.01);

    assert.equal(result.machining.stripeWaveform, 'sine');
    assert.ok(result.machining.stripeScale <= 0.5);
    assert.ok(result.machining.stripeStrength >= 0.018 && result.machining.stripeStrength <= 0.024);

    assert.ok(result.beadBlast.noiseScale >= 8.8 && result.beadBlast.noiseScale <= 10.5, `expected larger visible bead-blast profile scale, got ${result.beadBlast.noiseScale}`);
    assert.ok(result.beadBlast.normalStrength >= 0.006 && result.beadBlast.normalStrength <= 0.008, `expected readable bead-blast profile relief, got ${result.beadBlast.normalStrength}`);

    assert.equal(result.fdm.layerWaveform, 'stepped-extrusion');
    assert.ok(Math.abs(result.fdm.layerHeightMm - 0.2) < 0.001, `expected physical 0.2 mm FDM layer height, got ${result.fdm.layerHeightMm}`);
    assert.ok(result.fdm.layerLineStrength >= 0.03, `expected readable FDM layer strength, got ${result.fdm.layerLineStrength}`);
    assert.ok(result.fdm.layerBump >= 0.20, `expected FDM protrusion/recess bump, got ${result.fdm.layerBump}`);

    assert.equal(result.powder.layerWaveform, 'powder-bed-step');
    assert.ok(Math.abs(result.powder.layerHeightMm - 0.3) < 0.001, `expected physical 0.3 mm MJF/SLS layer height, got ${result.powder.layerHeightMm}`);
    assert.ok(result.powder.layerLineStrength >= 0.016 && result.powder.layerLineStrength <= 0.022, `expected MJF/SLS powder profile to carry visible layer stepping, got ${result.powder.layerLineStrength}`);
    assert.ok(result.powder.layerBump >= 0.10 && result.powder.layerBump <= 0.14, `expected visible powder-bed layer bump, got ${result.powder.layerBump}`);
});

test('realistic configurator updates visible material without temporary color mutation', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        configureMaterialFromConfigurator('viewer', 'aluminum', '#336699', 'BRUSHED', 'RA_1_6', 'CNC_MILL');
        setRenderMode('viewer', 'realistic');

        const previousMaterial = scene.meshes[0].material;
        const oldSet = previousMaterial.albedoColor.set.bind(previousMaterial.albedoColor);
        const oldMaterialColorWrites = [];
        previousMaterial.albedoColor.set = (r, g, b) => {
            oldMaterialColorWrites.push({ r, g, b });
            oldSet(r, g, b);
        };

        configureMaterialFromConfigurator('viewer', 'aluminum', '#669933', 'BEAD_BLAST', 'RA_3_2', 'CNC_MILL');

        const currentMaterial = scene.meshes[0].material;
        ({
            oldMaterialColorWriteCount: oldMaterialColorWrites.length,
            oldMaterialColorWrites,
            materialReferenceStable: currentMaterial === previousMaterial,
            currentR: Number(currentMaterial.albedoColor.r.toFixed(3)),
            currentG: Number(currentMaterial.albedoColor.g.toFixed(3)),
            currentB: Number(currentMaterial.albedoColor.b.toFixed(3)),
            currentEffectKey: currentMaterial?._malievSurfaceEffect?.key ?? null
        });
    `, context);

    assert.equal(result.oldMaterialColorWriteCount, 1);
    const roundedColorWrites = JSON.parse(JSON.stringify(result.oldMaterialColorWrites.map(write => ({
        r: Number(write.r.toFixed(3)),
        g: Number(write.g.toFixed(3)),
        b: Number(write.b.toFixed(3)),
    }))));
    assert.deepEqual(roundedColorWrites, [{ r: 0.4, g: 0.6, b: 0.2 }]);
    assert.equal(result.materialReferenceStable, true);
    assert.equal(result.currentR, 0.4);
    assert.equal(result.currentG, 0.6);
    assert.equal(result.currentB, 0.2);
    assert.equal(result.currentEffectKey, 'bead-blast');
});

test('realistic configurator reuses material for CNC finish switches to avoid shader recompilation stalls', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        configureMaterialFromConfigurator('viewer', 'aluminum', null, 'AS_MACHINED', 'RA_1_6', 'CNC_MILL');
        setRenderMode('viewer', 'realistic');
        const asMachinedMaterial = scene.meshes[0].material;
        const materialCountAfterMachined = scene.materials.length;

        configureMaterialFromConfigurator('viewer', 'aluminum', null, 'BEAD_BLAST', 'RA_3_2', 'CNC_MILL');
        const beadBlastMaterial = scene.meshes[0].material;

        configureMaterialFromConfigurator('viewer', 'aluminum', null, 'BRUSHED', 'RA_1_6', 'CNC_MILL');
        const brushedMaterial = scene.meshes[0].material;

        ({
            materialReferenceStable: asMachinedMaterial === beadBlastMaterial && beadBlastMaterial === brushedMaterial,
            materialCountAfterMachined,
            materialCountAfterSwitches: scene.materials.length,
            finalEffectKey: brushedMaterial?._malievSurfaceEffect?.key ?? null,
            finalRoughness: brushedMaterial?.roughness ?? null,
            pluginCount: brushedMaterial?._pluginInstances?.filter(plugin => plugin.name === 'MalievSurfaceEffect').length ?? 0
        });
    `, context);

    assert.equal(result.materialReferenceStable, true);
    assert.equal(result.materialCountAfterSwitches, result.materialCountAfterMachined);
    assert.equal(result.finalEffectKey, 'brushed');
    assert.ok(result.finalRoughness >= 0.42 && result.finalRoughness <= 0.56, `expected brushed roughness after in-place switch, got ${result.finalRoughness}`);
    assert.equal(result.pluginCount, 1);
});

test('realistic configurator does not recreate material for identical configuration pushes', () => {
    const context = loadViewerContext();
    const mesh = {
        name: 'part',
        uniqueId: 101,
        material: null,
        metadata: {},
        disableEdgesRendering: () => {},
        getVerticesData: () => null,
        getIndices: () => null,
        setVerticesData: () => {},
    };
    const scene = makeScene(mesh);
    context.scene = scene;

    const result = vm.runInContext(`
        scenes.viewer = scene;
        configureMaterialFromConfigurator('viewer', 'aluminum', '#336699', 'BRUSHED', 'RA_1_6', 'CNC_MILL');
        setRenderMode('viewer', 'realistic');
        const firstMaterial = scene.meshes[0].material;
        const materialCountBefore = scene.materials.length;

        configureMaterialFromConfigurator('viewer', 'aluminum', '#336699', 'BRUSHED', 'RA_1_6', 'CNC_MILL');

        ({
            materialReferenceStable: scene.meshes[0].material === firstMaterial,
            materialCountBefore,
            materialCountAfter: scene.materials.length,
            effectKey: scene.meshes[0].material?._malievSurfaceEffect?.key ?? null
        });
    `, context);

    assert.equal(result.materialReferenceStable, true);
    assert.equal(result.materialCountAfter, result.materialCountBefore);
    assert.equal(result.effectKey, 'brushed');
});
