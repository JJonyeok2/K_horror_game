using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class BongoTravelSequenceControllerTests
    {
        [Test]
        public void TravelSequenceActivatesAudioFadeAndMotionWhileBongoIsTraveling()
        {
            var root = new GameObject("TravelSequenceFixture");
            var gameLoopObject = new GameObject("GameLoop");
            var sequenceObject = new GameObject("TravelSequence");
            var motionRig = new GameObject("TravelMotionRig");

            try
            {
                gameLoopObject.transform.SetParent(root.transform, false);
                sequenceObject.transform.SetParent(root.transform, false);
                motionRig.transform.SetParent(sequenceObject.transform, false);

                var gameLoop = gameLoopObject.AddComponent<GameLoopController>();
                InvokePrivate(gameLoop, "Awake");

                var fade = sequenceObject.AddComponent<CanvasGroup>();
                var audio = sequenceObject.AddComponent<AudioSource>();
                var sequence = sequenceObject.AddComponent<BongoTravelSequenceController>();
                SetPrivateField(sequence, "gameLoop", gameLoop);
                SetPrivateField(sequence, "fadeGroup", fade);
                SetPrivateField(sequence, "travelAudio", audio);
                SetPrivateField(sequence, "motionRig", motionRig.transform);
                InvokePrivate(sequence, "Awake");

                sequence.ManualRefresh();
                Assert.IsFalse(sequence.IsTravelSequenceActive);
                Assert.AreEqual(0f, fade.alpha, 0.001f);

                Assert.IsTrue(gameLoop.OperateBongoTerminal());
                sequence.ManualRefresh();

                Assert.IsTrue(sequence.IsTravelSequenceActive);
                Assert.Greater(fade.alpha, 0.4f);
                Assert.IsNotNull(audio.clip, "Travel sequence should provide a generated engine hum clip.");
                Assert.IsTrue(sequence.TravelAudioRequested);

                var beforeMotion = motionRig.transform.localPosition;
                sequence.ManualTick(0.3f);
                Assert.AreNotEqual(beforeMotion, motionRig.transform.localPosition);

                gameLoop.State.CompleteBongoTravel();
                sequence.ManualRefresh();

                Assert.IsFalse(sequence.IsTravelSequenceActive);
                Assert.AreEqual(0f, fade.alpha, 0.001f);
                Assert.IsFalse(sequence.TravelAudioRequested);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, null);
        }
    }
}
