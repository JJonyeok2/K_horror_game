using NUnit.Framework;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class EnemyBrainTests
    {
        private GameObject enemyObject;
        private GameObject playerObject;

        [TearDown]
        public void TearDown()
        {
            if (enemyObject != null)
            {
                Object.DestroyImmediate(enemyObject);
            }

            if (playerObject != null)
            {
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void EnemyStaysIdleWhenTargetIsOutsideDetectionRange()
        {
            var target = CreatePlayer(new Vector3(80f, 0f, 0f), withHealth: false);
            var brain = CreateEnemy(Vector3.zero, EnemyKind.Ghost, ThreatStageProfile.ForStage(4), target);

            brain.ManualTick(1f, TerritoryKind.EstateInterior);

            Assert.AreEqual(EnemyBrainState.Idle, brain.State);
            Assert.AreEqual(Vector3.zero, enemyObject.transform.position);
        }

        [Test]
        public void EnemyChasesDetectedTargetInsideAllowedTerritory()
        {
            var target = CreatePlayer(new Vector3(8f, 0f, 0f), withHealth: false);
            var brain = CreateEnemy(Vector3.zero, EnemyKind.Ghost, ThreatStageProfile.ForStage(4), target);

            brain.ManualTick(1f, TerritoryKind.EstateInterior);

            Assert.AreEqual(EnemyBrainState.Chasing, brain.State);
            Assert.Greater(enemyObject.transform.position.x, 0f);
        }

        [Test]
        public void EnemyAttackDamagesPlayerAndRespectsCooldown()
        {
            var target = CreatePlayer(new Vector3(1.2f, 0f, 0f), withHealth: true);
            var receiver = target.GetComponent<PlayerDamageReceiver>();
            var brain = CreateEnemy(Vector3.zero, EnemyKind.Ghost, ThreatStageProfile.ForStage(4), target);

            brain.ManualTick(0.1f, TerritoryKind.EstateInterior);
            brain.ManualTick(0.1f, TerritoryKind.EstateInterior);

            Assert.AreEqual(EnemyBrainState.Attacking, brain.State);
            Assert.AreEqual(80, receiver.CurrentHealth);

            brain.ManualTick(2f, TerritoryKind.EstateInterior);

            Assert.AreEqual(60, receiver.CurrentHealth);
        }

        [Test]
        public void EnemyReturnsHomeWhenTargetMovesIntoForbiddenTerritory()
        {
            var target = CreatePlayer(new Vector3(4f, 0f, 0f), withHealth: false);
            var brain = CreateEnemy(
                new Vector3(3f, 0f, 0f),
                EnemyKind.Ghost,
                ThreatStageProfile.ForStage(4),
                target,
                Vector3.zero);

            brain.ManualTick(1f, TerritoryKind.ForestApproach);

            Assert.AreEqual(EnemyBrainState.Returning, brain.State);
            Assert.Less(enemyObject.transform.position.x, 3f);
        }

        [Test]
        public void EnemyStatsScaleWithThreatStage()
        {
            var stageThree = EnemyStats.FromProfile(EnemyKind.Dokkaebi, ThreatStageProfile.ForStage(3));
            var stageFive = EnemyStats.FromProfile(EnemyKind.Dokkaebi, ThreatStageProfile.ForStage(5));

            Assert.Greater(stageFive.DamagePerHit, stageThree.DamagePerHit);
            Assert.Greater(stageFive.MoveSpeed, stageThree.MoveSpeed);
            Assert.Greater(stageFive.AttackRange, stageThree.AttackRange);
            Assert.Greater(stageFive.PatternVariance, stageThree.PatternVariance);
            Assert.Less(stageFive.AttackIntervalSeconds, stageThree.AttackIntervalSeconds);
        }

        private Transform CreatePlayer(Vector3 position, bool withHealth)
        {
            playerObject = new GameObject("TestPlayer");
            playerObject.transform.position = position;

            if (withHealth)
            {
                var receiver = playerObject.AddComponent<PlayerDamageReceiver>();
                receiver.ResetHealth(100);
            }

            return playerObject.transform;
        }

        private EnemyBrain CreateEnemy(
            Vector3 position,
            EnemyKind enemyKind,
            ThreatStageProfile profile,
            Transform target,
            Vector3? homePosition = null)
        {
            enemyObject = new GameObject("TestEnemy");
            enemyObject.transform.position = position;
            var brain = enemyObject.AddComponent<EnemyBrain>();
            brain.Configure(enemyKind, profile, target, TerritoryKind.EstateInterior, homePosition ?? position);
            return brain;
        }
    }
}
