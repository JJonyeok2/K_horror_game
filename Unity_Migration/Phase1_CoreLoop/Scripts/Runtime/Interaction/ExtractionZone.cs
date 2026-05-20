using UnityEngine;

namespace KHorrorGame.Migration
{
    [RequireComponent(typeof(Collider))]
    public sealed class ExtractionZone : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameLoopController gameLoop;
        [SerializeField] private bool autoExtractOnEnter;

        public string InteractionLabel => "Load cargo [E]";

        private void Awake()
        {
            if (gameLoop == null)
            {
                gameLoop = FindObjectOfType<GameLoopController>();
            }

            var zoneCollider = GetComponent<Collider>();
            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }
        }

        public bool CanInteract(UnityPlayerController actor)
        {
            return gameLoop != null
                   && gameLoop.State.CurrentMap == GameMapId.JonggaEstate
                   && actor != null
                   && actor.Inventory.Items.Count > 0;
        }

        public void Interact(UnityPlayerController actor)
        {
            if (CanInteract(actor))
            {
                gameLoop.ExtractPlayerInventory();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!autoExtractOnEnter)
            {
                return;
            }

            var actor = other.GetComponentInParent<UnityPlayerController>();
            if (CanInteract(actor))
            {
                gameLoop.ExtractPlayerInventory();
            }
        }
    }
}
