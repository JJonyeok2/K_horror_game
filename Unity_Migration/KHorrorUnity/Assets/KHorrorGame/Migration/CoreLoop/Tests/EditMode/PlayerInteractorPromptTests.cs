using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class PlayerInteractorPromptTests
    {
        private const string PickupPrompt = "[E] \uBB3C\uAC74 \uC90D\uAE30 - Ledger";

        [Test]
        public void FullHandsStillShowsPickupPromptWithInvalidReason()
        {
            var root = new GameObject("PlayerInteractorPromptFixture");

            try
            {
                var actorObject = new GameObject("Player");
                actorObject.transform.SetParent(root.transform, false);
                actorObject.AddComponent<CharacterController>();
                var actor = actorObject.AddComponent<UnityPlayerController>();
                InvokePrivate(actor, "Awake");
                Assert.IsTrue(actor.TryCollectArtifact(new ArtifactDefinition("Ledger", 100, 1f, 1)));
                Assert.IsTrue(actor.TryCollectArtifact(new ArtifactDefinition("Mask", 100, 1f, 1)));
                Assert.AreEqual(0, actor.Inventory.FreeHandSlots());

                var cameraObject = new GameObject("PromptCamera");
                cameraObject.transform.SetParent(root.transform, false);
                cameraObject.transform.position = new Vector3(0f, 1f, 0f);
                cameraObject.transform.rotation = Quaternion.identity;
                var sourceCamera = cameraObject.AddComponent<Camera>();

                var pickupObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pickupObject.name = "LedgerPickup";
                pickupObject.transform.SetParent(root.transform, false);
                pickupObject.transform.position = new Vector3(0f, 1f, 1.5f);
                var pickup = pickupObject.AddComponent<ArtifactPickup>();
                pickup.ApplyDefinition(new ArtifactDefinition("Ledger", 230, 1.5f, 1));

                var interactorObject = new GameObject("Interactor");
                interactorObject.transform.SetParent(root.transform, false);
                var interactor = interactorObject.AddComponent<PlayerInteractor>();
                SetPrivateField(interactor, "actor", actor);
                SetPrivateField(interactor, "sourceCamera", sourceCamera);

                Physics.SyncTransforms();
                InvokePrivate(interactor, "RefreshTarget");

                Assert.AreEqual(PickupPrompt, interactor.CurrentLabel);
                Assert.AreEqual("Hands full", GetAutoProperty(interactor, "CurrentInvalidReason"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Expected private field " + fieldName + " on " + target.GetType().Name);
            field.SetValue(target, value);
        }

        private static object GetAutoProperty(object target, string propertyName)
        {
            var field = target.GetType().GetField("<" + propertyName + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Expected auto-property backing field for " + propertyName + " on " + target.GetType().Name);
            return field.GetValue(target);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Expected private method " + methodName + " on " + target.GetType().Name);
            method.Invoke(target, null);
        }
    }
}
