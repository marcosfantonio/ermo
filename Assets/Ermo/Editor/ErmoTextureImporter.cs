using UnityEditor;
using UnityEngine;

namespace Ermo.Editor
{
    public sealed class ErmoTextureImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Ermo/Textures/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.filterMode = FilterMode.Point; importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = false; importer.sRGBTexture = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
