using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KHorrorGame.Migration.Rendering.Editor
{
    public static class AmbientCgMaterialBuilder
    {
        private const string SourceRoot = "Assets/External/ambientcg/materials";
        private const string OutputRoot = "Assets/KHorrorGame/Materials/AmbientCG";
        private const string MaskOutputRoot = OutputRoot + "/MaskMaps";

        [MenuItem("Tools/K Horror Migration/Build ambientCG Materials")]
        public static void BuildAllMaterials()
        {
            if (!AssetDatabase.IsValidFolder(SourceRoot))
            {
                EditorUtility.DisplayDialog(
                    "ambientCG materials not found",
                    "Copy assets/external/ambientcg/materials into Assets/External/ambientcg/materials first.",
                    "OK");
                return;
            }

            EnsureAssetFolder(OutputRoot);
            EnsureAssetFolder(MaskOutputRoot);

            var materialFolders = AssetDatabase.GetSubFolders(SourceRoot);
            var builtCount = 0;

            foreach (var folder in materialFolders)
            {
                if (BuildMaterial(folder))
                {
                    builtCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Built {builtCount} ambientCG material assets into {OutputRoot}.");
        }

        private static bool BuildMaterial(string sourceFolder)
        {
            var materialName = Path.GetFileName(sourceFolder);
            if (string.IsNullOrWhiteSpace(materialName))
            {
                return false;
            }

            var colorPath = FindTexturePath(sourceFolder, "_Color");
            if (string.IsNullOrEmpty(colorPath))
            {
                Debug.LogWarning($"Skipping {materialName}: no color map found.");
                return false;
            }

            var normalPath = FindTexturePath(sourceFolder, "_NormalGL");
            if (string.IsNullOrEmpty(normalPath))
            {
                normalPath = FindTexturePath(sourceFolder, "_NormalDX");
            }

            var roughnessPath = FindTexturePath(sourceFolder, "_Roughness");
            var occlusionPath = FindTexturePath(sourceFolder, "_AmbientOcclusion");
            var heightPath = FindTexturePath(sourceFolder, "_Displacement");

            ConfigureTexture(colorPath, TextureImporterType.Default, true, false);
            ConfigureTexture(normalPath, TextureImporterType.NormalMap, false, false);
            ConfigureTexture(roughnessPath, TextureImporterType.Default, false, true);
            ConfigureTexture(occlusionPath, TextureImporterType.Default, false, true);
            ConfigureTexture(heightPath, TextureImporterType.Default, false, false);

            var maskMapPath = ComposeMaskMap(materialName, roughnessPath, occlusionPath);
            ConfigureTexture(maskMapPath, TextureImporterType.Default, false, false);

            var shader = FindBestLitShader();
            if (shader == null)
            {
                Debug.LogWarning("No supported Lit shader found. Install URP, HDRP, or enable the built-in Standard shader.");
                return false;
            }

            var materialPath = $"{OutputRoot}/{materialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = materialName
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = shader;
            }

            AssignMaterialTextures(material, colorPath, normalPath, maskMapPath, occlusionPath, heightPath);
            EditorUtility.SetDirty(material);
            return true;
        }

        private static void AssignMaterialTextures(
            Material material,
            string colorPath,
            string normalPath,
            string maskPath,
            string occlusionPath,
            string heightPath)
        {
            var maskTexture = LoadTexture(maskPath);

            SetTexture(material, new[] { "_BaseMap", "_BaseColorMap", "_MainTex" }, LoadTexture(colorPath));
            SetTexture(material, new[] { "_BumpMap", "_NormalMap" }, LoadTexture(normalPath));
            SetTexture(material, new[] { "_MetallicGlossMap", "_MaskMap" }, maskTexture);
            SetTexture(material, new[] { "_OcclusionMap" }, maskTexture != null ? maskTexture : LoadTexture(occlusionPath));
            SetTexture(material, new[] { "_ParallaxMap", "_HeightMap" }, LoadTexture(heightPath));

            SetFloat(material, new[] { "_Metallic" }, 0f);
            SetFloat(material, new[] { "_Smoothness", "_Glossiness" }, 0.38f);
            SetFloat(material, new[] { "_SmoothnessTextureChannel" }, 0f);
            SetFloat(material, new[] { "_BumpScale", "_NormalScale" }, 1f);
            SetFloat(material, new[] { "_OcclusionStrength" }, 0.78f);
            SetFloat(material, new[] { "_Parallax", "_HeightAmplitude" }, 0.018f);

            EnableKeyword(material, "_NORMALMAP", !string.IsNullOrEmpty(normalPath));
            EnableKeyword(material, "_METALLICSPECGLOSSMAP", !string.IsNullOrEmpty(maskPath));
            EnableKeyword(material, "_OCCLUSIONMAP", !string.IsNullOrEmpty(occlusionPath));
            EnableKeyword(material, "_PARALLAXMAP", !string.IsNullOrEmpty(heightPath));
        }

        private static string ComposeMaskMap(string materialName, string roughnessPath, string occlusionPath)
        {
            var roughness = LoadTexture(roughnessPath);
            var occlusion = LoadTexture(occlusionPath);

            if (roughness == null && occlusion == null)
            {
                return null;
            }

            var width = roughness != null ? roughness.width : occlusion.width;
            var height = roughness != null ? roughness.height : occlusion.height;
            var mask = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                name = materialName + "_Mask"
            };

            for (var y = 0; y < height; y++)
            {
                var v = height <= 1 ? 0f : y / (float)(height - 1);
                for (var x = 0; x < width; x++)
                {
                    var u = width <= 1 ? 0f : x / (float)(width - 1);
                    var roughnessValue = roughness != null ? roughness.GetPixelBilinear(u, v).grayscale : 0.62f;
                    var occlusionValue = occlusion != null ? occlusion.GetPixelBilinear(u, v).grayscale : 1f;
                    var smoothness = Mathf.Clamp01(1f - roughnessValue);
                    mask.SetPixel(x, y, new Color(0f, occlusionValue, 1f, smoothness));
                }
            }

            mask.Apply(false, false);

            EnsureAssetFolder(MaskOutputRoot);
            var maskAssetPath = $"{MaskOutputRoot}/{materialName}_Mask.png";
            var absolutePath = ToAbsoluteAssetPath(maskAssetPath);
            File.WriteAllBytes(absolutePath, mask.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(mask);
            AssetDatabase.ImportAsset(maskAssetPath);
            return maskAssetPath;
        }

        private static string FindTexturePath(string folder, string token)
        {
            if (string.IsNullOrEmpty(folder))
            {
                return null;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return path;
                }
            }

            return null;
        }

        private static Shader FindBestLitShader()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("HDRP/Lit");
            if (shader != null)
            {
                return shader;
            }

            return Shader.Find("Standard");
        }

        private static Texture2D LoadTexture(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void ConfigureTexture(
            string path,
            TextureImporterType textureType,
            bool sRgb,
            bool readable)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            var changed = false;
            if (importer.textureType != textureType)
            {
                importer.textureType = textureType;
                changed = true;
            }

            if (importer.sRGBTexture != sRgb)
            {
                importer.sRGBTexture = sRgb;
                changed = true;
            }

            if (importer.isReadable != readable)
            {
                importer.isReadable = readable;
                changed = true;
            }

            if (!importer.mipmapEnabled)
            {
                importer.mipmapEnabled = true;
                changed = true;
            }

            if (importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void SetTexture(Material material, string[] propertyNames, Texture texture)
        {
            if (texture == null)
            {
                return;
            }

            foreach (var propertyName in propertyNames)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetTexture(propertyName, texture);
                }
            }
        }

        private static void SetFloat(Material material, string[] propertyNames, float value)
        {
            foreach (var propertyName in propertyNames)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetFloat(propertyName, value);
                }
            }
        }

        private static void EnableKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
            {
                return;
            }

            var parts = assetFolder.Split('/');
            var current = parts[0];

            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private static string ToAbsoluteAssetPath(string assetPath)
        {
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Path must start with Assets/.", nameof(assetPath));
            }

            var relativePath = assetPath.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(Application.dataPath, relativePath);
        }
    }
}
