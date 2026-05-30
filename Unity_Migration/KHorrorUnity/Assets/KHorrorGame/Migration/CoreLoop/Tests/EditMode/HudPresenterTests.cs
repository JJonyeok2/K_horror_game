using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace KHorrorGame.Migration.Tests
{
    public sealed class HudPresenterTests
    {
        private const string PickupPrompt = "[E] \uBB3C\uAC74 \uC90D\uAE30 - Ledger";
        private const string PickupAction = "[E] \uBB3C\uAC74 \uC90D\uAE30";

        [Test]
        public void PickupPromptSplitsActionAndSubduedItemName()
        {
            var root = new GameObject("HudPromptFixture");

            try
            {
                var hud = root.AddComponent<HudPresenter>();
                var interactor = root.AddComponent<PlayerInteractor>();
                var actionText = CreateText("PromptAction", root.transform);
                var subjectText = CreateText("PromptSubject", root.transform);

                SetAutoProperty(interactor, "CurrentLabel", PickupPrompt);
                SetPrivateField(hud, "interactor", interactor);
                SetPrivateField(hud, "centerPromptText", actionText);
                SetPrivateField(hud, "centerPromptSubjectText", subjectText);

                InvokePrivate(hud, "Refresh");

                Assert.AreEqual(PickupAction, actionText.text);
                Assert.AreEqual("Ledger", subjectText.text);
                Assert.Less(subjectText.color.a, actionText.color.a);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FeedbackUsesDedicatedTextWithoutReplacingActivePrompt()
        {
            var root = new GameObject("HudFeedbackFixture");

            try
            {
                var hud = root.AddComponent<HudPresenter>();
                var gameLoop = root.AddComponent<GameLoopController>();
                var interactor = root.AddComponent<PlayerInteractor>();
                var actionText = CreateText("PromptAction", root.transform);
                var subjectText = CreateText("PromptSubject", root.transform);
                var feedbackText = CreateText("Feedback", root.transform);

                gameLoop.ShowFeedback("Hands full");
                SetAutoProperty(interactor, "CurrentLabel", PickupPrompt);
                SetPrivateField(hud, "gameLoop", gameLoop);
                SetPrivateField(hud, "interactor", interactor);
                SetPrivateField(hud, "centerPromptText", actionText);
                SetPrivateField(hud, "centerPromptSubjectText", subjectText);
                SetPrivateField(hud, "feedbackText", feedbackText);

                InvokePrivate(hud, "Refresh");

                Assert.AreEqual(PickupAction, actionText.text);
                Assert.AreEqual("Ledger", subjectText.text);
                Assert.AreEqual("Hands full", feedbackText.text);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HudShowsInteractorInvalidReasonInFeedbackText()
        {
            var root = new GameObject("HudInvalidReasonFixture");

            try
            {
                var hud = root.AddComponent<HudPresenter>();
                var interactor = root.AddComponent<PlayerInteractor>();
                var actionText = CreateText("PromptAction", root.transform);
                var subjectText = CreateText("PromptSubject", root.transform);
                var feedbackText = CreateText("Feedback", root.transform);

                SetAutoProperty(interactor, "CurrentLabel", PickupPrompt);
                SetAutoProperty(interactor, "CurrentInvalidReason", "Hands full");
                SetPrivateField(hud, "interactor", interactor);
                SetPrivateField(hud, "centerPromptText", actionText);
                SetPrivateField(hud, "centerPromptSubjectText", subjectText);
                SetPrivateField(hud, "feedbackText", feedbackText);

                InvokePrivate(hud, "Refresh");

                Assert.AreEqual(PickupAction, actionText.text);
                Assert.AreEqual("Ledger", subjectText.text);
                Assert.AreEqual("Hands full", feedbackText.text);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Text CreateText(string name, Transform parent)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.AddComponent<CanvasRenderer>();
            return textObject.AddComponent<Text>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Expected private field " + fieldName + " on " + target.GetType().Name);
            field.SetValue(target, value);
        }

        private static void SetAutoProperty(object target, string propertyName, object value)
        {
            var field = target.GetType().GetField("<" + propertyName + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Expected auto-property backing field for " + propertyName + " on " + target.GetType().Name);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Expected private method " + methodName + " on " + target.GetType().Name);
            method.Invoke(target, null);
        }
    }
}
