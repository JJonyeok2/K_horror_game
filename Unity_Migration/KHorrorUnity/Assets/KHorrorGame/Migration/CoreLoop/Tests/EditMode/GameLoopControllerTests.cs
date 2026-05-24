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
    }
}
