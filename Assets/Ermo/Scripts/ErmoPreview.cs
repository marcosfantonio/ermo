using System.Collections.Generic;
using UnityEngine;

namespace Ermo
{
    // Explicit, bounded editor preview. No movement, disk saves, audio or runtime streaming.
    public sealed class ErmoPreview : MonoBehaviour
    {
        public static List<ErmoPreview> FindLive()
        {
            var result = new List<ErmoPreview>();
            // Normal FindObjectsByType deliberately excludes temporary DontSave objects.
            foreach (var preview in Resources.FindObjectsOfTypeAll<ErmoPreview>())
                if (preview.gameObject.scene.IsValid() && preview.gameObject.scene.isLoaded) result.Add(preview);
            return result;
        }
        private void OnDestroy()
        {
            var materials = new HashSet<Material>();
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials)
                    if (material != null) materials.Add(material);
            foreach (var material in materials)
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
        }
    }
}
