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
        public void EstateHasInteractivePaperDoorAndTalismanSamples()
        {
            EditorSceneManager.OpenScene(ScenePath);

            AssertInteractivePaperDoor("MainHousePaperDoor_2");
            AssertInteractivePaperDoor("SarangchaeOpenPaperDoor_A");

            var talisman = GameObject.Find("Artifact_KitchenCharm");
            Assert.IsNotNull(talisman, "A talisman pickup should exist so the paper-door seal loop can be tested in the estate.");
            var pickup = talisman.GetComponent<ArtifactPickup>();
            Assert.IsNotNull(pickup, "Artifact_KitchenCharm should remain collectible.");
            AssertSerializedStringArrayContains(pickup, "tags", PaperDoorInteraction.TalismanTag);
        }

        [Test]
        public void EstateHasRuntimeThreatSpawnerWithTerritoryActors()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var spawner = UnityEngine.Object.FindObjectOfType<RuntimeThreatSpawner>(true);
            Assert.IsNotNull(spawner, "RuntimeThreatSpawner scene object should exist.");
            InvokeAwake(spawner);
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
            var ghosts = brains
                .Where(brain => brain.name.StartsWith("RuntimeGhostActor", StringComparison.Ordinal))
                .ToArray();
            var dokkaebi = brains
                .Where(brain => brain.name.StartsWith("RuntimeDokkaebiActor", StringComparison.Ordinal))
                .ToArray();
            Assert.GreaterOrEqual(ghosts.Length, 3, "Runtime ghost pool should support stage-scaled interior threat count.");
            Assert.GreaterOrEqual(dokkaebi.Length, 2, "Runtime dokkaebi pool should support stage-scaled forest threat count.");

            foreach (var ghost in ghosts)
            {
                Assert.IsFalse(ghost.gameObject.activeSelf, ghost.name + " should start hidden until the director requests it.");
                Assert.AreEqual(EnemyKind.Ghost, ghost.EnemyKind);
                Assert.IsNotNull(
                    ghost.GetComponent<GhostEnemy>(),
                    ghost.name + " should carry the GhostEnemy state controller, not only the generic EnemyBrain.");
            }

            foreach (var forestActor in dokkaebi)
            {
                Assert.IsFalse(forestActor.gameObject.activeSelf, forestActor.name + " should start hidden until the director requests it.");
                Assert.AreEqual(EnemyKind.Dokkaebi, forestActor.EnemyKind);
                Assert.IsNotNull(
                    forestActor.GetComponent<DokkaebiEnemy>(),
                    forestActor.name + " should carry the DokkaebiEnemy forest-state controller.");
            }
        }

        [Test]
        public void EstateThreatActorsHaveAudioOcclusionAndAtmosphereCue()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var spawner = UnityEngine.Object.FindObjectOfType<RuntimeThreatSpawner>(true);
            Assert.IsNotNull(spawner, "RuntimeThreatSpawner scene object should exist.");

            var atmosphere = UnityEngine.Object.FindObjectOfType<ThreatAtmosphereCue>(true);
            Assert.IsNotNull(atmosphere, "ThreatAtmosphereCue should be wired into the estate runtime threat rig.");
            Assert.GreaterOrEqual(atmosphere.LightCount, 3, "High-threat cue should pulse the shrine and rear-route lanterns.");
            Assert.Greater(atmosphere.HighThreatFogDensity, RenderSettings.fogDensity, "High-threat cue should visibly thicken fog.");

            var brains = Resources.FindObjectsOfTypeAll<EnemyBrain>()
                .Where(brain => brain.gameObject.scene.IsValid())
                .Where(brain => brain.name.StartsWith("RuntimeGhostActor", StringComparison.Ordinal) ||
                                brain.name.StartsWith("RuntimeDokkaebiActor", StringComparison.Ordinal))
                .ToArray();
            Assert.IsNotEmpty(brains, "Runtime enemy actors should exist.");

            foreach (var brain in brains)
            {
                var audio = brain.GetComponent<ThreatAudioOcclusion>();
                Assert.IsNotNull(audio, brain.name + " needs threat audio occlusion.");

                var source = brain.GetComponent<AudioSource>();
                var filter = brain.GetComponent<AudioLowPassFilter>();
                Assert.IsNotNull(source, brain.name + " needs a spatial AudioSource.");
                Assert.IsNotNull(filter, brain.name + " needs an AudioLowPassFilter.");
                Assert.AreEqual(1f, source.spatialBlend, 0.01f, brain.name + " should be fully spatialized.");

                audio.ManualRefresh();
                Assert.IsNotNull(source.clip, brain.name + " needs a generated playable threat cue clip.");
                if (brain.EnemyKind == EnemyKind.Dokkaebi)
                {
                    Assert.AreEqual("forest_dokkaebi_presence", audio.CurrentCueLabel);
                }
                else
                {
                    Assert.AreEqual("estate_ghost_presence", audio.CurrentCueLabel);
                }
            }
        }

        [Test]
        public void EstateHasTerritoryRootsResolverAndGateAiBoundary()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            var resolver = UnityEngine.Object.FindObjectOfType<TerritoryResolver>(true);
            Assert.IsNotNull(resolver, "Generated scene should include one TerritoryResolver.");

            var estateRoot = GameObject.Find("JonggaEstate");
            Assert.IsNotNull(estateRoot, "JonggaEstate root should exist.");

            var requiredZoneRoots = new[]
            {
                "ForestApproach",
                "FrontGateBoundary",
                "Courtyard",
                "MainHouse",
                "BackRoute",
                "Shrine",
            };

            foreach (var zoneName in requiredZoneRoots)
            {
                var zoneRoot = estateRoot.transform.Find(zoneName);
                Assert.IsNotNull(zoneRoot, zoneName + " should be a direct zone root under JonggaEstate.");
            }

            AssertTerritoryVolume("ForestApproachTerritoryVolume", TerritoryKind.ForestApproach, new Vector3(0f, 1f, 36f));
            AssertTerritoryVolume("EstateInteriorTerritoryVolume", TerritoryKind.EstateInterior, new Vector3(0f, 1f, 82f));
            AssertTerritoryVolume("ShrineTerritoryVolume", TerritoryKind.EstateInterior, new Vector3(-8f, 1f, 141f));

            var aiBlocker = GameObject.Find("FrontGateAIBoundaryBlocker");
            Assert.IsNotNull(aiBlocker, "Front gate should have an AI boundary blocker so enemies do not use the player portal.");
            var blockerCollider = aiBlocker.GetComponent<Collider>();
            Assert.IsNotNull(blockerCollider, "FrontGateAIBoundaryBlocker should have a collider.");
            Assert.IsTrue(blockerCollider.enabled, "FrontGateAIBoundaryBlocker collider should be enabled.");
            Assert.IsFalse(blockerCollider.isTrigger, "FrontGateAIBoundaryBlocker should physically block AI traversal by default.");
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
        public void RuntimeThreatSpawnerKeepsInteriorGhostActiveAtMaximumThreatEvenFromForest()
        {
            var root = new GameObject("MaxThreatSpawnerFixture");
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
                ghostAnchor.transform.position = new Vector3(-8f, 0f, 129f);
                dokkaebiAnchor.transform.position = new Vector3(4f, 0f, 36f);

                SetObject(spawner, "playerTarget", player.transform);
                SetObject(spawner, "ghostActor", ghostBrain);
                SetObject(spawner, "dokkaebiActor", dokkaebiBrain);
                SetObject(spawner, "ghostSpawnAnchor", ghostAnchor.transform);
                SetObject(spawner, "dokkaebiSpawnAnchor", dokkaebiAnchor.transform);
                SetObject(spawner, "spawnCueLight", cue);

                var decision = spawner.EvaluateThreats(true, 5, GameMapId.JonggaEstate, TerritoryKind.ForestApproach);

                Assert.AreEqual(ThreatDirectorAction.SpawnDokkaebi, decision.Action);
                Assert.IsTrue(dokkaebi.activeSelf, "Forest pressure should still create the outside dokkaebi.");
                Assert.IsTrue(ghost.activeSelf, "Maximum shrine threat must also keep an interior ghost alive.");
                Assert.AreEqual(ghostAnchor.transform.position, ghost.transform.position);
                Assert.AreEqual(TerritoryKind.EstateInterior, ghostBrain.HomeTerritory);
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
        public void RuntimeThreatSpawnerConfiguresGhostControllerWhenActivatingActor()
        {
            var root = new GameObject("GhostControllerSpawnerFixture");
            var player = new GameObject("PlayerFixture");
            var ghost = new GameObject("GhostActorFixture");
            var ghostAnchor = new GameObject("GhostAnchorFixture");
            var spawner = root.AddComponent<RuntimeThreatSpawner>();

            try
            {
                player.transform.position = new Vector3(1.2f, 0f, 88f);
                var ghostBrain = ghost.AddComponent<EnemyBrain>();
                var ghostController = ghost.AddComponent<GhostEnemy>();
                ghost.SetActive(false);
                ghostAnchor.transform.position = new Vector3(0f, 0f, 88f);

                SetObject(spawner, "playerTarget", player.transform);
                SetObjectArray(spawner, "ghostActors", ghostBrain);
                SetObjectArray(spawner, "ghostSpawnAnchors", ghostAnchor.transform);

                spawner.EvaluateThreats(true, 5, GameMapId.JonggaEstate, TerritoryKind.EstateInterior);

                Assert.IsTrue(ghost.activeSelf, "Stage-five estate threat should activate the ghost actor.");
                Assert.IsFalse(ghostController.AutomaticTickEnabled, "RuntimeThreatSpawner should own ticking for pooled ghost actors.");

                ghostController.ManualTick(0.1f, TerritoryKind.EstateInterior);

                Assert.AreEqual(GhostEnemyState.Chase, ghostController.State, "Activation should configure GhostEnemy with the current player target.");
                Assert.AreEqual(EnemyControllerState.Tracking, ghostController.ControllerState);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(ghost);
                UnityEngine.Object.DestroyImmediate(ghostAnchor);
            }
        }

        [Test]
        public void RuntimeThreatSpawnerConfiguresDokkaebiControllerWhenActivatingActor()
        {
            var root = new GameObject("DokkaebiControllerSpawnerFixture");
            var player = new GameObject("PlayerFixture");
            var dokkaebi = new GameObject("DokkaebiActorFixture");
            var dokkaebiAnchor = new GameObject("DokkaebiAnchorFixture");
            var spawner = root.AddComponent<RuntimeThreatSpawner>();

            try
            {
                player.transform.position = new Vector3(3.2f, 0f, 36f);
                var dokkaebiBrain = dokkaebi.AddComponent<EnemyBrain>();
                var dokkaebiController = dokkaebi.AddComponent<DokkaebiEnemy>();
                dokkaebi.SetActive(false);
                dokkaebiAnchor.transform.position = new Vector3(0f, 0f, 36f);

                SetObject(spawner, "playerTarget", player.transform);
                SetObjectArray(spawner, "dokkaebiActors", dokkaebiBrain);
                SetObjectArray(spawner, "dokkaebiSpawnAnchors", dokkaebiAnchor.transform);

                spawner.EvaluateThreats(true, 3, GameMapId.JonggaEstate, TerritoryKind.ForestApproach);

                Assert.IsTrue(dokkaebi.activeSelf, "Stage-three forest threat should activate the dokkaebi actor.");
                Assert.IsFalse(dokkaebiController.AutomaticTickEnabled, "RuntimeThreatSpawner should own ticking for pooled dokkaebi actors.");

                dokkaebiController.ManualTick(0.1f, TerritoryKind.ForestApproach);

                Assert.AreEqual(DokkaebiEnemyState.BlockPath, dokkaebiController.State, "Activation should configure DokkaebiEnemy with the current player target.");
                Assert.AreEqual(EnemyControllerState.Tracking, dokkaebiController.ControllerState);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(dokkaebi);
                UnityEngine.Object.DestroyImmediate(dokkaebiAnchor);
            }
        }

        [Test]
        public void RuntimeThreatSpawnerUpdateResolvesForestTerritoryAndTicksDokkaebiController()
        {
            var gameLoopObject = new GameObject("DokkaebiUpdateGameLoopFixture");
            var spawnerObject = new GameObject("DokkaebiUpdateSpawnerFixture");
            var player = new GameObject("PlayerFixture");
            var dokkaebi = new GameObject("DokkaebiActorFixture");
            var dokkaebiAnchor = new GameObject("DokkaebiAnchorFixture");

            try
            {
                var gameLoop = gameLoopObject.AddComponent<GameLoopController>();
                InvokeAwake(gameLoop);
                Assert.IsTrue(gameLoop.State.TravelToRetrievalMap(GameMapId.JonggaEstate));
                gameLoop.State.CompleteBongoTravel();
                gameLoop.Resentment.AddResentment(ResentmentTracker.MinimumValueForStage(3), "forest pressure test");

                player.transform.position = new Vector3(3.2f, 0f, 36f);
                var dokkaebiBrain = dokkaebi.AddComponent<EnemyBrain>();
                dokkaebi.SetActive(false);
                dokkaebiAnchor.transform.position = new Vector3(0f, 0f, 36f);

                var spawner = spawnerObject.AddComponent<RuntimeThreatSpawner>();
                SetObject(spawner, "gameLoop", gameLoop);
                SetObject(spawner, "playerTarget", player.transform);
                SetObjectArray(spawner, "dokkaebiActors", dokkaebiBrain);
                SetObjectArray(spawner, "dokkaebiSpawnAnchors", dokkaebiAnchor.transform);
                InvokeAwake(spawner);

                InvokeUpdate(spawner);

                var controller = dokkaebi.GetComponent<DokkaebiEnemy>();
                Assert.IsTrue(dokkaebi.activeSelf, "Spawner.Update should activate the forest dokkaebi when the player is outside the gate.");
                Assert.IsNotNull(controller, "Spawner.Update should tick the dokkaebi-specific controller path.");
                Assert.IsFalse(controller.AutomaticTickEnabled, "Spawner.Update should remain the only ticking owner.");
                Assert.AreEqual(DokkaebiEnemyState.BlockPath, controller.State);
                Assert.AreEqual(EnemyControllerState.Tracking, controller.ControllerState);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameLoopObject);
                UnityEngine.Object.DestroyImmediate(spawnerObject);
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(dokkaebi);
                UnityEngine.Object.DestroyImmediate(dokkaebiAnchor);
            }
        }

        [Test]
        public void RuntimeThreatSpawnerUsesStageBudgetToActivateMultipleInteriorGhosts()
        {
            var root = new GameObject("MultiGhostSpawnerFixture");
            var player = new GameObject("PlayerFixture");
            var ghostA = new GameObject("GhostActorFixture_A");
            var ghostB = new GameObject("GhostActorFixture_B");
            var dokkaebi = new GameObject("DokkaebiActorFixture");
            var ghostAnchorA = new GameObject("GhostAnchorFixture_A");
            var ghostAnchorB = new GameObject("GhostAnchorFixture_B");
            var dokkaebiAnchor = new GameObject("DokkaebiAnchorFixture");
            var cue = new GameObject("CueFixture").AddComponent<Light>();
            var spawner = root.AddComponent<RuntimeThreatSpawner>();

            try
            {
                player.AddComponent<PlayerDamageReceiver>();
                var ghostBrainA = ghostA.AddComponent<EnemyBrain>();
                var ghostBrainB = ghostB.AddComponent<EnemyBrain>();
                var dokkaebiBrain = dokkaebi.AddComponent<EnemyBrain>();
                ghostA.SetActive(false);
                ghostB.SetActive(false);
                dokkaebi.SetActive(false);
                ghostAnchorA.transform.position = new Vector3(-8f, 0f, 128f);
                ghostAnchorB.transform.position = new Vector3(-2f, 0f, 118f);
                dokkaebiAnchor.transform.position = new Vector3(4f, 0f, 36f);

                SetObject(spawner, "playerTarget", player.transform);
                SetObjectArray(spawner, "ghostActors", ghostBrainA, ghostBrainB);
                SetObjectArray(spawner, "dokkaebiActors", dokkaebiBrain);
                SetObjectArray(spawner, "ghostSpawnAnchors", ghostAnchorA.transform, ghostAnchorB.transform);
                SetObjectArray(spawner, "dokkaebiSpawnAnchors", dokkaebiAnchor.transform);
                SetObject(spawner, "spawnCueLight", cue);

                var firstDecision = spawner.EvaluateThreats(true, 5, GameMapId.JonggaEstate, TerritoryKind.EstateInterior);
                var secondDecision = spawner.EvaluateThreats(true, 5, GameMapId.JonggaEstate, TerritoryKind.EstateInterior);

                Assert.AreEqual(ThreatDirectorAction.SpawnGhost, firstDecision.Action);
                Assert.AreEqual(ThreatDirectorAction.SpawnGhost, secondDecision.Action);
                Assert.IsTrue(ghostA.activeSelf, "First stage-five interior evaluation should activate a ghost.");
                Assert.IsTrue(ghostB.activeSelf, "Second stage-five interior evaluation should activate another ghost while budget remains.");
                Assert.AreEqual(ghostAnchorA.transform.position, ghostA.transform.position);
                Assert.AreEqual(ghostAnchorB.transform.position, ghostB.transform.position);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(ghostA);
                UnityEngine.Object.DestroyImmediate(ghostB);
                UnityEngine.Object.DestroyImmediate(dokkaebi);
                UnityEngine.Object.DestroyImmediate(ghostAnchorA);
                UnityEngine.Object.DestroyImmediate(ghostAnchorB);
                UnityEngine.Object.DestroyImmediate(dokkaebiAnchor);
                UnityEngine.Object.DestroyImmediate(cue.gameObject);
            }
        }

        [Test]
        public void InteriorGhostSpawnSupportsDeepShrineEncounter()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var ghostAnchor = GameObject.Find("GhostSpawnAnchor_AnchaeInterior");
            var shrineFloor = GameObject.Find("ShrineFloor");

            Assert.IsNotNull(ghostAnchor, "Ghost spawn anchor should exist.");
            Assert.IsNotNull(shrineFloor, "ShrineFloor should exist.");
            Assert.GreaterOrEqual(ghostAnchor.transform.position.z, 118f, "Interior ghost should be able to contest the rear shrine objective.");
            Assert.LessOrEqual(
                Vector3.Distance(ghostAnchor.transform.position, shrineFloor.transform.position),
                EnemyStats.FromProfile(EnemyKind.Ghost, ThreatStageProfile.ForStage(5)).DetectionRange,
                "Stage five shrine theft should place a ghost within detection range of the deep objective.");
        }

        [Test]
        public void MaximumThreatPrioritizesShrineThresholdGhostAnchor()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var spawner = UnityEngine.Object.FindObjectOfType<RuntimeThreatSpawner>(true);
            var shrineFloor = GameObject.Find("ShrineFloor");

            Assert.IsNotNull(spawner, "RuntimeThreatSpawner scene object should exist.");
            Assert.IsNotNull(shrineFloor, "ShrineFloor should exist.");

            var serialized = new SerializedObject(spawner);
            var anchors = serialized.FindProperty("ghostSpawnAnchors");

            Assert.IsNotNull(anchors, "RuntimeThreatSpawner should expose ghostSpawnAnchors.");
            Assert.GreaterOrEqual(anchors.arraySize, 1, "RuntimeThreatSpawner should have at least one ghost anchor.");

            var firstAnchor = anchors.GetArrayElementAtIndex(0).objectReferenceValue as Transform;

            Assert.IsNotNull(firstAnchor, "First ghost anchor should be assigned.");
            Assert.AreEqual(
                "GhostSpawnAnchor_ShrineThreshold",
                firstAnchor.name,
                "The first stage-five ghost should appear from the shrine threshold, not from a distant hidden interior point.");
            Assert.LessOrEqual(
                Vector3.Distance(firstAnchor.position, shrineFloor.transform.position),
                8f,
                "Shrine theft retaliation should be visible near the stolen objective.");
        }

        [Test]
        public void RuntimeThreatSpawnerGraceCueDoesNotActivateActors()
        {
            var root = new GameObject("GraceSpawnerFixture");
            var player = new GameObject("PlayerFixture");
            var ghost = new GameObject("GhostActorFixture");
            var dokkaebi = new GameObject("DokkaebiActorFixture");
            var cue = new GameObject("CueFixture").AddComponent<Light>();
            var spawner = root.AddComponent<RuntimeThreatSpawner>();

            try
            {
                var ghostBrain = ghost.AddComponent<EnemyBrain>();
                var dokkaebiBrain = dokkaebi.AddComponent<EnemyBrain>();
                ghost.SetActive(false);
                dokkaebi.SetActive(false);

                SetObject(spawner, "playerTarget", player.transform);
                SetObject(spawner, "ghostActor", ghostBrain);
                SetObject(spawner, "dokkaebiActor", dokkaebiBrain);
                SetObject(spawner, "spawnCueLight", cue);

                var decision = spawner.EvaluateThreats(false, 5, GameMapId.JonggaEstate, TerritoryKind.EstateInterior);

                Assert.AreEqual(ThreatDirectorAction.CueOnly, decision.Action);
                Assert.IsFalse(ghost.activeSelf);
                Assert.IsFalse(dokkaebi.activeSelf);
                Assert.IsTrue(cue.enabled);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(ghost);
                UnityEngine.Object.DestroyImmediate(dokkaebi);
                UnityEngine.Object.DestroyImmediate(cue.gameObject);
            }
        }

        [Test]
        public void SettlementOfficeIsLargeEnoughToTraverseAndHasThreatEncounter()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var floor = GameObject.Find("SettlementFloor");
            var threatZone = GameObject.Find("SettlementCollectorThreatZone");
            var returnTerminal = GameObject.Find("SettlementReturnTablet");
            var threatType = Type.GetType("KHorrorGame.Migration.SettlementThreatZone, KHorrorGame.Migration");

            Assert.IsNotNull(floor, "SettlementFloor should exist.");
            Assert.GreaterOrEqual(floor.transform.localScale.x, 14f, "Settlement office needs more width than the old proxy room.");
            Assert.GreaterOrEqual(floor.transform.localScale.z, 14f, "Settlement office needs enough depth for a lethal-company-style stop.");
            Assert.IsNotNull(returnTerminal, "Settlement office needs a bongo terminal so the player can return after settling.");
            Assert.IsNotNull(returnTerminal.GetComponent<BongoTerminal>(), "SettlementReturnTablet should operate the bongo loop.");
            Assert.IsNotNull(threatType, "SettlementThreatZone runtime component should exist.");
            Assert.IsNotNull(threatZone, "Settlement office should contain a visible threat trigger.");
            Assert.IsNotNull(threatZone.GetComponent(threatType), "SettlementCollectorThreatZone should use SettlementThreatZone.");

            var trigger = threatZone.GetComponent<Collider>();
            Assert.IsNotNull(trigger, "SettlementCollectorThreatZone should have a collider.");
            Assert.IsTrue(trigger.isTrigger, "Settlement threat must be trigger-based so it can hurt without blocking the room.");
        }

        [Test]
        public void SettlementThreatZoneCanDamageThePlayer()
        {
            var threatType = Type.GetType("KHorrorGame.Migration.SettlementThreatZone, KHorrorGame.Migration");
            Assert.IsNotNull(threatType, "SettlementThreatZone runtime component should exist.");

            var zoneObject = new GameObject("SettlementThreatZoneFixture");
            var playerObject = new GameObject("SettlementPlayerFixture");

            try
            {
                var zone = zoneObject.AddComponent(threatType);
                var player = playerObject.AddComponent<UnityPlayerController>();
                var health = playerObject.AddComponent<PlayerDamageReceiver>();
                health.ResetHealth(100);

                var manualTick = threatType.GetMethod("ManualTick");
                Assert.IsNotNull(manualTick, "SettlementThreatZone should expose ManualTick for deterministic tests.");

                var damaged = (bool)manualTick.Invoke(zone, new object[] { player, 0.1f });

                Assert.IsTrue(damaged, "Settlement threat should damage immediately when the player enters it.");
                Assert.Less(health.CurrentHealth, 100);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(zoneObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void ReturnBongoHasGCargoDepositZone()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var depositType = Type.GetType("KHorrorGame.Migration.VanCargoDepositZone, KHorrorGame.Migration");
            var depositZone = GameObject.Find("ReturnBongoCargoDepositZone");

            Assert.IsNotNull(depositType, "VanCargoDepositZone runtime component should exist.");
            Assert.IsNotNull(depositZone, "Return bongo needs a cargo zone for loading items with G before returning.");
            Assert.IsNotNull(depositZone.GetComponent(depositType), "ReturnBongoCargoDepositZone should use VanCargoDepositZone.");

            var returnBongo = GameObject.Find("EstateReturnBongo");
            Assert.IsNotNull(returnBongo, "Estate return bongo should exist.");
            Assert.IsNotNull(returnBongo.GetComponent<VanCargoHold>(), "Estate return bongo should own a physical cargo hold.");
            Assert.IsNull(returnBongo.GetComponent<BongoReturnTerminal>(), "The whole bongo body should not be an interaction target.");
            Assert.IsNotNull(GameObject.Find("ReturnBongoReturnLever"), "Return action should live on a small terminal inside the open van.");
            Assert.IsNotNull(GameObject.Find("ReturnBongoReturnLever").GetComponent<BongoReturnTerminal>(), "Return lever should own the return interaction.");

            var leftDoor = GameObject.Find("ReturnBongoRearDoorLeft");
            var rightDoor = GameObject.Find("ReturnBongoRearDoorRight");
            Assert.IsNotNull(leftDoor, "Open rear door visual should exist.");
            Assert.IsNotNull(rightDoor, "Open rear door visual should exist.");
            Assert.IsFalse(leftDoor.GetComponent<Collider>().enabled, "Open rear doors should not block the cargo bay.");
            Assert.IsFalse(rightDoor.GetComponent<Collider>().enabled, "Open rear doors should not block the cargo bay.");

            var trigger = depositZone.GetComponent<Collider>();
            Assert.IsNotNull(trigger, "ReturnBongoCargoDepositZone should have a collider.");
            Assert.IsTrue(trigger.isTrigger, "The cargo deposit zone should detect entry without blocking the van.");
        }

        [Test]
        public void BongoHubHasCargoHoldForImmediateSettlement()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var gameLoop = UnityEngine.Object.FindObjectOfType<GameLoopController>(true);
            var bongoHub = GameObject.Find("BongoHub");
            var estateBongo = GameObject.Find("EstateReturnBongo");

            Assert.IsNotNull(gameLoop, "GameLoopController should exist.");
            Assert.IsNotNull(bongoHub, "BongoHub root should exist.");
            Assert.IsNotNull(estateBongo, "Estate return bongo should exist.");
            Assert.IsNotNull(estateBongo.GetComponent<VanCargoHold>(), "Estate return bongo should keep its loading hold.");

            InvokeAwake(gameLoop);

            var serialized = new SerializedObject(gameLoop);
            var hubHold = serialized.FindProperty("hubCargoHold").objectReferenceValue as VanCargoHold;
            var estateHold = serialized.FindProperty("estateCargoHold").objectReferenceValue as VanCargoHold;

            Assert.IsNotNull(hubHold, "GameLoopController should resolve or create a hub cargo hold at runtime.");
            Assert.AreEqual("BongoHubCargoHold", hubHold.name, "Runtime-created hub cargo hold should use the expected name.");
            Assert.IsTrue(hubHold.transform.IsChildOf(bongoHub.transform), "Hub cargo hold should live under the BongoHub root.");
            Assert.AreSame(
                hubHold,
                serialized.FindProperty("hubCargoHold").objectReferenceValue,
                "GameLoopController should settle from the hub cargo hold.");
            Assert.AreSame(
                estateBongo.GetComponent<VanCargoHold>(),
                estateHold,
                "GameLoopController should transfer estate cargo into the hub hold on return.");
        }

        [Test]
        public void VanCargoDepositZoneManualDepositLoadsCargoWithoutLeavingEstate()
        {
            var depositType = Type.GetType("KHorrorGame.Migration.VanCargoDepositZone, KHorrorGame.Migration");
            Assert.IsNotNull(depositType, "VanCargoDepositZone runtime component should exist.");

            var playerObject = new GameObject("DepositPlayerFixture");
            var controllerObject = new GameObject("DepositGameLoopFixture");
            var zoneObject = new GameObject("DepositZoneFixture");
            var cargoHoldObject = new GameObject("DepositCargoHoldFixture");

            try
            {
                var player = playerObject.AddComponent<UnityPlayerController>();
                var gameLoop = controllerObject.AddComponent<GameLoopController>();
                InvokeAwake(gameLoop);
                Assert.IsTrue(gameLoop.State.TravelToRetrievalMap(GameMapId.JonggaEstate));
                gameLoop.State.CompleteBongoTravel();
                Assert.AreEqual(GameMapId.JonggaEstate, gameLoop.State.CurrentMap);

                var artifact = new ArtifactDefinition("Brass bowl", 260, 2.1f, 1, null, 2);
                Assert.IsTrue(player.TryCollectArtifact(artifact));

                var cargoHold = cargoHoldObject.AddComponent<VanCargoHold>();
                var slot = new GameObject("DepositCargoSlot").transform;
                slot.SetParent(cargoHoldObject.transform, false);
                cargoHold.RegisterSlot(slot);

                zoneObject.AddComponent<BoxCollider>();
                var zone = zoneObject.AddComponent(depositType);
                SetObject((UnityEngine.Object)zone, "gameLoop", gameLoop);
                SetObject((UnityEngine.Object)zone, "cargoHold", cargoHold);

                var manualDeposit = depositType.GetMethod("ManualDeposit");
                Assert.IsNotNull(manualDeposit, "VanCargoDepositZone should expose ManualDeposit for deterministic tests.");

                var deposited = (bool)manualDeposit.Invoke(zone, new object[] { player });

                Assert.IsTrue(deposited, "G cargo deposit should accept the held item.");
                Assert.AreEqual(0, player.Inventory.Items.Count, "Hands should be empty so the player can leave the van again.");
                Assert.AreEqual(0, gameLoop.State.PendingRecoveredValue, "G cargo deposit should not convert cargo to invisible pending value.");
                Assert.AreEqual(1, cargoHold.CargoCount, "Deposited cargo should stay visible in the van cargo hold.");
                Assert.AreEqual(260, cargoHold.TotalCargoValue, "Physical cargo should preserve the held artifact value.");
                Assert.AreEqual(GameMapId.JonggaEstate, gameLoop.State.CurrentMap, "Depositing with G must not return to hub.");
                Assert.IsFalse(gameLoop.State.IsTraveling, "Depositing with G should not start travel.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(controllerObject);
                UnityEngine.Object.DestroyImmediate(zoneObject);
                UnityEngine.Object.DestroyImmediate(cargoHoldObject);
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

        private static void AssertInteractivePaperDoor(string objectName)
        {
            var doorObject = GameObject.Find(objectName);
            Assert.IsNotNull(doorObject, objectName + " should exist.");

            var door = doorObject.GetComponent<PaperDoorInteraction>();
            Assert.IsNotNull(door, objectName + " should have PaperDoorInteraction.");
            Assert.IsInstanceOf<IInteractable>(door, objectName + " should be targetable through PlayerInteractor.");

            var collider = doorObject.GetComponent<Collider>();
            Assert.IsNotNull(collider, objectName + " should keep a collider for interaction and AI blocking.");
            Assert.IsTrue(collider.enabled, objectName + " collider should start enabled.");
        }

        private static void AssertSerializedStringArrayContains(UnityEngine.Object target, string propertyName, string expectedValue)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            Assert.IsNotNull(property, "Missing serialized array " + propertyName + ".");

            for (var i = 0; i < property.arraySize; i++)
            {
                if (property.GetArrayElementAtIndex(i).stringValue == expectedValue)
                {
                    return;
                }
            }

            Assert.Fail(target.name + " should contain tag " + expectedValue + ".");
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

        private static void SetObjectArray(UnityEngine.Object target, string propertyName, params UnityEngine.Object[] values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            Assert.IsNotNull(property, "Missing serialized array " + propertyName + ".");
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InvokeAwake(UnityEngine.Object target)
        {
            target.GetType()
                .GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(target, null);
        }

        private static void InvokeUpdate(UnityEngine.Object target)
        {
            target.GetType()
                .GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(target, null);
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

        private static void AssertTerritoryVolume(string volumeName, TerritoryKind expectedTerritory, Vector3 probePoint)
        {
            var volumeObject = GameObject.Find(volumeName);
            Assert.IsNotNull(volumeObject, volumeName + " should exist.");

            var volume = volumeObject.GetComponent<TerritoryVolume>();
            Assert.IsNotNull(volume, volumeName + " should have a TerritoryVolume component.");
            Assert.AreEqual(expectedTerritory, volume.Territory, volumeName + " has wrong territory.");

            var collider = volumeObject.GetComponent<Collider>();
            Assert.IsNotNull(collider, volumeName + " should have a collider.");
            Assert.IsTrue(collider.isTrigger, volumeName + " should be a trigger volume.");
            Assert.IsTrue(collider.bounds.Contains(probePoint), volumeName + " should contain probe point " + probePoint);
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
