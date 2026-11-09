using UnityEditor;
using UnityEngine;

namespace Ermo.Editor
{
    public static class ErmoScenePreview
    {
        [MenuItem("Ermo/Refresh 3D Preview")]
        public static void Refresh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var bootstrap = Object.FindFirstObjectByType<ErmoBootstrap>();
            if (bootstrap == null || bootstrap.Configuration == null)
            {
                Debug.LogWarning("Abra Assets/Ermo/Scenes/Ermo.unity antes de gerar a prévia."); return;
            }
            Clear();
            var settings = Object.Instantiate(bootstrap.Configuration);
            var root = new GameObject("Ermo 3D Preview — temporary");
            root.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            root.AddComponent<ErmoPreview>();
            var sampler = new WorldSampler(settings);
            var palette = new WorldPalette(bootstrap.SurfaceShader,settings);
            GodotAtmosphere.Apply(settings,false);
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                {
                    var record = sampler.Generate(new Vector2Int(x,z));
                    WorldGeometry.BuildChunk(record,settings,sampler,palette,root.transform);
                }
            var sun = new GameObject("Preview Sun").AddComponent<Light>();
            sun.transform.SetParent(root.transform,false); sun.transform.rotation = Quaternion.Euler(28,-48,0);
            sun.type = LightType.Directional; sun.color = new Color(1,.88f,.62f); sun.intensity = 1.22f;
            Object.DestroyImmediate(settings);
            Selection.activeGameObject = root;
            SceneView.RepaintAll();
        }

        [MenuItem("Ermo/Clear 3D Preview")]
        public static void Clear()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (var preview in ErmoPreview.FindLive())
                Object.DestroyImmediate(preview.gameObject);
        }
    }
}
