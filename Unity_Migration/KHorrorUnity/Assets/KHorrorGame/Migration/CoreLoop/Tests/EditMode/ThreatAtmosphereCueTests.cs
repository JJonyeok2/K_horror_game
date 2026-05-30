using NUnit.Framework;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class ThreatAtmosphereCueTests
    {
        private GameObject cueObject;
        private GameObject lightObject;
        private GameObject playerObject;
        private GameObject gameLoopObject;
        private bool previousFogEnabled;
        private float previousFogDensity;

        [SetUp]
        public void SetUp()
        {
            previousFogEnabled = RenderSettings.fog;
            previousFogDensity = RenderSettings.fogDensity;
        }

        [TearDown]
        public void TearDown()
        {
            RenderSettings.fog = previousFogEnabled;
            RenderSettings.fogDensity = previousFogDensity;
            DestroyImmediate(cueObject);
            DestroyImmediate(lightObject);
            DestroyImmediate(playerObject);
            DestroyImmediate(gameLoopObject);
        }

        [Test]
        public void TriggerHighThreatCueFlickersLightsAndRaisesFog()
        {
            var light = CreateThreatLight();
            var gameLoop = CreateGameLoop();
            cueObject = new GameObject("ThreatAtmosphereCue");
            var cue = cueObject.AddComponent<ThreatAtmosphereCue>();
            cue.Configure(gameLoop, new[] { light });

            cue.TriggerHighThreatCue();
            cue.ManualTick(0.2f, ThreatStageProfile.MaxStage, GameMapId.JonggaEstate);

            Assert.IsTrue(cue.IsCueActive);
            Assert.Greater(light.intensity, 1f);
            Assert.GreaterOrEqual(RenderSettings.fogDensity, cue.HighThreatFogDensity);
            Assert.IsTrue(gameLoop.FeedbackMessage.Contains("Shrine", System.StringComparison.Ordinal));
        }

        [Test]
        public void CueRestoresLightAndFogWhenWindowExpires()
        {
            var light = CreateThreatLight();
            light.enabled = false;
            cueObject = new GameObject("ThreatAtmosphereCue");
            var cue = cueObject.AddComponent<ThreatAtmosphereCue>();
            cue.Configure(null, new[] { light });

            cue.TriggerHighThreatCue();
            cue.ManualTick(cue.CueDurationSeconds + 0.2f, ThreatStageProfile.MaxStage, GameMapId.JonggaEstate);

            Assert.IsFalse(cue.IsCueActive);
            Assert.IsFalse(light.enabled);
            Assert.AreEqual(1f, light.intensity, 0.01f);
            Assert.AreEqual(previousFogDensity, RenderSettings.fogDensity, 0.001f);
        }

        [Test]
        public void CueRestoresAtmosphereWhenDisabledMidPulse()
        {
            var light = CreateThreatLight();
            light.enabled = false;
            cueObject = new GameObject("ThreatAtmosphereCue");
            var cue = cueObject.AddComponent<ThreatAtmosphereCue>();
            cue.Configure(null, new[] { light });

            cue.TriggerHighThreatCue();
            Assert.IsTrue(light.enabled);
            Assert.GreaterOrEqual(RenderSettings.fogDensity, cue.HighThreatFogDensity);

            InvokePrivate(cue, "OnDisable");

            Assert.IsFalse(cue.IsCueActive);
            Assert.IsFalse(light.enabled);
            Assert.AreEqual(previousFogEnabled, RenderSettings.fog);
            Assert.AreEqual(previousFogDensity, RenderSettings.fogDensity, 0.001f);
        }

        private Light CreateThreatLight()
        {
            lightObject = new GameObject("ThreatCueLantern");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = 1f;
            return light;
        }

        private GameLoopController CreateGameLoop()
        {
            playerObject = new GameObject("ThreatCuePlayer");
            playerObject.AddComponent<CharacterController>();
            var player = playerObject.AddComponent<UnityPlayerController>();
            gameLoopObject = new GameObject("ThreatCueLoop");
            var gameLoop = gameLoopObject.AddComponent<GameLoopController>();
            SetPrivateObject(gameLoop, "player", player);
            InvokeAwake(gameLoop);
            return gameLoop;
        }

        private static void SetPrivateObject(Object target, string fieldName, Object value)
        {
            target.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static void InvokeAwake(object target)
        {
            InvokePrivate(target, "Awake");
        }

        private static void InvokePrivate(object target, string methodName)
        {
            target.GetType()
                .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(target, null);
        }

        private static void DestroyImmediate(Object target)
        {
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
