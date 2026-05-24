using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class VanCargoItem : MonoBehaviour
    {
        public ArtifactDefinition Definition { get; private set; }
        public VanCargoHold CargoHold { get; private set; }

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
    }
}
