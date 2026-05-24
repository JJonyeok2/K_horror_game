using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class BongoTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameLoopController gameLoop;

        public string InteractionLabel => gameLoop != null ? gameLoop.TerminalActionText() + " [E]" : "Use terminal [E]";

        private void Awake()
        {
            if (gameLoop == null)
            {
                gameLoop = FindObjectOfType<GameLoopController>();
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
            }
        }
    }
}
