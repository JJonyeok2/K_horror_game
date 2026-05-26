using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class GameLoopControllerTests
    {
        [Test]
        public void ShrineArtifactPickupForcesMaximumThreatStageAndStartsGrace()
        {
            var fixture = new GameObject("GameLoopControllerFixture");

            try
            {
                var controller = fixture.AddComponent<GameLoopController>();
                typeof(GameLoopController)
                    .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(controller, null);

                var shrineItem = new ArtifactDefinition(
                    "Shrine bell",
                    420,
                    0.7f,
                    1,
                    new[] { "shrine_item" },
                    1);

                controller.RegisterArtifactPicked(shrineItem);

                Assert.AreEqual(ThreatStageProfile.MaxStage, controller.Resentment.Stage());
                Assert.IsFalse(controller.ThreatGate.CanSpawnThreats);
                Assert.AreEqual(ThreatSpawnGate.ShrineThreatGraceSeconds, controller.ThreatGate.RemainingGraceSeconds);
            }
            finally
            {
                Object.DestroyImmediate(fixture);
            }
        }

        [Test]
        public void ShrineArtifactGraceWindowIsShortEnoughForVisibleRetaliation()
        {
            Assert.LessOrEqual(
                ThreatSpawnGate.ShrineThreatGraceSeconds,
                2.5f,
                "Stealing a shrine item should make a stage-five threat visible almost immediately.");
        }

        [Test]
        public void BongoHubTerminalSettlesLoadedVanCargoImmediately()
        {
            var fixture = new GameObject("GameLoopControllerFixture");
            var holdObject = new GameObject("HubCargoHoldFixture");

            try
            {
                var controller = fixture.AddComponent<GameLoopController>();
                var hold = holdObject.AddComponent<VanCargoHold>();
                Assert.IsTrue(hold.TryStore(new ArtifactDefinition("Brass Bowl", 400, 2f, 2), out _));

                typeof(GameLoopController)
                    .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(controller, null);
                SetPrivateField(controller, "hubCargoHold", hold);

                Assert.IsTrue(controller.OperateBongoTerminal());

                Assert.AreEqual(GameMapId.BongoHub, controller.State.CurrentMap);
                Assert.AreEqual(400, controller.Quota.RecoveredValue);
                Assert.AreEqual(0, hold.CargoCount);
                StringAssert.Contains("+400", controller.FeedbackMessage);
            }
            finally
            {
                Object.DestroyImmediate(fixture);
                Object.DestroyImmediate(holdObject);
            }
        }

        [Test]
        public void BongoMonitorTextIncludesPhysicalCargoValueAndQuota()
        {
            var fixture = new GameObject("GameLoopControllerFixture");
            var holdObject = new GameObject("HubCargoHoldFixture");

            try
            {
                var controller = fixture.AddComponent<GameLoopController>();
                var hold = holdObject.AddComponent<VanCargoHold>();
                Assert.IsTrue(hold.TryStore(new ArtifactDefinition("Ledger", 230, 1.5f, 1), out _));

                typeof(GameLoopController)
                    .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(controller, null);
                SetPrivateField(controller, "hubCargoHold", hold);

                Assert.AreEqual("[E]\nSettle Cargo", controller.TerminalScreenText());
                Assert.AreEqual("Settle loaded cargo", controller.TerminalActionText());
                StringAssert.Contains("Loaded cargo: 230", controller.MonitorBodyText());
                StringAssert.Contains("Cargo count: 1", controller.MonitorBodyText());
                StringAssert.Contains("Quota: 0 / 800", controller.MonitorBodyText());
            }
            finally
            {
                Object.DestroyImmediate(fixture);
                Object.DestroyImmediate(holdObject);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }
    }
}
