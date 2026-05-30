using NUnit.Framework;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class VanCargoHoldTests
    {
        [Test]
        public void StoreCargoCreatesVisibleCargoItemAndTracksValue()
        {
            var fixture = new CargoHoldFixture();

            try
            {
                var ledger = new ArtifactDefinition("Ledger", 230, 1.5f, 1);

                Assert.IsTrue(fixture.Hold.TryStore(ledger, out var cargoItem));

                Assert.IsNotNull(cargoItem);
                Assert.AreSame(ledger, cargoItem.Definition);
                Assert.AreSame(fixture.Hold, cargoItem.CargoHold);
                Assert.AreEqual(1, fixture.Hold.CargoCount);
                Assert.AreEqual(230, fixture.Hold.TotalCargoValue);
                Assert.AreEqual(fixture.Slot.position, cargoItem.transform.position);
                Assert.IsNotNull(cargoItem.GetComponent<Collider>(), "Loaded cargo needs a collider so the player can raycast it for re-pickup.");
                Assert.IsTrue(cargoItem.gameObject.activeSelf);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void StoredCargoInteractionLabelUsesReadableKoreanPickupPrompt()
        {
            var fixture = new CargoHoldFixture();

            try
            {
                Assert.IsTrue(fixture.Hold.TryStore(new ArtifactDefinition("Ledger", 230, 1.5f, 1), out var cargoItem));

                Assert.AreEqual("[E] 화물 다시 들기 - Ledger", ((IInteractable)cargoItem).InteractionLabel);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void StoredCargoRepickupShowsSuccessFeedback()
        {
            var fixture = new CargoHoldFixture();
            var actor = CreateActor("CargoRepickupFeedbackActor");
            var gameLoopObject = new GameObject("CargoRepickupSuccessLoop");

            try
            {
                var gameLoop = gameLoopObject.AddComponent<GameLoopController>();
                Assert.IsTrue(fixture.Hold.TryStore(new ArtifactDefinition("Ledger", 230, 1.5f, 1), out var cargoItem));

                ((IInteractable)cargoItem).Interact(actor);

                Assert.AreEqual("Cargo picked up", gameLoop.FeedbackMessage);
            }
            finally
            {
                Object.DestroyImmediate(gameLoopObject);
                DestroyActor(actor);
                fixture.Destroy();
            }
        }

        [Test]
        public void StoredCargoCanBePickedBackUpFromHold()
        {
            var fixture = new CargoHoldFixture();
            var actor = CreateActor("CargoRepickupActor");
            VanCargoItem cargoItem = null;

            try
            {
                var ledger = new ArtifactDefinition("Ledger", 230, 1.5f, 1, new[] { "paper" }, 1);
                Assert.IsTrue(fixture.Hold.TryStore(ledger, out cargoItem));

                var interactable = cargoItem as IInteractable;
                Assert.IsNotNull(interactable, "Loaded cargo should be interactable for E re-pickup.");
                Assert.IsTrue(interactable.CanInteract(actor));

                interactable.Interact(actor);

                Assert.AreEqual(0, fixture.Hold.CargoCount);
                Assert.AreEqual(0, fixture.Hold.TotalCargoValue);
                Assert.AreEqual(1, actor.Inventory.Items.Count);
                Assert.AreEqual("Ledger", actor.Inventory.Items[0].DisplayName);
                Assert.AreEqual(230, actor.Inventory.Items[0].Value);
                Assert.IsNotNull(GameObject.Find("Held_Ledger"), "Re-picked cargo should restore the first-person held view.");
                Assert.IsTrue(cargoItem == null, "Cargo object should be removed from the van after re-pickup.");
            }
            finally
            {
                DestroyActor(actor);
                if (cargoItem != null)
                {
                    Object.DestroyImmediate(cargoItem.gameObject);
                }

                fixture.Destroy();
            }
        }

        [Test]
        public void StoredCargoPickupFailsWhenHandsAreFull()
        {
            var fixture = new CargoHoldFixture();
            var actor = CreateActor("CargoRepickupFullHandsActor");
            var gameLoopObject = new GameObject("CargoRepickupFeedbackLoop");
            var gameLoop = gameLoopObject.AddComponent<GameLoopController>();

            try
            {
                Assert.IsTrue(actor.TryCollectArtifact(new ArtifactDefinition("Large Chest", 500, 4f, 2, null, 2)));
                Assert.IsTrue(fixture.Hold.TryStore(new ArtifactDefinition("Ledger", 230, 1.5f, 1, null, 1), out var cargoItem));

                var interactable = cargoItem as IInteractable;
                Assert.IsNotNull(interactable, "Loaded cargo should be interactable even when pickup will fail.");
                Assert.IsTrue(interactable.CanInteract(actor), "Loaded cargo should stay targetable so E can show failure feedback.");

                interactable.Interact(actor);

                Assert.AreEqual(1, fixture.Hold.CargoCount, "Failed re-pickup should leave cargo loaded in the van.");
                Assert.AreEqual(230, fixture.Hold.TotalCargoValue);
                Assert.AreEqual(1, actor.Inventory.Items.Count);
                Assert.AreEqual("Hands full", gameLoop.FeedbackMessage);
                Assert.IsFalse(cargoItem == null, "Cargo object should stay in the van when re-pickup fails.");
            }
            finally
            {
                Object.DestroyImmediate(gameLoopObject);
                DestroyActor(actor);
                fixture.Destroy();
            }
        }

        [Test]
        public void SettlementConsumesOnlyCargoLoadedInHold()
        {
            var fixture = new CargoHoldFixture();
            var looseObject = new GameObject("LooseCargoOutsideVan");

            try
            {
                var brassBowl = new ArtifactDefinition("Brass Bowl", 400, 2f, 1);
                var looseTablet = new ArtifactDefinition("Loose Tablet", 999, 1f, 1);
                looseObject.AddComponent<VanCargoItem>().InitializeLoose(looseTablet);

                Assert.IsTrue(fixture.Hold.TryStore(brassBowl, out var loadedCargo));
                Assert.AreEqual(400, fixture.Hold.TotalCargoValue);

                var settledValue = fixture.Hold.ConsumeSettledCargo();

                Assert.AreEqual(400, settledValue);
                Assert.AreEqual(0, fixture.Hold.CargoCount);
                Assert.AreEqual(0, fixture.Hold.TotalCargoValue);
                Assert.IsTrue(loadedCargo == null);
                Assert.IsNotNull(looseObject.GetComponent<VanCargoItem>());
            }
            finally
            {
                Object.DestroyImmediate(looseObject);
                fixture.Destroy();
            }
        }

        [Test]
        public void FallbackCargoDoesNotStackAtSamePosition()
        {
            var fixture = new CargoHoldFixture();

            try
            {
                var fallback = new GameObject("CargoFallback").transform;
                fallback.SetParent(fixture.Root.transform, false);
                fallback.localPosition = new Vector3(0f, 0.2f, 0.8f);
                fixture.Hold.RegisterFallbackSlot(fallback);

                Assert.IsTrue(fixture.Hold.TryStore(new ArtifactDefinition("First", 100), out _));
                Assert.IsTrue(fixture.Hold.TryStore(new ArtifactDefinition("Second", 120), out var second));
                Assert.IsTrue(fixture.Hold.TryStore(new ArtifactDefinition("Third", 140), out var third));

                Assert.AreEqual(3, fixture.Hold.CargoCount);
                Assert.AreEqual(fallback, second.transform.parent);
                Assert.AreEqual(fallback, third.transform.parent);
                Assert.AreNotEqual(second.transform.position, third.transform.position);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void RemoveCargoClearsHoldAndFreesSlot()
        {
            var fixture = new CargoHoldFixture();
            VanCargoItem releasedCargo = null;

            try
            {
                var first = new ArtifactDefinition("First", 100);
                var second = new ArtifactDefinition("Second", 120);

                Assert.IsTrue(fixture.Hold.TryStore(first, out releasedCargo));
                Assert.IsTrue(fixture.Hold.RemoveCargo(releasedCargo));

                Assert.AreEqual(0, fixture.Hold.CargoCount);
                Assert.IsFalse(releasedCargo.IsLoadedInHold);
                Assert.IsNull(releasedCargo.CargoHold);
                Assert.IsNull(releasedCargo.transform.parent);

                Assert.IsTrue(fixture.Hold.TryStore(second, out var nextCargo));
                Assert.AreEqual(fixture.Slot.position, nextCargo.transform.position);
            }
            finally
            {
                if (releasedCargo != null)
                {
                    Object.DestroyImmediate(releasedCargo.gameObject);
                }

                fixture.Destroy();
            }
        }

        [Test]
        public void TransferCargoToMovesLoadedDefinitionsToDestinationHold()
        {
            var source = new CargoHoldFixture();
            var destination = new CargoHoldFixture();

            try
            {
                var brassBowl = new ArtifactDefinition("Brass Bowl", 400, 2f, 2, null, 2);
                var ledger = new ArtifactDefinition("Ledger", 180, 0.8f, 1);

                Assert.IsTrue(source.Hold.TryStore(brassBowl, out _));
                Assert.IsTrue(source.Hold.TryStore(ledger, out _));

                var transferredValue = source.Hold.TransferCargoTo(destination.Hold);

                Assert.AreEqual(580, transferredValue);
                Assert.AreEqual(0, source.Hold.CargoCount);
                Assert.AreEqual(2, destination.Hold.CargoCount);
                Assert.AreEqual(580, destination.Hold.TotalCargoValue);
            }
            finally
            {
                source.Destroy();
                destination.Destroy();
            }
        }

        private static UnityPlayerController CreateActor(string name)
        {
            var actorObject = new GameObject(name);
            actorObject.AddComponent<CharacterController>();
            return actorObject.AddComponent<UnityPlayerController>();
        }

        private static void DestroyActor(UnityPlayerController actor)
        {
            if (actor != null)
            {
                Object.DestroyImmediate(actor.gameObject);
            }
        }

        private sealed class CargoHoldFixture
        {
            public GameObject Root { get; }
            public VanCargoHold Hold { get; }
            public Transform Slot { get; }

            public CargoHoldFixture()
            {
                Root = new GameObject("VanCargoHoldFixture");
                Hold = Root.AddComponent<VanCargoHold>();
                Slot = new GameObject("CargoSlot").transform;
                Slot.SetParent(Root.transform, false);
                Slot.localPosition = new Vector3(0.35f, 0.2f, 0.1f);
                Hold.RegisterSlot(Slot);
            }

            public void Destroy()
            {
                Object.DestroyImmediate(Root);
            }
        }
    }
}
