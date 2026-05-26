using UnityEngine;
using UnityEngine.UI;

namespace KHorrorGame.Migration
{
    public sealed class BongoMonitorDisplay : MonoBehaviour
    {
        [SerializeField] private GameLoopController gameLoop;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;

        private void Awake()
        {
            if (gameLoop == null)
            {
                gameLoop = FindObjectOfType<GameLoopController>();
            }
        }

        private void OnEnable()
        {
            if (gameLoop != null)
            {
                gameLoop.StateChanged += OnStateChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (gameLoop != null)
            {
                gameLoop.StateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(GameLoopController controller)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (gameLoop == null || gameLoop.State == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = gameLoop.TerminalScreenText();
            }

            if (bodyText != null)
            {
                bodyText.text = gameLoop.MonitorBodyText();
            }
        }
    }
}
