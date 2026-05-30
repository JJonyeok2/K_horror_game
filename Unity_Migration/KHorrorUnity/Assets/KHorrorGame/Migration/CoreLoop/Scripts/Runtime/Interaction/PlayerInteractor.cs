using UnityEngine;
using UnityEngine.InputSystem;

namespace KHorrorGame.Migration
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private UnityPlayerController actor;
        [SerializeField] private Camera sourceCamera;
        [SerializeField] private InputActionReference interactAction;
        [SerializeField] private float interactionDistance = 2.7f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private IInteractable currentTarget;
        private bool currentTargetCanInteract;

        public string CurrentLabel { get; private set; } = string.Empty;
        public string CurrentInvalidReason { get; private set; } = string.Empty;

        private void Awake()
        {
            if (actor == null)
            {
                actor = GetComponentInParent<UnityPlayerController>();
            }

            if (sourceCamera == null)
            {
                sourceCamera = GetComponentInParent<Camera>();
            }
        }

        private void OnEnable()
        {
            if (interactAction != null && interactAction.action != null)
            {
                interactAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (interactAction != null && interactAction.action != null)
            {
                interactAction.action.Disable();
            }
        }

        private void Update()
        {
            RefreshTarget();

            if (currentTarget != null && currentTargetCanInteract && WasInteractPressed())
            {
                currentTarget.Interact(actor);
            }
        }

        private void RefreshTarget()
        {
            currentTarget = null;
            currentTargetCanInteract = false;
            CurrentLabel = string.Empty;
            CurrentInvalidReason = string.Empty;

            if (sourceCamera == null)
            {
                return;
            }

            RaycastHit hit;
            var ray = new Ray(sourceCamera.transform.position, sourceCamera.transform.forward);
            if (!Physics.Raycast(ray, out hit, interactionDistance, interactionMask, QueryTriggerInteraction.Collide))
            {
                return;
            }

            var target = ResolveInteractable(hit.collider);
            if (target == null)
            {
                return;
            }

            currentTarget = target;
            CurrentLabel = target.InteractionLabel;
            currentTargetCanInteract = target.CanInteract(actor);
            if (!currentTargetCanInteract)
            {
                CurrentInvalidReason = ResolveInvalidReason(target);
            }
        }

        private static IInteractable ResolveInteractable(Collider collider)
        {
            if (collider == null)
            {
                return null;
            }

            var behaviours = collider.GetComponentsInParent<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                var interactable = behaviour as IInteractable;
                if (interactable != null)
                {
                    return interactable;
                }
            }

            return null;
        }

        private string ResolveInvalidReason(IInteractable target)
        {
            var invalidReason = target as IInteractionFailureReason;
            if (invalidReason == null)
            {
                return string.Empty;
            }

            return invalidReason.InvalidInteractionReason(actor) ?? string.Empty;
        }

        private bool WasInteractPressed()
        {
            if (interactAction != null && interactAction.action != null)
            {
                return interactAction.action.WasPressedThisFrame();
            }

            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        }
    }
}
