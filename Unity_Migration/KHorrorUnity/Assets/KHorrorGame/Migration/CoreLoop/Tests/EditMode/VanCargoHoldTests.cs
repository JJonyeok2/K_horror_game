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
                Assert.IsNull(cargoItem.GetComponent<Collider>());
                Assert.IsTrue(cargoItem.gameObject.activeSelf);
            }
            finally
            {
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
