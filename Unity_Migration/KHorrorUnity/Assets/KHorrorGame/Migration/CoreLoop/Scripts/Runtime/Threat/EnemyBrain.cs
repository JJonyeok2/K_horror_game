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
        private float patternTimerSeconds;
        private int patternStep;
        private int patternSeed;

        public EnemyBrainState State { get; private set; } = EnemyBrainState.Idle;
        public EnemyBrainPattern CurrentPattern { get; private set; } = EnemyBrainPattern.Direct;
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
            patternTimerSeconds = 0f;
            patternStep = 0;
            patternSeed = CreatePatternSeed(newEnemyKind, newStats, newHomePosition);
            CurrentPattern = EnemyBrainPattern.Direct;
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
                CurrentPattern = EnemyBrainPattern.Direct;
                return;
            }

            if (TryGetBlockingPaperDoor(transform.position, targetPosition, out var paperDoor, out var doorHitPoint))
            {
                CurrentPattern = EnemyBrainPattern.Direct;
                if (paperDoor.State == PaperDoorState.Intact)
                {
                    var distanceToDoor = Vector3.Distance(transform.position, doorHitPoint);
                    if (distanceToDoor <= stats.AttackRange + 0.15f)
                    {
                        paperDoor.TryApplyMonsterAttack(enemyKind);
                        State = EnemyBrainState.Attacking;
                        return;
                    }

                    State = EnemyBrainState.Chasing;
                    MoveToward(ApproachPointBeforeDoor(doorHitPoint), delta);
                    return;
                }

                State = EnemyBrainState.Idle;
                return;
            }

            if (distanceToTarget <= stats.AttackRange)
            {
                State = EnemyBrainState.Attacking;
                CurrentPattern = EnemyBrainPattern.Direct;
                TryAttackTarget();
                return;
            }

            State = EnemyBrainState.Chasing;
            MoveTowardWithPattern(targetPosition, delta);
            UpdatePattern(delta);
        }

        private void ReturnHome(float deltaSeconds)
        {
            CurrentPattern = EnemyBrainPattern.Direct;
            var destination = FlattenToSelfHeight(homePosition);
            if (Vector3.Distance(transform.position, destination) <= returnArrivalDistance)
            {
                State = EnemyBrainState.Idle;
                return;
            }

            State = EnemyBrainState.Returning;
            MoveToward(destination, deltaSeconds);
        }

        private void MoveTowardWithPattern(Vector3 targetPosition, float deltaSeconds)
        {
            var directionToTarget = targetPosition - transform.position;
            if (directionToTarget.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var direction = directionToTarget.normalized;
            switch (CurrentPattern)
            {
                case EnemyBrainPattern.Pause:
                    return;
                case EnemyBrainPattern.Burst:
                    MoveToward(targetPosition, deltaSeconds * 1.65f);
                    return;
                case EnemyBrainPattern.FeintRetreat:
                    MoveToward(transform.position - direction * Mathf.Max(0.8f, stats.AttackRange), deltaSeconds * 0.8f);
                    return;
                case EnemyBrainPattern.SideStep:
                    var sideSign = patternStep % 2 == 0 ? 1f : -1f;
                    var side = new Vector3(-direction.z, 0f, direction.x) * sideSign;
                    MoveToward(transform.position + side * 1.6f + direction * 0.25f, deltaSeconds);
                    return;
                default:
                    MoveToward(targetPosition, deltaSeconds);
                    return;
            }
        }

        private void UpdatePattern(float deltaSeconds)
        {
            if (stats.UnlockedPatternCount <= 1)
            {
                CurrentPattern = EnemyBrainPattern.Direct;
                return;
            }

            patternTimerSeconds -= deltaSeconds;
            if (patternTimerSeconds > 0f)
            {
                return;
            }

            var index = Mathf.Abs(patternSeed + patternStep * 73 + Mathf.RoundToInt(stats.PatternVariance * 100f))
                % stats.UnlockedPatternCount;
            CurrentPattern = (EnemyBrainPattern)index;
            patternStep++;
            patternTimerSeconds = Mathf.Lerp(2.2f, 0.85f, stats.PatternVariance);
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
            var nextPosition = Vector3.MoveTowards(transform.position, destination, step);
            if (TryGetBlockingPaperDoor(transform.position, nextPosition, out _, out _))
            {
                return;
            }

            transform.position = nextPosition;
        }

        private Vector3 ApproachPointBeforeDoor(Vector3 doorHitPoint)
        {
            var directionToDoor = doorHitPoint - transform.position;
            if (directionToDoor.sqrMagnitude <= 0.0001f)
            {
                return transform.position;
            }

            var stoppingDistance = Mathf.Max(0.35f, stats.AttackRange * 0.8f);
            return doorHitPoint - directionToDoor.normalized * stoppingDistance;
        }

        private Vector3 FlattenToSelfHeight(Vector3 position)
        {
            return new Vector3(position.x, transform.position.y, position.z);
        }

        private bool TryGetBlockingPaperDoor(
            Vector3 from,
            Vector3 to,
            out PaperDoorInteraction door,
            out Vector3 hitPoint)
        {
            door = null;
            hitPoint = to;
            var direction = to - from;
            var distance = direction.magnitude;
            if (distance <= 0.001f)
            {
                return false;
            }

            var hits = Physics.RaycastAll(
                from,
                direction / distance,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);

            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (var hit in hits)
            {
                var candidate = hit.collider.GetComponentInParent<PaperDoorInteraction>();
                if (candidate == null || !candidate.BlocksLineOfSight || !candidate.BlocksEnemyPassage(enemyKind))
                {
                    continue;
                }

                door = candidate;
                hitPoint = hit.point;
                return true;
            }

            return false;
        }

        private static int CreatePatternSeed(EnemyKind kind, EnemyStats stats, Vector3 home)
        {
            return Mathf.Abs(
                Mathf.RoundToInt(home.x * 17f) ^
                Mathf.RoundToInt(home.z * 31f) ^
                ((int)kind * 101) ^
                (stats.DamagePerHit * 7));
        }
    }
}
