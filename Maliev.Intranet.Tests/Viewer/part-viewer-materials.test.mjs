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
            MaterialPluginBase: class MaterialPluginBase {},
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
            effectKind: scene.meshes[0].material?._malievSurfaceEffect?.kind ?? null
        });
    `, context);

    assert.equal(result.materialType, 'aluminum');
    assert.equal(result.roughness, 0.78);
    assert.equal(result.metallic, 0.77);
    assert.equal(result.effectKey, 'bead-blast');
    assert.equal(result.effectKind, 1);
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
    assert.equal(result.effectKind, 2);
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

test('realistic configurator uses NodeMaterial PBR profiles with procedural normal detail for textured finishes', () => {
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
                isNodeMaterial: material instanceof BABYLON.NodeMaterial,
                pipeline: material?._malievMaterialPipeline ?? null,
                wasBuilt: material?.wasBuilt === true,
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

    assert.equal(result.beadBlast.isNodeMaterial, true);
    assert.equal(result.beadBlast.pipeline, 'node-pbr-procedural');
    assert.equal(result.beadBlast.wasBuilt, true);
    assert.equal(result.beadBlast.effectKey, 'bead-blast');
    assert.equal(result.beadBlast.nodeEffectKey, 'bead-blast');
    assert.ok(result.beadBlast.normalStrength > 0, 'bead blast should perturb normals with grain');

    assert.equal(result.brushed.isNodeMaterial, true);
    assert.equal(result.brushed.nodeEffectKey, 'brushed');
    assert.ok(result.brushed.stripeStrength > 0, 'brushed finish should include directional stripe normals');

    assert.equal(result.fdm.isNodeMaterial, true);
    assert.equal(result.fdm.nodeEffectKey, 'fdm-layer-lines');
    assert.ok(result.fdm.layerLineStrength > 0, 'FDM printing should include visible layer-line normals');

    assert.equal(result.sla.isNodeMaterial, true);
    assert.equal(result.sla.nodeEffectKey, 'fdm-layer-lines');
    assert.ok(result.sla.layerLineStrength > 0, 'SLA printing should include visible layer-line normals');
});
