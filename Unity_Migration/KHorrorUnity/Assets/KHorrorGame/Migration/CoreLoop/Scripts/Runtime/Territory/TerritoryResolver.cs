using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class TerritoryResolver : MonoBehaviour
    {
        [SerializeField] private TerritoryKind fallbackTerritory = TerritoryKind.BongoHub;
        [SerializeField] private float pointProbeRadius = 0.05f;
        [SerializeField] private LayerMask probeMask = ~0;

        public TerritoryKind FallbackTerritory
        {
            get { return fallbackTerritory; }
        }

        public TerritoryKind ResolveAt(Vector3 worldPosition)
        {
            var hits = Physics.OverlapSphere(
                worldPosition,
                Mathf.Max(0.01f, pointProbeRadius),
                probeMask,
                QueryTriggerInteraction.Collide);
            return ResolveHits(hits, null);
        }

        public TerritoryKind ResolveCollider(Collider targetCollider)
        {
            if (targetCollider == null)
            {
                return fallbackTerritory;
            }

            var bounds = targetCollider.bounds;
            var hits = Physics.OverlapBox(
                bounds.center,
                bounds.extents,
                targetCollider.transform.rotation,
                probeMask,
                QueryTriggerInteraction.Collide);
            return ResolveHits(hits, targetCollider);
        }

        public void SetFallbackTerritoryForTests(TerritoryKind territory)
        {
            fallbackTerritory = territory;
        }

        private TerritoryKind ResolveHits(Collider[] hits, Collider ignoredCollider)
        {
            TerritoryVolume selectedVolume = null;
            var selectedPriority = int.MinValue;
            var hasPriorityTie = false;

            foreach (var hit in hits)
            {
                if (hit == null || hit == ignoredCollider)
                {
                    continue;
                }

                var volume = hit.GetComponentInParent<TerritoryVolume>();
                if (volume == null)
                {
                    continue;
                }

                if (selectedVolume == null || volume.Priority > selectedPriority)
                {
                    selectedVolume = volume;
                    selectedPriority = volume.Priority;
                    hasPriorityTie = false;
                    continue;
                }

                if (volume != selectedVolume && volume.Priority == selectedPriority)
                {
                    hasPriorityTie = true;
                }
            }

            if (selectedVolume == null || hasPriorityTie)
            {
                return fallbackTerritory;
            }

            return selectedVolume.Territory;
        }
    }
}
