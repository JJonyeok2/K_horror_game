using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class EstateVisualIdentityTests
    {
        private const string ScenePath = "Assets/Scenes/KHorror_Main.unity";

        [Test]
        public void CoreEstateSurfacesUseAmbientCgPbrMaterials()
        {
            EditorSceneManager.OpenScene(ScenePath);

            AssertPbrMaterial("ApproachRoad_MuddyCenter", "Mat_MuddyRoad_PBR");
            AssertPbrMaterial("LeftForestTree_0_Trunk", "Mat_PineBark_PBR");
            AssertPbrMaterial("OuterGateThresholdStone", "Mat_WetStone_PBR");
            AssertPbrMaterial("CourtyardLeftWall", "Mat_OldStoneWall_PBR");
            AssertPbrMaterial("MainHouseBackWall_Right", "Mat_StainedPlaster_PBR");
            AssertPbrMaterial("MainHouseRoof_LeftSlope", "Mat_BlackGiwaTile_PBR");
            AssertPbrMaterial("MainHouseWoodenMaru", "Mat_AgedWood_PBR");
        }

        [Test]
        public void KoreanHorrorDetailsAreDenseAndReadable()
        {
            EditorSceneManager.OpenScene(ScenePath);

            Assert.GreaterOrEqual(CountSceneObjects("RoofEaveTileDetail_"), 32, "Hanok roofs need repeated tile/eave details, not flat slabs.");
            Assert.GreaterOrEqual(CountSceneObjects("JangseungFaceDetail_"), 24, "Jangseung props need carved eyes, teeth, and facial marks.");
            Assert.GreaterOrEqual(CountSceneObjects("KoreanTalismanCluster_"), 10, "The estate needs repeated talisman clusters around doors and route warnings.");
            Assert.GreaterOrEqual(CountSceneObjects("WornPlasterPatch_"), 12, "Plaster walls need stained and worn patches to avoid clay-like surfaces.");
            Assert.GreaterOrEqual(CountSceneObjects("ShrineRopeTwist_"), 8, "Shrine rope should read as twisted straw, not a single bar.");
        }

        [Test]
        public void SmallVisualUpgradePiecesDoNotCreateCollisionSnags()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var visualPrefixes = new[]
            {
                "RoofEaveTileDetail_",
                "JangseungFaceDetail_",
                "KoreanTalismanCluster_",
                "WornPlasterPatch_",
                "ShrineRopeTwist_",
            };

            var blockingDetails = UnityEngine.Object.FindObjectsOfType<Collider>()
                .Where(collider => collider.gameObject.scene.IsValid())
                .Where(collider => visualPrefixes.Any(prefix => collider.name.StartsWith(prefix, StringComparison.Ordinal)))
                .Where(collider => collider.enabled && !collider.isTrigger)
                .Select(collider => collider.name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.IsEmpty(blockingDetails, "High-density visual details should be non-blocking: " + string.Join(", ", blockingDetails));
        }

        private static void AssertPbrMaterial(string objectName, string expectedMaterialName)
        {
            var target = GameObject.Find(objectName);
            Assert.IsNotNull(target, objectName + " should exist in the generated scene.");

            var renderer = target.GetComponent<MeshRenderer>();
            Assert.IsNotNull(renderer, objectName + " should have a MeshRenderer.");

            var material = renderer.sharedMaterial;
            Assert.IsNotNull(material, objectName + " should have a material.");
            Assert.That(material.name, Does.Contain(expectedMaterialName), objectName + " should use " + expectedMaterialName + ".");

            var texture = GetPrimaryTexture(material);
            Assert.IsNotNull(texture, objectName + " should use an ambientCG texture map, not only a flat color.");
        }

        private static Texture GetPrimaryTexture(Material material)
        {
            foreach (var property in new[] { "_BaseMap", "_BaseColorMap", "_MainTex" })
            {
                if (material.HasProperty(property))
                {
                    var texture = material.GetTexture(property);
                    if (texture != null)
                    {
                        return texture;
                    }
                }
            }

            return null;
        }

        private static int CountSceneObjects(string prefix)
        {
            return UnityEngine.Object.FindObjectsOfType<Transform>()
                .Where(transform => transform.gameObject.scene.IsValid())
                .Count(transform => transform.name.StartsWith(prefix, StringComparison.Ordinal));
        }
    }
}
