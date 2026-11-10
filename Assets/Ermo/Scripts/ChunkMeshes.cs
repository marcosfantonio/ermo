using UnityEngine;

namespace Ermo
{
    public sealed class ChunkMeshes : MonoBehaviour
    {
        private void OnDestroy()
        {
            foreach (var filter in GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                if (mesh == null || (mesh.name != "Meadow terrain" && mesh.name != "Winding path" && mesh.name != "Pitched roof" && !mesh.name.StartsWith("Water tower "))) continue;
                if (Application.isPlaying) Destroy(mesh);
                else DestroyImmediate(mesh);
            }
        }
    }
}
