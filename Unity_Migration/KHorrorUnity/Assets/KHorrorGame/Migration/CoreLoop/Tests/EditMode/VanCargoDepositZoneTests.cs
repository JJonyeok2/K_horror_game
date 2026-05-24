using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace KHorrorGame.Migration.Tests
{
    public sealed class VanCargoDepositZoneTests
    {
        [Test]
        public void ManualDepositStoresPhysicalCargoWithoutPendingValue()
        {
            var fixture = new CargoDepositFixture(true);

            try
            {
                var ledger = new ArtifactDefinition("Ledger", 230, 1.5f, 1);
                Assert.IsTrue(fixture.Actor.TryCollectArtifact(ledger));

                Assert.IsTrue(fixture.DepositZone.ManualDeposit(fixture.Actor));

                Assert.AreEqual(1, fixture.CargoHold.CargoCount);
                Assert.AreEqual(230, fixture.CargoHold.TotalCargoValue);
                Assert.AreEqual(0, fixture.Actor.Inventory.Items.Count);
                Assert.AreEqual(0, fixture.GameLoop.State.PendingRecoveredValue);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void ManualDepositWithoutCargoHoldKeepsHandsAndPendingValue()
        {
            var fixture = new CargoDepositFixture(false);

            try
            {
                Assert.IsTrue(fixture.Actor.TryCollectArtifact(new ArtifactDefinition("Ledger", 230, 1.5f, 1)));

                Assert.IsFalse(fixture.DepositZone.ManualDeposit(fixture.Actor));

                Assert.AreEqual(1, fixture.Actor.Inventory.Items.Count);
                Assert.AreEqual(0, fixture.GameLoop.State.PendingRecoveredValue);
                Assert.AreEqual("Cargo hold missing", fixture.DepositZone.LastFeedbackMessage);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void CargoLoadFailureAppearsInHudPrompt()
        {
            var fixture = new CargoDepositFixture(false);
            var hudObject = new GameObject("Hud");
            var textObject = new GameObject("CenterPromptText");

            try
            {
                Assert.IsTrue(fixture.Actor.TryCollectArtifact(new ArtifactDefinition("Ledger", 230, 1.5f, 1)));
                Assert.IsFalse(fixture.DepositZone.ManualDeposit(fixture.Actor));

                textObject.AddComponent<CanvasRenderer>();
                var centerText = textObject.AddComponent<Text>();
                var hud = hudObject.AddComponent<HudPresenter>();
                SetPrivateField(hud, "gameLoop", fixture.GameLoop);
                SetPrivateField(hud, "player", fixture.Actor);
                SetPrivateField(hud, "centerPromptText", centerText);

                InvokePrivate(hud, "Refresh");

                Assert.AreEqual("Cargo hold missing", centerText.text);
            }
            finally
            {
                Object.DestroyImmediate(hudObject);
                Object.DestroyImmediate(textObject);
                fixture.Destroy();
            }
        }

        [Test]
        public void ReturnTerminalDoesNotAutoExtractHeldCargo()
        {
            var fixture = new CargoDepositFixture(true);

            try
            {
                var terminalObject = new GameObject("BongoReturnTerminal");
                terminalObject.transform.SetParent(fixture.Root.transform, false);
                var terminal = terminalObject.AddComponent<BongoReturnTerminal>();
                SetPrivateField(terminal, "gameLoop", fixture.GameLoop);
                InvokePrivate(terminal, "Awake");

                Assert.IsTrue(fixture.Actor.TryCollectArtifact(new ArtifactDefinition("Ledger", 230, 1.5f, 1)));

                terminal.Interact(fixture.Actor);

                Assert.AreEqual(1, fixture.Actor.Inventory.Items.Count);
                Assert.AreEqual(0, fixture.GameLoop.State.PendingRecoveredValue);
                Assert.IsTrue(fixture.GameLoop.State.IsTraveling);
                Assert.AreEqual(GameMapId.BongoHub, fixture.GameLoop.State.TravelDestination.Value);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void ResolveCurrentActorUsesCharacterControllerBoundsInsideCargoZone()
        {
            var fixture = new CargoDepositFixture(true);

            try
            {
                var zoneCollider = fixture.DepositZone.GetComponent<BoxCollider>();
                zoneCollider.size = new Vector3(2f, 2f, 2f);
                fixture.Actor.transform.position = new Vector3(1.15f, 0f, 0f);
                Physics.SyncTransforms();

                var resolvedActor = InvokePrivate<UnityPlayerController>(fixture.DepositZone, "ResolveCurrentActor");

                Assert.AreSame(fixture.Actor, resolvedActor);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void CollectedArtifactCreatesVisibleHeldView()
        {
            var fixture = new CargoDepositFixture(true);

            try
            {
                Assert.IsTrue(fixture.Actor.TryCollectArtifact(new ArtifactDefinition("Ledger", 230, 1.5f, 1)));

                var held = GameObject.Find("Held_Ledger");

                Assert.IsNotNull(held, "Collected artifact should create a first-person held view.");
                Assert.IsNotNull(held.GetComponent<Renderer>(), "Held view should have a renderer.");
                Assert.IsNotNull(held.GetComponent<Renderer>().sharedMaterial, "Held view should use a visible material.");
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void LargeHeldArtifactObstructsLowerCenterView()
        {
            var fixture = new CargoDepositFixture(true);

            try
            {
                Assert.IsTrue(fixture.Actor.TryCollectArtifact(new ArtifactDefinition("Jongga Chest", 420, 4.5f, 2, null, 2)));

                var held = GameObject.Find("Held_Jongga Chest");
                Assert.IsNotNull(held, "Large artifacts should create a two-hand first-person obstruction.");

                var mount = held.transform.parent;
                Assert.IsNotNull(mount);
                Assert.AreEqual("TwoHandHeldMount", mount.name);
                Assert.LessOrEqual(Mathf.Abs(mount.localPosition.x), 0.05f, "Two-hand cargo should sit near the center of the view.");
                Assert.GreaterOrEqual(mount.localPosition.y, -0.32f, "Two-hand cargo should be high enough to block the lower view.");
                Assert.LessOrEqual(mount.localPosition.z, 0.5f, "Two-hand cargo should be close to the camera, not floating far ahead.");
                Assert.GreaterOrEqual(held.transform.localScale.x, 1.05f, "Large cargo needs enough width to visibly cover the center view.");
                Assert.GreaterOrEqual(held.transform.localScale.y, 0.62f, "Large cargo needs enough height to obscure the lower view.");
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void SmallHeldArtifactsOccupyBothSidesOfFirstPersonView()
        {
            var fixture = new CargoDepositFixture(true);

            try
            {
                Assert.IsTrue(fixture.Actor.TryCollectArtifact(new ArtifactDefinition("Ledger", 230, 1.5f, 1, null, 1)));
                Assert.IsTrue(fixture.Actor.TryCollectArtifact(new ArtifactDefinition("Brass Bowl", 180, 1.2f, 1, null, 1)));

                var leftHeld = GameObject.Find("Held_Ledger");
                var rightHeld = GameObject.Find("Held_Brass Bowl");

                Assert.IsNotNull(leftHeld, "First one-hand artifact should be visible in the left hand.");
                Assert.IsNotNull(rightHeld, "Second one-hand artifact should be visible in the right hand.");
                Assert.AreEqual("LeftHandHeldMount", leftHeld.transform.parent.name);
                Assert.AreEqual("RightHandHeldMount", rightHeld.transform.parent.name);
                Assert.Less(leftHeld.transform.parent.localPosition.x, -0.18f);
                Assert.Greater(rightHeld.transform.parent.localPosition.x, 0.18f);
                Assert.GreaterOrEqual(leftHeld.transform.localScale.x, 0.42f, "One-hand cargo should not be a tiny placeholder cube.");
                Assert.GreaterOrEqual(rightHeld.transform.localScale.x, 0.42f, "One-hand cargo should not be a tiny placeholder cube.");
            }
            finally
            {
                fixture.Destroy();
            }
        }

        private sealed class CargoDepositFixture
        {
            public GameObject Root { get; }
            public UnityPlayerController Actor { get; }
            public GameLoopController GameLoop { get; }
            public VanCargoHold CargoHold { get; }
            public VanCargoDepositZone DepositZone { get; }

            public CargoDepositFixture(bool includeCargoHold)
            {
                Root = new GameObject("CargoDepositFixture");

                var playerObject = new GameObject("Player");
                playerObject.transform.SetParent(Root.transform, false);
                playerObject.AddComponent<CharacterController>();
                Actor = playerObject.AddComponent<UnityPlayerController>();

                var gameLoopObject = new GameObject("GameLoop");
                gameLoopObject.transform.SetParent(Root.transform, false);
                GameLoop = gameLoopObject.AddComponent<GameLoopController>();
                SetPrivateField(GameLoop, "player", Actor);
                InvokePrivate(GameLoop, "Awake");
                Assert.IsTrue(GameLoop.State.OperateBongoTerminal());
                GameLoop.State.CompleteBongoTravel();
                Assert.AreEqual(GameMapId.JonggaEstate, GameLoop.State.CurrentMap);

                if (includeCargoHold)
                {
                    var cargoHoldObject = new GameObject("CargoHold");
                    cargoHoldObject.transform.SetParent(Root.transform, false);
                    CargoHold = cargoHoldObject.AddComponent<VanCargoHold>();

                    var slot = new GameObject("CargoSlot").transform;
                    slot.SetParent(cargoHoldObject.transform, false);
                    slot.localPosition = new Vector3(0f, 0.2f, 0f);
                    CargoHold.RegisterSlot(slot);
                }

                var depositObject = new GameObject("VanCargoDepositZone");
                depositObject.transform.SetParent(Root.transform, false);
                depositObject.AddComponent<BoxCollider>();
                DepositZone = depositObject.AddComponent<VanCargoDepositZone>();
                SetPrivateField(DepositZone, "gameLoop", GameLoop);
                if (includeCargoHold)
                {
                    SetPrivateField(DepositZone, "cargoHold", CargoHold);
                }

                InvokePrivate(DepositZone, "Awake");
            }

            public void Destroy()
            {
                Object.DestroyImmediate(Root);
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

        private static T InvokePrivate<T>(object target, string methodName)
        {
            return (T)target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, null);
        }
    }
}
