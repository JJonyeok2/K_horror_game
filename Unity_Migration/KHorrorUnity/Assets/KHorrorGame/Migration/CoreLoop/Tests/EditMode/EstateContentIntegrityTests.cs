using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class EstateContentIntegrityTests
    {
        private const string ScenePath = "Assets/Scenes/KHorror_Main.unity";

        [Test]
        public void ShrineEntranceDecorationsDoNotBlockPlayerCapsule()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            var blockers = UnityEngine.Object.FindObjectsOfType<Collider>()
                .Where(collider => collider.enabled && !collider.isTrigger)
                .Where(collider => collider.name.StartsWith("ShrineClothStrip", StringComparison.Ordinal) ||
                                   collider.name == "ShrineHangingRope")
                .Select(collider => collider.name)
                .ToArray();

            Assert.IsEmpty(blockers, "Shrine entrance decorations should be visual-only: " + string.Join(", ", blockers));
        }

        [Test]
        public void EstateContainsEnoughPickupArtifactsForAReadableLootRun()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var pickups = UnityEngine.Object.FindObjectsOfType<ArtifactPickup>()
                .Where(pickup => pickup.gameObject.scene.IsValid())
                .Select(pickup => pickup.name)
                .OrderBy(name => name)
                .ToArray();

            Assert.GreaterOrEqual(pickups.Length, 6, "Expected at least six pickup artifacts, found: " + string.Join(", ", pickups));
        }

        [Test]
        public void EstateHasRuntimeThreatProxySpawner()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var spawnerType = Type.GetType("KHorrorGame.Migration.ThreatProxySpawner, KHorrorGame.Migration");
            Assert.IsNotNull(spawnerType, "ThreatProxySpawner runtime type should exist.");

            var spawnerObject = GameObject.Find("ThreatProxySpawner");
            Assert.IsNotNull(spawnerObject, "ThreatProxySpawner scene object should exist.");

            var ghost = GameObject.Find("GhostThreatProxy");
            var dokkaebi = GameObject.Find("DokkaebiThreatProxy");
            Assert.IsNotNull(ghost, "GhostThreatProxy should exist so monster spawn is visible in estate.");
            Assert.IsNotNull(dokkaebi, "DokkaebiThreatProxy should exist so forest threat spawn is visible.");
        }

        [Test]
        public void ThreatProxySpawnerRevealsMonstersOnlyAfterThreatGateOpens()
        {
            var root = new GameObject("SpawnerFixture");
            var ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghost.name = "GhostFixture";
            var dokkaebi = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dokkaebi.name = "DokkaebiFixture";
            var spawner = root.AddComponent<ThreatProxySpawner>();

            try
            {
                ghost.GetComponent<Renderer>().enabled = false;
                dokkaebi.GetComponent<Renderer>().enabled = false;
                SetObject(spawner, "ghostProxy", ghost);
                SetObject(spawner, "dokkaebiProxy", dokkaebi);

                spawner.EvaluateThreats(false, 5, GameMapId.JonggaEstate);
                Assert.IsFalse(ghost.GetComponent<Renderer>().enabled);
                Assert.IsFalse(dokkaebi.GetComponent<Renderer>().enabled);

                spawner.EvaluateThreats(true, 2, GameMapId.JonggaEstate);
                Assert.IsTrue(ghost.GetComponent<Renderer>().enabled);
                Assert.IsTrue(dokkaebi.GetComponent<Renderer>().enabled);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(ghost);
                UnityEngine.Object.DestroyImmediate(dokkaebi);
            }
        }

        [Test]
        public void ShrineEntranceOnlyAllowsApproachFromBackRoute()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            AssertShrineSideBoundary(
                new Vector3(-4.2f, 1.2f, 94.5f),
                Vector3.left,
                "Shrine entrance can be reached directly from the courtyard/kitchen side.");
            AssertShrineSideBoundary(
                new Vector3(-4.2f, 1.2f, 98.7f),
                Vector3.left,
                "Shrine front can be reached directly from the courtyard/kitchen side.");

            AssertShrineSideBoundary(
                new Vector3(-11.8f, 1.2f, 94.5f),
                Vector3.right,
                "Shrine entrance can be reached directly from the outer west side.");
            AssertShrineSideBoundary(
                new Vector3(-11.8f, 1.2f, 98.7f),
                Vector3.right,
                "Shrine front can be reached directly from the outer west side.");

            var intendedHits = Physics.RaycastAll(new Vector3(-8f, 1.2f, 91f), Vector3.forward, 5.4f)
                .Where(hit => IsShrineRouteBoundary(hit.collider.name))
                .ToArray();
            Assert.IsEmpty(intendedHits, "Intended back shrine path is blocked by shrine route boundary.");
        }

        private static void AssertShrineSideBoundary(Vector3 origin, Vector3 direction, string failureMessage)
        {
            Assert.IsTrue(
                TryFindShrineRouteBoundary(origin, direction, out var hit),
                failureMessage);
            Assert.LessOrEqual(hit.distance, 2.5f, "Shrine side blocker is too far from the shortcut route.");
        }

        private static void SetObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool TryFindShrineRouteBoundary(Vector3 origin, Vector3 direction, out RaycastHit boundaryHit)
        {
            var hits = Physics.RaycastAll(origin, direction.normalized, 8f)
                .Where(hit => IsShrineRouteBoundary(hit.collider.name))
                .OrderBy(hit => hit.distance);

            foreach (var hit in hits)
            {
                boundaryHit = hit;
                return true;
            }

            boundaryHit = default;
            return false;
        }

        private static bool IsShrineRouteBoundary(string objectName)
        {
            return objectName.Contains("ShrineRouteBoundary", StringComparison.Ordinal) ||
                   objectName.Contains("ShrineApproachBlocker", StringComparison.Ordinal);
        }
    }
}
