using System;
using System.IO;
using System.Reflection;
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

        [MenuItem("Tools/K Horror Migration/Capture Terminal UI Proof")]
        public static void CaptureTerminalUiProof()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("ScreenshotCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.022f, 0.02f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 3.2f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 20f;

            CreateBackdrop();
            CreateLight();

            var playerObject = new GameObject("TerminalScreenshotPlayer");
            playerObject.AddComponent<CharacterController>();
            var player = playerObject.AddComponent<UnityPlayerController>();

            var hubRoot = new GameObject("BongoHub");
            var cargoHoldObject = new GameObject("BongoHubCargoHold");
            cargoHoldObject.transform.SetParent(hubRoot.transform, false);
            var cargoHold = cargoHoldObject.AddComponent<VanCargoHold>();
            var slot = new GameObject("CargoSlot").transform;
            slot.SetParent(cargoHoldObject.transform, false);
            cargoHold.RegisterSlot(slot);
            if (!cargoHold.TryStore(new ArtifactDefinition("Ledger", 230, 1.5f, 1), out _))
            {
                throw new InvalidOperationException("Could not create terminal screenshot cargo item.");
            }

            var gameLoopObject = new GameObject("GameLoop");
            var gameLoop = gameLoopObject.AddComponent<GameLoopController>();
            SetObject(gameLoop, "player", player);
            SetObject(gameLoop, "bongoHubRoot", hubRoot);
            SetObject(gameLoop, "hubCargoHold", cargoHold);
            InvokePrivate(gameLoop, "Awake");

            var terminal = VanTerminalController.EnsureRuntimePanel(gameLoop);
            terminal.ManualRefresh(true);
            terminal.ManualTick(20f);
            AttachTerminalCanvasToCamera(terminal, camera);

            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), ScreenshotRoot, "terminal-ui-proof.png");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            RenderCameraToPng(camera, outputPath, 1280, 720);
            Debug.Log("Saved terminal UI screenshot to: " + outputPath);
        }

        [MenuItem("Tools/K Horror Migration/Capture Paper Door Proof")]
        public static void CapturePaperDoorProof()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("ScreenshotCamera");
            cameraObject.transform.position = new Vector3(0f, 1.4f, -6.2f);
            cameraObject.transform.rotation = Quaternion.Euler(4f, 0f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.019f, 0.018f, 1f);
            camera.fieldOfView = 46f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 30f;

            CreateBackdrop();
            CreateLight();

            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "PaperDoorProof_SealedDoor";
            door.transform.position = new Vector3(0f, 0.65f, 0f);
            door.transform.localScale = new Vector3(0.12f, 2.2f, 2.2f);
            door.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(new Color(0.55f, 0.50f, 0.36f, 1f));
            var paperDoor = door.AddComponent<PaperDoorInteraction>();
            paperDoor.Configure("proof paper door");

            var playerObject = new GameObject("PaperDoorProof_Player");
            playerObject.transform.position = new Vector3(2.15f, 0f, 0f);
            playerObject.AddComponent<CharacterController>();
            var player = playerObject.AddComponent<UnityPlayerController>();
            player.TryCollectArtifact(new ArtifactDefinition("Door talisman", 0, 0.1f, 0, new[] { PaperDoorInteraction.TalismanTag }, 1));
            ((IInteractable)paperDoor).Interact(player);

            var talisman = GameObject.CreatePrimitive(PrimitiveType.Cube);
            talisman.name = "PaperDoorProof_Talisman";
            talisman.transform.position = new Vector3(0f, 1.1f, -0.08f);
            talisman.transform.localScale = new Vector3(0.34f, 0.82f, 0.035f);
            talisman.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(new Color(0.86f, 0.64f, 0.18f, 1f));
            DestroyCollider(talisman);

            var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "PaperDoorProof_GhostBlocked";
            enemy.transform.position = new Vector3(-2.3f, 0.45f, 0f);
            enemy.transform.localScale = new Vector3(0.55f, 0.9f, 0.55f);
            enemy.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(new Color(0.45f, 0.95f, 0.78f, 1f));

            var target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            target.name = "PaperDoorProof_PlayerSide";
            target.transform.position = new Vector3(2.35f, 0.45f, 0f);
            target.transform.localScale = new Vector3(0.45f, 0.8f, 0.45f);
            target.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(new Color(0.35f, 0.52f, 0.8f, 1f));

            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), ScreenshotRoot, "paper-door-seal-proof.png");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            RenderCameraToPng(camera, outputPath, 1280, 720);
            Debug.Log("Saved paper door proof screenshot to: " + outputPath);
        }

        [MenuItem("Tools/K Horror Migration/Capture Threat Atmosphere Proof")]
        public static void CaptureThreatAtmosphereProof()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("ScreenshotCamera");
            cameraObject.transform.position = new Vector3(0f, 1.35f, -6.8f);
            cameraObject.transform.rotation = Quaternion.Euler(7f, 0f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.018f, 0.017f, 1f);
            camera.fieldOfView = 48f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 34f;

            CreateBackdrop();
            CreateLight();

            var lanternLeft = CreateLantern("ThreatProof_LanternLeft", new Vector3(-1.45f, 1.4f, -0.15f));
            var lanternRight = CreateLantern("ThreatProof_LanternRight", new Vector3(1.45f, 1.4f, -0.15f));
            var shrinePanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shrinePanel.name = "ThreatProof_ShrineBackPanel";
            shrinePanel.transform.position = new Vector3(0f, 0.65f, 0.35f);
            shrinePanel.transform.localScale = new Vector3(2.4f, 1.85f, 0.12f);
            shrinePanel.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(new Color(0.16f, 0.055f, 0.045f, 1f));

            var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "ThreatProof_OccludedGhost";
            enemy.transform.position = new Vector3(-2.1f, 0.42f, 0.2f);
            enemy.transform.localScale = new Vector3(0.55f, 0.85f, 0.55f);
            enemy.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(new Color(0.42f, 0.96f, 0.76f, 1f));

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "ThreatProof_AudioOcclusionWall";
            wall.transform.position = new Vector3(-0.65f, 0.72f, 0.05f);
            wall.transform.localScale = new Vector3(0.18f, 1.55f, 1.8f);
            wall.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(new Color(0.24f, 0.20f, 0.16f, 1f));

            var listener = new GameObject("ThreatProof_PlayerListener");
            listener.transform.position = new Vector3(2f, 0.8f, 0.2f);
            var brain = enemy.AddComponent<EnemyBrain>();
            brain.Configure(EnemyKind.Ghost, ThreatStageProfile.ForStage(5), listener.transform, TerritoryKind.EstateInterior, enemy.transform.position);
            var audioSource = enemy.AddComponent<AudioSource>();
            var filter = enemy.AddComponent<AudioLowPassFilter>();
            var occlusion = enemy.AddComponent<ThreatAudioOcclusion>();
            occlusion.Configure(brain, listener.transform, audioSource, filter);
            Physics.SyncTransforms();
            occlusion.ManualRefresh();

            var cueObject = new GameObject("ThreatProof_AtmosphereCue");
            var cue = cueObject.AddComponent<ThreatAtmosphereCue>();
            cue.Configure(null, new[] { lanternLeft, lanternRight });
            cue.TriggerHighThreatCue("Shrine proof cue");
            cue.ManualTick(0.2f, ThreatStageProfile.MaxStage, GameMapId.JonggaEstate);

            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), ScreenshotRoot, "threat-atmosphere-proof.png");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            RenderCameraToPng(camera, outputPath, 1280, 720);
            Debug.Log("Saved threat atmosphere proof screenshot to: " + outputPath);
        }

        [MenuItem("Tools/K Horror Migration/Capture Internal Playflow Proof")]
        public static void CaptureInternalPlayFlowProof()
        {
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), ScreenshotRoot, "internal-playflow-proof.png");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            CaptureInternalPlayFlowProofToPath(outputPath, !Application.isBatchMode);
        }

        public static void CaptureInternalPlayFlowProofForTest(string outputPath)
        {
            CaptureInternalPlayFlowProofToPath(outputPath, !Application.isBatchMode);
        }

        private static void CaptureInternalPlayFlowProofToPath(string outputPath, bool restorePreviousScene)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Screenshot output path is required.", nameof(outputPath));
            }

            var previousScenePath = restorePreviousScene ? EditorSceneManager.GetActiveScene().path : string.Empty;

            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("InternalPlayflowProofCamera");
            cameraObject.transform.position = new Vector3(0f, 2.05f, -8.4f);
            cameraObject.transform.rotation = Quaternion.Euler(13f, 0f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.017f, 0.018f, 0.016f, 1f);
            camera.fieldOfView = 43f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 42f;

            var keyLight = new GameObject("InternalPlayflowProof_KeyLight").AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.transform.rotation = Quaternion.Euler(38f, -24f, 0f);
            keyLight.intensity = 1.08f;
            keyLight.color = new Color(0.86f, 0.91f, 0.78f, 1f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "InternalPlayflowProof_Floor";
            floor.transform.position = new Vector3(0f, -0.62f, 0.6f);
            floor.transform.localScale = new Vector3(8.8f, 0.08f, 5.2f);
            floor.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(new Color(0.055f, 0.065f, 0.058f, 1f));

            var rearWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearWall.name = "InternalPlayflowProof_RearWall";
            rearWall.transform.position = new Vector3(0f, 1.1f, 2.55f);
            rearWall.transform.localScale = new Vector3(8.8f, 3.2f, 0.1f);
            rearWall.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(new Color(0.035f, 0.043f, 0.037f, 1f));

            CreateProofBongoTerminal(new Vector3(-3.05f, 0.35f, 0.5f));
            CreateProofCargoLoop(new Vector3(0f, 0.24f, 0.5f));
            CreateProofShrineThreat(new Vector3(3.05f, 0.22f, 0.46f));

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            RenderCameraToPng(camera, outputPath, 1280, 720);
                Debug.Log("Saved internal playflow proof screenshot to: " + outputPath);
            }
            finally
            {
                if (restorePreviousScene && !string.IsNullOrEmpty(previousScenePath))
                {
                    EditorSceneManager.OpenScene(previousScenePath);
                }
            }
        }

        private static void CreateProofBongoTerminal(Vector3 origin)
        {
            CreateProofLabel("BONGO HUB", origin + new Vector3(0f, 1.45f, -0.08f), 0.18f, new Color(0.78f, 0.96f, 0.72f, 1f));

            var vanBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vanBody.name = "InternalPlayflowProof_BongoBody";
            vanBody.transform.position = origin + new Vector3(0f, 0.22f, 0.18f);
            vanBody.transform.localScale = new Vector3(1.9f, 1.1f, 1.25f);
            vanBody.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(new Color(0.08f, 0.16f, 0.105f, 1f));

            var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "InternalPlayflowProof_TerminalScreen";
            screen.transform.position = origin + new Vector3(0f, 0.74f, -0.48f);
            screen.transform.localScale = new Vector3(1.18f, 0.58f, 0.04f);
            var material = CreateLitMaterial(new Color(0.02f, 0.20f, 0.14f, 1f));
            material.SetColor("_EmissionColor", new Color(0.02f, 0.42f, 0.22f, 1f));
            material.EnableKeyword("_EMISSION");
            screen.GetComponent<Renderer>().sharedMaterial = material;

            CreateProofLabel("CARGO 230\nQUOTA 0/800\n[E] SETTLE", origin + new Vector3(-0.47f, 0.9f, -0.54f), 0.115f, new Color(0.64f, 1f, 0.72f, 1f));
        }

        private static void CreateProofCargoLoop(Vector3 origin)
        {
            CreateProofLabel("LOAD / RE-PICKUP", origin + new Vector3(0f, 1.45f, -0.08f), 0.18f, new Color(0.96f, 0.86f, 0.62f, 1f));

            var hold = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hold.name = "InternalPlayflowProof_VanCargoHold";
            hold.transform.position = origin + new Vector3(0f, 0.05f, 0f);
            hold.transform.localScale = new Vector3(1.9f, 0.18f, 1.15f);
            hold.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(new Color(0.12f, 0.10f, 0.075f, 1f));

            var cargoLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cargoLeft.name = "InternalPlayflowProof_LoadedLedger";
            cargoLeft.transform.position = origin + new Vector3(-0.44f, 0.34f, -0.05f);
            cargoLeft.transform.rotation = Quaternion.Euler(0f, -9f, 0f);
            cargoLeft.transform.localScale = new Vector3(0.48f, 0.28f, 0.34f);
            cargoLeft.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(new Color(0.62f, 0.43f, 0.20f, 1f));

            var cargoRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cargoRight.name = "InternalPlayflowProof_HeldLedger";
            cargoRight.transform.position = origin + new Vector3(0.44f, 0.56f, -0.36f);
            cargoRight.transform.rotation = Quaternion.Euler(5f, 12f, -7f);
            cargoRight.transform.localScale = new Vector3(0.58f, 0.36f, 0.36f);
            cargoRight.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(new Color(0.86f, 0.72f, 0.43f, 1f));

            CreateProofLabel("G LOAD\nE PICK UP", origin + new Vector3(-0.55f, 0.86f, -0.58f), 0.115f, new Color(0.94f, 0.83f, 0.55f, 1f));
        }

        private static void CreateProofShrineThreat(Vector3 origin)
        {
            CreateProofLabel("SHRINE THREAT", origin + new Vector3(0f, 1.45f, -0.08f), 0.18f, new Color(1f, 0.58f, 0.44f, 1f));

            var shrine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shrine.name = "InternalPlayflowProof_ShrinePanel";
            shrine.transform.position = origin + new Vector3(0f, 0.48f, 0.22f);
            shrine.transform.localScale = new Vector3(1.55f, 1.38f, 0.13f);
            shrine.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(new Color(0.18f, 0.055f, 0.045f, 1f));

            var ghost = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            ghost.name = "InternalPlayflowProof_InteriorGhost";
            ghost.transform.position = origin + new Vector3(-0.58f, 0.28f, -0.36f);
            ghost.transform.localScale = new Vector3(0.38f, 0.68f, 0.38f);
            var ghostMaterial = CreateLitMaterial(new Color(0.38f, 0.92f, 0.72f, 1f));
            ghostMaterial.SetColor("_EmissionColor", new Color(0.10f, 0.50f, 0.36f, 1f));
            ghostMaterial.EnableKeyword("_EMISSION");
            ghost.GetComponent<Renderer>().sharedMaterial = ghostMaterial;

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "InternalPlayflowProof_OcclusionWall";
            wall.transform.position = origin + new Vector3(-0.04f, 0.46f, -0.28f);
            wall.transform.localScale = new Vector3(0.12f, 1.18f, 0.92f);
            wall.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(new Color(0.23f, 0.20f, 0.16f, 1f));

            var lanternLeft = CreateLantern("InternalPlayflowProof_LanternLeft", origin + new Vector3(-0.62f, 1.08f, -0.03f));
            var lanternRight = CreateLantern("InternalPlayflowProof_LanternRight", origin + new Vector3(0.62f, 1.08f, -0.03f));
            lanternLeft.intensity = 2.5f;
            lanternRight.intensity = 2.5f;

            CreateProofLabel("STAGE 5\nFOG + AUDIO", origin + new Vector3(0.14f, 0.88f, -0.58f), 0.115f, new Color(1f, 0.52f, 0.42f, 1f));
        }

        private static void CreateProofLabel(string text, Vector3 position, float size, Color color)
        {
            var labelObject = new GameObject("ProofLabel_" + text.Split('\n')[0].Replace(' ', '_'));
            labelObject.transform.position = position;
            labelObject.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            var textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = size * 0.24f;
            textMesh.fontSize = 32;
            textMesh.color = color;
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

        private static Light CreateLantern(string name, Vector3 position)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = name + "_Body";
            body.transform.position = position;
            body.transform.localScale = new Vector3(0.18f, 0.42f, 0.18f);
            body.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(new Color(0.62f, 0.18f, 0.08f, 1f));

            var lightObject = new GameObject(name);
            lightObject.transform.position = position + Vector3.up * 0.25f;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.22f, 0.08f, 1f);
            light.intensity = 1.35f;
            light.range = 5.5f;
            return light;
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

        private static Material CreateLitMaterial(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            return material;
        }

        private static void DestroyCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void AttachTerminalCanvasToCamera(VanTerminalController terminal, Camera camera)
        {
            var canvas = terminal.GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                return;
            }

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 50;
        }

        private static void SetObject(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(target.GetType().FullName, methodName);
            }

            method.Invoke(target, null);
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
