using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class EstateForestApproachTests
    {
        private const string ScenePath = "Assets/Scenes/KHorror_Main.unity";
        private const float RequiredWalkSeconds = 60f;
        private const float PlayerRadius = 0.35f;

        [Test]
        public void ForestApproachRouteWaypointsMeasureAtLeastSixtySeconds()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var waypoints = FindOrderedTransforms("ForestRouteWaypoint_");
            Assert.GreaterOrEqual(waypoints.Length, 14, "Forest route needs enough switchback waypoints to document the intended one-minute walk.");

            var totalDistance = MeasureDistance(waypoints);
            var baseWalkSpeed = ReadPlayerBaseSpeed();
            var walkSeconds = totalDistance / baseWalkSpeed;

            Assert.LessOrEqual(waypoints[0].position.z, 18.5f, "Forest route should start where the player exits the bongo travel placement.");
            Assert.GreaterOrEqual(waypoints[waypoints.Length - 1].position.z, 50f, "Forest route should finish at the exterior gate approach.");
            Assert.GreaterOrEqual(
                walkSeconds,
                RequiredWalkSeconds,
                $"Forest route is too short: {totalDistance:0.0}m at {baseWalkSpeed:0.0}m/s = {walkSeconds:0.0}s.");
        }

        [Test]
        public void ForestApproachContainsTallCanopyAndKoreanFolkloreDensity()
        {
            EditorSceneManager.OpenScene(ScenePath);

            Assert.GreaterOrEqual(CountSceneObjects("DeepForestTree_"), 36, "Approach needs taller deep-forest tree clusters, not roadside-height props.");
            Assert.GreaterOrEqual(CountSceneObjects("ForestCanopyOccluder_"), 10, "Approach needs canopy and fog occluders to break sightlines.");
            Assert.GreaterOrEqual(CountSceneObjects("ForestDeadGrassPatch_"), 16, "Approach ground needs dead grass density.");
            Assert.GreaterOrEqual(CountSceneObjects("ForestRouteJangseungPair_"), 6, "Approach needs repeated jangseung pairs along the route.");
        }

        [Test]
        public void ForestSwitchbackBlockersPreventStraightGateShortcut()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            var shortcutOrigins = new[]
            {
                new Vector3(0f, 1.2f, 18.5f),
                new Vector3(0f, 1.2f, 25.5f),
                new Vector3(0f, 1.2f, 32.5f),
                new Vector3(0f, 1.2f, 39.5f),
            };

            foreach (var origin in shortcutOrigins)
            {
                Assert.IsTrue(
                    TryHitSwitchbackBlocker(origin, out var hit),
                    $"Straight approach shortcut from {origin} reaches the gate without a forest route blocker.");
                Assert.Less(hit.distance, 8.5f, "Switchback blocker should appear quickly enough to shape the route: " + hit.collider.name);
            }
        }

        [Test]
        public void ForestRouteWaypointsAreGroundedAndCapsuleClear()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            var waypoints = FindOrderedTransforms("ForestRouteWaypoint_");
            Assert.IsNotEmpty(waypoints, "Forest route waypoints should exist.");

            foreach (var waypoint in waypoints)
            {
                Assert.IsTrue(
                    TryFindWalkableGround(waypoint.position, out var ground),
                    waypoint.name + " has no walkable floor below it.");
                Assert.LessOrEqual(
                    Mathf.Abs(ground.point.y - waypoint.position.y),
                    0.28f,
                    waypoint.name + " is not aligned with nearby walkable floor: " + ground.collider.name);
                Assert.IsFalse(
                    CapsuleHitsRouteBlocker(waypoint.position),
                    waypoint.name + " is inside or too close to a route blocker.");
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

        private static float MeasureDistance(IReadOnlyList<Transform> waypoints)
        {
            var total = 0f;
            for (var i = 1; i < waypoints.Count; i++)
            {
                total += Vector3.Distance(waypoints[i - 1].position, waypoints[i].position);
            }

            return total;
        }

        private static float ReadPlayerBaseSpeed()
        {
            var player = UnityEngine.Object.FindObjectOfType<UnityPlayerController>();
            Assert.IsNotNull(player, "Scene should contain a UnityPlayerController.");

            var serialized = new SerializedObject(player);
            var baseSpeed = serialized.FindProperty("baseSpeed");
            Assert.IsNotNull(baseSpeed, "UnityPlayerController should serialize baseSpeed.");
            Assert.Greater(baseSpeed.floatValue, 0f, "baseSpeed should be positive.");
            return baseSpeed.floatValue;
        }

        private static bool TryHitSwitchbackBlocker(Vector3 origin, out RaycastHit blockerHit)
        {
            var hits = Physics.RaycastAll(origin, Vector3.forward, 40f)
                .Where(hit => hit.collider.name.StartsWith("ForestSwitchbackBlocker_", StringComparison.Ordinal))
                .OrderBy(hit => hit.distance);

            foreach (var hit in hits)
            {
                blockerHit = hit;
                return true;
            }

            blockerHit = default;
            return false;
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

        private static bool CapsuleHitsRouteBlocker(Vector3 position)
        {
            var bottom = position + new Vector3(0f, 0.35f, 0f);
            var top = position + new Vector3(0f, 1.65f, 0f);
            return Physics.OverlapCapsule(bottom, top, PlayerRadius)
                .Where(collider => collider.enabled && !collider.isTrigger)
                .Any(collider => collider.name.StartsWith("ForestSwitchbackBlocker_", StringComparison.Ordinal));
        }

        private static bool IsWalkableName(string objectName)
        {
            return objectName.Contains("Apron", StringComparison.Ordinal) ||
                   objectName.Contains("Bridge", StringComparison.Ordinal) ||
                   objectName.Contains("Earth", StringComparison.Ordinal) ||
                   objectName.Contains("Floor", StringComparison.Ordinal) ||
                   objectName.Contains("Lane", StringComparison.Ordinal) ||
                   objectName.Contains("Pad", StringComparison.Ordinal) ||
                   objectName.Contains("Path", StringComparison.Ordinal) ||
                   objectName.Contains("Road", StringComparison.Ordinal) ||
                   objectName.Contains("Step", StringComparison.Ordinal) ||
                   objectName.Contains("Threshold", StringComparison.Ordinal);
        }
    }
}
