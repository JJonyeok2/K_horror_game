using UnityEngine;
using UnityEngine.InputSystem;

namespace KHorrorGame.Migration
{
    [RequireComponent(typeof(Collider))]
    public sealed class VanCargoDepositZone : MonoBehaviour
    {
        [SerializeField] private GameLoopController gameLoop;

        private UnityPlayerController currentActor;

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
                return false;
            }

            if (gameLoop.State.CurrentMap != GameMapId.JonggaEstate || gameLoop.State.IsTraveling)
            {
                return false;
            }

            if (actor.Inventory.Items.Count <= 0)
            {
                return false;
            }

            return gameLoop.ExtractPlayerInventory();
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
            return actor != null && zoneCollider != null && zoneCollider.bounds.Contains(actor.transform.position);
        }
    }
}
