using UnityEngine;
using UnityEngine.Events;

namespace KHorrorGame.Migration
{
    public sealed class StatefulInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt = "Interact [E]";
        [SerializeField] private bool oneShot;
        [SerializeField] private bool startsActive = true;
        [SerializeField] private UnityEvent interacted;

        private bool hasInteracted;

        public string InteractionLabel => prompt;

        public bool CanInteract(UnityPlayerController actor)
        {
            return actor != null && startsActive && (!oneShot || !hasInteracted);
        }

        public void Interact(UnityPlayerController actor)
        {
            if (!CanInteract(actor))
            {
                return;
            }

            hasInteracted = true;
            interacted?.Invoke();
        }
    }
}
