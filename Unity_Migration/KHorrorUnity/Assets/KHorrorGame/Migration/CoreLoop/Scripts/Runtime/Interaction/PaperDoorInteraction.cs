using UnityEngine;

namespace KHorrorGame.Migration
{
    public enum PaperDoorState
    {
        Intact,
        Torn,
        Sealed
    }

    [RequireComponent(typeof(Collider))]
    public sealed class PaperDoorInteraction : MonoBehaviour, IInteractable
    {
        public const string TalismanTag = "talisman";
        public const float DefaultSealSeconds = 18f;

        [SerializeField] private string displayName = "paper door";
        [SerializeField] private PaperDoorState state = PaperDoorState.Intact;
        [SerializeField] private float sealSeconds = DefaultSealSeconds;
        [SerializeField] private int tearIntegrity = 1;

        private int remainingIntegrity;
        private float sealRemainingSeconds;
        private Collider cachedCollider;

        public PaperDoorState State => state;
        public bool BlocksLineOfSight => state != PaperDoorState.Torn;
        public string InteractionLabel
        {
            get
            {
                switch (state)
                {
                    case PaperDoorState.Sealed:
                        return "[E] sealed paper door";
                    case PaperDoorState.Torn:
                        return "[E] torn paper door";
                    default:
                        return "[E] tear / seal " + displayName;
                }
            }
        }

        private void Awake()
        {
            EnsureInitialized();
            RefreshColliderState();
        }

        private void OnValidate()
        {
            sealSeconds = Mathf.Max(0.1f, sealSeconds);
            tearIntegrity = Mathf.Max(1, tearIntegrity);
        }

        private void Update()
        {
            ManualTick(Time.deltaTime);
        }

        public void Configure(string newDisplayName, float newSealSeconds = DefaultSealSeconds, int newTearIntegrity = 1)
        {
            displayName = string.IsNullOrEmpty(newDisplayName) ? displayName : newDisplayName;
            sealSeconds = Mathf.Max(0.1f, newSealSeconds);
            tearIntegrity = Mathf.Max(1, newTearIntegrity);
            remainingIntegrity = tearIntegrity;
            RefreshColliderState();
        }

        public bool CanInteract(UnityPlayerController actor)
        {
            return actor != null && state == PaperDoorState.Intact;
        }

        public void Interact(UnityPlayerController actor)
        {
            if (!CanInteract(actor))
            {
                return;
            }

            if (actor.Inventory.TryRemoveFirstByTag(TalismanTag, out _))
            {
                actor.RefreshHeldItemViews();
                SealForSeconds(sealSeconds);
                return;
            }

            TearOpen();
        }

        public void SealForSeconds(float seconds)
        {
            state = PaperDoorState.Sealed;
            sealRemainingSeconds = Mathf.Max(seconds, 0.1f);
            RefreshColliderState();
        }

        public void ManualTick(float deltaSeconds)
        {
            if (state != PaperDoorState.Sealed)
            {
                return;
            }

            sealRemainingSeconds -= Mathf.Max(deltaSeconds, 0f);
            if (sealRemainingSeconds > 0f)
            {
                return;
            }

            state = PaperDoorState.Intact;
            remainingIntegrity = Mathf.Max(tearIntegrity, 1);
            RefreshColliderState();
        }

        public bool BlocksEnemyPassage(EnemyKind enemyKind)
        {
            return state == PaperDoorState.Sealed || state == PaperDoorState.Intact;
        }

        public bool TryApplyMonsterAttack(EnemyKind enemyKind)
        {
            EnsureInitialized();
            if (state == PaperDoorState.Sealed)
            {
                return false;
            }

            if (state == PaperDoorState.Torn)
            {
                return true;
            }

            remainingIntegrity--;
            if (remainingIntegrity <= 0)
            {
                TearOpen();
            }

            return state == PaperDoorState.Torn;
        }

        public void TearOpen()
        {
            state = PaperDoorState.Torn;
            sealRemainingSeconds = 0f;
            RefreshColliderState();
        }

        private void EnsureInitialized()
        {
            cachedCollider = cachedCollider != null ? cachedCollider : GetComponent<Collider>();
            if (remainingIntegrity <= 0)
            {
                remainingIntegrity = Mathf.Max(tearIntegrity, 1);
            }
        }

        private void RefreshColliderState()
        {
            EnsureInitialized();
            if (cachedCollider != null)
            {
                cachedCollider.enabled = state != PaperDoorState.Torn;
            }
        }
    }
}
