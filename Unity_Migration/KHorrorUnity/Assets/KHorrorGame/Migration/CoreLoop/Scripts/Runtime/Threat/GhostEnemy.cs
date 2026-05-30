using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class GhostEnemy : EnemyController
    {
        [SerializeField] private float hauntDistance = 28f;
        [SerializeField] private float investigateDistance = 18f;
        [SerializeField] private float stalkDistance = 10f;
        [SerializeField] private float chaseDistance = 4f;

        public GhostEnemyState State { get; private set; } = GhostEnemyState.Dormant;

        public override void Configure(
            EnemyBrain newBrain,
            Transform newTarget,
            TerritoryKind newHomeTerritory,
            Vector3 newHomePosition)
        {
            base.Configure(newBrain, newTarget, newHomeTerritory, newHomePosition);
            State = GhostEnemyState.Dormant;
        }

        public override void ManualTick(float deltaSeconds, TerritoryKind targetTerritory)
        {
            var currentBrain = Brain;
            if (currentBrain == null)
            {
                State = GhostEnemyState.Dormant;
                return;
            }

            if (!TryTrackTargetOrReturn(deltaSeconds, targetTerritory, out _, out var distance))
            {
                State = StateForControllerReturn();
                return;
            }

            if (distance > hauntDistance)
            {
                State = GhostEnemyState.Haunt;
                return;
            }

            if (distance > investigateDistance)
            {
                State = GhostEnemyState.Investigate;
                return;
            }

            if (distance > stalkDistance)
            {
                State = GhostEnemyState.Investigate;
                currentBrain.ManualTick(deltaSeconds * 0.35f, targetTerritory);
                return;
            }

            if (distance > chaseDistance)
            {
                State = GhostEnemyState.Stalk;
                currentBrain.ManualTick(deltaSeconds * 0.65f, targetTerritory);
                return;
            }

            State = GhostEnemyState.Chase;
            currentBrain.ManualTick(deltaSeconds, targetTerritory);
        }

        private GhostEnemyState StateForControllerReturn()
        {
            switch (ControllerState)
            {
                case EnemyControllerState.ReturnHome:
                    return GhostEnemyState.ReturnHome;
                case EnemyControllerState.Despawn:
                    return GhostEnemyState.Despawn;
                default:
                    return GhostEnemyState.Dormant;
            }
        }
    }
}
