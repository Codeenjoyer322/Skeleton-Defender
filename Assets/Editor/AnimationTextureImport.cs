using UnityEditor;
using UnityEngine;

namespace SkeletonDefender.EditorTools
{
    /// <summary>Keep every delivered pixel and alpha value on every build target.</summary>
    public sealed class AnimationTextureImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/AnimationPack/Textures/") &&
                !assetPath.StartsWith("Assets/Resources/AnimationOverrides/") &&
                !assetPath.StartsWith("Assets/Resources/NeonGothic/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.maxTextureSize = 8192;
            foreach (string platform in new[] { "Standalone", "WebGL", "Android", "iPhone" })
            {
                var settings = importer.GetPlatformTextureSettings(platform);
                settings.name = platform;
                settings.overridden = true;
                settings.maxTextureSize = 8192;
                settings.format = TextureImporterFormat.RGBA32;
                settings.textureCompression = TextureImporterCompression.Uncompressed;
                settings.crunchedCompression = false;
                importer.SetPlatformTextureSettings(settings);
            }
        }
    }
}
