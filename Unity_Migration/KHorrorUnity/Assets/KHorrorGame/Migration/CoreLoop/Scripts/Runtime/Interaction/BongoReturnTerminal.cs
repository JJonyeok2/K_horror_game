using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class BongoReturnTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameLoopController gameLoop;

        private string currentLabel = "Return to bongo [E]";

        public string InteractionLabel => currentLabel;

        private void Awake()
        {
            if (gameLoop == null)
            {
                gameLoop = FindObjectOfType<GameLoopController>();
            }
        }

        public bool CanInteract(UnityPlayerController actor)
        {
            if (actor == null || gameLoop == null || gameLoop.State == null)
            {
                return false;
            }

            if (gameLoop.State.IsTraveling || gameLoop.State.CurrentMap != GameMapId.JonggaEstate)
            {
                return false;
            }

            currentLabel = actor.Inventory.Items.Count > 0
                ? "Store cargo [G] / Return [E]"
                : "Return to bongo [E]";
            return true;
        }

        public void Interact(UnityPlayerController actor)
        {
            if (!CanInteract(actor))
            {
                return;
            }

            if (actor.Inventory.Items.Count > 0)
            {
                gameLoop.ExtractPlayerInventory();
            }

            gameLoop.ReturnToBongoHub();
        }
    }
}
