using NUnit.Framework;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class ThreatAudioOcclusionTests
    {
        private GameObject enemyObject;
        private GameObject listenerObject;
        private GameObject blockerObject;

        [TearDown]
        public void TearDown()
        {
            DestroyImmediate(enemyObject);
            DestroyImmediate(listenerObject);
            DestroyImmediate(blockerObject);
        }

        [Test]
        public void ClearLineKeepsThreatAudioReadable()
        {
            var enemy = CreateEnemy(Vector3.zero, EnemyKind.Ghost, TerritoryKind.EstateInterior);
            var listener = CreateListener(new Vector3(5f, 0f, 0f));
            var source = enemyObject.AddComponent<AudioSource>();
            var filter = enemyObject.AddComponent<AudioLowPassFilter>();
            var occlusion = enemyObject.AddComponent<ThreatAudioOcclusion>();
            occlusion.Configure(enemy, listener, source, filter);
            Physics.SyncTransforms();

            occlusion.ManualRefresh();

            Assert.IsFalse(occlusion.IsOccluded);
            Assert.Greater(source.volume, 0.55f);
            Assert.Greater(filter.cutoffFrequency, 10000f);
            Assert.AreEqual("estate_ghost_presence", occlusion.CurrentCueLabel);
            Assert.IsNotNull(source.clip);
            StringAssert.Contains("estate_ghost_presence", source.clip.name);
        }

        [Test]
        public void WallBetweenPlayerAndThreatMufflesAudio()
        {
            var enemy = CreateEnemy(Vector3.zero, EnemyKind.Ghost, TerritoryKind.EstateInterior);
            var listener = CreateListener(new Vector3(5f, 0f, 0f));
            var source = enemyObject.AddComponent<AudioSource>();
            var filter = enemyObject.AddComponent<AudioLowPassFilter>();
            var occlusion = enemyObject.AddComponent<ThreatAudioOcclusion>();
            occlusion.Configure(enemy, listener, source, filter);
            blockerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blockerObject.name = "AudioOcclusionWall";
            blockerObject.transform.position = new Vector3(2.5f, 0f, 0f);
            blockerObject.transform.localScale = new Vector3(0.4f, 3f, 3f);
            Physics.SyncTransforms();

            occlusion.ManualRefresh();

            Assert.IsTrue(occlusion.IsOccluded);
            Assert.Less(source.volume, 0.35f);
            Assert.Less(filter.cutoffFrequency, 2500f);
        }

        [Test]
        public void DokkaebiForestThreatUsesSeparateCueLabel()
        {
            var enemy = CreateEnemy(Vector3.zero, EnemyKind.Dokkaebi, TerritoryKind.ForestApproach);
            var listener = CreateListener(new Vector3(3f, 0f, 0f));
            var occlusion = enemyObject.AddComponent<ThreatAudioOcclusion>();
            occlusion.Configure(
                enemy,
                listener,
                enemyObject.GetComponent<AudioSource>(),
                enemyObject.GetComponent<AudioLowPassFilter>());

            occlusion.ManualRefresh();

            Assert.AreEqual("forest_dokkaebi_presence", occlusion.CurrentCueLabel);
            Assert.IsNotNull(enemyObject.GetComponent<AudioSource>().clip);
            StringAssert.Contains("forest_dokkaebi_presence", enemyObject.GetComponent<AudioSource>().clip.name);
        }

        [Test]
        public void FallbackAudioListenerIgnoresPlayerParentCollider()
        {
            var enemy = CreateEnemy(Vector3.zero, EnemyKind.Ghost, TerritoryKind.EstateInterior);
            listenerObject = new GameObject("FallbackPlayerRoot");
            listenerObject.transform.position = new Vector3(3f, 0f, 0f);
            listenerObject.AddComponent<CharacterController>();

            var cameraObject = new GameObject("FallbackMainCamera");
            cameraObject.transform.SetParent(listenerObject.transform, false);
            cameraObject.AddComponent<AudioListener>();

            var source = enemyObject.AddComponent<AudioSource>();
            var filter = enemyObject.AddComponent<AudioLowPassFilter>();
            var occlusion = enemyObject.AddComponent<ThreatAudioOcclusion>();
            occlusion.Configure(enemy, null, source, filter);
            Physics.SyncTransforms();

            occlusion.ManualRefresh();

            Assert.IsFalse(occlusion.IsOccluded);
        }

        private EnemyBrain CreateEnemy(Vector3 position, EnemyKind kind, TerritoryKind territory)
        {
            enemyObject = new GameObject("ThreatAudioEnemy");
            enemyObject.transform.position = position;
            var brain = enemyObject.AddComponent<EnemyBrain>();
            brain.Configure(kind, ThreatStageProfile.ForStage(5), null, territory, position);
            return brain;
        }

        private Transform CreateListener(Vector3 position)
        {
            listenerObject = new GameObject("ThreatAudioListener");
            listenerObject.transform.position = position;
            return listenerObject.transform;
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
