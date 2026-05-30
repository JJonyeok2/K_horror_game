using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class EstateMainHouseInteriorTests
    {
        private const string ScenePath = "Assets/Scenes/KHorror_Main.unity";
        private const float PlayerRadius = 0.35f;

        [Test]
        public void MainHouseInteriorContainsRoomsCeilingsFramesAndRouteMarkers()
        {
            EditorSceneManager.OpenScene(ScenePath);

            Assert.GreaterOrEqual(CountSceneObjects("MainHouseRoom_"), 3, "Main house needs readable named room zones.");
            Assert.GreaterOrEqual(CountSceneObjects("MainHouseCeilingPanel_"), 3, "Main house needs ceiling panels under the roof.");
            Assert.GreaterOrEqual(CountSceneObjects("MainHouseCeilingBeam_"), 5, "Main house needs visible overhead beam density.");
            Assert.GreaterOrEqual(CountSceneObjects("MainHouseDoorFrame_"), 6, "Main house needs door frames around interior passages.");
            Assert.GreaterOrEqual(CountSceneObjects("MainHouseInteriorPaperDoor_"), 2, "Main house needs interior paper doors.");
            Assert.GreaterOrEqual(CountSceneObjects("MainHouseInteriorRouteWaypoint_"), 7, "Main house needs a named interior route from front to rear.");
            Assert.GreaterOrEqual(CountSceneObjects("MainHouseArtifactRouteWaypoint_"), 3, "Main house artifact route should be explicit.");
            Assert.GreaterOrEqual(CountSceneObjects("MainHouseInteriorSightlineBlocker_"), 3, "Main house needs interior sightline blockers.");

            AssertInteriorPaperDoorIsInteractive("MainHouseInteriorPaperDoor_LeftRoom");
            AssertInteriorPaperDoorIsInteractive("MainHouseInteriorPaperDoor_RightRoom");
        }

        [Test]
        public void MainHouseInteriorRouteWaypointsAreGroundedAndCapsuleClear()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            var route = FindOrderedTransforms("MainHouseInteriorRouteWaypoint_");
            Assert.GreaterOrEqual(route.Length, 7, "Main house route should include entry, artifact, side passage, and rear exit points.");

            foreach (var waypoint in route)
            {
                Assert.IsTrue(
                    TryFindWalkableGround(waypoint.position, out var ground),
                    waypoint.name + " has no walkable floor below it.");
                Assert.LessOrEqual(
                    Mathf.Abs(ground.point.y - waypoint.position.y),
                    0.28f,
                    waypoint.name + " is not aligned with walkable ground: " + ground.collider.name);
                Assert.IsFalse(
                    CapsuleHitsInteriorBlocker(waypoint.position),
                    waypoint.name + " is blocked by an interior prop or wall.");
            }
        }

        [Test]
        public void MainHouseArtifactRouteRequiresInteriorTraversalButLeavesRearEscape()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            var artifactRoute = FindOrderedTransforms("MainHouseArtifactRouteWaypoint_");
            Assert.GreaterOrEqual(artifactRoute.Length, 3, "Artifact route needs entry, approach, and pickup waypoints.");

            Assert.Less(artifactRoute.First().position.z, 81f, "Artifact route should start at the front threshold.");
            Assert.Greater(artifactRoute.Last().position.z, 82f, "Artifact route should pull the player inside the house.");
            Assert.IsNotNull(GameObject.Find("Artifact_MainHouseScroll"), "Main house scroll should remain the interior loot anchor.");

            var rearExit = GameObject.Find("MainHouseInteriorRouteWaypoint_06_RearExit");
            Assert.IsNotNull(rearExit, "Main house route should expose a rear escape waypoint.");
            Assert.IsFalse(CapsuleHitsInteriorBlocker(rearExit.transform.position), "Rear exit route should not be a dead-end trap.");
        }

        [Test]
        public void MainHouseInteriorSightlinesAreBrokenByScreensOrPaperDoors()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            AssertSightlineBlocked(new Vector3(-5.6f, 1.45f, 81.2f), Vector3.forward, "left room front-to-back sightline");
            AssertSightlineBlocked(new Vector3(5.6f, 1.45f, 81.2f), Vector3.forward, "right room front-to-back sightline");
            AssertSightlineBlocked(new Vector3(0f, 1.45f, 82.2f), Vector3.forward, "central hall rear sightline");
        }

        private static void AssertInteriorPaperDoorIsInteractive(string objectName)
        {
            var doorObject = GameObject.Find(objectName);
            Assert.IsNotNull(doorObject, objectName + " should exist.");
            Assert.IsNotNull(doorObject.GetComponent<PaperDoorInteraction>(), objectName + " should be interactive.");
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

        private static void AssertSightlineBlocked(Vector3 origin, Vector3 direction, string label)
        {
            var hits = Physics.RaycastAll(origin, direction.normalized, 9f)
                .Where(hit => hit.collider.name.StartsWith("MainHouseInteriorSightlineBlocker_", StringComparison.Ordinal) ||
                              hit.collider.name.StartsWith("MainHouseInteriorPaperDoor_", StringComparison.Ordinal) ||
                              hit.collider.name.StartsWith("MainHouseInnerBackRoomScreen", StringComparison.Ordinal))
                .OrderBy(hit => hit.distance)
                .ToArray();

            Assert.IsNotEmpty(hits, "Main house sightline is too open at " + label + ".");
        }

        private static bool CapsuleHitsInteriorBlocker(Vector3 position)
        {
            var bottom = position + new Vector3(0f, 0.35f, 0f);
            var top = position + new Vector3(0f, 1.65f, 0f);
            return Physics.OverlapCapsule(bottom, top, PlayerRadius)
                .Where(collider => collider.enabled && !collider.isTrigger)
                .Any(collider => IsInteriorBlockerName(collider.name));
        }

        private static bool IsInteriorBlockerName(string objectName)
        {
            return objectName.StartsWith("MainHouseInteriorWall_", StringComparison.Ordinal) ||
                   objectName.StartsWith("MainHouseInteriorSightlineBlocker_", StringComparison.Ordinal) ||
                   objectName.StartsWith("MainHouseDoorFrame_", StringComparison.Ordinal) ||
                   objectName.StartsWith("MainHouseCeilingBeam_", StringComparison.Ordinal) ||
                   objectName.StartsWith("MainHouseInteriorPaperDoor_", StringComparison.Ordinal) ||
                   objectName.StartsWith("MainHouseRoomProp_", StringComparison.Ordinal);
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
            return objectName.Contains("Connector", StringComparison.Ordinal) ||
                   objectName.Contains("Floor", StringComparison.Ordinal) ||
                   objectName.Contains("Landing", StringComparison.Ordinal) ||
                   objectName.Contains("Maru", StringComparison.Ordinal) ||
                   objectName.Contains("Pad", StringComparison.Ordinal) ||
                   objectName.Contains("Path", StringComparison.Ordinal) ||
                   objectName.Contains("Platform", StringComparison.Ordinal) ||
                   objectName.Contains("Road", StringComparison.Ordinal) ||
                   objectName.Contains("Step", StringComparison.Ordinal) ||
                   objectName.Contains("Threshold", StringComparison.Ordinal);
        }
    }
}
