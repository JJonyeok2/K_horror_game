using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class EstateFloorContinuityTests
    {
        private const string ScenePath = "Assets/Scenes/KHorror_Main.unity";
        private const float MaxControllerStepHeight = 0.32f;

        [Test]
        public void EstateRouteHasWalkableGroundAtCriticalConnectors()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            var samples = new[]
            {
                new GroundSample("bongo-to-forest apron", new Vector3(0f, 0f, 8f)),
                new GroundSample("approach road start", new Vector3(0f, 0f, 16.4f)),
                new GroundSample("gate threshold", new Vector3(0f, 0f, 54.2f)),
                new GroundSample("inside gate courtyard", new Vector3(0f, 0f, 58.8f)),
                new GroundSample("main house front threshold", new Vector3(0f, 0f, 79.2f)),
                new GroundSample("main house center floor", new Vector3(0f, 0f, 82.5f)),
                new GroundSample("main house rear seam", new Vector3(-6.1f, 0f, 86.58f)),
                new GroundSample("rear compound first turn", new Vector3(-8.9f, 0f, 96.2f)),
                new GroundSample("rear compound east run", new Vector3(4.0f, 0f, 114.5f)),
                new GroundSample("rear compound west run", new Vector3(-8f, 0f, 132.8f)),
                new GroundSample("shrine front floor", new Vector3(-8f, 0f, 138.8f)),
            };

            foreach (var sample in samples)
            {
                Assert.IsTrue(
                    TryFindWalkableGround(sample.Position, out var hit),
                    $"{sample.Name} has no walkable floor under {sample.Position}.");
                Assert.LessOrEqual(hit.point.y, 0.85f, $"{sample.Name} hit an unexpectedly high surface: {hit.collider.name}.");
                Assert.GreaterOrEqual(hit.point.y, -0.08f, $"{sample.Name} hit ground too far below the playable surface: {hit.collider.name}.");
            }
        }

        [Test]
        public void MainHouseRearRouteUsesTraversableStepHeights()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            var samples = new[]
            {
                new GroundSample("main house rear room", new Vector3(-6.1f, 0f, 86.35f)),
                new GroundSample("rear threshold", new Vector3(-6.1f, 0f, 86.85f)),
                new GroundSample("rear lower step", new Vector3(-6.7f, 0f, 87.65f)),
                new GroundSample("back path pad", new Vector3(-7.4f, 0f, 88.9f)),
                new GroundSample("rear entry path", new Vector3(-8.9f, 0f, 92.5f)),
                new GroundSample("rear first crossing", new Vector3(-2.7f, 0f, 105.2f)),
                new GroundSample("rear east run", new Vector3(4.0f, 0f, 114.5f)),
                new GroundSample("rear second crossing", new Vector3(-2.0f, 0f, 123.6f)),
                new GroundSample("deep shrine path", new Vector3(-8f, 0f, 132.8f)),
            };

            var previous = samples[0];
            var previousY = GroundY(previous);
            for (var i = 1; i < samples.Length; i++)
            {
                var current = samples[i];
                var currentY = GroundY(current);
                var heightDelta = Math.Abs(currentY - previousY);

                Assert.LessOrEqual(
                    heightDelta,
                    MaxControllerStepHeight,
                    $"{previous.Name} to {current.Name} changes height by {heightDelta:0.000}, which can block the player controller.");

                previous = current;
                previousY = currentY;
            }
        }

        private static float GroundY(GroundSample sample)
        {
            Assert.IsTrue(
                TryFindWalkableGround(sample.Position, out var hit),
                $"{sample.Name} has no walkable floor under {sample.Position}.");
            return hit.point.y;
        }

        private static bool TryFindWalkableGround(Vector3 position, out RaycastHit bestHit)
        {
            var origin = new Vector3(position.x, 8f, position.z);
            var hits = Physics.RaycastAll(origin, Vector3.down, 12f);
            var walkableHits = hits
                .Where(hit => IsWalkableName(hit.collider.name))
                .OrderByDescending(hit => hit.point.y);

            foreach (var hit in walkableHits)
            {
                bestHit = hit;
                return true;
            }

            bestHit = default;
            return false;
        }

        private static bool IsWalkableName(string objectName)
        {
            return objectName.Contains("Apron", StringComparison.Ordinal) ||
                   objectName.Contains("Bridge", StringComparison.Ordinal) ||
                   objectName.Contains("Earth", StringComparison.Ordinal) ||
                   objectName.Contains("Floor", StringComparison.Ordinal) ||
                   objectName.Contains("Foundation", StringComparison.Ordinal) ||
                   objectName.Contains("Gap", StringComparison.Ordinal) ||
                   objectName.Contains("Landing", StringComparison.Ordinal) ||
                   objectName.Contains("Maru", StringComparison.Ordinal) ||
                   objectName.Contains("Pad", StringComparison.Ordinal) ||
                   objectName.Contains("Path", StringComparison.Ordinal) ||
                   objectName.Contains("Road", StringComparison.Ordinal) ||
                   objectName.Contains("Step", StringComparison.Ordinal) ||
                   objectName.Contains("Threshold", StringComparison.Ordinal);
        }

        private readonly struct GroundSample
        {
            public GroundSample(string name, Vector3 position)
            {
                Name = name;
                Position = position;
            }

            public string Name { get; }
            public Vector3 Position { get; }
        }
    }
}
