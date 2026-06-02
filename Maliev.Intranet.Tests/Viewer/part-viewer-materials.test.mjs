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
                    this.albedoColor = new Color3();
                    this.metallic = 0;
                    this.roughness = 0;
                    scene?.materials?.push(this);
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

function makeScene(mesh) {
    return {
        meshes: [mesh],
        materials: [],
        environmentIntensity: 0,
        environmentTexture: null,
        imageProcessingConfiguration: {},
        getMaterialByName(name) {
            return this.materials.find(material => material.name === name) ?? null;
        },
    };
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
    // realTimeFiltering must NOT be enabled (expensive + driver-dependent flicker risk).
    assert.notEqual(result.realTimeFiltering, true);
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
        dark: CONFIG.STUDIO_DARK.key.shadowDarkness
    })`, context);

    assert.ok(result.light <= 0.12);
    assert.ok(result.dark <= 0.10);
    assert.ok(result.dark <= result.light);
});

test('realistic render mode preserves stable cutting mat texture materials without receiving shadows', () => {
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
        setRenderMode('viewer', 'realistic');
        ({
            topIsPbr: scene.meshes[1].material instanceof BABYLON.PBRMaterial,
            slabIsPbr: scene.meshes[2].material instanceof BABYLON.PBRMaterial,
            actualTexturePreserved: scene.meshes[1].material.diffuseTexture?.name === 'cutting-mat-texture',
            topReceivesShadows: scene.meshes[1].receiveShadows,
            slabReceivesShadows: scene.meshes[2].receiveShadows
        });
    `, context);

    assert.equal(result.topIsPbr, false);
    assert.equal(result.slabIsPbr, false);
    assert.equal(result.actualTexturePreserved, true);
    assert.equal(result.topReceivesShadows, false);
    assert.equal(result.slabReceivesShadows, false);
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
    assert.ok(result.roughness >= 0.52 && result.roughness <= 0.62, `expected satin bead-blast roughness, got ${result.roughness}`);
    assert.ok(result.metallic >= 0.90, `expected bead-blasted aluminum to stay metallic, got ${result.metallic}`);
    assert.equal(result.effectKey, 'bead-blast');
    assert.equal(result.effectKind, 1);
    assert.ok(result.effectScale >= 4.0, `expected fine bead-blast micrograin scale, got ${result.effectScale}`);
    assert.ok(result.effectStrength <= 0.08, `expected low-amplitude bead-blast roughness variation, got ${result.effectStrength}`);
    assert.ok(result.effectBump <= 0.08, `expected subtle bead-blast relief amplitude, got ${result.effectBump}`);
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
                effectKind: material?._malievSurfaceEffect?.kind ?? null
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
    assert.equal(result.beadBlast.effectKey, 'bead-blast');
    assert.equal(result.brushed.effectKey, 'brushed');
    assert.equal(result.brushed.effectKind, 2);
    assert.equal(result.mirrorPolish.effectKey, null);
    assert.ok(result.mirrorPolish.metallic >= result.brushed.metallic);
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
        assert.ok(material.effectStripeStrength <= 0.025, `${name} machining marks should be subtle, got ${material.effectStripeStrength}`);
        assert.ok(material.effectBump <= 0.016, `${name} machining bump should avoid scanline banding, got ${material.effectBump}`);
        assert.equal(material.profile?.surfaceEffectKey, 'machining', `${name} profile should carry machining`);
        assert.ok(material.profile?.stripeStrength >= 0.012, `${name} should keep visible directional tool marks`);
        assert.match(material.beforeLights, /_isMachined/);
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
                return {
                    materialType: materialTypes.viewer,
                    roughness: scene.meshes[0].material?.roughness ?? null,
                    effectKey: scene.meshes[0].material?._malievSurfaceEffect?.key ?? null,
                    effectKind: scene.meshes[0].material?._malievSurfaceEffect?.kind ?? null
                };
            })(),
            slsEffect: (() => {
                configureMaterialFromConfigurator('viewer', 'nylon-powder', null, 'DYED_BLACK', null, 'SLS');
                setRenderMode('viewer', 'realistic');
                return scene.meshes[0].material?._malievSurfaceEffect?.key ?? null;
            })()
        });
    `, context);

    assert.equal(result.powderPresetExists, true);
    assert.equal(result.mjf.materialType, 'nylon-powder');
    assert.ok(result.mjf.roughness >= 0.68);
    assert.equal(result.mjf.effectKey, 'powder-grain');
    assert.equal(result.mjf.effectKind, 3);
    assert.equal(result.slsEffect, 'powder-grain');
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
                layerLineStrength: material?._malievNodeMaterialProfile?.layerLineStrength ?? 0
            };
        };
        ({
            beadBlast: snapshot('aluminum', 'BEAD_BLAST', 'RA_3_2', 'CNC_MILL'),
            brushed: snapshot('aluminum', 'BRUSHED', 'RA_1_6', 'CNC_MILL'),
            fdm: snapshot('pla', 'AS_PRINTED', null, 'FDM'),
            sla: snapshot('resin', 'AS_PRINTED', null, 'SLA')
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

    assert.equal(result.sla.isPbr, true);
    assert.equal(result.sla.nodeEffectKey, 'fdm-layer-lines');
    assert.ok(result.sla.layerLineStrength > 0, 'SLA printing should include visible layer-line detail');
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

        ({
            surfaceEnabled: surfacePlugin?._isEnabled ?? null,
            surfaceEnableCalls: surfacePlugin?._enableCalls ?? 0,
            surfaceDefine: surfaceDefines.MALIEV_SURFACE_EFFECT ?? null,
            fdmEnabled: fdmPlugin?._isEnabled ?? null,
            fdmEnableCalls: fdmPlugin?._enableCalls ?? 0,
            fdmDefine: fdmDefines.FDMLAYER ?? null,
            fdmBeforeFragColor: fdmCustomCode.CUSTOM_FRAGMENT_BEFORE_FRAGCOLOR ?? ''
        });
    `, context);

    assert.equal(result.surfaceEnabled, true);
    assert.equal(result.surfaceEnableCalls, 1);
    assert.equal(result.surfaceDefine, true);
    assert.equal(result.fdmEnabled, true);
    assert.equal(result.fdmEnableCalls, 1);
    assert.equal(result.fdmDefine, true);
    assert.match(result.fdmBeforeFragColor, /finalColor\.rgb/);
    assert.doesNotMatch(result.fdmBeforeFragColor, /\bcolor\.rgb\b/);
});

test('bead blasted surface shader renders fine satin aluminum micrograin without coarse relief', () => {
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

    assert.ok(result.roughness >= 0.52 && result.roughness <= 0.62, `expected satin bead-blast roughness, got ${result.roughness}`);
    assert.ok(result.metallic >= 0.90, `expected bead-blasted aluminum to stay metallic, got ${result.metallic}`);
    assert.ok(result.scale >= 4.0, `expected fine bead-blast grain scale, got ${result.scale}`);
    assert.ok(result.strength <= 0.08, `expected subtle bead-blast roughness variation, got ${result.strength}`);
    assert.ok(result.bump <= 0.08, `expected subtle bead-blast relief amplitude, got ${result.bump}`);
    assert.match(result.definitions, /malievHeightGradient/);
    assert.match(result.definitions, /malievSurfaceSpeckle/);
    assert.match(result.definitions, /malievBeadCraterHeight/);
    assert.match(result.definitions, /malievBeadCraterFootprint/);
    assert.match(result.beforeLights, /normalW\s*=\s*normalize/);
    assert.match(result.beforeLights, /malievBeadCraterGradient/);
    assert.match(result.beforeLights, /_beadCraterAa/);
    assert.match(result.beforeLights, /smoothstep/);
    assert.match(result.beforeLights, /surfaceEffectBump/);
    assert.match(result.updateAlbedo, /_isBead/);
    assert.match(result.updateAlbedo, /malievSurfaceRelief/);
    assert.match(result.updateMetallicRoughness, /surfaceEffectBump/);
    assert.match(result.beforeFragColor, /_isBead/);
    assert.match(result.beforeFragColor, /malievSurfaceSpeckle/);
    assert.match(result.beforeFragColor, /malievSurfaceRelief/);
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
            fdm: profile('pla', 'AS_PRINTED', null, 'FDM')
        });
    `, context);

    assert.equal(result.brushed.stripeWaveform, 'sine');
    assert.ok(result.brushed.stripeScale <= 0.9);
    assert.ok(result.brushed.stripeStrength <= 0.02);
    assert.ok(result.brushed.normalStrength <= 0.01);

    assert.equal(result.machining.stripeWaveform, 'sine');
    assert.ok(result.machining.stripeScale <= 0.5);
    assert.ok(result.machining.stripeStrength <= 0.014);

    assert.ok(result.beadBlast.noiseScale >= 4.0, `expected fine bead-blast profile scale, got ${result.beadBlast.noiseScale}`);
    assert.ok(result.beadBlast.normalStrength <= 0.008, `expected low bead-blast profile relief, got ${result.beadBlast.normalStrength}`);

    assert.equal(result.fdm.layerWaveform, 'sine');
    assert.ok(result.fdm.layerHeightMm >= 0.8);
    assert.ok(result.fdm.layerLineStrength <= 0.025);
});

test('realistic configurator replaces visible material without temporary color mutation', () => {
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
            materialWasReplaced: currentMaterial !== previousMaterial,
            currentR: Number(currentMaterial.albedoColor.r.toFixed(3)),
            currentG: Number(currentMaterial.albedoColor.g.toFixed(3)),
            currentB: Number(currentMaterial.albedoColor.b.toFixed(3)),
            currentEffectKey: currentMaterial?._malievSurfaceEffect?.key ?? null
        });
    `, context);

    assert.equal(result.oldMaterialColorWriteCount, 0);
    assert.equal(result.materialWasReplaced, true);
    assert.equal(result.currentR, 0.4);
    assert.equal(result.currentG, 0.6);
    assert.equal(result.currentB, 0.2);
    assert.equal(result.currentEffectKey, 'bead-blast');
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
