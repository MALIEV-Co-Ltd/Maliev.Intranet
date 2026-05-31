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
            PBRMaterial: class PBRMaterial {
                constructor(name, scene) {
                    this.name = name;
                    this.albedoColor = new Color3();
                    this.metallic = 0;
                    this.roughness = 0;
                    scene?.materials?.push(this);
                }
            },
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

test('realistic render mode upgrades a visible cutting mat to PBR materials', () => {
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
            actualTexturePreserved: scene.meshes[1].material.albedoTexture?.name === 'cutting-mat-texture',
            topReceivesShadows: scene.meshes[1].receiveShadows,
            slabReceivesShadows: scene.meshes[2].receiveShadows
        });
    `, context);

    assert.equal(result.topIsPbr, true);
    assert.equal(result.slabIsPbr, true);
    assert.equal(result.actualTexturePreserved, true);
    assert.equal(result.topReceivesShadows, true);
    assert.equal(result.slabReceivesShadows, true);
});
