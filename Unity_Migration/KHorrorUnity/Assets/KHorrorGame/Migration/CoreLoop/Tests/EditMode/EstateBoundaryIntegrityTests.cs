using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class EstateBoundaryIntegrityTests
    {
        private const string ScenePath = "Assets/Scenes/KHorror_Main.unity";

        [Test]
        public void EstatePlayableAreaHasEscapeBlockingBoundaries()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            var checks = new[]
            {
                new BoundaryCheck("south approach limit", new Vector3(0f, 1.2f, 3f), Vector3.back),
                new BoundaryCheck("west forest limit", new Vector3(-8f, 1.2f, 24f), Vector3.left),
                new BoundaryCheck("east forest limit", new Vector3(8f, 1.2f, 38f), Vector3.right),
                new BoundaryCheck("west shrine limit", new Vector3(-9f, 1.2f, 140f), Vector3.left),
                new BoundaryCheck("east courtyard limit", new Vector3(9f, 1.2f, 70f), Vector3.right),
                new BoundaryCheck("north shrine limit", new Vector3(-8f, 1.2f, 146f), Vector3.forward),
            };

            foreach (var check in checks)
            {
                Assert.IsTrue(
                    TryFindBoundary(check.Origin, check.Direction, out var hit),
                    $"{check.Name} did not hit a boundary collider.");
                Assert.LessOrEqual(hit.distance, 8f, $"{check.Name} boundary is too far away: {hit.collider.name} at {hit.distance:0.00}m.");
            }
        }

        [Test]
        public void EstateBoundariesBlockJumpHeightEscape()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            var checks = new[]
            {
                new BoundaryCheck("south approach upper limit", new Vector3(0f, 2.4f, 3f), Vector3.back),
                new BoundaryCheck("west forest upper limit", new Vector3(-8f, 2.4f, 24f), Vector3.left),
                new BoundaryCheck("east forest upper limit", new Vector3(8f, 2.4f, 38f), Vector3.right),
                new BoundaryCheck("north shrine upper limit", new Vector3(-8f, 2.4f, 146f), Vector3.forward),
            };

            foreach (var check in checks)
            {
                Assert.IsTrue(
                    TryFindBoundary(check.Origin, check.Direction, out var hit),
                    $"{check.Name} can be bypassed at jump/head height.");
                Assert.LessOrEqual(hit.distance, 8f, $"{check.Name} upper boundary is too far away: {hit.collider.name} at {hit.distance:0.00}m.");
            }
        }

        [Test]
        public void OuterGateFlanksDoNotAllowBypassAroundGate()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();

            var checks = new[]
            {
                new BoundaryCheck("left gate-wall seam", new Vector3(-4.25f, 1.2f, 51.2f), Vector3.forward),
                new BoundaryCheck("right gate-wall seam", new Vector3(4.25f, 1.2f, 51.2f), Vector3.forward),
                new BoundaryCheck("left gate-wall upper seam", new Vector3(-4.25f, 2.4f, 51.2f), Vector3.forward),
                new BoundaryCheck("right gate-wall upper seam", new Vector3(4.25f, 2.4f, 51.2f), Vector3.forward),
            };

            foreach (var check in checks)
            {
                Assert.IsTrue(
                    TryFindGateFlankBlocker(check.Origin, check.Direction, out var hit),
                    $"{check.Name} can bypass the front gate without interacting.");
                Assert.LessOrEqual(hit.distance, 5.5f, $"{check.Name} blocker is too far behind the gate: {hit.collider.name} at {hit.distance:0.00}m.");
            }
        }

        private static bool TryFindBoundary(Vector3 origin, Vector3 direction, out RaycastHit boundaryHit)
        {
            var hits = Physics.RaycastAll(origin, direction.normalized, 16f)
                .Where(hit => IsBoundaryName(hit.collider.name))
                .OrderBy(hit => hit.distance);

            foreach (var hit in hits)
            {
                boundaryHit = hit;
                return true;
            }

            boundaryHit = default;
            return false;
        }

        private static bool TryFindGateFlankBlocker(Vector3 origin, Vector3 direction, out RaycastHit boundaryHit)
        {
            var hits = Physics.RaycastAll(origin, direction.normalized, 8f)
                .Where(hit => IsBoundaryName(hit.collider.name) || IsGateFlankBlockerName(hit.collider.name))
                .OrderBy(hit => hit.distance);

            foreach (var hit in hits)
            {
                boundaryHit = hit;
                return true;
            }

            boundaryHit = default;
            return false;
        }

        private static bool IsBoundaryName(string objectName)
        {
            return objectName.Contains("MapBoundary", StringComparison.Ordinal) ||
                   objectName.Contains("EscapeBoundary", StringComparison.Ordinal);
        }

        private static bool IsGateFlankBlockerName(string objectName)
        {
            return objectName.Contains("GateWallConnector", StringComparison.Ordinal) ||
                   objectName.Contains("GateFlankBlocker", StringComparison.Ordinal);
        }

        private readonly struct BoundaryCheck
        {
            public BoundaryCheck(string name, Vector3 origin, Vector3 direction)
            {
                Name = name;
                Origin = origin;
                Direction = direction;
            }

            public string Name { get; }
            public Vector3 Origin { get; }
            public Vector3 Direction { get; }
        }
    }
}
