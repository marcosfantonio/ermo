# Gera materiais URP, metas de importação e prefabs a partir de kit.json.
# O fileID da malha segue o importador da Unity 2019.3+: xxHash64 de "Type:Mesh-><nome>".
# Se a Unity resolver outro id, Ermo/Rebuild Atacarejo Prefabs regrava o vínculo.

import hashlib
import json
import os

ERMO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ASSETS = os.path.abspath(os.path.join(ERMO, ".."))
KIT = os.path.join(os.path.dirname(__file__), "kit.json")

MASK64 = (1 << 64) - 1
P1 = 0x9E3779B185EBCA87
P2 = 0xC2B2AE3D27D4EB4F
P3 = 0x165667B19E3779F9
P4 = 0x85EBCA77C2B2AE63
P5 = 0x27D4EB2F165667C5


def rotl(value, count):
    return ((value << count) | (value >> (64 - count))) & MASK64


def xxh_round(acc, lane):
    acc = (acc + lane * P2) & MASK64
    acc = rotl(acc, 31)
    acc = (acc * P1) & MASK64
    return acc


def xxh64(data, seed=0):
    length = len(data)
    index = 0
    if length >= 32:
        acc1 = (seed + P1 + P2) & MASK64
        acc2 = (seed + P2) & MASK64
        acc3 = seed & MASK64
        acc4 = (seed - P1) & MASK64
        while index + 32 <= length:
            acc1 = xxh_round(acc1, int.from_bytes(data[index : index + 8], "little"))
            acc2 = xxh_round(acc2, int.from_bytes(data[index + 8 : index + 16], "little"))
            acc3 = xxh_round(acc3, int.from_bytes(data[index + 16 : index + 24], "little"))
            acc4 = xxh_round(acc4, int.from_bytes(data[index + 24 : index + 32], "little"))
            index += 32
        digest = (
            rotl(acc1, 1) + rotl(acc2, 7) + rotl(acc3, 12) + rotl(acc4, 18)
        ) & MASK64
        for acc in (acc1, acc2, acc3, acc4):
            digest ^= xxh_round(0, acc)
            digest = (digest * P1 + P4) & MASK64
    else:
        digest = (seed + P5) & MASK64
    digest = (digest + length) & MASK64
    while index + 8 <= length:
        lane = int.from_bytes(data[index : index + 8], "little")
        digest ^= xxh_round(0, lane)
        digest = (rotl(digest, 27) * P1 + P4) & MASK64
        index += 8
    if index + 4 <= length:
        lane = int.from_bytes(data[index : index + 4], "little")
        digest ^= (lane * P1) & MASK64
        digest = (rotl(digest, 23) * P2 + P3) & MASK64
        index += 4
    while index < length:
        digest ^= (data[index] * P5) & MASK64
        digest = (rotl(digest, 11) * P1) & MASK64
        index += 1
    digest ^= digest >> 33
    digest = (digest * P2) & MASK64
    digest ^= digest >> 29
    digest = (digest * P3) & MASK64
    digest ^= digest >> 32
    return digest


def mesh_file_id(mesh_name):
    digest = xxh64(f"Type:Mesh->{mesh_name}".encode("utf-8"))
    return digest - (1 << 64) if digest >= (1 << 63) else digest


def guid(name):
    return hashlib.md5(f"ermo.atacarejo.{name}".encode("utf-8")).hexdigest()


def srgb_channel(value):
    channel = value / 255.0
    if channel <= 0.04045:
        return channel / 12.92
    return ((channel + 0.055) / 1.055) ** 2.4


def linear_color(hex_color):
    hex_color = hex_color.lstrip("#")
    channels = [int(hex_color[i : i + 2], 16) for i in (0, 2, 4)]
    red, green, blue = (srgb_channel(channel) for channel in channels)
    return red, green, blue


def write(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(text)


def folder_meta(name):
    return f"fileFormatVersion: 2\nguid: {guid('folder.' + name)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"


def material_yaml(name, hex_color):
    red, green, blue = linear_color(hex_color)
    color = "{r: %.8f, g: %.8f, b: %.8f, a: 1}" % (red, green, blue)
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  m_Shader: {{fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap:
    RenderType: Opaque
  disabledShaderPasses: []
  m_LockedProperties: 
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _BumpMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _DetailAlbedoMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _DetailMask:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _DetailNormalMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _EmissionMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MainTex:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MetallicGlossMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _OcclusionMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _ParallaxMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _SpecGlossMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Ints: []
    m_Floats:
    - _AlphaClip: 0
    - _AlphaToMask: 0
    - _Blend: 0
    - _BumpScale: 1
    - _Cull: 2
    - _Cutoff: 0.5
    - _DetailAlbedoMapScale: 1
    - _DetailNormalMapScale: 1
    - _DstBlend: 0
    - _DstBlendAlpha: 0
    - _EnvironmentReflections: 1
    - _GlossMapScale: 0
    - _Glossiness: 0
    - _GlossyReflections: 0
    - _Metallic: 0
    - _OcclusionStrength: 1
    - _Parallax: 0.005
    - _QueueOffset: 0
    - _ReceiveShadows: 1
    - _Smoothness: 0.1
    - _SmoothnessTextureChannel: 0
    - _SpecularHighlights: 1
    - _SrcBlend: 1
    - _SrcBlendAlpha: 1
    - _Surface: 0
    - _WorkflowMode: 1
    - _ZWrite: 1
    m_Colors:
    - _BaseColor: {color}
    - _Color: {color}
    - _EmissionColor: {{r: 0, g: 0, b: 0, a: 1}}
    - _SpecColor: {{r: 0.19999996, g: 0.19999996, b: 0.19999996, a: 1}}
  m_BuildTextureStacks: []
  m_AllowLocking: 1
"""


def material_meta(name):
    return f"""fileFormatVersion: 2
guid: {guid('mat.' + name)}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 2100000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def fbx_meta(file_name):
    return f"""fileFormatVersion: 2
guid: {guid('fbx.' + file_name)}
ModelImporter:
  serializedVersion: 24600
  internalIDToNameTable: []
  externalObjects: {{}}
  materials:
    materialImportMode: 0
    materialName: 0
    materialSearch: 1
    materialLocation: 1
    searchTexturesGlobally: 0
  animations:
    legacyGenerateAnimations: 4
    bakeSimulation: 0
    resampleCurves: 1
    optimizeGameObjects: 0
    removeConstantScaleCurves: 0
    motionNodeName: 
    animationImportErrors: 
    animationImportWarnings: 
    animationRetargetingWarnings: 
    animationDoRetargetingWarnings: 0
    importAnimatedCustomProperties: 0
    importConstraints: 0
    animationCompression: 1
    animationRotationError: 0.5
    animationPositionError: 0.5
    animationScaleError: 0.5
    animationWrapMode: 0
    extraExposedTransformPaths: []
    extraUserProperties: []
    clipAnimations: []
    isReadable: 0
  meshes:
    lODScreenPercentages: []
    globalScale: 1
    meshCompression: 0
    addColliders: 0
    useSRGBMaterialColor: 1
    sortHierarchyByName: 1
    preBakeConvexCollisionMesh: 0
    preBakeTriangleCollisionMesh: 0
    importPhysicalCameras: 0
    importVisibility: 1
    importBlendShapes: 0
    importCameras: 0
    importLights: 0
    nodeNameCollisionStrategy: 1
    fileIdsGeneration: 2
    swapUVChannels: 0
    generateSecondaryUV: 0
    useFileUnits: 1
    keepQuads: 0
    weldVertices: 1
    bakeAxisConversion: 0
    preserveHierarchy: 1
    skinWeightsMode: 0
    maxBonesPerVertex: 4
    minBoneWeight: 0.001
    optimizeBones: 1
    generateMeshLods: 0
    meshLodGenerationFlags: 0
    maximumMeshLod: -1
    importUVs: -1
    importVertexColors: 0
    meshOptimizationFlags: -1
    indexFormat: 0
    secondaryUVAngleDistortion: 8
    secondaryUVAreaDistortion: 15.000001
    secondaryUVHardAngle: 88
    secondaryUVMarginMethod: 1
    secondaryUVMinLightmapResolution: 40
    secondaryUVMinObjectScale: 1
    secondaryUVPackMargin: 4
    useFileScale: 1
    strictVertexDataChecks: 0
  tangentSpace:
    normalSmoothAngle: 60
    normalImportMode: 0
    tangentImportMode: 3
    normalCalculationMode: 4
    legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes: 0
    blendShapeNormalImportMode: 1
    normalSmoothingSource: 0
    calculateBlendshapeNormalsDeltaFromImportedNormals: 0
  referencedClips: []
  importAnimation: 0
  humanDescription:
    serializedVersion: 3
    human: []
    skeleton: []
    armTwist: 0.5
    foreArmTwist: 0.5
    upperLegTwist: 0.5
    legTwist: 0.5
    armStretch: 0.05
    legStretch: 0.05
    feetSpacing: 0
    globalScale: 1
    rootMotionBoneName: 
    hasTranslationDoF: 0
    hasExtraRoot: 0
    skeletonHasParents: 1
  lastHumanDescriptionAvatarSource: {{instanceID: 0}}
  autoGenerateAvatarMappingIfUnspecified: 1
  animationType: 0
  humanoidOversampling: 1
  avatarSetup: 0
  addHumanoidExtraRootOnlyWhenUsingAvatar: 1
  importBlendShapeDeformPercent: 0
  remapMaterialsIfMaterialImportModeIsNone: 0
  additionalBone: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def vec(values):
    return "{x: %s, y: %s, z: %s}" % (fmt(values[0]), fmt(values[1]), fmt(values[2]))


def fmt(value):
    text = "%.5f" % float(value)
    if "." in text:
        text = text.rstrip("0").rstrip(".")
    if text in {"-0", ""}:
        return "0"
    return text


def component_header(type_id, file_id, body):
    return f"--- !u!{type_id} &{file_id}\n{body}"


def game_object(file_id, name, components):
    lines = "\n".join(f"  - component: {{fileID: {component}}}" for component in components)
    return f"""--- !u!1 &{file_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{lines}
  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
"""


def transform(file_id, game_object_id, position, children):
    child_block = " []" if not children else "\n" + "\n".join(
        f"  - {{fileID: {child}}}" for child in children
    )
    return f"""--- !u!4 &{file_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {game_object_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {vec(position)}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:{child_block}
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
"""


def child_transform(file_id, game_object_id, father_id):
    return f"""--- !u!4 &{file_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {game_object_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {father_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
"""


def mesh_filter(file_id, game_object_id, mesh_id, mesh_guid):
    return f"""--- !u!33 &{file_id}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {game_object_id}}}
  m_Mesh: {{fileID: {mesh_id}, guid: {mesh_guid}, type: 3}}
"""


def mesh_renderer(file_id, game_object_id, material_guids):
    slots = "\n".join(
        f"  - {{fileID: 2100000, guid: {material_guid}, type: 2}}" for material_guid in material_guids
    )
    return f"""--- !u!23 &{file_id}
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {game_object_id}}}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 2
  m_RayTraceProcedural: 0
  m_RayTracingAccelStructBuildFlagsOverride: 0
  m_RayTracingAccelStructBuildFlags: 1
  m_SmallMeshCulling: 1
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
{slots}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_AdditionalVertexStreams: {{fileID: 0}}
"""


def box_collider(file_id, game_object_id, center, size):
    return f"""--- !u!65 &{file_id}
BoxCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {game_object_id}}}
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_ProvidesContacts: 0
  m_Enabled: 1
  m_Radius: 0.5
  m_Center: {vec(center)}
  m_Size: {vec(size)}
"""


def prefab_yaml(asset, mesh_id):
    mesh_guid = guid("fbx." + asset["file"])
    material_guids = [guid("mat." + name) for name in asset["materials"]]
    root = 100000
    root_transform = 100001
    pieces = [
        game_object(
            root,
            asset["prefab"],
            [root_transform, 100002, 100003] + ([100004] if len(asset["colliders"]) == 1 else []),
        ),
        transform(root_transform, root, asset["pivot"], [200001 + index * 10 for index in range(len(asset["colliders"]))] if len(asset["colliders"]) > 1 else []),
        mesh_filter(100002, root, mesh_id, mesh_guid),
        mesh_renderer(100003, root, material_guids),
    ]
    if len(asset["colliders"]) == 1:
        collider = asset["colliders"][0]
        pieces.append(box_collider(100004, root, collider["center"], collider["size"]))
    elif len(asset["colliders"]) > 1:
        for index, collider in enumerate(asset["colliders"]):
            child = 200000 + index * 10
            child_transform_id = child + 1
            pieces[0] = pieces[0]  # root components already listed
            name = "MontanteNegativo" if index == 0 else "MontantePositivo"
            pieces.append(game_object(child, name, [child_transform_id, child + 2]))
            pieces.append(child_transform(child_transform_id, child, root_transform))
            pieces.append(box_collider(child + 2, child, collider["center"], collider["size"]))
    return "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n" + "".join(piece if piece.endswith("\n") else piece + "\n" for piece in pieces)


def prefab_meta(name):
    return f"""fileFormatVersion: 2
guid: {guid('prefab.' + name)}
PrefabImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def main():
    empty = xxh64(b"")
    if empty != 0xEF46DB3751D8E999:
        raise SystemExit("xxHash64 vazio deu %x" % empty)
    with open(KIT, encoding="utf-8") as handle:
        kit = json.load(handle)
    folders = [
        "Materials",
        "Materials/Atacarejo",
        "Modelos/Atacarejo",
        "Prefabs",
        "Prefabs/Atacarejo",
        "Prefabs/Atacarejo/Arquitetura",
        "Prefabs/Atacarejo/Estacoes",
        "Prefabs/Atacarejo/Props",
    ]
    for folder in folders:
        meta_path = os.path.join(ERMO, folder + ".meta")
        if not os.path.exists(meta_path):
            write(meta_path, folder_meta(folder))
    for name, hex_color in kit["colors"].items():
        directory = os.path.join(ERMO, "Materials", "Atacarejo")
        write(os.path.join(directory, name + ".mat"), material_yaml(name, hex_color))
        write(os.path.join(directory, name + ".mat.meta"), material_meta(name))
    for asset in kit["assets"]:
        file_name = asset["file"]
        fbx = os.path.join(ERMO, "Modelos", "Atacarejo", file_name + ".fbx")
        if not os.path.exists(fbx):
            raise SystemExit("FBX ausente: " + fbx)
        write(fbx + ".meta", fbx_meta(file_name))
        mesh_id = mesh_file_id(file_name)
        prefab_path = os.path.join(ERMO, "Prefabs", "Atacarejo", asset["folder"], asset["prefab"] + ".prefab")
        write(prefab_path, prefab_yaml(asset, mesh_id))
        write(prefab_path + ".meta", prefab_meta(asset["prefab"]))
        print(
            "%s fileID=%s size=%s tris=%s"
            % (file_name, mesh_id, asset["size"], asset["triangles"])
        )


if __name__ == "__main__":
    main()
