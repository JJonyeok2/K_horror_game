using NUnit.Framework;

namespace KHorrorGame.Migration.Tests
{
    public sealed class BongoRunStateMachineTests
    {
        [Test]
        public void TerminalFromHubStartsEstateTravel()
        {
            var state = CreateState(0.1f);

            Assert.IsTrue(state.OperateBongoTerminal());
            Assert.IsTrue(state.IsTraveling);
            Assert.AreEqual(GameMapId.BongoTravel, state.CurrentMap);
            Assert.AreEqual(GameMapId.JonggaEstate, state.TravelDestination.Value);

            Assert.IsTrue(state.TickTravel(0.1f));
            Assert.AreEqual(GameMapId.JonggaEstate, state.CurrentMap);
            Assert.IsFalse(state.IsTraveling);
            Assert.AreEqual(1, state.MapTravelCount);
        }

        [Test]
        public void ExtractingCargoRequiresEstateAndClearsHands()
        {
            var state = CreateState(0f);
            var relic = new ArtifactDefinition("Ledger", 230, 1.5f, 1);

            Assert.IsFalse(state.ExtractPlayerInventory());
            Assert.IsTrue(state.PlayerInventory.TryAdd(relic));
            Assert.IsTrue(state.OperateBongoTerminal());
            state.CompleteBongoTravel();

            Assert.IsTrue(state.ExtractPlayerInventory());
            Assert.AreEqual(230, state.PendingRecoveredValue);
            Assert.AreEqual(0, state.PlayerInventory.Items.Count);
            Assert.AreEqual(1, state.PendingCargoItems.Count);
        }

        [Test]
        public void CargoSettlementRequiresReturnToHubThenSettlementOffice()
        {
            var state = CreateState(0f);
            var relic = new ArtifactDefinition("Brass Bowl", 400, 2f, 2, null, 2);
            state.PlayerInventory.TryAdd(relic);

            Assert.IsTrue(state.OperateBongoTerminal());
            state.CompleteBongoTravel();
            Assert.IsTrue(state.ExtractPlayerInventory());
            Assert.IsFalse(state.SettleStoredCargo());

            Assert.IsTrue(state.OperateBongoTerminal());
            state.CompleteBongoTravel();
            Assert.AreEqual(GameMapId.BongoHub, state.CurrentMap);

            Assert.IsTrue(state.OperateBongoTerminal());
            state.CompleteBongoTravel();
            Assert.AreEqual(GameMapId.SettlementOffice, state.CurrentMap);

            Assert.IsTrue(state.SettleStoredCargo());
            Assert.AreEqual(400, state.Quota.RecoveredValue);
            Assert.AreEqual(0, state.PendingRecoveredValue);
        }

        [Test]
        public void EstateReturnDoesNotRequireCargo()
        {
            var state = CreateState(0f);

            Assert.IsTrue(state.OperateBongoTerminal());
            state.CompleteBongoTravel();
            Assert.AreEqual(GameMapId.JonggaEstate, state.CurrentMap);

            Assert.IsTrue(state.ReturnToBongoHub());
            state.CompleteBongoTravel();

            Assert.AreEqual(GameMapId.BongoHub, state.CurrentMap);
            Assert.AreEqual(0, state.PendingRecoveredValue);
        }

        [Test]
        public void InventoryEnforcesTwoHands()
        {
            var inventory = new Inventory(12f, 2);

            Assert.IsTrue(inventory.TryAdd(new ArtifactDefinition("Small 1", 50, 1f, 0, null, 1)));
            Assert.IsTrue(inventory.TryAdd(new ArtifactDefinition("Small 2", 50, 1f, 0, null, 1)));
            Assert.IsFalse(inventory.TryAdd(new ArtifactDefinition("Small 3", 50, 1f, 0, null, 1)));

            inventory.Clear();
            Assert.IsTrue(inventory.TryAdd(new ArtifactDefinition("Large", 200, 4f, 1, null, 2)));
            Assert.IsFalse(inventory.TryAdd(new ArtifactDefinition("Blocked", 50, 1f, 0, null, 1)));
        }

        [Test]
        public void ShrineItemCreatesThreatGraceWindow()
        {
            var gate = new ThreatSpawnGate();
            var shrineItem = new ArtifactDefinition("Shrine Token", 500, 1f, 4, new[] { "shrine_item" });

            gate.NotifyArtifactPicked(shrineItem);

            Assert.IsFalse(gate.CanSpawnThreats);
            Assert.AreEqual(ThreatSpawnGate.ShrineThreatGraceSeconds, gate.RemainingGraceSeconds);

            gate.Tick(ThreatSpawnGate.ShrineThreatGraceSeconds);
            Assert.IsTrue(gate.CanSpawnThreats);
        }

        private static BongoRunStateMachine CreateState(float travelSeconds)
        {
            return new BongoRunStateMachine(
                new Inventory(12f, 2),
                new QuotaTracker(800),
                new ResentmentTracker(),
                travelSeconds);
        }
    }
}
