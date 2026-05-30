using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class SettlementStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameLoopController gameLoop;

        public string InteractionLabel => "Settle cargo [E]";

        private void Awake()
        {
            if (gameLoop == null)
            {
                gameLoop = FindObjectOfType<GameLoopController>();
            }
        }

        public bool CanInteract(UnityPlayerController actor)
        {
            return actor != null
                   && gameLoop != null
                   && gameLoop.State != null
                   && !gameLoop.State.IsTraveling
                   && gameLoop.State.CurrentMap == GameMapId.SettlementOffice;
        }

        public void Interact(UnityPlayerController actor)
        {
            if (CanInteract(actor))
            {
                gameLoop.SettleStoredCargo();
            }
        }
    }
}
