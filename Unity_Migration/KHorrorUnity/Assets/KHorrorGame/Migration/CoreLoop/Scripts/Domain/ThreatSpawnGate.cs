using System;

namespace KHorrorGame.Migration
{
    [Serializable]
    public sealed class ThreatSpawnGate
    {
        public const float ShrineThreatGraceSeconds = 2f;

        public float RemainingGraceSeconds { get; private set; }
        public bool CanSpawnThreats
        {
            get { return RemainingGraceSeconds <= 0f; }
        }

        public void NotifyArtifactPicked(ArtifactDefinition definition)
        {
            if (definition != null && definition.HasTag("shrine_item"))
            {
                RemainingGraceSeconds = Math.Max(RemainingGraceSeconds, ShrineThreatGraceSeconds);
            }
        }

        public void Tick(float deltaSeconds)
        {
            if (RemainingGraceSeconds <= 0f)
            {
                return;
            }

            RemainingGraceSeconds = Math.Max(0f, RemainingGraceSeconds - Math.Max(deltaSeconds, 0f));
        }
    }
}
