using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class EstateCourtyardNavigationTests
    {
        private const string ScenePath = "Assets/Scenes/KHorror_Main.unity";
        private const float PlayerRadius = 0.35f;

        [Test]
        public void CourtyardContainsRouteAnchorsAndKoreanYardDensity()
        {
            EditorSceneManager.OpenScene(ScenePath);

            Assert.IsNotNull(GameObject.Find("CourtyardWell"), "Courtyard well should remain a route anchor.");
            Assert.IsNotNull(GameObject.Find("JangdokPlatform"), "Jangdok platform should remain a route anchor.");
            Assert.GreaterOrEqual(CountSceneObjects("CourtyardRouteAnchor_"), 6, "Courtyard needs named route anchors for readability.");
            Assert.GreaterOrEqual(CountSceneObjects("CourtyardStackedWood_"), 8, "Courtyard needs stacked wood to break up the open-lot feel.");
            Assert.GreaterOrEqual(CountSceneObjects("CourtyardLowWall_"), 4, "Courtyard needs low walls that shape movement without sealing the yard.");
            Assert.GreaterOrEqual(CountSceneObjects("CourtyardSideShed_"), 1, "Courtyard needs at least one shed-scale anchor.");
            Assert.GreaterOrEqual(CountSceneObjects("CourtyardLanternPoint_"), 4, "Courtyard needs lantern points for readable night navigation.");
            Assert.GreaterOrEqual(CountSceneObjects("CourtyardSightlineOccluder_"), 4, "Courtyard needs occluders away from the main route.");
        }

        [Test]
        public void CourtyardMainAndSideRouteWaypointsAreGroundedAndClear()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            var mainRoute = FindOrderedTransforms("CourtyardMainRouteWaypoint_");
            var sideRoute = FindOrderedTransforms("CourtyardSideRouteWaypoint_");
            Assert.GreaterOrEqual(mainRoute.Length, 6, "Main route from gate to anchae needs documented waypoints.");
            Assert.GreaterOrEqual(sideRoute.Length, 4, "Optional courtyard side route needs documented waypoints.");

            foreach (var waypoint in mainRoute.Concat(sideRoute))
            {
                Assert.IsTrue(
                    TryFindWalkableGround(waypoint.position, out var ground),
                    waypoint.name + " has no walkable floor below it.");
                Assert.LessOrEqual(
                    Mathf.Abs(ground.point.y - waypoint.position.y),
                    0.28f,
                    waypoint.name + " is not aligned with walkable ground: " + ground.collider.name);
                Assert.IsFalse(
                    CapsuleHitsCourtyardBlocker(waypoint.position),
                    waypoint.name + " is blocked by a courtyard prop.");
            }
        }

        [Test]
        public void CourtyardSightlinesAreBrokenWithoutBlockingTheCentralRoute()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            AssertSightlineOccluder(new Vector3(-8.2f, 1.2f, 61.2f), Vector3.forward, "left yard edge");
            AssertSightlineOccluder(new Vector3(8.0f, 1.2f, 62.4f), Vector3.forward, "right yard edge");
            AssertSightlineOccluder(new Vector3(-6.2f, 1.2f, 73.5f), Vector3.right, "well-to-center lateral read");
            AssertSightlineOccluder(new Vector3(6.6f, 1.2f, 71.2f), Vector3.left, "east shed-to-center lateral read");

            foreach (var waypoint in FindOrderedTransforms("CourtyardMainRouteWaypoint_"))
            {
                Assert.IsFalse(
                    CapsuleHitsCourtyardBlocker(waypoint.position),
                    "Main route should stay readable and walkable at " + waypoint.name);
            }
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

        private static void AssertSightlineOccluder(Vector3 origin, Vector3 direction, string label)
        {
            var hits = Physics.RaycastAll(origin, direction.normalized, 12f)
                .Where(hit => hit.collider.name.StartsWith("CourtyardSightlineOccluder_", StringComparison.Ordinal) ||
                              hit.collider.name.StartsWith("CourtyardLowWall_", StringComparison.Ordinal) ||
                              hit.collider.name.StartsWith("CourtyardStackedWood_", StringComparison.Ordinal))
                .OrderBy(hit => hit.distance)
                .ToArray();

            Assert.IsNotEmpty(hits, "Courtyard sightline is too open at " + label + ".");
        }

        private static bool CapsuleHitsCourtyardBlocker(Vector3 position)
        {
            var bottom = position + new Vector3(0f, 0.35f, 0f);
            var top = position + new Vector3(0f, 1.65f, 0f);
            return Physics.OverlapCapsule(bottom, top, PlayerRadius)
                .Where(collider => collider.enabled && !collider.isTrigger)
                .Any(collider => IsCourtyardBlockerName(collider.name));
        }

        private static bool IsCourtyardBlockerName(string objectName)
        {
            return objectName.StartsWith("CourtyardLowWall_", StringComparison.Ordinal) ||
                   objectName.StartsWith("CourtyardStackedWood_", StringComparison.Ordinal) ||
                   objectName.StartsWith("CourtyardSideShed_", StringComparison.Ordinal) ||
                   objectName.StartsWith("CourtyardSightlineOccluder_", StringComparison.Ordinal) ||
                   objectName.StartsWith("JangdokJar_", StringComparison.Ordinal) ||
                   objectName == "CourtyardWell";
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

        private static bool IsWalkableName(string objectName)
        {
            return objectName.Contains("Bridge", StringComparison.Ordinal) ||
                   objectName.Contains("Courtyard", StringComparison.Ordinal) ||
                   objectName.Contains("Earth", StringComparison.Ordinal) ||
                   objectName.Contains("Floor", StringComparison.Ordinal) ||
                   objectName.Contains("Pad", StringComparison.Ordinal) ||
                   objectName.Contains("Path", StringComparison.Ordinal) ||
                   objectName.Contains("Platform", StringComparison.Ordinal) ||
                   objectName.Contains("Road", StringComparison.Ordinal) ||
                   objectName.Contains("Step", StringComparison.Ordinal) ||
                   objectName.Contains("Threshold", StringComparison.Ordinal);
        }
    }
}
