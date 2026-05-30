using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class AudioEventHookTests
    {
        private const string ScenePath = "Assets/Scenes/KHorror_Main.unity";

        [Test]
        public void AudioCueBusDefinesRequiredCueLabelsAndProceduralFallback()
        {
            var busType = RequireAudioCueBusType();
            var fixture = new GameObject("AudioCueBusFixture");

            try
            {
                var bus = (Component)fixture.AddComponent(busType);
                var gateTransition = CueValue(busType, "GateTransition");
                var terminalAccepted = CueValue(busType, "TerminalAccepted");
                var terminalDenied = CueValue(busType, "TerminalDenied");
                var cargoLoaded = CueValue(busType, "CargoLoaded");
                var resentmentStageUp = CueValue(busType, "ResentmentStageUp");
                var ghostNearby = CueValue(busType, "GhostNearby");
                var dokkaebiCue = CueValue(busType, "DokkaebiCue");
                var threatWarningCue = CueValue(busType, "ThreatWarningCue");

                CollectionAssert.AllItemsAreUnique(new[]
                {
                    gateTransition,
                    terminalAccepted,
                    terminalDenied,
                    cargoLoaded,
                    resentmentStageUp,
                    ghostNearby,
                    dokkaebiCue,
                    threatWarningCue
                });

                Assert.IsTrue(RequestCue(bus, gateTransition));
                var source = fixture.GetComponent<AudioSource>();

                Assert.IsNotNull(source, "The cue bus should own a non-spatial AudioSource.");
                Assert.AreEqual(0f, source.spatialBlend);
                Assert.IsNotNull(source.clip, "Missing final files should still produce placeholder audio clips.");
                StringAssert.Contains(gateTransition, source.clip.name);
                CollectionAssert.Contains(CueHistory(bus), gateTransition);
                Assert.IsTrue(IsAggressiveThreatCue(busType, ghostNearby));
                Assert.IsTrue(IsAggressiveThreatCue(busType, dokkaebiCue));
                Assert.IsFalse(IsAggressiveThreatCue(busType, threatWarningCue));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fixture);
            }
        }

        [Test]
        public void RuntimeHooksEmitTerminalCargoAndResentmentCues()
        {
            var busType = RequireAudioCueBusType();
            var root = new GameObject("AudioHookRuntimeFixture");

            try
            {
                var bus = CreateCueBus(root.transform, busType);
                var actorObject = new GameObject("Player");
                actorObject.transform.SetParent(root.transform, false);
                actorObject.AddComponent<CharacterController>();
                var actor = actorObject.AddComponent<UnityPlayerController>();

                var gameLoopObject = new GameObject("GameLoop");
                gameLoopObject.transform.SetParent(root.transform, false);
                var gameLoop = gameLoopObject.AddComponent<GameLoopController>();
                SetPrivateField(gameLoop, "player", actor);
                SetPrivateField(gameLoop, "audioCueBus", bus);
                InvokePrivate(gameLoop, "Awake");

                Assert.IsTrue(gameLoop.OperateBongoTerminal());
                CollectionAssert.Contains(CueHistory(bus), CueValue(busType, "TerminalAccepted"));

                Assert.IsFalse(gameLoop.OperateBongoTerminal());
                CollectionAssert.Contains(CueHistory(bus), CueValue(busType, "TerminalDenied"));

                gameLoop.State.CompleteBongoTravel();
                Assert.AreEqual(GameMapId.JonggaEstate, gameLoop.State.CurrentMap);

                var cargoHoldObject = new GameObject("CargoHold");
                cargoHoldObject.transform.SetParent(root.transform, false);
                var cargoHold = cargoHoldObject.AddComponent<VanCargoHold>();
                cargoHold.RegisterSlot(new GameObject("CargoSlot").transform);

                var zoneObject = new GameObject("VanCargoDepositZone");
                zoneObject.transform.SetParent(root.transform, false);
                zoneObject.AddComponent<BoxCollider>();
                var depositZone = zoneObject.AddComponent<VanCargoDepositZone>();
                SetPrivateField(depositZone, "gameLoop", gameLoop);
                SetPrivateField(depositZone, "cargoHold", cargoHold);
                SetPrivateField(depositZone, "audioCueBus", bus);
                InvokePrivate(depositZone, "Awake");

                Assert.IsTrue(actor.TryCollectArtifact(new ArtifactDefinition("Family ledger", 180, 0.8f, 1)));
                Assert.IsTrue(depositZone.ManualDeposit(actor));
                CollectionAssert.Contains(CueHistory(bus), CueValue(busType, "CargoLoaded"));

                var stageBefore = gameLoop.Resentment.Stage();
                gameLoop.RegisterArtifactPicked(new ArtifactDefinition("Angry keepsake", 140, 0.4f, 1));

                Assert.Greater(gameLoop.Resentment.Stage(), stageBefore);
                CollectionAssert.Contains(CueHistory(bus), CueValue(busType, "ResentmentStageUp"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RuntimeThreatSpawnerUsesNonAggressiveCueWhenSpawnIsBlocked()
        {
            var busType = RequireAudioCueBusType();
            var root = new GameObject("ThreatCueFixture");

            try
            {
                var bus = CreateCueBus(root.transform, busType);
                var target = new GameObject("PlayerTarget").transform;
                target.SetParent(root.transform, false);
                target.position = new Vector3(0f, 1f, 88f);

                var ghostAnchor = CreateMarker(root.transform, "GhostAnchor", new Vector3(0f, 1f, 90f));
                var dokkaebiAnchor = CreateMarker(root.transform, "DokkaebiAnchor", new Vector3(2f, 1f, 35f));
                var ghost = CreateEnemy(root.transform, "GhostActor");
                var dokkaebi = CreateEnemy(root.transform, "DokkaebiActor");
                var spawnerObject = new GameObject("RuntimeThreatSpawner");
                spawnerObject.transform.SetParent(root.transform, false);
                var spawner = spawnerObject.AddComponent<RuntimeThreatSpawner>();
                SetPrivateField(spawner, "playerTarget", target);
                SetPrivateField(spawner, "audioCueBus", bus);
                SetPrivateField(spawner, "ghostActor", ghost);
                SetPrivateField(spawner, "dokkaebiActor", dokkaebi);
                SetPrivateField(spawner, "ghostSpawnAnchor", ghostAnchor);
                SetPrivateField(spawner, "dokkaebiSpawnAnchor", dokkaebiAnchor);
                InvokePrivate(spawner, "Awake");

                spawner.EvaluateThreats(false, ThreatStageProfile.MaxStage, GameMapId.JonggaEstate, TerritoryKind.EstateInterior);
                var blockedHistory = CueHistory(bus);
                CollectionAssert.Contains(blockedHistory, CueValue(busType, "ThreatWarningCue"));
                CollectionAssert.DoesNotContain(blockedHistory, CueValue(busType, "GhostNearby"));
                CollectionAssert.DoesNotContain(blockedHistory, CueValue(busType, "DokkaebiCue"));

                spawner.EvaluateThreats(true, 4, GameMapId.JonggaEstate, TerritoryKind.EstateInterior);
                CollectionAssert.Contains(CueHistory(bus), CueValue(busType, "GhostNearby"));

                spawner.EvaluateThreats(true, 3, GameMapId.JonggaEstate, TerritoryKind.ForestApproach);
                CollectionAssert.Contains(CueHistory(bus), CueValue(busType, "DokkaebiCue"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GeneratedSceneWiresAudioCueBusToGameplayHookComponents()
        {
            var busType = RequireAudioCueBusType();
            EditorSceneManager.OpenScene(ScenePath);

            var buses = Resources.FindObjectsOfTypeAll(busType)
                .OfType<Component>()
                .Where(component => component.gameObject.scene.path == ScenePath)
                .ToArray();

            Assert.AreEqual(1, buses.Length, "Generated scene should have one central audio cue bus.");

            var gameLoop = UnityEngine.Object.FindObjectOfType<GameLoopController>();
            AssertSerializedReference(gameLoop, "audioCueBus", buses[0]);
            AssertSerializedReference(GameObject.Find("OuterGateTraversalPortal").GetComponent<EstateGatePortal>(), "audioCueBus", buses[0]);
            AssertSerializedReference(GameObject.Find("ReturnBongoCargoDepositZone").GetComponent<VanCargoDepositZone>(), "audioCueBus", buses[0]);
            AssertSerializedReference(GameObject.Find("RuntimeThreatSpawner").GetComponent<RuntimeThreatSpawner>(), "audioCueBus", buses[0]);
        }

        private static Component CreateCueBus(Transform parent, Type busType)
        {
            var busObject = new GameObject("AudioCueBus");
            busObject.transform.SetParent(parent, false);
            return (Component)busObject.AddComponent(busType);
        }

        private static Transform CreateMarker(Transform parent, string name, Vector3 position)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            marker.position = position;
            return marker;
        }

        private static EnemyBrain CreateEnemy(Transform parent, string name)
        {
            var enemyObject = new GameObject(name);
            enemyObject.transform.SetParent(parent, false);
            return enemyObject.AddComponent<EnemyBrain>();
        }

        private static Type RequireAudioCueBusType()
        {
            var type = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType("KHorrorGame.Migration.KoreanHorrorAudioCueBus"))
                .FirstOrDefault(candidate => candidate != null);

            Assert.IsNotNull(type, "Task 18 requires a central KoreanHorrorAudioCueBus runtime component.");
            return type;
        }

        private static string CueValue(Type busType, string fieldName)
        {
            var field = busType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(field, "Missing cue label field: " + fieldName);
            return (string)field.GetValue(null);
        }

        private static bool RequestCue(Component bus, string cueKey)
        {
            return (bool)bus.GetType()
                .GetMethod("RequestCue", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(bus, new object[] { cueKey });
        }

        private static IReadOnlyList<string> CueHistory(Component bus)
        {
            var value = bus.GetType()
                .GetProperty("CueHistory", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(bus);
            return ((System.Collections.IEnumerable)value).Cast<string>().ToArray();
        }

        private static bool IsAggressiveThreatCue(Type busType, string cueKey)
        {
            return (bool)busType.GetMethod("IsAggressiveThreatCue", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { cueKey });
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, target.GetType().Name + " is missing serialized field " + fieldName);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, null);
        }

        private static void AssertSerializedReference(UnityEngine.Object target, string propertyName, UnityEngine.Object expected)
        {
            Assert.IsNotNull(target, "Missing scene object for " + propertyName);
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            Assert.IsNotNull(property, target.GetType().Name + " is missing serialized property " + propertyName);
            Assert.AreSame(expected, property.objectReferenceValue);
        }
    }
}
