using UnityEngine;
using UnityEngine.InputSystem;

namespace KHorrorGame.Migration
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class UnityPlayerController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference sprintAction;
        [SerializeField] private InputActionReference dropAction;

        [Header("Movement")]
        [SerializeField] private float baseSpeed = 4.5f;
        [SerializeField] private float sprintSpeedMultiplier = 1.65f;
        [SerializeField] private float exhaustedWalkMultiplier = 0.65f;
        [SerializeField] private float gravity = 21f;
        [SerializeField] private float jumpVelocity = 6.4f;
        [SerializeField] private float lookSensitivity = 0.12f;
        [SerializeField] private bool movementEnabled = true;

        [Header("Stamina")]
        [SerializeField] private float maxStaminaSeconds = 15f;
        [SerializeField] private float staminaRecoverySeconds = 11f;
        [SerializeField] private float weightStaminaDrainMultiplier = 1f;
        [SerializeField] private float exhaustedRecoveryThresholdSeconds = 1f;

        [Header("Inventory")]
        [SerializeField] private float maxInventoryWeight = 12f;
        [SerializeField] private int maxHandSlots = 2;
        [SerializeField] private GameObject droppedArtifactPrefab;
        [SerializeField] private Transform leftHandMount;
        [SerializeField] private Transform rightHandMount;
        [SerializeField] private Transform twoHandMount;

        [Header("References")]
        [SerializeField] private Camera playerCamera;

        private CharacterController characterController;
        private Inventory inventory;
        private float verticalVelocity;
        private float cameraPitch;
        private bool isExhausted;

        public Inventory Inventory
        {
            get
            {
                if (inventory == null)
                {
                    inventory = new Inventory(maxInventoryWeight, maxHandSlots);
                }

                return inventory;
            }
        }

        public float StaminaSeconds { get; private set; }
        public float StaminaRatio => maxStaminaSeconds <= 0f ? 0f : Mathf.Clamp01(StaminaSeconds / maxStaminaSeconds);
        public bool IsSprinting { get; private set; }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            inventory = new Inventory(maxInventoryWeight, maxHandSlots);
            StaminaSeconds = maxStaminaSeconds;
            EnsureHandMounts();
        }

        private void OnEnable()
        {
            EnableAction(moveAction);
            EnableAction(lookAction);
            EnableAction(jumpAction);
            EnableAction(sprintAction);
            EnableAction(dropAction);
        }

        private void OnDisable()
        {
            DisableAction(moveAction);
            DisableAction(lookAction);
            DisableAction(jumpAction);
            DisableAction(sprintAction);
            DisableAction(dropAction);
        }

        private void Update()
        {
            UpdateLook();
            UpdateMovement(Time.deltaTime);

            if (WasActionPressed(dropAction, Key.Q))
            {
                DropCurrentArtifact();
            }
        }

        public void SetMovementEnabled(bool enabled)
        {
            movementEnabled = enabled;
            if (!movementEnabled)
            {
                IsSprinting = false;
            }
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            var wasEnabled = characterController.enabled;
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            verticalVelocity = 0f;
            characterController.enabled = wasEnabled;
        }

        public bool TryCollectArtifact(ArtifactDefinition item)
        {
            var accepted = Inventory.TryAdd(item);
            if (accepted)
            {
                RefreshHeldItemViews();
            }

            return accepted;
        }

        public bool DropCurrentArtifact()
        {
            var item = Inventory.PopLastItem();
            if (item == null)
            {
                return false;
            }

            SpawnDroppedArtifact(item);
            RefreshHeldItemViews();
            return true;
        }

        private void UpdateMovement(float deltaTime)
        {
            if (characterController == null)
            {
                return;
            }

            var moveInput = movementEnabled ? ReadMoveInput() : Vector2.zero;
            var moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            var weightRatio = Mathf.Clamp01(Inventory.TotalWeight() / Mathf.Max(Inventory.MaxWeight, 0.001f));
            var speed = Mathf.Lerp(baseSpeed, baseSpeed * 0.55f, weightRatio);
            var wantsSprint = movementEnabled && moveDirection.sqrMagnitude > 0f && IsActionPressed(sprintAction, Key.LeftShift);
            var recoveryRate = maxStaminaSeconds / Mathf.Max(staminaRecoverySeconds, 0.001f);

            if (wantsSprint && !isExhausted && StaminaSeconds > 0f)
            {
                IsSprinting = true;
                speed *= sprintSpeedMultiplier;
                StaminaSeconds -= deltaTime * (1f + weightRatio * weightStaminaDrainMultiplier);
                if (StaminaSeconds <= 0f)
                {
                    StaminaSeconds = 0f;
                    isExhausted = true;
                }
            }
            else
            {
                IsSprinting = false;
                if (isExhausted)
                {
                    speed *= exhaustedWalkMultiplier;
                }

                StaminaSeconds = Mathf.Min(maxStaminaSeconds, StaminaSeconds + recoveryRate * deltaTime);
                if (isExhausted && StaminaSeconds >= exhaustedRecoveryThresholdSeconds)
                {
                    isExhausted = false;
                }
            }

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }

            if (movementEnabled && characterController.isGrounded && WasActionPressed(jumpAction, Key.Space))
            {
                verticalVelocity = jumpVelocity;
            }

            verticalVelocity -= gravity * deltaTime;
            var velocity = moveDirection * speed;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * deltaTime);
        }

        private void UpdateLook()
        {
            if (!movementEnabled || playerCamera == null)
            {
                return;
            }

            var look = ReadLookInput();
            transform.Rotate(Vector3.up, look.x * lookSensitivity, Space.World);
            cameraPitch = Mathf.Clamp(cameraPitch - look.y * lookSensitivity, -82f, 82f);
            playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        private Vector2 ReadMoveInput()
        {
            if (moveAction != null && moveAction.action != null)
            {
                return moveAction.action.ReadValue<Vector2>();
            }

            if (Keyboard.current == null)
            {
                return Vector2.zero;
            }

            var keyboard = Keyboard.current;
            var x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            var y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }

        private Vector2 ReadLookInput()
        {
            if (lookAction != null && lookAction.action != null)
            {
                return lookAction.action.ReadValue<Vector2>();
            }

            return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        }

        private bool IsActionPressed(InputActionReference actionReference, Key fallbackKey)
        {
            if (actionReference != null && actionReference.action != null)
            {
                return actionReference.action.IsPressed();
            }

            return Keyboard.current != null && Keyboard.current[fallbackKey].isPressed;
        }

        private bool WasActionPressed(InputActionReference actionReference, Key fallbackKey)
        {
            if (actionReference != null && actionReference.action != null)
            {
                return actionReference.action.WasPressedThisFrame();
            }

            return Keyboard.current != null && Keyboard.current[fallbackKey].wasPressedThisFrame;
        }

        private static void EnableAction(InputActionReference actionReference)
        {
            if (actionReference != null && actionReference.action != null)
            {
                actionReference.action.Enable();
            }
        }

        private static void DisableAction(InputActionReference actionReference)
        {
            if (actionReference != null && actionReference.action != null)
            {
                actionReference.action.Disable();
            }
        }

        private void SpawnDroppedArtifact(ArtifactDefinition item)
        {
            var origin = playerCamera != null ? playerCamera.transform : transform;
            var spawnPosition = origin.position + origin.forward * 1.45f + Vector3.down * 0.35f;
            var spawnRotation = Quaternion.LookRotation(origin.forward, Vector3.up);
            GameObject artifactObject = null;

            if (droppedArtifactPrefab != null)
            {
                artifactObject = Instantiate(droppedArtifactPrefab, spawnPosition, spawnRotation);
            }
            else
            {
                artifactObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                artifactObject.name = "DroppedArtifact";
                artifactObject.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
                artifactObject.transform.localScale = item.HandSlots >= 2 ? new Vector3(0.65f, 0.42f, 0.42f) : new Vector3(0.32f, 0.24f, 0.24f);
            }

            var pickup = artifactObject.GetComponent<ArtifactPickup>();
            if (pickup == null)
            {
                pickup = artifactObject.AddComponent<ArtifactPickup>();
            }

            pickup.ApplyDefinition(item);
        }

        private void EnsureHandMounts()
        {
            var parent = playerCamera != null ? playerCamera.transform : transform;
            if (leftHandMount == null)
            {
                leftHandMount = CreateMount(parent, "LeftHandHeldMount", new Vector3(-0.33f, -0.28f, 0.58f));
            }

            if (rightHandMount == null)
            {
                rightHandMount = CreateMount(parent, "RightHandHeldMount", new Vector3(0.33f, -0.28f, 0.58f));
            }

            if (twoHandMount == null)
            {
                twoHandMount = CreateMount(parent, "TwoHandHeldMount", new Vector3(0f, -0.31f, 0.62f));
            }
        }

        private static Transform CreateMount(Transform parent, string name, Vector3 localPosition)
        {
            var mount = new GameObject(name).transform;
            mount.SetParent(parent, false);
            mount.localPosition = localPosition;
            mount.localRotation = Quaternion.identity;
            return mount;
        }

        public void RefreshHeldItemViews()
        {
            EnsureHandMounts();
            ClearMount(leftHandMount);
            ClearMount(rightHandMount);
            ClearMount(twoHandMount);

            var items = Inventory.Items;
            if (items.Count == 0)
            {
                return;
            }

            if (items[0].HandSlots >= 2)
            {
                CreateHeldBox(twoHandMount, items[0], new Vector3(0.78f, 0.42f, 0.28f));
                return;
            }

            CreateHeldBox(leftHandMount, items[0], new Vector3(0.28f, 0.22f, 0.2f));
            if (items.Count > 1)
            {
                CreateHeldBox(rightHandMount, items[1], new Vector3(0.28f, 0.22f, 0.2f));
            }
        }

        private static void ClearMount(Transform mount)
        {
            if (mount == null)
            {
                return;
            }

            for (var i = mount.childCount - 1; i >= 0; i--)
            {
                DestroyHeldObject(mount.GetChild(i).gameObject);
            }
        }

        private static void CreateHeldBox(Transform mount, ArtifactDefinition item, Vector3 scale)
        {
            if (mount == null || item == null)
            {
                return;
            }

            var held = GameObject.CreatePrimitive(PrimitiveType.Cube);
            held.name = $"Held_{item.DisplayName}";
            held.transform.SetParent(mount, false);
            held.transform.localPosition = Vector3.zero;
            held.transform.localRotation = Quaternion.identity;
            held.transform.localScale = scale;

            var collider = held.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyHeldObject(collider);
            }
        }

        private static void DestroyHeldObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
                return;
            }

            DestroyImmediate(target);
        }
    }
}
