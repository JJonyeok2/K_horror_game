using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class VanCargoItem : MonoBehaviour, IInteractable
    {
        public ArtifactDefinition Definition { get; private set; }
        public VanCargoHold CargoHold { get; private set; }
        public string LastFeedbackMessage { get; private set; } = string.Empty;

        public string InteractionLabel
        {
            get
            {
                var name = Definition != null && !string.IsNullOrWhiteSpace(Definition.DisplayName)
                    ? Definition.DisplayName
                    : "Cargo";
                return "[E] 화물 다시 들기 - " + name;
            }
        }

        public int Value
        {
            get { return Definition != null ? Definition.Value : 0; }
        }

        public bool IsLoadedInHold
        {
            get { return CargoHold != null; }
        }

        public void Initialize(ArtifactDefinition definition, VanCargoHold cargoHold)
        {
            Definition = definition;
            CargoHold = cargoHold;
        }

        public void InitializeLoose(ArtifactDefinition definition)
        {
            Initialize(definition, null);
        }

        public bool CanInteract(UnityPlayerController actor)
        {
            return actor != null && CargoHold != null && Definition != null;
        }

        public void Interact(UnityPlayerController actor)
        {
            if (CargoHold == null)
            {
                SetFeedback("Cargo hold missing");
                return;
            }

            if (!CargoHold.TryReleaseToInventory(this, actor, out var failureReason))
            {
                SetFeedback(failureReason);
                return;
            }

            SetFeedback("Cargo picked up");
        }

        private void SetFeedback(string message)
        {
            LastFeedbackMessage = string.IsNullOrWhiteSpace(message) ? "Cannot pick up cargo" : message;
            var gameLoop = FindObjectOfType<GameLoopController>();
            if (gameLoop != null)
            {
                gameLoop.ShowFeedback(LastFeedbackMessage);
            }
        }
    }
}
