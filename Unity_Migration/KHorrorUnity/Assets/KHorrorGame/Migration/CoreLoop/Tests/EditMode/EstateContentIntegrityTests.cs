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
        public void BestJonggaArtifactSitsAtTheDeepRearObjective()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var shrineFloor = GameObject.Find("ShrineFloor");
            Assert.IsNotNull(shrineFloor, "ShrineFloor should exist.");
            Assert.GreaterOrEqual(shrineFloor.transform.position.z, 136f, "Shrine should sit at the rear of the estate, not directly behind the main house.");

            var pickups = UnityEngine.Object.FindObjectsOfType<ArtifactPickup>()
                .Where(pickup => pickup.gameObject.scene.IsValid())
                .Select(pickup => new
                {
                    pickup.name,
                    pickup.transform.position,
                    Value = SerializedValue(pickup, "value")
                })
                .OrderByDescending(pickup => pickup.Value)
                .ToArray();

            Assert.IsNotEmpty(pickups, "Expected estate artifacts.");
            Assert.AreEqual("Artifact_JonggaSpiritTablet", pickups[0].name, "The best loot should be the deep Jongga objective.");
            Assert.GreaterOrEqual(pickups[0].position.z, 136f, "The best loot should require the full rear route.");
        }

        [Test]
        public void MainHouseSideShortcutsDoNotLeadTowardShrine()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            AssertShortcutBlocker(
                new Vector3(-10.1f, 1.2f, 87.6f),
                Vector3.forward,
                "left side of main house still leads directly toward the shrine route.");
            AssertShortcutBlocker(
                new Vector3(8.3f, 1.2f, 87.6f),
                Vector3.forward,
                "right side of main house still leads directly toward the shrine route.");
        }

        [Test]
        public void RearEstateContainsDenseRouteAnchorsBeforeShrine()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var routeAnchors = UnityEngine.Object.FindObjectsOfType<Transform>()
                .Where(transform => transform.gameObject.scene.IsValid())
                .Select(transform => transform.name)
                .Where(IsRearRouteAnchor)
                .Distinct()
                .ToArray();

            Assert.GreaterOrEqual(
                routeAnchors.Length,
                18,
                "Rear estate route is too empty before the shrine. Found: " + string.Join(", ", routeAnchors));
        }

        [Test]
        public void RearShrineRouteHasDarkHanokAtmosphereAnchors()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var requiredObjects = new[]
            {
                "RearHanokGate_First",
                "RearHanokGate_Second",
                "RearHanokGate_Third",
                "RearRouteEaves_First",
                "RearRouteEaves_Second",
                "RearRouteEaves_Third",
                "RearRouteLanternPool_First",
                "RearRouteLanternPool_Second",
                "RearRouteLanternPool_Third",
            };

            foreach (var objectName in requiredObjects)
            {
                Assert.IsNotNull(GameObject.Find(objectName), objectName + " should exist on the dark hanok shrine route.");
            }

            var sightlineBreakers = UnityEngine.Object.FindObjectsOfType<Transform>()
                .Where(transform => transform.gameObject.scene.IsValid())
                .Select(transform => transform.name)
                .Where(name => name.StartsWith("RearRouteSightlineBreak_", StringComparison.Ordinal))
                .Distinct()
                .ToArray();
            Assert.GreaterOrEqual(sightlineBreakers.Length, 3, "Shrine route needs at least three sightline-breaking turns.");

            var lanternLights = UnityEngine.Object.FindObjectsOfType<Light>()
                .Where(light => light.gameObject.scene.IsValid())
                .Where(light => light.name.StartsWith("RearRouteLanternPool_", StringComparison.Ordinal))
                .Where(light => light.transform.position.z >= 96f && light.transform.position.z <= 139f)
                .ToArray();
            Assert.GreaterOrEqual(lanternLights.Length, 3, "Rear route needs at least three readable light pools before the shrine.");
        }

        [Test]
        public void RearShrineRouteDecorativePaperDoesNotBlockPlayerCapsule()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            var blockers = UnityEngine.Object.FindObjectsOfType<Collider>()
                .Where(collider => collider.enabled && !collider.isTrigger)
                .Where(collider => collider.name.StartsWith("RearRoutePaperCharm", StringComparison.Ordinal) ||
                                   collider.name.StartsWith("RearHanokGateCharm", StringComparison.Ordinal))
                .Select(collider => collider.name)
                .ToArray();

            Assert.IsEmpty(blockers, "Rear route paper charms should be visual-only: " + string.Join(", ", blockers));
        }

        [Test]
        public void EstateHasRuntimeThreatSpawnerWithTerritoryActors()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var spawner = UnityEngine.Object.FindObjectOfType<RuntimeThreatSpawner>(true);
            Assert.IsNotNull(spawner, "RuntimeThreatSpawner scene object should exist.");
            Assert.IsNull(
                UnityEngine.Object.FindObjectOfType<ThreatProxySpawner>(true),
                "Static threat proxy spawner should be replaced by runtime AI actors.");

            var ghostAnchor = GameObject.Find("GhostSpawnAnchor_AnchaeInterior");
            var dokkaebiAnchor = GameObject.Find("DokkaebiSpawnAnchor_ForestApproach");
            Assert.IsNotNull(ghostAnchor, "GhostSpawnAnchor_AnchaeInterior should exist.");
            Assert.IsNotNull(dokkaebiAnchor, "DokkaebiSpawnAnchor_ForestApproach should exist.");
            Assert.Greater(ghostAnchor.transform.position.z, 78f, "Ghost anchor must stay inside the estate/building side.");
            Assert.Less(dokkaebiAnchor.transform.position.z, 54f, "Dokkaebi anchor must stay outside the gate in the forest approach.");

            var brains = Resources.FindObjectsOfTypeAll<EnemyBrain>()
                .Where(brain => brain.gameObject.scene.IsValid())
                .ToArray();
            var ghost = brains.SingleOrDefault(brain => brain.name == "RuntimeGhostActor");
            var dokkaebi = brains.SingleOrDefault(brain => brain.name == "RuntimeDokkaebiActor");
            Assert.IsNotNull(ghost, "RuntimeGhostActor should exist.");
            Assert.IsNotNull(dokkaebi, "RuntimeDokkaebiActor should exist.");
            Assert.IsFalse(ghost.gameObject.activeSelf, "Ghost actor should start hidden until the director requests it.");
            Assert.IsFalse(dokkaebi.gameObject.activeSelf, "Dokkaebi actor should start hidden until the director requests it.");
            Assert.AreEqual(EnemyKind.Ghost, ghost.EnemyKind);
            Assert.AreEqual(EnemyKind.Dokkaebi, dokkaebi.EnemyKind);
        }

        [Test]
        public void RuntimeThreatSpawnerActivatesDokkaebiForForestThreat()
        {
            var root = new GameObject("SpawnerFixture");
            var player = new GameObject("PlayerFixture");
            var ghost = new GameObject("GhostActorFixture");
            var dokkaebi = new GameObject("DokkaebiActorFixture");
            var ghostAnchor = new GameObject("GhostAnchorFixture");
            var dokkaebiAnchor = new GameObject("DokkaebiAnchorFixture");
            var cue = new GameObject("CueFixture").AddComponent<Light>();
            var spawner = root.AddComponent<RuntimeThreatSpawner>();

            try
            {
                player.AddComponent<PlayerDamageReceiver>();
                var ghostBrain = ghost.AddComponent<EnemyBrain>();
                var dokkaebiBrain = dokkaebi.AddComponent<EnemyBrain>();
                ghost.SetActive(false);
                dokkaebi.SetActive(false);
                ghostAnchor.transform.position = new Vector3(0f, 0f, 88f);
                dokkaebiAnchor.transform.position = new Vector3(4f, 0f, 36f);

                SetObject(spawner, "playerTarget", player.transform);
                SetObject(spawner, "ghostActor", ghostBrain);
                SetObject(spawner, "dokkaebiActor", dokkaebiBrain);
                SetObject(spawner, "ghostSpawnAnchor", ghostAnchor.transform);
                SetObject(spawner, "dokkaebiSpawnAnchor", dokkaebiAnchor.transform);
                SetObject(spawner, "spawnCueLight", cue);

                var decision = spawner.EvaluateThreats(true, 3, GameMapId.JonggaEstate, TerritoryKind.ForestApproach);

                Assert.AreEqual(ThreatDirectorAction.SpawnDokkaebi, decision.Action);
                Assert.IsFalse(ghost.activeSelf);
                Assert.IsTrue(dokkaebi.activeSelf);
                Assert.IsTrue(cue.enabled);
                Assert.AreEqual(dokkaebiAnchor.transform.position, dokkaebi.transform.position);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(ghost);
                UnityEngine.Object.DestroyImmediate(dokkaebi);
                UnityEngine.Object.DestroyImmediate(ghostAnchor);
                UnityEngine.Object.DestroyImmediate(dokkaebiAnchor);
                UnityEngine.Object.DestroyImmediate(cue.gameObject);
            }
        }

        [Test]
        public void ShrineEntranceOnlyAllowsApproachFromBackRoute()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            AssertShrineSideBoundary(
                new Vector3(-4.2f, 1.2f, 136.5f),
                Vector3.left,
                "Shrine entrance can be reached directly from the courtyard/kitchen side.");
            AssertShrineSideBoundary(
                new Vector3(-4.2f, 1.2f, 140.7f),
                Vector3.left,
                "Shrine front can be reached directly from the courtyard/kitchen side.");

            AssertShrineSideBoundary(
                new Vector3(-11.8f, 1.2f, 136.5f),
                Vector3.right,
                "Shrine entrance can be reached directly from the outer west side.");
            AssertShrineSideBoundary(
                new Vector3(-11.8f, 1.2f, 140.7f),
                Vector3.right,
                "Shrine front can be reached directly from the outer west side.");

            var intendedHits = Physics.RaycastAll(new Vector3(-8f, 1.2f, 131f), Vector3.forward, 5.4f)
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

        private static void AssertShortcutBlocker(Vector3 origin, Vector3 direction, string failureMessage)
        {
            Assert.IsTrue(
                TryFindShortcutBlocker(origin, direction, out var hit),
                failureMessage);
            Assert.LessOrEqual(hit.distance, 4f, "Main house side shortcut blocker is too far away: " + hit.collider.name);
        }

        private static int SerializedValue(UnityEngine.Object target, string propertyName)
        {
            var serialized = new SerializedObject(target);
            return serialized.FindProperty(propertyName).intValue;
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

        private static bool TryFindShortcutBlocker(Vector3 origin, Vector3 direction, out RaycastHit boundaryHit)
        {
            var hits = Physics.RaycastAll(origin, direction.normalized, 8f)
                .Where(hit => IsMainHouseShortcutBlocker(hit.collider.name))
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

        private static bool IsMainHouseShortcutBlocker(string objectName)
        {
            return objectName.Contains("MainHouseSideShortcutBlocker", StringComparison.Ordinal) ||
                   objectName.Contains("RearCompoundWall", StringComparison.Ordinal);
        }

        private static bool IsRearRouteAnchor(string objectName)
        {
            return objectName.Contains("RearCompound", StringComparison.Ordinal) ||
                   objectName.Contains("RearRoute", StringComparison.Ordinal) ||
                   objectName.Contains("RearGarden", StringComparison.Ordinal) ||
                   objectName.Contains("Jongga", StringComparison.Ordinal) ||
                   objectName.Contains("Ancestral", StringComparison.Ordinal) ||
                   objectName.Contains("DeepShrine", StringComparison.Ordinal);
        }
    }
}
