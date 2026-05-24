using UnityEngine;

namespace KHorrorGame.Migration
{
    [RequireComponent(typeof(Collider))]
    public sealed class TerritoryVolume : MonoBehaviour
    {
        [SerializeField] private TerritoryKind territory = TerritoryKind.BongoHub;
        [SerializeField] private int priority;

        public TerritoryKind Territory
        {
            get { return territory; }
        }

        public int Priority
        {
            get { return priority; }
        }

        private void Reset()
        {
            EnsureTriggerCollider();
        }

        private void Awake()
        {
            EnsureTriggerCollider();
        }

        public void Configure(TerritoryKind newTerritory, int newPriority)
        {
            territory = newTerritory;
            priority = newPriority;
            EnsureTriggerCollider();
        }

        public void ConfigureForTests(TerritoryKind newTerritory, int newPriority)
        {
            Configure(newTerritory, newPriority);
        }

        private void EnsureTriggerCollider()
        {
            var ownCollider = GetComponent<Collider>();
            if (ownCollider != null)
            {
                ownCollider.isTrigger = true;
            }
        }
    }
}
