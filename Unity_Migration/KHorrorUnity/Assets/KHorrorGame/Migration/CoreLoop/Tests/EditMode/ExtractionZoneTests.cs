using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class ExtractionZoneTests
    {
        [Test]
        public void LegacyExtractionZoneDoesNotConvertHeldCargoToPendingValue()
        {
            var root = new GameObject("LegacyExtractionZoneFixture");

            try
            {
                var actorObject = new GameObject("Player");
                actorObject.transform.SetParent(root.transform, false);
                actorObject.AddComponent<CharacterController>();
                var actor = actorObject.AddComponent<UnityPlayerController>();

                var gameLoopObject = new GameObject("GameLoop");
                gameLoopObject.transform.SetParent(root.transform, false);
                var gameLoop = gameLoopObject.AddComponent<GameLoopController>();
                SetPrivateField(gameLoop, "player", actor);
                InvokePrivate(gameLoop, "Awake");
                Assert.IsTrue(gameLoop.State.OperateBongoTerminal());
                gameLoop.State.CompleteBongoTravel();
                Assert.AreEqual(GameMapId.JonggaEstate, gameLoop.State.CurrentMap);

                Assert.IsTrue(actor.TryCollectArtifact(new ArtifactDefinition("Ledger", 230, 1.5f, 1)));

                var zoneObject = new GameObject("LegacyExtractionZone");
                zoneObject.transform.SetParent(root.transform, false);
                zoneObject.AddComponent<BoxCollider>();
                var zone = zoneObject.AddComponent<ExtractionZone>();
                SetPrivateField(zone, "gameLoop", gameLoop);
                InvokePrivate(zone, "Awake");

                Assert.IsTrue(zone.CanInteract(actor));

                zone.Interact(actor);

                Assert.AreEqual(1, actor.Inventory.Items.Count, "Legacy extraction must not clear held cargo.");
                Assert.AreEqual(0, gameLoop.State.PendingRecoveredValue, "Legacy extraction must not create invisible pending value.");
                StringAssert.Contains("G", gameLoop.FeedbackMessage);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        private static void InvokePrivate(object target, string methodName)
        {
            target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, null);
        }
    }
}
