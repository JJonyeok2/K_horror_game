using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class InternalPlayFlowSmokeTests
    {
        private const string ScenePath = "Assets/Scenes/KHorror_Main.unity";

        [Test]
        public void MainSceneCompletesPhysicalCargoReturnAndSettlementLoop()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var player = FindSceneComponent<UnityPlayerController>();
            var gameLoop = FindSceneComponent<GameLoopController>();
            var depositZone = FindSceneComponent<VanCargoDepositZone>("ReturnBongoCargoDepositZone");
            var estateCargoHold = FindSceneComponent<VanCargoHold>("EstateReturnBongo");

            InvokePrivate(player, "Awake");
            InvokePrivate(gameLoop, "Awake");
            InvokePrivate(gameLoop, "Start");
            InvokePrivate(depositZone, "Awake");

            Assert.AreEqual(GameMapId.BongoHub, gameLoop.State.CurrentMap);
            Assert.IsTrue(gameLoop.OperateBongoTerminal(), "Hub terminal should start travel to Jongga estate.");
            Assert.AreEqual(GameMapId.BongoTravel, gameLoop.State.CurrentMap);

            gameLoop.State.CompleteBongoTravel();

            Assert.AreEqual(GameMapId.JonggaEstate, gameLoop.State.CurrentMap);
            Assert.IsTrue(player.TryCollectArtifact(new ArtifactDefinition("Smoke Ledger", 230, 1.5f, 1, new[] { "paper" }, 1)));
            Assert.IsNotNull(FindSceneObject("Held_Smoke Ledger"), "Collected cargo should appear in the first-person hands.");

            Assert.IsTrue(depositZone.ManualDeposit(player), "G cargo loading should store held cargo in the estate return bongo.");
            Assert.AreEqual(0, player.Inventory.Items.Count);
            Assert.AreEqual(1, estateCargoHold.CargoCount);
            Assert.AreEqual(230, estateCargoHold.TotalCargoValue);

            var loadedCargo = estateCargoHold.CargoItems[0];
            ((IInteractable)loadedCargo).Interact(player);

            Assert.AreEqual(0, estateCargoHold.CargoCount, "Re-pickup should remove cargo from the van hold.");
            Assert.AreEqual(1, player.Inventory.Items.Count, "Re-pickup should restore cargo to the player's hands.");
            Assert.IsNotNull(FindSceneObject("Held_Smoke Ledger"), "Re-picked cargo should restore the held view.");

            Assert.IsTrue(depositZone.ManualDeposit(player), "Cargo should be loadable again after re-pickup.");
            Assert.AreEqual(1, estateCargoHold.CargoCount);

            Assert.IsTrue(gameLoop.ReturnToBongoHub(), "Return lever should be able to start hub travel after cargo is loaded.");
            gameLoop.State.CompleteBongoTravel();

            var hubCargoHold = GetPrivateField<VanCargoHold>(gameLoop, "hubCargoHold");
            Assert.IsNotNull(hubCargoHold);
            Assert.AreEqual(GameMapId.BongoHub, gameLoop.State.CurrentMap);
            Assert.AreEqual(0, estateCargoHold.CargoCount, "Estate bongo should transfer cargo to the hub hold on return.");
            Assert.AreEqual(1, hubCargoHold.CargoCount);
            Assert.AreEqual(230, hubCargoHold.TotalCargoValue);

            Assert.IsTrue(gameLoop.OperateBongoTerminal(), "Hub terminal should immediately settle loaded cargo.");

            Assert.AreEqual(230, gameLoop.Quota.RecoveredValue);
            Assert.AreEqual(0, hubCargoHold.CargoCount);
            StringAssert.Contains("Settled", gameLoop.FeedbackMessage);
        }

        [Test]
        public void MainSceneActivatesInteriorGhostAfterShrineTheftGrace()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var player = FindSceneComponent<UnityPlayerController>();
            var gameLoop = FindSceneComponent<GameLoopController>();
            var spawner = FindSceneComponent<RuntimeThreatSpawner>();
            var atmosphereCue = FindSceneComponent<ThreatAtmosphereCue>();

            InvokePrivate(player, "Awake");
            InvokePrivate(gameLoop, "Awake");
            InvokePrivate(gameLoop, "Start");
            InvokePrivate(spawner, "Awake");

            Assert.IsTrue(gameLoop.OperateBongoTerminal());
            gameLoop.State.CompleteBongoTravel();
            Assert.AreEqual(GameMapId.JonggaEstate, gameLoop.State.CurrentMap);

            gameLoop.RegisterArtifactPicked(new ArtifactDefinition(
                "Jongga Spirit Tablet",
                900,
                1.2f,
                1,
                new[] { "shrine_item" },
                1));

            Assert.AreEqual(ThreatStageProfile.MaxStage, gameLoop.Resentment.Stage());
            Assert.IsFalse(gameLoop.ThreatGate.CanSpawnThreats, "Shrine theft should keep the short grace window.");

            gameLoop.ThreatGate.Tick(ThreatSpawnGate.ShrineThreatGraceSeconds + 0.1f);
            Assert.IsTrue(gameLoop.ThreatGate.CanSpawnThreats);

            spawner.EvaluateThreats(
                gameLoop.ThreatGate.CanSpawnThreats,
                gameLoop.Resentment.Stage(),
                gameLoop.State.CurrentMap,
                TerritoryKind.EstateInterior);

            var activeGhosts = FindSceneComponents<EnemyBrain>()
                .Where(actor => actor.gameObject.activeSelf && actor.EnemyKind == EnemyKind.Ghost)
                .ToArray();

            Assert.GreaterOrEqual(activeGhosts.Length, 1, "At least one interior ghost actor should become active after shrine grace.");

            var ghostAudio = activeGhosts[0].GetComponent<ThreatAudioOcclusion>();
            var source = activeGhosts[0].GetComponent<AudioSource>();
            var filter = activeGhosts[0].GetComponent<AudioLowPassFilter>();
            Assert.IsNotNull(ghostAudio);
            Assert.IsNotNull(source);
            Assert.IsNotNull(filter);
            ghostAudio.ManualRefresh();
            Assert.IsNotNull(source.clip, "Threat audio should have a generated cue clip in the generated scene.");

            atmosphereCue.TriggerHighThreatCue("Smoke shrine cue");
            atmosphereCue.ManualTick(0.1f, ThreatStageProfile.MaxStage, GameMapId.JonggaEstate);

            Assert.IsTrue(atmosphereCue.IsCueActive);
            Assert.GreaterOrEqual(atmosphereCue.LightCount, 3);
        }

        [Test]
        public void InternalPlayflowScreenshotCaptureWritesProofPng()
        {
            var outputPath = Path.Combine(
                Application.temporaryCachePath,
                "internal-playflow-proof-test.png");

            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            var captureType = FindLoadedType("KHorrorGame.EditorTools.KHorrorScreenshotCapture");
            Assert.IsNotNull(captureType, "Screenshot capture editor type should be loaded in EditMode.");

            Assert.IsNotNull(captureType.GetMethod(
                "CaptureInternalPlayFlowProof",
                BindingFlags.Public | BindingFlags.Static));

            var method = captureType.GetMethod(
                "CaptureInternalPlayFlowProofForTest",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "Internal playflow proof test capture method should exist.");

            method.Invoke(null, new object[] { outputPath });

            Assert.IsTrue(File.Exists(outputPath), "Internal playflow proof screenshot should be written.");
            Assert.Greater(new FileInfo(outputPath).Length, 12000, "Screenshot should contain rendered image data, not an empty placeholder.");
        }

        private static Type FindLoadedType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static T FindSceneComponent<T>(string objectName = null) where T : Component
        {
            foreach (var component in Resources.FindObjectsOfTypeAll<T>())
            {
                if (component == null || EditorUtility.IsPersistent(component))
                {
                    continue;
                }

                if (!component.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (string.IsNullOrEmpty(objectName) || component.name == objectName)
                {
                    return component;
                }
            }

            Assert.Fail(string.IsNullOrEmpty(objectName)
                ? $"Scene component {typeof(T).Name} was not found."
                : $"Scene component {typeof(T).Name} named {objectName} was not found.");
            return null;
        }

        private static T[] FindSceneComponents<T>() where T : Component
        {
            return Resources.FindObjectsOfTypeAll<T>()
                .Where(component => component != null
                    && !EditorUtility.IsPersistent(component)
                    && component.gameObject.scene.IsValid())
                .ToArray();
        }

        private static GameObject FindSceneObject(string objectName)
        {
            foreach (var transform in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (transform == null || transform.name != objectName || EditorUtility.IsPersistent(transform))
                {
                    continue;
                }

                if (transform.gameObject.scene.IsValid())
                {
                    return transform.gameObject;
                }
            }

            return null;
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, target.GetType().Name + "." + methodName + " should exist.");
            method.Invoke(target, null);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, target.GetType().Name + "." + fieldName + " should exist.");
            return (T)field.GetValue(target);
        }
    }
}
