using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class BongoTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameLoopController gameLoop;

        public string InteractionLabel
        {
            get
            {
                if (gameLoop == null)
                {
                    return "[E] 단말기 조작";
                }

                if (gameLoop.State != null && gameLoop.State.IsTraveling)
                {
                    return "[E] 단말기 조작 - 이동 중 사용 불가";
                }

                return "[E] 단말기 조작 - " + gameLoop.TerminalActionText();
            }
        }

        private void Awake()
        {
            if (gameLoop == null)
            {
                gameLoop = FindObjectOfType<GameLoopController>();
            }

            if (gameLoop != null)
            {
                VanTerminalController.EnsureRuntimePanel(gameLoop);
            }
        }

        public bool CanInteract(UnityPlayerController actor)
        {
            return actor != null && gameLoop != null && !gameLoop.State.IsTraveling;
        }

        public void Interact(UnityPlayerController actor)
        {
            if (CanInteract(actor))
            {
                gameLoop.OperateBongoTerminal();
                VanTerminalController.EnsureRuntimePanel(gameLoop);
            }
        }
    }
}
