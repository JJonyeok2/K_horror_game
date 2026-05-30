using NUnit.Framework;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class GhostEnemyTests
    {
        private GameObject ghostObject;
        private GameObject playerObject;

        [TearDown]
        public void TearDown()
        {
            if (ghostObject != null)
            {
                Object.DestroyImmediate(ghostObject);
            }

            if (playerObject != null)
            {
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void GhostStartsDormantThenHauntsFarInteriorTarget()
        {
            var target = CreatePlayer(new Vector3(40f, 0f, 70f));
            var ghost = CreateGhost(Vector3.zero, target);

            Assert.AreEqual(GhostEnemyState.Dormant, ghost.State);

            ghost.ManualTick(0.5f, TerritoryKind.EstateInterior);

            Assert.AreEqual(GhostEnemyState.Haunt, ghost.State);
            Assert.AreEqual(Vector3.zero, ghostObject.transform.position);
        }

        [Test]
        public void GhostUsesInvestigateStalkAndChaseBandsInsideEstate()
        {
            var target = CreatePlayer(new Vector3(16f, 0f, 0f));
            var ghost = CreateGhost(Vector3.zero, target);

            ghost.ManualTick(0.5f, TerritoryKind.EstateInterior);
            Assert.AreEqual(GhostEnemyState.Investigate, ghost.State);

            target.position = new Vector3(9f, 0f, 0f);
            ghost.ManualTick(0.5f, TerritoryKind.EstateInterior);
            Assert.AreEqual(GhostEnemyState.Stalk, ghost.State);

            target.position = new Vector3(2f, 0f, 0f);
            ghost.ManualTick(0.5f, TerritoryKind.EstateInterior);
            Assert.AreEqual(GhostEnemyState.Chase, ghost.State);
            Assert.Greater(ghostObject.transform.position.x, 0f);
        }

        [Test]
        public void GhostReturnsHomeAndDespawnsWhenPlayerLeavesThroughFrontGate()
        {
            var target = CreatePlayer(new Vector3(0f, 0f, 40f));
            var ghost = CreateGhost(new Vector3(0f, 0f, 58f), target, new Vector3(0f, 0f, 80f));

            ghost.ManualTick(1f, TerritoryKind.ForestApproach);

            Assert.AreEqual(GhostEnemyState.ReturnHome, ghost.State);
            Assert.Greater(ghostObject.transform.position.z, 58f);

            for (var i = 0; i < 20; i++)
            {
                ghost.ManualTick(1f, TerritoryKind.ForestApproach);
            }

            Assert.AreEqual(GhostEnemyState.Despawn, ghost.State);
            Assert.IsFalse(ghostObject.activeSelf);
        }

        [Test]
        public void GhostNeverMovesTowardForestTargetPastGateBoundary()
        {
            var target = CreatePlayer(new Vector3(0f, 0f, 36f));
            var ghost = CreateGhost(new Vector3(0f, 0f, 56f), target, new Vector3(0f, 0f, 78f));

            ghost.ManualTick(1f, TerritoryKind.ForestApproach);

            Assert.AreEqual(GhostEnemyState.ReturnHome, ghost.State);
            Assert.GreaterOrEqual(ghostObject.transform.position.z, 56f);
        }

        [Test]
        public void GhostUsesBaseControllerTrackingAndTerritoryState()
        {
            var target = CreatePlayer(new Vector3(2f, 0f, 0f));
            var ghost = CreateGhost(Vector3.zero, target);

            ghost.ManualTick(0.25f, TerritoryKind.EstateInterior);

            Assert.AreEqual(EnemyControllerState.Tracking, ghost.ControllerState);
            Assert.IsTrue(ghost.TargetTerritoryAllowed);
            Assert.Less(ghost.TargetDistance, 3f);

            ghost.ManualTick(0.25f, TerritoryKind.ForestApproach);

            Assert.AreEqual(EnemyControllerState.ReturnHome, ghost.ControllerState);
            Assert.IsFalse(ghost.TargetTerritoryAllowed);
            Assert.AreEqual(GhostEnemyState.ReturnHome, ghost.State);
        }

        private Transform CreatePlayer(Vector3 position)
        {
            playerObject = new GameObject("GhostTarget");
            playerObject.transform.position = position;
            return playerObject.transform;
        }

        private GhostEnemy CreateGhost(Vector3 position, Transform target)
        {
            return CreateGhost(position, target, position);
        }

        private GhostEnemy CreateGhost(Vector3 position, Transform target, Vector3 homePosition)
        {
            ghostObject = new GameObject("GhostEnemyFixture");
            ghostObject.transform.position = position;
            var brain = ghostObject.AddComponent<EnemyBrain>();
            brain.Configure(EnemyKind.Ghost, ThreatStageProfile.ForStage(4), target, TerritoryKind.EstateInterior, homePosition);
            brain.SetAutomaticTick(false);

            var ghost = ghostObject.AddComponent<GhostEnemy>();
            ghost.Configure(brain, target, TerritoryKind.EstateInterior, homePosition);
            ghost.SetAutomaticTick(false);
            return ghost;
        }
    }
}
