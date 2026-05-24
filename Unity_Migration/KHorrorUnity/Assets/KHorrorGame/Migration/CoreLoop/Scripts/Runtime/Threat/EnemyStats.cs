using System;
using UnityEngine;

namespace KHorrorGame.Migration
{
    [Serializable]
    public sealed class EnemyStats
    {
        public EnemyStats(
            float detectionRange,
            float moveSpeed,
            float attackRange,
            float attackIntervalSeconds,
            int damagePerHit,
            float patternVariance)
        {
            DetectionRange = Mathf.Max(0f, detectionRange);
            MoveSpeed = Mathf.Max(0f, moveSpeed);
            AttackRange = Mathf.Max(0f, attackRange);
            AttackIntervalSeconds = Mathf.Max(0.1f, attackIntervalSeconds);
            DamagePerHit = Math.Max(0, damagePerHit);
            PatternVariance = Mathf.Clamp01(patternVariance);
        }

        public float DetectionRange { get; }
        public float MoveSpeed { get; }
        public float AttackRange { get; }
        public float AttackIntervalSeconds { get; }
        public int DamagePerHit { get; }
        public float PatternVariance { get; }
        public int UnlockedPatternCount
        {
            get
            {
                if (PatternVariance >= 0.6f)
                {
                    return 5;
                }

                if (PatternVariance >= 0.4f)
                {
                    return 4;
                }

                if (PatternVariance >= 0.2f)
                {
                    return 3;
                }

                if (PatternVariance >= 0.1f)
                {
                    return 2;
                }

                return 1;
            }
        }

        public static EnemyStats FromProfile(EnemyKind enemyKind, ThreatStageProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var speed = profile.PursuitSpeed > 0f ? profile.PursuitSpeed : 1.8f;
            var attackRange = profile.AttackRange > 0f ? profile.AttackRange : 1.35f;
            var attackInterval = profile.AttackIntervalSeconds > 0f ? profile.AttackIntervalSeconds : 3f;
            var detectionRange = 14f + profile.Stage * 5f;

            if (enemyKind == EnemyKind.Dokkaebi)
            {
                detectionRange += 5f;
                speed += 0.35f;
                attackRange += 0.15f;
            }

            return new EnemyStats(
                detectionRange,
                speed,
                attackRange,
                attackInterval,
                profile.DamagePerHit,
                profile.PatternVariance);
        }
    }
}
