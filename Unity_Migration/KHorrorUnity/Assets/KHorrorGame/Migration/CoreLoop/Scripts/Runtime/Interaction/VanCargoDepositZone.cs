using UnityEngine;
using UnityEngine.InputSystem;

namespace KHorrorGame.Migration
{
    [RequireComponent(typeof(Collider))]
    public sealed class VanCargoDepositZone : MonoBehaviour
    {
        [SerializeField] private GameLoopController gameLoop;
        [SerializeField] private VanCargoHold cargoHold;

        private UnityPlayerController currentActor;

        public string LastFeedbackMessage { get; private set; } = string.Empty;

        private void Awake()
        {
            if (gameLoop == null)
            {
                gameLoop = FindObjectOfType<GameLoopController>();
            }

            if (cargoHold == null)
            {
                cargoHold = GetComponentInParent<VanCargoHold>();
            }

            if (cargoHold == null)
            {
                cargoHold = GetComponentInChildren<VanCargoHold>();
            }

            var zoneCollider = GetComponent<Collider>();
            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
            {
                ManualDeposit(ResolveCurrentActor());
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            CaptureActor(other);
        }

        private void OnTriggerStay(Collider other)
        {
            CaptureActor(other);
        }

        private void OnTriggerExit(Collider other)
        {
            var actor = other != null ? other.GetComponentInParent<UnityPlayerController>() : null;
            if (actor != null && actor == currentActor)
            {
                currentActor = null;
            }
        }

        public bool ManualDeposit(UnityPlayerController actor)
        {
            if (actor == null || gameLoop == null || gameLoop.State == null)
            {
                SetFeedback("Cannot load cargo");
                return false;
            }

            if (gameLoop.State.CurrentMap != GameMapId.JonggaEstate || gameLoop.State.IsTraveling)
            {
                SetFeedback("Cargo loading unavailable");
                return false;
            }

            if (actor.Inventory.Items.Count <= 0)
            {
                SetFeedback("No cargo to load");
                return false;
            }

            if (cargoHold == null)
            {
                SetFeedback("Cargo hold missing");
                return false;
            }

            var item = actor.Inventory.PopLastItem();
            if (item == null)
            {
                SetFeedback("No cargo to load");
                return false;
            }

            if (!cargoHold.TryStore(item, out _))
            {
                actor.Inventory.TryAdd(item);
                SetFeedback("Cargo hold full");
                return false;
            }

            actor.RefreshHeldItemViews();
            SetFeedback("Cargo loaded");
            return true;
        }

        private void CaptureActor(Collider other)
        {
            var actor = other != null ? other.GetComponentInParent<UnityPlayerController>() : null;
            if (actor != null)
            {
                currentActor = actor;
            }
        }

        private UnityPlayerController ResolveCurrentActor()
        {
            if (IsInsideZone(currentActor))
            {
                return currentActor;
            }

            var actor = FindObjectOfType<UnityPlayerController>();
            return IsInsideZone(actor) ? actor : null;
        }

        private bool IsInsideZone(UnityPlayerController actor)
        {
            var zoneCollider = GetComponent<Collider>();
            if (actor == null || zoneCollider == null)
            {
                return false;
            }

            var characterController = actor.GetComponent<CharacterController>();
            if (characterController != null)
            {
                return zoneCollider.bounds.Intersects(characterController.bounds);
            }

            return zoneCollider.bounds.Contains(actor.transform.position);
        }

        private void SetFeedback(string message)
        {
            LastFeedbackMessage = message ?? string.Empty;
            if (gameLoop != null)
            {
                gameLoop.ShowFeedback(LastFeedbackMessage);
            }
        }
    }
}
