using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class DokkaebiEnemy : EnemyController
    {
        [SerializeField] private float lurkDistance = 18f;
        [SerializeField] private float misdirectDistance = 7f;

        public DokkaebiEnemyState State { get; private set; } = DokkaebiEnemyState.Lurk;

        public override void Configure(
            EnemyBrain newBrain,
            Transform newTarget,
            TerritoryKind newHomeTerritory,
            Vector3 newHomePosition)
        {
            base.Configure(newBrain, newTarget, newHomeTerritory, newHomePosition);
            State = DokkaebiEnemyState.Lurk;
        }

        public override void ManualTick(float deltaSeconds, TerritoryKind targetTerritory)
        {
            var currentBrain = Brain;
            if (currentBrain == null)
            {
                State = DokkaebiEnemyState.Lurk;
                return;
            }

            if (!TryTrackTargetOrReturn(deltaSeconds, targetTerritory, out _, out var distance))
            {
                State = DokkaebiEnemyState.Retreat;
                return;
            }

            if (distance > lurkDistance)
            {
                State = DokkaebiEnemyState.Lurk;
                return;
            }

            if (distance > misdirectDistance)
            {
                State = DokkaebiEnemyState.Misdirect;
                currentBrain.ManualTick(deltaSeconds * 0.35f, targetTerritory);
                return;
            }

            State = DokkaebiEnemyState.BlockPath;
            currentBrain.ManualTick(deltaSeconds, targetTerritory);
        }
    }
}
