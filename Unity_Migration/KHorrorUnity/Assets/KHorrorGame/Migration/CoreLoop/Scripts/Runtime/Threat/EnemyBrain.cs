using System;
using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class EnemyBrain : MonoBehaviour
    {
        [SerializeField] private EnemyKind enemyKind = EnemyKind.Ghost;
        [SerializeField] private TerritoryKind homeTerritory = TerritoryKind.EstateInterior;
        [SerializeField] private Transform target;
        [SerializeField] private float returnArrivalDistance = 0.25f;
        [SerializeField] private bool automaticTick = true;

        private readonly EnemyTerritoryRules territoryRules = EnemyTerritoryRules.Default;
        private EnemyStats stats;
        private Vector3 homePosition;
        private PlayerDamageReceiver targetDamageReceiver;
        private float attackCooldownSeconds;

        public EnemyBrainState State { get; private set; } = EnemyBrainState.Idle;
        public EnemyKind EnemyKind
        {
            get { return enemyKind; }
        }

        public TerritoryKind HomeTerritory
        {
            get { return homeTerritory; }
        }

        public EnemyStats Stats
        {
            get { return stats; }
        }

        private void Awake()
        {
            homePosition = transform.position;
            if (stats == null)
            {
                stats = EnemyStats.FromProfile(enemyKind, ThreatStageProfile.ForStage(3));
            }
        }

        private void Update()
        {
            if (automaticTick)
            {
                ManualTick(Time.deltaTime, homeTerritory);
            }
        }

        public void SetAutomaticTick(bool enabled)
        {
            automaticTick = enabled;
        }

        public void Configure(
            EnemyKind newEnemyKind,
            ThreatStageProfile profile,
            Transform newTarget,
            TerritoryKind newHomeTerritory,
            Vector3 newHomePosition)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            Configure(
                newEnemyKind,
                EnemyStats.FromProfile(newEnemyKind, profile),
                newTarget,
                newHomeTerritory,
                newHomePosition);
        }

        public void Configure(
            EnemyKind newEnemyKind,
            EnemyStats newStats,
            Transform newTarget,
            TerritoryKind newHomeTerritory,
            Vector3 newHomePosition)
        {
            enemyKind = newEnemyKind;
            stats = newStats ?? throw new ArgumentNullException(nameof(newStats));
            target = newTarget;
            homeTerritory = newHomeTerritory;
            homePosition = newHomePosition;
            targetDamageReceiver = target != null ? target.GetComponent<PlayerDamageReceiver>() : null;
            attackCooldownSeconds = 0f;
            State = EnemyBrainState.Idle;
        }

        public void ManualTick(float deltaSeconds, TerritoryKind targetTerritory)
        {
            if (stats == null)
            {
                stats = EnemyStats.FromProfile(enemyKind, ThreatStageProfile.ForStage(3));
            }

            var delta = Mathf.Max(0f, deltaSeconds);
            attackCooldownSeconds = Mathf.Max(0f, attackCooldownSeconds - delta);

            if (target == null)
            {
                ReturnHome(delta);
                return;
            }

            if (!territoryRules.CanEnter(enemyKind, targetTerritory))
            {
                ReturnHome(delta);
                return;
            }

            var targetPosition = FlattenToSelfHeight(target.position);
            var distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            if (distanceToTarget > stats.DetectionRange)
            {
                State = EnemyBrainState.Idle;
                return;
            }

            if (distanceToTarget <= stats.AttackRange)
            {
                State = EnemyBrainState.Attacking;
                TryAttackTarget();
                return;
            }

            State = EnemyBrainState.Chasing;
            MoveToward(targetPosition, delta);
        }

        private void ReturnHome(float deltaSeconds)
        {
            var destination = FlattenToSelfHeight(homePosition);
            if (Vector3.Distance(transform.position, destination) <= returnArrivalDistance)
            {
                State = EnemyBrainState.Idle;
                return;
            }

            State = EnemyBrainState.Returning;
            MoveToward(destination, deltaSeconds);
        }

        private void TryAttackTarget()
        {
            if (attackCooldownSeconds > 0f || stats.DamagePerHit <= 0)
            {
                return;
            }

            if (targetDamageReceiver == null && target != null)
            {
                targetDamageReceiver = target.GetComponent<PlayerDamageReceiver>();
            }

            if (targetDamageReceiver != null && targetDamageReceiver.ApplyDamage(stats.DamagePerHit, enemyKind))
            {
                attackCooldownSeconds = stats.AttackIntervalSeconds;
            }
        }

        private void MoveToward(Vector3 destination, float deltaSeconds)
        {
            var step = stats.MoveSpeed * deltaSeconds;
            transform.position = Vector3.MoveTowards(transform.position, destination, step);
        }

        private Vector3 FlattenToSelfHeight(Vector3 position)
        {
            return new Vector3(position.x, transform.position.y, position.z);
        }
    }
}
