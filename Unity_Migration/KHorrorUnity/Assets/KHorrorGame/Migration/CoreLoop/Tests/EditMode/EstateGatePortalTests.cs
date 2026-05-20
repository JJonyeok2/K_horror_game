using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class EstateGatePortalTests
    {
        [Test]
        public void OutsideActorUsesGateToEnterEstate()
        {
            var fixture = CreateFixture(new Vector3(0f, 0f, 51f));

            try
            {
                Assert.IsTrue(fixture.Portal.CanInteract(fixture.Actor));
                Assert.AreEqual("Enter estate [E]", fixture.Portal.InteractionLabel);

                fixture.Portal.Interact(fixture.Actor);

                AssertVector(fixture.Inside.position, fixture.Actor.transform.position);
                AssertQuaternion(fixture.Inside.rotation, fixture.Actor.transform.rotation);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void InsideActorUsesGateToLeaveEstate()
        {
            var fixture = CreateFixture(new Vector3(0f, 0f, 58f));

            try
            {
                Assert.IsTrue(fixture.Portal.CanInteract(fixture.Actor));
                Assert.AreEqual("Leave estate [E]", fixture.Portal.InteractionLabel);

                fixture.Portal.Interact(fixture.Actor);

                AssertVector(fixture.Outside.position, fixture.Actor.transform.position);
                AssertQuaternion(fixture.Outside.rotation, fixture.Actor.transform.rotation);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        private static GateFixture CreateFixture(Vector3 actorPosition)
        {
            var actorObject = new GameObject("Actor");
            actorObject.transform.position = actorPosition;
            actorObject.AddComponent<CharacterController>();
            var actor = actorObject.AddComponent<UnityPlayerController>();

            var inside = new GameObject("InsideSpawn").transform;
            inside.SetPositionAndRotation(new Vector3(0f, 1f, 60f), Quaternion.Euler(0f, 15f, 0f));

            var outside = new GameObject("OutsideSpawn").transform;
            outside.SetPositionAndRotation(new Vector3(0f, 1f, 51f), Quaternion.Euler(0f, 180f, 0f));

            var portalObject = new GameObject("GatePortal");
            var portal = portalObject.AddComponent<EstateGatePortal>();
            SetObject(portal, "insideSpawn", inside);
            SetObject(portal, "outsideSpawn", outside);
            SetFloat(portal, "gatePlaneZ", 54f);

            return new GateFixture(actor, portal, inside, outside);
        }

        private static void SetObject(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.001f);
            Assert.AreEqual(expected.y, actual.y, 0.001f);
            Assert.AreEqual(expected.z, actual.z, 0.001f);
        }

        private static void AssertQuaternion(Quaternion expected, Quaternion actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.001f);
            Assert.AreEqual(expected.y, actual.y, 0.001f);
            Assert.AreEqual(expected.z, actual.z, 0.001f);
            Assert.AreEqual(expected.w, actual.w, 0.001f);
        }

        private readonly struct GateFixture
        {
            public GateFixture(
                UnityPlayerController actor,
                EstateGatePortal portal,
                Transform inside,
                Transform outside)
            {
                Actor = actor;
                Portal = portal;
                Inside = inside;
                Outside = outside;
            }

            public UnityPlayerController Actor { get; }
            public EstateGatePortal Portal { get; }
            public Transform Inside { get; }
            public Transform Outside { get; }

            public void Destroy()
            {
                Object.DestroyImmediate(Actor.gameObject);
                Object.DestroyImmediate(Portal.gameObject);
                Object.DestroyImmediate(Inside.gameObject);
                Object.DestroyImmediate(Outside.gameObject);
            }
        }
    }
}
