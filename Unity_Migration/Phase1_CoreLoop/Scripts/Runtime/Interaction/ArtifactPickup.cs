using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class ArtifactPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private string displayName = "Artifact";
        [SerializeField] private int value = 100;
        [SerializeField] private float weight = 1f;
        [SerializeField] private int resentmentGain = 1;
        [SerializeField] private string[] tags = new string[0];
        [SerializeField] private int handSlots = 1;
        [SerializeField] private bool countResentmentOnPickup = true;
        [SerializeField] private GameLoopController gameLoop;

        public string InteractionLabel => $"Pick up [E] {displayName}";

        private void Awake()
        {
            if (gameLoop == null)
            {
                gameLoop = FindObjectOfType<GameLoopController>();
            }
        }

        private void OnValidate()
        {
            value = Mathf.Max(value, 0);
            weight = Mathf.Max(weight, 0f);
            resentmentGain = Mathf.Max(resentmentGain, 0);
            handSlots = Mathf.Clamp(handSlots, 1, 2);
        }

        public bool CanInteract(UnityPlayerController actor)
        {
            return actor != null && actor.Inventory.FreeHandSlots() >= handSlots;
        }

        public void Interact(UnityPlayerController actor)
        {
            if (actor == null)
            {
                return;
            }

            var definition = BuildDefinition();
            if (!actor.TryCollectArtifact(definition))
            {
                return;
            }

            if (countResentmentOnPickup && gameLoop != null)
            {
                gameLoop.RegisterArtifactPicked(definition);
            }

            Destroy(gameObject);
        }

        public void ApplyDefinition(ArtifactDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            displayName = definition.DisplayName;
            value = definition.Value;
            weight = definition.Weight;
            resentmentGain = definition.ResentmentGain;
            handSlots = definition.HandSlots;
            tags = new string[definition.Tags.Count];

            for (var i = 0; i < definition.Tags.Count; i++)
            {
                tags[i] = definition.Tags[i];
            }
        }

        private ArtifactDefinition BuildDefinition()
        {
            return new ArtifactDefinition(
                displayName,
                value,
                weight,
                resentmentGain,
                tags,
                handSlots);
        }
    }
}
