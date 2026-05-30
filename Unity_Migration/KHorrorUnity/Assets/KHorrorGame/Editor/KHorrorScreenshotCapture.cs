using System;
using System.IO;
using KHorrorGame.Migration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KHorrorGame.EditorTools
{
    public static class KHorrorScreenshotCapture
    {
        private const string ScreenshotRoot = "Artifacts/Screenshots";

        [MenuItem("Tools/K Horror Migration/Capture Cargo Repickup Proof")]
        public static void CaptureCargoRepickupProof()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var player = new GameObject("ScreenshotPlayer");
            var cameraObject = new GameObject("ScreenshotCamera");
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = Vector3.zero;
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.028f, 0.026f, 1f);
            camera.fieldOfView = 70f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 20f;

            player.AddComponent<CharacterController>();
            var actor = player.AddComponent<UnityPlayerController>();

            var holdObject = new GameObject("ScreenshotVanCargoHold");
            holdObject.transform.position = new Vector3(0f, -5f, 0f);
            var hold = holdObject.AddComponent<VanCargoHold>();
            var slot = new GameObject("ScreenshotCargoSlot").transform;
            slot.SetParent(holdObject.transform, false);
            hold.RegisterSlot(slot);

            var artifact = new ArtifactDefinition("Ledger", 230, 1.5f, 1, new[] { "paper", "cargo" }, 1);
            if (!hold.TryStore(artifact, out var cargoItem))
            {
                throw new InvalidOperationException("Could not create screenshot cargo item.");
            }

            ((IInteractable)cargoItem).Interact(actor);
            TintHeldArtifact();

            CreateBackdrop();
            CreateLight();

            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), ScreenshotRoot, "cargo-repickup-proof.png");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            RenderCameraToPng(camera, outputPath, 1280, 720);
            Debug.Log("Saved cargo re-pickup screenshot to: " + outputPath);
        }

        private static void TintHeldArtifact()
        {
            var held = GameObject.Find("Held_Ledger");
            if (held == null)
            {
                return;
            }

            var renderer = held.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = new Color(0.46f, 0.34f, 0.16f, 1f);
            renderer.sharedMaterial = material;
        }

        private static void CreateBackdrop()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "ScreenshotDarkBongoWall";
            wall.transform.position = new Vector3(0f, 0f, 2.1f);
            wall.transform.localScale = new Vector3(4.8f, 2.7f, 0.08f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "ScreenshotBongoFloor";
            floor.transform.position = new Vector3(0f, -0.85f, 0.95f);
            floor.transform.localScale = new Vector3(4.8f, 0.08f, 3.2f);

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = new Color(0.08f, 0.095f, 0.085f, 1f);
            wall.GetComponent<Renderer>().sharedMaterial = material;
            floor.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateLight()
        {
            var lightObject = new GameObject("ScreenshotInspectionLight");
            lightObject.transform.rotation = Quaternion.Euler(32f, -18f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(0.85f, 0.9f, 0.78f, 1f);
        }

        private static void RenderCameraToPng(Camera camera, string outputPath, int width, int height)
        {
            var renderTexture = new RenderTexture(width, height, 24);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;

            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();

                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }
    }
}
