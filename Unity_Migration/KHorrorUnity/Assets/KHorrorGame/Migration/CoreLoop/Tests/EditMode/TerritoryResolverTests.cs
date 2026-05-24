using NUnit.Framework;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class TerritoryResolverTests
    {
        private GameObject[] createdObjects;

        [SetUp]
        public void SetUp()
        {
            createdObjects = new GameObject[0];
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var createdObject in createdObjects)
            {
                if (createdObject != null)
                {
                    Object.DestroyImmediate(createdObject);
                }
            }
        }

        [Test]
        public void ResolverReturnsFallbackWhenNoTerritoryVolumeContainsPoint()
        {
            var resolver = CreateResolver(TerritoryKind.BongoHub);

            var territory = resolver.ResolveAt(new Vector3(99f, 0f, 99f));

            Assert.AreEqual(TerritoryKind.BongoHub, territory);
        }

        [Test]
        public void ResolverFindsTerritoryVolumeByWorldPosition()
        {
            var resolver = CreateResolver(TerritoryKind.BongoHub);
            CreateVolume("ForestVolume", TerritoryKind.ForestApproach, Vector3.zero, Vector3.one * 6f, 0);

            var territory = resolver.ResolveAt(new Vector3(0f, 0f, 0f));

            Assert.AreEqual(TerritoryKind.ForestApproach, territory);
        }

        [Test]
        public void ResolverUsesHigherPriorityForOverlappingVolumes()
        {
            var resolver = CreateResolver(TerritoryKind.BongoHub);
            CreateVolume("LowPriorityForest", TerritoryKind.ForestApproach, Vector3.zero, Vector3.one * 8f, 0);
            CreateVolume("HighPriorityEstate", TerritoryKind.EstateInterior, Vector3.zero, Vector3.one * 3f, 10);

            var territory = resolver.ResolveAt(Vector3.zero);

            Assert.AreEqual(TerritoryKind.EstateInterior, territory);
        }

        [Test]
        public void ResolverFallsBackWhenOverlappingVolumesHaveSamePriority()
        {
            var resolver = CreateResolver(TerritoryKind.BongoHub);
            CreateVolume("ForestTie", TerritoryKind.ForestApproach, Vector3.zero, Vector3.one * 8f, 1);
            CreateVolume("EstateTie", TerritoryKind.EstateInterior, Vector3.zero, Vector3.one * 3f, 1);

            var territory = resolver.ResolveAt(Vector3.zero);

            Assert.AreEqual(TerritoryKind.BongoHub, territory);
        }

        [Test]
        public void ResolverCanResolveAPlayerColliderOverlap()
        {
            var resolver = CreateResolver(TerritoryKind.BongoHub);
            CreateVolume("EstateVolume", TerritoryKind.EstateInterior, Vector3.zero, Vector3.one * 8f, 0);
            var player = CreateObject("PlayerCollider");
            var capsule = player.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.radius = 0.35f;
            player.transform.position = new Vector3(0f, 0.9f, 0f);
            Physics.SyncTransforms();

            var territory = resolver.ResolveCollider(capsule);

            Assert.AreEqual(TerritoryKind.EstateInterior, territory);
        }

        private TerritoryResolver CreateResolver(TerritoryKind fallback)
        {
            var resolverObject = CreateObject("TerritoryResolver");
            var resolver = resolverObject.AddComponent<TerritoryResolver>();
            resolver.SetFallbackTerritoryForTests(fallback);
            return resolver;
        }

        private TerritoryVolume CreateVolume(
            string name,
            TerritoryKind territory,
            Vector3 position,
            Vector3 size,
            int priority)
        {
            var volumeObject = CreateObject(name);
            volumeObject.transform.position = position;
            var collider = volumeObject.AddComponent<BoxCollider>();
            collider.size = size;
            var volume = volumeObject.AddComponent<TerritoryVolume>();
            volume.ConfigureForTests(territory, priority);
            Physics.SyncTransforms();
            return volume;
        }

        private GameObject CreateObject(string name)
        {
            var obj = new GameObject(name);
            var next = new GameObject[createdObjects.Length + 1];
            for (var i = 0; i < createdObjects.Length; i++)
            {
                next[i] = createdObjects[i];
            }

            next[next.Length - 1] = obj;
            createdObjects = next;
            return obj;
        }
    }
}
