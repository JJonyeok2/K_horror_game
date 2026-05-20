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
                centerPromptText.text = interactor != null ? interactor.CurrentLabel : string.Empty;
            }

            if (staminaFill != null && player != null)
            {
                staminaFill.fillAmount = player.StaminaRatio;
            }
        }
    }
}
