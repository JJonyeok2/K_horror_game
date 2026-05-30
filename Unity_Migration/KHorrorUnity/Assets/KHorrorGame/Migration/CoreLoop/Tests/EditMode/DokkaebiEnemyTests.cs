using NUnit.Framework;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class DokkaebiEnemyTests
    {
        private GameObject dokkaebiObject;
        private GameObject playerObject;

        [TearDown]
        public void TearDown()
        {
            if (dokkaebiObject != null)
            {
                Object.DestroyImmediate(dokkaebiObject);
            }

            if (playerObject != null)
            {
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void DokkaebiLurksWhenForestTargetIsFarAway()
        {
            var target = CreatePlayer(new Vector3(22f, 0f, 36f));
            var dokkaebi = CreateDokkaebi(new Vector3(0f, 0f, 36f), target);

            dokkaebi.ManualTick(0.5f, TerritoryKind.ForestApproach);

            Assert.AreEqual(DokkaebiEnemyState.Lurk, dokkaebi.State);
            Assert.AreEqual(new Vector3(0f, 0f, 36f), dokkaebiObject.transform.position);
        }

        [Test]
        public void DokkaebiMisdirectsThenBlocksPathInsideForest()
        {
            var target = CreatePlayer(new Vector3(12f, 0f, 36f));
            var dokkaebi = CreateDokkaebi(new Vector3(0f, 0f, 36f), target);

            dokkaebi.ManualTick(0.5f, TerritoryKind.ForestApproach);
            Assert.AreEqual(DokkaebiEnemyState.Misdirect, dokkaebi.State);

            target.position = new Vector3(4f, 0f, 36f);
            dokkaebi.ManualTick(0.5f, TerritoryKind.ForestApproach);

            Assert.AreEqual(DokkaebiEnemyState.BlockPath, dokkaebi.State);
            Assert.Greater(dokkaebiObject.transform.position.x, 0f);
            Assert.AreEqual(EnemyControllerState.Tracking, dokkaebi.ControllerState);
            Assert.IsTrue(dokkaebi.TargetTerritoryAllowed);
        }

        [Test]
        public void DokkaebiRetreatsAndDespawnsWhenPlayerEntersEstateInterior()
        {
            var target = CreatePlayer(new Vector3(0f, 0f, 64f));
            var dokkaebi = CreateDokkaebi(new Vector3(0f, 0f, 52f), target, new Vector3(0f, 0f, 34f));

            dokkaebi.ManualTick(1f, TerritoryKind.EstateInterior);

            Assert.AreEqual(DokkaebiEnemyState.Retreat, dokkaebi.State);
            Assert.IsFalse(dokkaebi.TargetTerritoryAllowed);
            Assert.LessOrEqual(dokkaebiObject.transform.position.z, 52f);

            for (var i = 0; i < 24; i++)
            {
                dokkaebi.ManualTick(1f, TerritoryKind.EstateInterior);
            }

            Assert.AreEqual(DokkaebiEnemyState.Retreat, dokkaebi.State);
            Assert.AreEqual(EnemyControllerState.Despawn, dokkaebi.ControllerState);
            Assert.IsFalse(dokkaebiObject.activeSelf);
        }

        [Test]
        public void DokkaebiDespawnStateStaysStableAfterManualInactiveTick()
        {
            var target = CreatePlayer(new Vector3(0f, 0f, 64f));
            var dokkaebi = CreateDokkaebi(new Vector3(0f, 0f, 34.1f), target, new Vector3(0f, 0f, 34f));

            dokkaebi.ManualTick(1f, TerritoryKind.EstateInterior);
            dokkaebi.ManualTick(1f, TerritoryKind.EstateInterior);
            Assert.AreEqual(EnemyControllerState.Despawn, dokkaebi.ControllerState);

            dokkaebi.ManualTick(1f, TerritoryKind.EstateInterior);

            Assert.AreEqual(EnemyControllerState.Despawn, dokkaebi.ControllerState);
            Assert.AreEqual(DokkaebiEnemyState.Retreat, dokkaebi.State);
            Assert.IsFalse(dokkaebiObject.activeSelf);
        }

        private Transform CreatePlayer(Vector3 position)
        {
            playerObject = new GameObject("DokkaebiTarget");
            playerObject.transform.position = position;
            return playerObject.transform;
        }

        private DokkaebiEnemy CreateDokkaebi(Vector3 position, Transform target)
        {
            return CreateDokkaebi(position, target, position);
        }

        private DokkaebiEnemy CreateDokkaebi(Vector3 position, Transform target, Vector3 homePosition)
        {
            dokkaebiObject = new GameObject("DokkaebiEnemyFixture");
            dokkaebiObject.transform.position = position;
            var brain = dokkaebiObject.AddComponent<EnemyBrain>();
            brain.Configure(EnemyKind.Dokkaebi, ThreatStageProfile.ForStage(4), target, TerritoryKind.ForestApproach, homePosition);
            brain.SetAutomaticTick(false);

            var dokkaebi = dokkaebiObject.AddComponent<DokkaebiEnemy>();
            dokkaebi.Configure(brain, target, TerritoryKind.ForestApproach, homePosition);
            dokkaebi.SetAutomaticTick(false);
            return dokkaebi;
        }
    }
}
