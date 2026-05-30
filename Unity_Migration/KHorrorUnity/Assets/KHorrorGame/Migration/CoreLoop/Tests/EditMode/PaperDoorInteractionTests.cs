using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class PaperDoorInteractionTests
    {
        private GameObject playerObject;
        private GameObject enemyObject;
        private GameObject targetObject;
        private GameObject doorObject;

        [TearDown]
        public void TearDown()
        {
            DestroyImmediate(playerObject);
            DestroyImmediate(enemyObject);
            DestroyImmediate(targetObject);
            DestroyImmediate(doorObject);
        }

        [Test]
        public void PlayerWithoutTalismanTearsPaperDoorOpen()
        {
            var player = CreatePlayer();
            var door = CreateDoor(Vector3.zero);

            ((IInteractable)door).Interact(player);

            Assert.AreEqual(PaperDoorState.Torn, door.State);
            Assert.IsFalse(door.BlocksLineOfSight);
            Assert.IsFalse(door.GetComponent<Collider>().enabled);
        }

        [Test]
        public void PlayerUsesHeldTalismanToSealDoorTemporarily()
        {
            var player = CreatePlayer();
            Assert.IsTrue(player.TryCollectArtifact(new ArtifactDefinition(
                "Door talisman",
                0,
                0.1f,
                0,
                new[] { PaperDoorInteraction.TalismanTag },
                1)));
            var door = CreateDoor(Vector3.zero);

            ((IInteractable)door).Interact(player);

            Assert.AreEqual(PaperDoorState.Sealed, door.State);
            Assert.AreEqual(0, player.Inventory.Items.Count);
            Assert.IsTrue(door.BlocksEnemyPassage(EnemyKind.Ghost));
            Assert.IsTrue(door.GetComponent<Collider>().enabled);

            door.ManualTick(PaperDoorInteraction.DefaultSealSeconds + 0.1f);

            Assert.AreEqual(PaperDoorState.Intact, door.State);
            Assert.IsTrue(door.BlocksLineOfSight);
        }

        [Test]
        public void SealedDoorBlocksEnemyBrainWithoutTakingDamage()
        {
            var target = CreateTarget(new Vector3(4f, 0f, 0f));
            var door = CreateDoor(Vector3.zero);
            door.SealForSeconds(10f);
            var brain = CreateEnemy(new Vector3(-4f, 0f, 0f), target);
            Physics.SyncTransforms();

            brain.ManualTick(1f, TerritoryKind.EstateInterior);

            Assert.AreEqual(PaperDoorState.Sealed, door.State);
            Assert.AreEqual(EnemyBrainState.Idle, brain.State);
            Assert.AreEqual(new Vector3(-4f, 0f, 0f), enemyObject.transform.position);
        }

        [Test]
        public void EnemyIgnoresPaperDoorWhenTargetIsOutsideDetectionRange()
        {
            var target = CreateTarget(new Vector3(80f, 0f, 0f));
            var door = CreateDoor(Vector3.zero);
            var brain = CreateEnemy(new Vector3(-4f, 0f, 0f), target);
            Physics.SyncTransforms();

            brain.ManualTick(1f, TerritoryKind.EstateInterior);

            Assert.AreEqual(PaperDoorState.Intact, door.State);
            Assert.AreEqual(EnemyBrainState.Idle, brain.State);
            Assert.AreEqual(new Vector3(-4f, 0f, 0f), enemyObject.transform.position);
        }

        [Test]
        public void IntactDoorTearsFromEnemyAttackBeforeEnemyCanPass()
        {
            var target = CreateTarget(new Vector3(4f, 0f, 0f));
            var door = CreateDoor(Vector3.zero);
            var brain = CreateEnemy(new Vector3(-1.6f, 0f, 0f), target);
            Physics.SyncTransforms();

            brain.ManualTick(1f, TerritoryKind.EstateInterior);

            Assert.AreEqual(PaperDoorState.Torn, door.State);
            Assert.AreEqual(EnemyBrainState.Attacking, brain.State);
            Assert.AreEqual(new Vector3(-1.6f, 0f, 0f), enemyObject.transform.position);

            brain.ManualTick(1f, TerritoryKind.EstateInterior);

            Assert.AreEqual(EnemyBrainState.Chasing, brain.State);
            Assert.Greater(enemyObject.transform.position.x, -1.6f);
        }

        private UnityPlayerController CreatePlayer()
        {
            playerObject = new GameObject("PaperDoorPlayer");
            playerObject.AddComponent<CharacterController>();
            var player = playerObject.AddComponent<UnityPlayerController>();
            InvokePrivate(player, "Awake");
            return player;
        }

        private Transform CreateTarget(Vector3 position)
        {
            targetObject = new GameObject("PaperDoorTarget");
            targetObject.transform.position = position;
            return targetObject.transform;
        }

        private EnemyBrain CreateEnemy(Vector3 position, Transform target)
        {
            enemyObject = new GameObject("PaperDoorEnemy");
            enemyObject.transform.position = position;
            var brain = enemyObject.AddComponent<EnemyBrain>();
            brain.Configure(
                EnemyKind.Ghost,
                ThreatStageProfile.ForStage(4),
                target,
                TerritoryKind.EstateInterior,
                position);
            return brain;
        }

        private PaperDoorInteraction CreateDoor(Vector3 position)
        {
            doorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorObject.name = "PaperDoorFixture";
            doorObject.transform.position = position;
            doorObject.transform.localScale = new Vector3(0.18f, 2.2f, 3f);
            return doorObject.AddComponent<PaperDoorInteraction>();
        }

        private static void InvokePrivate(object target, string methodName)
        {
            target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, null);
        }

        private static void DestroyImmediate(Object target)
        {
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
