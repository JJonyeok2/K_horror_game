using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class EstateSidePathRiskTests
    {
        private const string ScenePath = "Assets/Scenes/KHorror_Main.unity";
        private const float PlayerRadius = 0.35f;

        [Test]
        public void RiskySidePathKeepsFrontGateMandatory()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            Assert.IsNotNull(GameObject.Find("OuterGateTraversalPortal"), "Front gate portal must remain the mandatory entry.");
            Assert.IsNotNull(GameObject.Find("RiskySidePathOutsideBlocker_Lower"), "Side path needs an outside blocker so it cannot bypass the gate.");
            AssertGateFlankBlocked(new Vector3(7.15f, 1.2f, 51.2f), "right lower flank");
            AssertGateFlankBlocked(new Vector3(7.15f, 2.4f, 51.2f), "right upper flank");

            foreach (var waypoint in FindOrderedTransforms("RiskySidePathInteriorWaypoint_"))
            {
                Assert.GreaterOrEqual(waypoint.position.z, 59.2f, waypoint.name + " starts before the inside gate landing.");
            }
        }

        [Test]
        public void RiskySidePathIsNarrowButPassableAfterGate()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            var waypoints = FindOrderedTransforms("RiskySidePathInteriorWaypoint_");
            Assert.GreaterOrEqual(waypoints.Length, 5, "Risky side path needs a readable optional route after the gate.");
            Assert.GreaterOrEqual(CountSceneObjects("RiskySidePathBlocker_"), 4, "Side path needs blockers that make it narrower than the main courtyard route.");

            foreach (var waypoint in waypoints)
            {
                Assert.IsTrue(TryFindWalkableGround(waypoint.position, out var ground), waypoint.name + " has no walkable ground.");
                Assert.LessOrEqual(Mathf.Abs(ground.point.y - waypoint.position.y), 0.28f, waypoint.name + " is not aligned with ground.");
                Assert.IsFalse(CapsuleHitsSidePathBlocker(waypoint.position), waypoint.name + " is blocked by the risky side path geometry.");
            }
        }

        [Test]
        public void RiskySidePathHasThreatCueAnchors()
        {
            EditorSceneManager.OpenScene(ScenePath);

            Assert.GreaterOrEqual(CountSceneObjects("RiskySidePathSoundCue_"), 2, "Risky side path needs sound cue trigger anchors.");
            Assert.GreaterOrEqual(CountSceneObjects("RiskySidePathDokkaebiExposure_"), 2, "Risky side path needs dokkaebi exposure anchors.");
            Assert.GreaterOrEqual(CountSceneObjects("RiskySidePathOccluder_"), 3, "Risky side path needs occluders that make the shortcut feel exposed.");

            foreach (var cue in FindOrderedTransforms("RiskySidePathSoundCue_"))
            {
                var collider = cue.GetComponent<Collider>();
                Assert.IsNotNull(collider, cue.name + " should have a trigger collider.");
                Assert.IsTrue(collider.isTrigger, cue.name + " should be a trigger, not a blocking wall.");
                Assert.GreaterOrEqual(cue.position.z, 59.2f, cue.name + " should stay inside the gate boundary.");
            }

            foreach (var exposure in FindOrderedTransforms("RiskySidePathDokkaebiExposure_"))
            {
                Assert.GreaterOrEqual(exposure.position.z, 59.2f, exposure.name + " should describe risk after mandatory gate entry.");
            }
        }

        [Test]
        public void SidePathDoesNotOpenEstateBoundaryGaps()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            AssertGateFlankBlocked(new Vector3(6.65f, 1.2f, 51.4f), "right outside side path edge");
            AssertGateFlankBlocked(new Vector3(8.0f, 1.2f, 51.4f), "far right outside side path edge");
            AssertGateFlankBlocked(new Vector3(8.0f, 2.4f, 51.4f), "far right outside head height");
        }

        private static void AssertGateFlankBlocked(Vector3 origin, string label)
        {
            var hits = Physics.RaycastAll(origin, Vector3.forward, 8f)
                .Where(hit => IsGateOrSidePathBlocker(hit.collider.name))
                .OrderBy(hit => hit.distance)
                .ToArray();

            Assert.IsNotEmpty(hits, "Side path gap is open at " + label + ".");
            Assert.LessOrEqual(hits[0].distance, 5.5f, "Side path blocker is too far behind the gate at " + label + ": " + hits[0].collider.name);
        }

        private static Transform[] FindOrderedTransforms(string prefix)
        {
            return UnityEngine.Object.FindObjectsOfType<Transform>()
                .Where(transform => transform.gameObject.scene.IsValid())
                .Where(transform => transform.name.StartsWith(prefix, StringComparison.Ordinal))
                .OrderBy(transform => transform.name, StringComparer.Ordinal)
                .ToArray();
        }

        private static int CountSceneObjects(string prefix)
        {
            return FindOrderedTransforms(prefix).Length;
        }

        private static bool CapsuleHitsSidePathBlocker(Vector3 position)
        {
            var bottom = position + new Vector3(0f, 0.35f, 0f);
            var top = position + new Vector3(0f, 1.65f, 0f);
            return Physics.OverlapCapsule(bottom, top, PlayerRadius)
                .Where(collider => collider.enabled && !collider.isTrigger)
                .Any(collider => collider.name.StartsWith("RiskySidePathBlocker_", StringComparison.Ordinal) ||
                                 collider.name.StartsWith("RiskySidePathOccluder_", StringComparison.Ordinal) ||
                                 collider.name.StartsWith("RiskySidePathOutsideBlocker_", StringComparison.Ordinal));
        }

        private static bool TryFindWalkableGround(Vector3 position, out RaycastHit bestHit)
        {
            var origin = new Vector3(position.x, 8f, position.z);
            var hits = Physics.RaycastAll(origin, Vector3.down, 12f)
                .Where(hit => IsWalkableName(hit.collider.name))
                .OrderByDescending(hit => hit.point.y);

            foreach (var hit in hits)
            {
                bestHit = hit;
                return true;
            }

            bestHit = default;
            return false;
        }

        private static bool IsGateOrSidePathBlocker(string objectName)
        {
            return objectName.Contains("GateWallConnector", StringComparison.Ordinal) ||
                   objectName.Contains("GateFlankBlocker", StringComparison.Ordinal) ||
                   objectName.StartsWith("RiskySidePathOutsideBlocker_", StringComparison.Ordinal);
        }

        private static bool IsWalkableName(string objectName)
        {
            return objectName.Contains("Courtyard", StringComparison.Ordinal) ||
                   objectName.Contains("Floor", StringComparison.Ordinal) ||
                   objectName.Contains("Landing", StringComparison.Ordinal) ||
                   objectName.Contains("Pad", StringComparison.Ordinal) ||
                   objectName.Contains("Path", StringComparison.Ordinal) ||
                   objectName.Contains("Road", StringComparison.Ordinal) ||
                   objectName.Contains("Step", StringComparison.Ordinal);
        }
    }
}
