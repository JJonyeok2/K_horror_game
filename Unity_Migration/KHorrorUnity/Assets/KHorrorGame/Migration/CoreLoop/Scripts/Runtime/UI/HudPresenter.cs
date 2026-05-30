using UnityEngine;
using UnityEngine.UI;

namespace KHorrorGame.Migration
{
    public sealed class HudPresenter : MonoBehaviour
    {
        [SerializeField] private GameLoopController gameLoop;
        [SerializeField] private UnityPlayerController player;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private Text statusText;
        [SerializeField] private Text centerPromptText;
        [SerializeField] private Text centerPromptSubjectText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Image staminaFill;

        private void Awake()
        {
            if (gameLoop == null)
            {
                gameLoop = FindObjectOfType<GameLoopController>();
            }

            if (player == null)
            {
                player = FindObjectOfType<UnityPlayerController>();
            }

            if (interactor == null)
            {
                interactor = FindObjectOfType<PlayerInteractor>();
            }
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (statusText != null && gameLoop != null && player != null)
            {
                statusText.text =
                    $"Weight {player.Inventory.TotalWeight():0.0}/{player.Inventory.MaxWeight:0.0}\n" +
                    $"Resentment {gameLoop.Resentment.Stage()}\n" +
                    $"Quota {gameLoop.Quota.RecoveredValue}/{gameLoop.Quota.RequiredValue}\n" +
                    $"Stamina {Mathf.RoundToInt(player.StaminaRatio * 100f)}%\n" +
                    player.Inventory.HandStatus();
            }

            if (centerPromptText != null)
            {
                RefreshCenterPrompt();
            }

            if (feedbackText != null)
            {
                feedbackText.text = ResolveFeedbackText();
            }

            if (staminaFill != null && player != null)
            {
                staminaFill.fillAmount = player.StaminaRatio;
            }
        }

        private string ResolveCenterPrompt()
        {
            if (feedbackText == null && gameLoop != null && !string.IsNullOrEmpty(gameLoop.FeedbackMessage))
            {
                return gameLoop.FeedbackMessage;
            }

            return interactor != null ? interactor.CurrentLabel : string.Empty;
        }

        private void RefreshCenterPrompt()
        {
            var prompt = ResolveCenterPrompt();
            var split = SplitPrompt(prompt);
            centerPromptText.text = split.Action;

            if (centerPromptSubjectText == null)
            {
                return;
            }

            centerPromptSubjectText.text = split.Subject;
            var actionColor = centerPromptText.color;
            centerPromptSubjectText.color = new Color(
                actionColor.r,
                actionColor.g,
                actionColor.b,
                Mathf.Min(actionColor.a, 0.58f));
        }

        private string ResolveFeedbackText()
        {
            if (gameLoop != null && !string.IsNullOrEmpty(gameLoop.FeedbackMessage))
            {
                return gameLoop.FeedbackMessage;
            }

            return interactor != null ? interactor.CurrentInvalidReason : string.Empty;
        }

        private static PromptParts SplitPrompt(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
            {
                return new PromptParts(string.Empty, string.Empty);
            }

            var separatorIndex = prompt.IndexOf(" - ", System.StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                return new PromptParts(prompt, string.Empty);
            }

            return new PromptParts(
                prompt.Substring(0, separatorIndex),
                prompt.Substring(separatorIndex + 3));
        }

        private readonly struct PromptParts
        {
            public string Action { get; }
            public string Subject { get; }

            public PromptParts(string action, string subject)
            {
                Action = action;
                Subject = subject;
            }
        }
    }
}
