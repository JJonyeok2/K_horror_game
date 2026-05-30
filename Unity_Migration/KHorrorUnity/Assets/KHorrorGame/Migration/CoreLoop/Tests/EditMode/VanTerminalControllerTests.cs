using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class VanTerminalControllerTests
    {
        [Test]
        public void TerminalPanelShowsLoadedCargoQuotaAndRemainingValue()
        {
            var fixture = new TerminalFixture();

            try
            {
                Assert.IsTrue(fixture.CargoHold.TryStore(new ArtifactDefinition("Ledger", 230, 1.5f, 1), out _));

                fixture.Terminal.ManualRefresh(true);
                fixture.Terminal.ManualTick(20f);

                StringAssert.Contains("CARGO VALUE: 230", fixture.Terminal.VisibleBodyText);
                StringAssert.Contains("CARGO COUNT: 1", fixture.Terminal.VisibleBodyText);
                StringAssert.Contains("QUOTA: 0 / 800", fixture.Terminal.VisibleBodyText);
                StringAssert.Contains("REMAINING: 570", fixture.Terminal.VisibleBodyText);
                Assert.AreEqual("K-BONGO CARGO TERMINAL", fixture.Terminal.TitleText);
                Assert.AreEqual(VanTerminalVisualState.ReadyToSettle, fixture.Terminal.VisualState);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void TerminalPanelTypesBodyTextOverTime()
        {
            var fixture = new TerminalFixture();

            try
            {
                SetPrivateField(fixture.Terminal, "charactersPerSecond", 4f);
                Assert.IsTrue(fixture.CargoHold.TryStore(new ArtifactDefinition("Ledger", 230, 1.5f, 1), out _));

                fixture.Terminal.ManualRefresh(true);
                fixture.Terminal.ManualTick(0.25f);
                var partialText = fixture.Terminal.VisibleBodyText;

                fixture.Terminal.ManualTick(40f);

                Assert.Less(partialText.Length, fixture.Terminal.TargetBodyText.Length);
                Assert.AreEqual(fixture.Terminal.TargetBodyText, fixture.Terminal.VisibleBodyText);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void SettlementFeedbackSetsSuccessVisualState()
        {
            var fixture = new TerminalFixture();

            try
            {
                fixture.GameLoop.ShowFeedback("Settled +230");

                fixture.Terminal.ManualRefresh(false);

                Assert.AreEqual(VanTerminalVisualState.Success, fixture.Terminal.VisualState);
                StringAssert.Contains("SETTLED", fixture.Terminal.FooterText);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void FailureFeedbackSetsFailureVisualState()
        {
            var fixture = new TerminalFixture();

            try
            {
                fixture.GameLoop.ShowFeedback("Cannot settle missing cargo");

                fixture.Terminal.ManualRefresh(false);

                Assert.AreEqual(VanTerminalVisualState.Failure, fixture.Terminal.VisualState);
                StringAssert.Contains("ACTION DENIED", fixture.Terminal.FooterText);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void RuntimePanelRefreshesWhenGameLoopStateChanges()
        {
            var fixture = new TerminalFixture();

            try
            {
                var terminal = VanTerminalController.EnsureRuntimePanel(fixture.GameLoop);

                fixture.GameLoop.ShowFeedback("Settled +230");

                Assert.AreEqual(VanTerminalVisualState.Success, terminal.VisualState);
                StringAssert.Contains("SETTLED", terminal.FooterText);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void TerminalInteractableUsesCenterPromptFriendlyKoreanLabel()
        {
            var fixture = new TerminalFixture();

            try
            {
                var terminalObject = new GameObject("BongoTerminalFixture");
                terminalObject.transform.SetParent(fixture.Root.transform, false);
                var terminal = terminalObject.AddComponent<BongoTerminal>();
                SetPrivateField(terminal, "gameLoop", fixture.GameLoop);

                Assert.AreEqual("[E] 단말기 조작 - Drive to Jongga estate", terminal.InteractionLabel);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        private sealed class TerminalFixture
        {
            public GameObject Root { get; }
            public GameLoopController GameLoop { get; }
            public VanCargoHold CargoHold { get; }
            public VanTerminalController Terminal { get; }

            public TerminalFixture()
            {
                Root = new GameObject("TerminalFixture");

                var playerObject = new GameObject("TerminalPlayer");
                playerObject.transform.SetParent(Root.transform, false);
                playerObject.AddComponent<CharacterController>();
                var player = playerObject.AddComponent<UnityPlayerController>();

                var hubRoot = new GameObject("BongoHub");
                hubRoot.transform.SetParent(Root.transform, false);
                var cargoHoldObject = new GameObject("BongoHubCargoHold");
                cargoHoldObject.transform.SetParent(hubRoot.transform, false);
                CargoHold = cargoHoldObject.AddComponent<VanCargoHold>();
                var slot = new GameObject("CargoSlot").transform;
                slot.SetParent(cargoHoldObject.transform, false);
                CargoHold.RegisterSlot(slot);

                var gameLoopObject = new GameObject("GameLoop");
                gameLoopObject.transform.SetParent(Root.transform, false);
                GameLoop = gameLoopObject.AddComponent<GameLoopController>();
                SetPrivateField(GameLoop, "player", player);
                SetPrivateField(GameLoop, "bongoHubRoot", hubRoot);
                SetPrivateField(GameLoop, "hubCargoHold", CargoHold);
                InvokePrivate(GameLoop, "Awake");

                var terminalObject = new GameObject("VanTerminalController");
                terminalObject.transform.SetParent(Root.transform, false);
                Terminal = terminalObject.AddComponent<VanTerminalController>();
                SetPrivateField(Terminal, "gameLoop", GameLoop);
                InvokePrivate(Terminal, "Awake");
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
    }
}
