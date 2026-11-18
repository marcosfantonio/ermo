using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ermo.Editor
{
    // Links the Blender FBX kit to URP materials, prefabs and the AtacarejoKit
    // asset. Reads Assets/Ermo/Modelos/Atacarejo/kit.json, written by
    // gerar_atacarejo.py. Safe to run again: it only fixes what differs.
    public static class AtacarejoPrefabs
    {
        const string ModelDir = "Assets/Ermo/Modelos/Atacarejo/";
        const string KitJson = ModelDir + "kit.json";
        const string MaterialDir = "Assets/Ermo/Materials/Atacarejo/";
        const string PrefabDir = "Assets/Ermo/Prefabs/Atacarejo/";
        const string KitAssetPath = "Assets/Ermo/Settings/AtacarejoKit.asset";

        static readonly Dictionary<string, string> KitFields = new Dictionary<string, string>
        {
            { "Piso", "piso" }, { "Forro", "forro" }, { "Luminaria", "luminaria" }, { "Pilar", "pilar" },
            { "Parede", "parede" }, { "PortaCamara", "portaCamara" }, { "PortaSaida", "portaSaida" },
            { "PortalEntrada", "portal" }, { "PlacaSetor13", "placaSetor" }, { "PlacaCorredor", "placaCorredor" },
            { "Gondola", "gondola" }, { "Testeira", "testeira" }, { "PortaPalete", "portaPalete" },
            { "CaixaAtendimento", "atendimento" }, { "IlhaFreezer", "ilha" }, { "FreezerVertical", "freezer" },
            { "Bandeja", "bandeja" }, { "Palete", "palete" }, { "CaixaPapelao", "caixa" }, { "Fardo", "fardo" },
            { "Cartucho", "cartucho" }, { "Garrafao", "garrafao" }, { "Pacote", "pacote" },
            { "LuminariaMorta", "luminariaMorta" }, { "PlacaSaida", "placaSaida" }, { "SetaCorredor", "seta" },
            { "Placa13", "placa13" }, { "Evaporador", "evaporador" }
        };

        [MenuItem("Ermo/Rebuild Atacarejo Prefabs")]
        public static void RebuildFromMenu() => Rebuild();

        // Batch entry: Unity.exe -batchmode -executeMethod Ermo.Editor.AtacarejoPrefabs.RebuildBatch
        public static void RebuildBatch()
        {
            int code = 0;
            try { Rebuild(); }
            catch (Exception e) { Debug.LogError("ATACAREJO PREFABS FAILED: " + e); code = 1; }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        public static void Rebuild()
        {
            if (!File.Exists(KitJson)) throw new FileNotFoundException("Kit do Atacarejo não encontrado", KitJson);
            var kit = JsonUtility.FromJson<KitFile>(File.ReadAllText(KitJson));
            if (kit?.assets == null || kit.assets.Length == 0) throw new InvalidOperationException("kit.json sem modelos.");

            foreach (var asset in kit.assets) ConfigureImporter(ModelDir + asset.file + ".fbx");
            AssetDatabase.Refresh();
            int materials = SyncMaterials(kit);
            var problems = new List<string>();
            int created = 0;
            foreach (var asset in kit.assets)
            {
                string fbx = ModelDir + asset.file + ".fbx";
                var mesh = LoadMesh(fbx, asset.file);
                if (mesh == null) { problems.Add("malha ausente: " + fbx); continue; }
                CheckBounds(asset, mesh, problems);
                string path = PrefabDir + asset.folder + "/" + asset.prefab + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) { CreatePrefab(asset, mesh, path); created++; }
                else RelinkPrefab(asset, mesh, path);
            }
            CheckOrientation(problems);
            int linked = AssignKit(problems);
            AssetDatabase.SaveAssets();
            if (problems.Count > 0) throw new InvalidOperationException(string.Join("\n", problems));
            Debug.Log($"ERMO ATACAREJO PREFABS PASSED: {kit.assets.Length} modelos, {created} prefabs novos, {materials} materiais, {linked} campos do kit.");
        }

        static void ConfigureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) { AssetDatabase.ImportAsset(path); importer = AssetImporter.GetAtPath(path) as ModelImporter; }
            if (importer == null) return;
            bool dirty = importer.globalScale != 1f || !importer.useFileScale || importer.addCollider ||
                         importer.bakeAxisConversion || importer.materialImportMode != ModelImporterMaterialImportMode.None ||
                         importer.importAnimation || importer.importCameras || importer.importLights;
            if (!dirty) return;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.addCollider = false;
            importer.bakeAxisConversion = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
        }

        // Material colours are serialized in sRGB. Writes the Blender hex as
        // is: URP/Lit, Metallic 0, Smoothness 0.1, emission where listed.
        static int SyncMaterials(KitFile kit)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP/Lit não encontrado.");
            var glow = new Dictionary<string, float>();
            if (kit.emissive != null) foreach (var e in kit.emissive) glow[e.name] = e.strength;
            int count = 0;
            Directory.CreateDirectory(MaterialDir);
            foreach (var entry in kit.palette ?? Array.Empty<Swatch>())
            {
                if (!ColorUtility.TryParseHtmlString("#" + entry.hex, out var color)) continue;
                string path = MaterialDir + entry.name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader) { name = entry.name };
                    AssetDatabase.CreateAsset(material, path);
                }
                if (material.shader != shader) material.shader = shader;
                material.SetColor("_BaseColor", color);
                material.SetColor("_Color", color);
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Smoothness", .1f);
                material.SetFloat("_Glossiness", .1f);
                if (glow.TryGetValue(entry.name, out float strength))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * strength);
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else
                {
                    material.DisableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", Color.black);
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }
                EditorUtility.SetDirty(material);
                count++;
            }
            return count;
        }

        static Mesh LoadMesh(string fbxPath, string meshName)
        {
            var matches = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Mesh>().ToArray();
            return matches.FirstOrDefault(mesh => mesh.name == meshName) ?? matches.FirstOrDefault();
        }

        static void CheckBounds(KitAsset asset, Mesh mesh, List<string> problems)
        {
            if (asset.min == null || asset.max == null) return;
            Vector3 pivot = ToVector(asset.pivot);
            Vector3 min = mesh.bounds.min + pivot, max = mesh.bounds.max + pivot;
            Vector3 wantMin = ToVector(asset.min), wantMax = ToVector(asset.max);
            if ((min - wantMin).magnitude > .003f || (max - wantMax).magnitude > .003f)
                problems.Add($"{asset.file}: medida na Unity {min:F3}..{max:F3}, Blender {wantMin:F3}..{wantMax:F3}");
            else Debug.Log($"{asset.file} {mesh.bounds.size.x:0.###} x {mesh.bounds.size.y:0.###} x {mesh.bounds.size.z:0.###} m, {mesh.triangles.Length / 3} tri");
        }

        // The digits of the 13 plate are modelled on its +X face. If they land
        // on -X here, the axis conversion in gerar_atacarejo.py is wrong.
        static void CheckOrientation(List<string> problems)
        {
            var mesh = LoadMesh(ModelDir + "placa_setor_13.fbx", "placa_setor_13");
            if (mesh == null || mesh.subMeshCount < 2) { problems.Add("placa_setor_13 sem submalha de dígitos."); return; }
            float x = mesh.GetSubMesh(1).bounds.center.x;
            var sign = LoadMesh(ModelDir + "placa_saida.fbx", "placa_saida");
            float letters = sign != null && sign.subMeshCount > 2 ? sign.GetSubMesh(2).bounds.center.x : 1f;
            if (x <= .005f || letters <= .1f)
                problems.Add($"ORIENTACAO INVERTIDA: dígitos em x = {x:0.###}, letras em x = {letters:0.###}. Troque FLIP em gerar_atacarejo.py.");
            else Debug.Log($"ERMO ATACAREJO ORIENTATION PASSED: dígitos da placa 13 em x = {x:0.###}, letras SAÍDA em x = {letters:0.###}.");
        }

        static Material[] Materials(KitAsset asset) => asset.materials
            .Select(name => AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + name + ".mat")).ToArray();

        static void CreatePrefab(KitAsset asset, Mesh mesh, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets");
            var root = new GameObject(asset.prefab);
            try
            {
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = Materials(asset);
                renderer.shadowCastingMode = asset.colliders == null || asset.colliders.Length == 0 ? ShadowCastingMode.Off : ShadowCastingMode.On;
                AddColliders(root, asset);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void RelinkPrefab(KitAsset asset, Mesh mesh, string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var filter = root.GetComponent<MeshFilter>();
                var renderer = root.GetComponent<MeshRenderer>();
                var materials = Materials(asset);
                bool dirty = filter == null || filter.sharedMesh != mesh || renderer == null ||
                             !renderer.sharedMaterials.SequenceEqual(materials);
                if (!dirty) return;
                if (filter == null) filter = root.AddComponent<MeshFilter>();
                if (renderer == null) renderer = root.AddComponent<MeshRenderer>();
                filter.sharedMesh = mesh;
                renderer.sharedMaterials = materials;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static int AssignKit(List<string> problems)
        {
            var kit = AssetDatabase.LoadAssetAtPath<AtacarejoKit>(KitAssetPath);
            if (kit == null) { problems.Add("AtacarejoKit.asset ausente."); return 0; }
            var serialized = new SerializedObject(kit);
            int linked = 0;
            foreach (var pair in KitFields)
            {
                string path = Directory.GetFiles(PrefabDir, pair.Key + ".prefab", SearchOption.AllDirectories)
                    .Select(p => p.Replace('\\', '/')).FirstOrDefault();
                var prefab = path != null ? AssetDatabase.LoadAssetAtPath<GameObject>(path) : null;
                var property = serialized.FindProperty(pair.Value);
                if (property == null) { problems.Add("Campo do kit ausente: " + pair.Value); continue; }
                if (prefab == null) { problems.Add("Prefab ausente: " + pair.Key); continue; }
                property.objectReferenceValue = prefab;
                linked++;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(kit);
            return linked;
        }

        static void AddColliders(GameObject root, KitAsset asset)
        {
            if (asset.colliders == null || asset.colliders.Length == 0) return;
            if (asset.colliders.Length == 1)
            {
                var box = root.AddComponent<BoxCollider>();
                box.center = ToVector(asset.colliders[0].center);
                box.size = ToVector(asset.colliders[0].size);
                return;
            }
            for (var index = 0; index < asset.colliders.Length; index++)
            {
                var child = new GameObject(index == 0 ? "MontanteNegativo" : "MontantePositivo");
                child.transform.SetParent(root.transform, false);
                var box = child.AddComponent<BoxCollider>();
                box.center = ToVector(asset.colliders[index].center);
                box.size = ToVector(asset.colliders[index].size);
            }
        }

        static Vector3 ToVector(float[] value) =>
            value == null || value.Length < 3 ? Vector3.zero : new Vector3(value[0], value[1], value[2]);

        [Serializable] class KitFile { public KitAsset[] assets; public Swatch[] palette; public Glow[] emissive; }
        [Serializable] class KitAsset
        {
            public string file, prefab, folder;
            public string[] materials;
            public float[] pivot, min, max;
            public KitCollider[] colliders;
        }
        [Serializable] class KitCollider { public float[] center, size; }
        [Serializable] class Swatch { public string name, hex; }
        [Serializable] class Glow { public string name; public float strength; }
    }
}
