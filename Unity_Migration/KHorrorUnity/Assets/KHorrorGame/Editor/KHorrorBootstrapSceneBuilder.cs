using System.IO;
using KHorrorGame.Migration;
using KHorrorGame.Migration.Rendering;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace KHorrorGame.Editor
{
    public static class KHorrorBootstrapSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/KHorror_Main.unity";
        private const string LightingProfilePath = "Assets/KHorrorGame/Rendering/KHorrorLightingProfile.asset";
        private const string RenderPipelinePath = "Assets/KHorrorGame/Rendering/KHorror_URP_RenderPipeline.asset";
        private const string RenderPipelineRendererPath = "Assets/KHorrorGame/Rendering/KHorror_URP_Renderer.asset";

        [MenuItem("Tools/K Horror Migration/Create Bootstrap Scene")]
        public static void CreateBootstrapScene()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/KHorrorGame/Rendering");
            EnsureRenderPipeline();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var systems = new GameObject("Systems");
            var world = new GameObject("World");
            var bongoHub = new GameObject("BongoHub");
            var estate = new GameObject("JonggaEstate");
            var settlement = new GameObject("SettlementOffice");
            var travel = new GameObject("BongoTravel");
            bongoHub.transform.SetParent(world.transform);
            estate.transform.SetParent(world.transform);
            settlement.transform.SetParent(world.transform);
            travel.transform.SetParent(world.transform);

            var bongoSpawn = CreateMarker("BongoHubSpawn", new Vector3(0f, 1f, -3f), Quaternion.identity, systems.transform);
            var estateSpawn = CreateMarker("EstateSpawn", new Vector3(0f, 1f, 18f), Quaternion.identity, systems.transform);
            var settlementSpawn = CreateMarker("SettlementSpawn", new Vector3(0f, 1f, -34f), Quaternion.identity, systems.transform);

            var player = CreatePlayer();
            var controller = systems.AddComponent<GameLoopController>();
            SetObject(controller, "player", player.Controller);
            SetObject(controller, "bongoHubSpawn", bongoSpawn);
            SetObject(controller, "estateSpawn", estateSpawn);
            SetObject(controller, "settlementSpawn", settlementSpawn);
            SetObject(controller, "bongoHubRoot", bongoHub);
            SetObject(controller, "estateRoot", estate);
            SetObject(controller, "settlementRoot", settlement);
            SetObject(controller, "travelRoot", travel);
            SetFloat(controller, "travelSeconds", 0.55f);

            CreateLighting(systems.transform, player.CameraLight);
            CreateHud(systems.transform, controller, player.Controller, player.Interactor);
            CreateBongoHub(bongoHub.transform, controller);
            CreateEstateProxy(estate.transform, controller);
            CreateSettlementProxy(settlement.transform, controller);
            CreateTravelProxy(travel.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log($"Created bootstrap Unity migration scene at {ScenePath}.");
        }

        private static PlayerBundle CreatePlayer()
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 1f, -3f);
            var characterController = player.AddComponent<CharacterController>();
            characterController.radius = 0.35f;
            characterController.height = 1.8f;
            characterController.center = new Vector3(0f, 0.9f, 0f);

            var controller = player.AddComponent<UnityPlayerController>();

            var cameraObject = new GameObject("MainCamera");
            cameraObject.transform.SetParent(player.transform);
            cameraObject.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            cameraObject.transform.localRotation = Quaternion.identity;
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 120f;
            cameraObject.AddComponent<AudioListener>();

            var flashlightObject = new GameObject("Flashlight");
            flashlightObject.transform.SetParent(cameraObject.transform);
            flashlightObject.transform.localPosition = Vector3.zero;
            flashlightObject.transform.localRotation = Quaternion.identity;
            var flashlight = flashlightObject.AddComponent<Light>();
            flashlight.type = LightType.Spot;
            flashlight.range = 15f;
            flashlight.spotAngle = 42f;
            flashlight.intensity = 5.5f;

            var interactor = cameraObject.AddComponent<PlayerInteractor>();
            SetObject(interactor, "actor", controller);
            SetObject(interactor, "sourceCamera", camera);
            SetFloat(interactor, "interactionDistance", 2.7f);

            var mounts = new GameObject("HeldItemMounts");
            mounts.transform.SetParent(cameraObject.transform);
            mounts.transform.localPosition = new Vector3(0f, -0.35f, 0.65f);
            var left = CreateMarker("LeftHandHeldMount", new Vector3(-0.32f, -0.1f, 0.65f), Quaternion.identity, mounts.transform);
            var right = CreateMarker("RightHandHeldMount", new Vector3(0.32f, -0.1f, 0.65f), Quaternion.identity, mounts.transform);
            var both = CreateMarker("TwoHandHeldMount", new Vector3(0f, -0.05f, 0.72f), Quaternion.identity, mounts.transform);

            SetObject(controller, "playerCamera", camera);
            SetObject(controller, "leftHandMount", left);
            SetObject(controller, "rightHandMount", right);
            SetObject(controller, "twoHandMount", both);

            return new PlayerBundle(controller, flashlight, interactor);
        }

        private static void CreateLighting(Transform parent, Light playerFlashlight)
        {
            var moonObject = new GameObject("MoonLight");
            moonObject.transform.SetParent(parent);
            var moon = moonObject.AddComponent<Light>();
            moon.type = LightType.Directional;

            var profile = AssetDatabase.LoadAssetAtPath<KHorrorLightingProfile>(LightingProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<KHorrorLightingProfile>();
                AssetDatabase.CreateAsset(profile, LightingProfilePath);
            }

            var rigObject = new GameObject("LightingRig");
            rigObject.transform.SetParent(parent);
            var rig = rigObject.AddComponent<KHorrorLightingRig>();
            SetObject(rig, "profile", profile);
            SetObject(rig, "moonLight", moon);
            SetObject(rig, "playerFlashlight", playerFlashlight);
            rig.ApplyProfile();
        }

        private static void CreateBongoHub(Transform parent, GameLoopController gameLoop)
        {
            CreateCube("BongoInteriorFloor", parent, new Vector3(0f, 0f, -3f), new Vector3(4.2f, 0.2f, 5.8f), Materials.VanFloor);
            CreateCube("BongoCargoWallLeft", parent, new Vector3(-2.15f, 1.2f, -3f), new Vector3(0.2f, 2.4f, 5.8f), Materials.VanWall);
            CreateCube("BongoCargoWallRight", parent, new Vector3(2.15f, 1.2f, -3f), new Vector3(0.2f, 2.4f, 5.8f), Materials.VanWall);
            CreateCube("BongoCargoRoof", parent, new Vector3(0f, 2.45f, -3f), new Vector3(4.5f, 0.18f, 5.8f), Materials.VanRoof);
            CreateCube("BongoCabBulkhead", parent, new Vector3(0f, 1.25f, -5.8f), new Vector3(4.2f, 2.3f, 0.25f), Materials.VanWall);
            CreateCube("LowExitRamp", parent, new Vector3(0f, 0.12f, 0.25f), new Vector3(3.4f, 0.18f, 1.1f), Materials.Road);
            CreateCube("OpenRearDoorLeft", parent, new Vector3(-2.35f, 1.15f, 0f), new Vector3(0.16f, 2f, 1.45f), Materials.VanDoor);
            CreateCube("OpenRearDoorRight", parent, new Vector3(2.35f, 1.15f, 0f), new Vector3(0.16f, 2f, 1.45f), Materials.VanDoor);

            var terminal = CreateCube("BongoTerminalTablet", parent, new Vector3(0f, 1.15f, -0.6f), new Vector3(1.25f, 0.7f, 0.12f), Materials.Terminal);
            var bongoTerminal = terminal.AddComponent<BongoTerminal>();
            SetObject(bongoTerminal, "gameLoop", gameLoop);

            var screen = CreateCube("TerminalScreenGlow", parent, new Vector3(0f, 1.15f, -0.525f), new Vector3(1.05f, 0.5f, 0.03f), Materials.ScreenGlow);
            screen.GetComponent<Collider>().enabled = false;
        }

        private static void CreateEstateProxy(Transform parent, GameLoopController gameLoop)
        {
            CreateCube("ApproachRoad_MuddyCenter", parent, new Vector3(0f, 0f, 34f), new Vector3(7f, 0.2f, 36f), Materials.Road);
            CreateCube("ApproachRoad_GrassLeft", parent, new Vector3(-5.5f, 0.02f, 34f), new Vector3(4f, 0.16f, 36f), Materials.Grass);
            CreateCube("ApproachRoad_GrassRight", parent, new Vector3(5.5f, 0.02f, 34f), new Vector3(4f, 0.16f, 36f), Materials.Grass);
            CreateCube("OuterGateThresholdStone", parent, new Vector3(0f, 0.08f, 53f), new Vector3(8f, 0.24f, 1.2f), Materials.Stone);

            CreateForest(parent);
            CreateOuterGate(parent);
            CreateCourtyard(parent);
            CreateMainHouse(parent);
            CreateShrineLoop(parent);

            var artifact = CreateCube("Artifact_BrassBowl", parent, new Vector3(2.2f, 0.5f, 70f), new Vector3(0.45f, 0.3f, 0.45f), Materials.Artifact);
            var pickup = artifact.AddComponent<ArtifactPickup>();
            SetObject(pickup, "gameLoop", gameLoop);

            var shrineArtifact = CreateCube("Artifact_ShrineToken", parent, new Vector3(-7.8f, 0.45f, 95f), new Vector3(0.36f, 0.28f, 0.36f), Materials.ShrineToken);
            var shrinePickup = shrineArtifact.AddComponent<ArtifactPickup>();
            SetObject(shrinePickup, "gameLoop", gameLoop);

            var extraction = CreateCube("VanInteriorReturnZone", parent, new Vector3(-2f, 0.7f, 20f), new Vector3(2.8f, 1.4f, 2.4f), Materials.Extraction);
            var collider = extraction.GetComponent<Collider>();
            collider.isTrigger = true;
            var extractionZone = extraction.AddComponent<ExtractionZone>();
            SetObject(extractionZone, "gameLoop", gameLoop);
        }

        private static void CreateSettlementProxy(Transform parent, GameLoopController gameLoop)
        {
            CreateCube("SettlementFloor", parent, new Vector3(0f, 0f, -34f), new Vector3(8f, 0.2f, 8f), Materials.OfficeFloor);
            CreateCube("SettlementBackWall", parent, new Vector3(0f, 1.8f, -30.2f), new Vector3(8f, 3.6f, 0.25f), Materials.Plaster);
            CreateCube("SettlementLeftWall", parent, new Vector3(-4.1f, 1.8f, -34f), new Vector3(0.25f, 3.6f, 8f), Materials.Plaster);
            CreateCube("SettlementRightWall", parent, new Vector3(4.1f, 1.8f, -34f), new Vector3(0.25f, 3.6f, 8f), Materials.Plaster);
            CreateCube("SettlementCeiling", parent, new Vector3(0f, 3.6f, -34f), new Vector3(8.4f, 0.25f, 8.4f), Materials.Wood);
            var station = CreateCube("SettlementStation", parent, new Vector3(0f, 1.0f, -31.5f), new Vector3(1.4f, 1f, 0.45f), Materials.Terminal);
            var settlementStation = station.AddComponent<SettlementStation>();
            SetObject(settlementStation, "gameLoop", gameLoop);
        }

        private static void CreateTravelProxy(Transform parent)
        {
            CreateCube("TravelMotionBackdrop", parent, new Vector3(0f, 1.2f, 8f), new Vector3(8f, 2.5f, 0.2f), Materials.Night);
        }

        private static void CreateForest(Transform parent)
        {
            for (var i = 0; i < 36; i++)
            {
                var z = 18f + i * 0.95f;
                var leftOffset = -7.5f - (i % 4) * 0.85f;
                var rightOffset = 7.3f + ((i + 2) % 4) * 0.8f;
                CreateTree(parent, "LeftForestTree_" + i, new Vector3(leftOffset, 0f, z), 4.8f + (i % 5) * 0.45f);
                CreateTree(parent, "RightForestTree_" + i, new Vector3(rightOffset, 0f, z + 0.4f), 4.5f + ((i + 1) % 5) * 0.5f);
            }

            for (var i = 0; i < 14; i++)
            {
                var z = 19f + i * 2.2f;
                CreateCube("BrokenStoneStep_" + i, parent, new Vector3((i % 2 == 0 ? -1.2f : 1.35f), 0.16f, z), new Vector3(1.2f, 0.18f, 0.7f), Materials.Stone);
            }

            CreateJangseung(parent, new Vector3(-4.8f, 0f, 48f), "Jangseung_Left");
            CreateJangseung(parent, new Vector3(4.8f, 0f, 48.4f), "Jangseung_Right");
        }

        private static void CreateOuterGate(Transform parent)
        {
            CreateCube("OuterGateLeftPost", parent, new Vector3(-3.7f, 2.1f, 54f), new Vector3(0.75f, 4.2f, 0.75f), Materials.DarkWood);
            CreateCube("OuterGateRightPost", parent, new Vector3(3.7f, 2.1f, 54f), new Vector3(0.75f, 4.2f, 0.75f), Materials.DarkWood);
            CreateCube("OuterGateLintel", parent, new Vector3(0f, 4.15f, 54f), new Vector3(8.2f, 0.65f, 0.8f), Materials.DarkWood);
            CreateCube("OuterGateRoof", parent, new Vector3(0f, 4.75f, 54f), new Vector3(9.3f, 0.35f, 1.6f), Materials.Roof);
            CreateCube("LeftSwingGatePanel", parent, new Vector3(-1.95f, 1.75f, 54.15f), new Vector3(3.2f, 3f, 0.25f), Materials.GatePanel);
            CreateCube("RightSwingGatePanel", parent, new Vector3(1.95f, 1.75f, 54.15f), new Vector3(3.2f, 3f, 0.25f), Materials.GatePanel);
            CreateCube("OuterWallLeft", parent, new Vector3(-10.5f, 1.7f, 58f), new Vector3(12f, 3.4f, 0.35f), Materials.StoneWall);
            CreateCube("OuterWallRight", parent, new Vector3(10.5f, 1.7f, 58f), new Vector3(12f, 3.4f, 0.35f), Materials.StoneWall);
            CreateCube("RiskySidePassage", parent, new Vector3(7.1f, 0.1f, 58.3f), new Vector3(1.5f, 0.2f, 8.5f), Materials.Road);
            CreateCube("SidePassageLowBeam", parent, new Vector3(7.1f, 1.35f, 60f), new Vector3(1.6f, 0.3f, 2.3f), Materials.DarkWood);
        }

        private static void CreateCourtyard(Transform parent)
        {
            CreateCube("CourtyardPackedEarth", parent, new Vector3(0f, 0f, 68f), new Vector3(22f, 0.2f, 24f), Materials.Courtyard);
            CreateCube("CourtyardLeftWall", parent, new Vector3(-11.2f, 1.4f, 70f), new Vector3(0.35f, 2.8f, 26f), Materials.StoneWall);
            CreateCube("CourtyardRightWall", parent, new Vector3(11.2f, 1.4f, 70f), new Vector3(0.35f, 2.8f, 26f), Materials.StoneWall);
            CreateCube("SightlineScreen_A", parent, new Vector3(-4.2f, 1.0f, 65f), new Vector3(0.28f, 2f, 5.5f), Materials.DarkWood);
            CreateCube("SightlineScreen_B", parent, new Vector3(4.5f, 1.0f, 72f), new Vector3(0.28f, 2f, 6.2f), Materials.DarkWood);
            CreateCylinder("CourtyardWell", parent, new Vector3(-5.6f, 0.55f, 71f), new Vector3(1.5f, 0.55f, 1.5f), Materials.Stone);

            for (var i = 0; i < 10; i++)
            {
                var x = -8f + (i % 5) * 1.2f;
                var z = 62f + (i / 5) * 1.3f;
                CreateCylinder("JangdokJar_" + i, parent, new Vector3(x, 0.45f, z), new Vector3(0.55f, 0.45f, 0.55f), Materials.Jar);
            }
        }

        private static void CreateMainHouse(Transform parent)
        {
            CreateCube("MainHouseFloor", parent, new Vector3(0f, 0.25f, 83f), new Vector3(14f, 0.5f, 7f), Materials.Wood);
            CreateCube("MainHouseBackWall", parent, new Vector3(0f, 2f, 86.6f), new Vector3(14f, 3.5f, 0.35f), Materials.Plaster);
            CreateCube("MainHouseLeftWall", parent, new Vector3(-7f, 2f, 83f), new Vector3(0.35f, 3.5f, 7f), Materials.Plaster);
            CreateCube("MainHouseRightWall", parent, new Vector3(7f, 2f, 83f), new Vector3(0.35f, 3.5f, 7f), Materials.Plaster);
            CreateCube("MainHouseSlidingDoor", parent, new Vector3(0f, 1.6f, 79.55f), new Vector3(4.5f, 2.7f, 0.22f), Materials.DoorPaper);
            CreateCube("MainHouseRoof", parent, new Vector3(0f, 4.1f, 83f), new Vector3(15.5f, 0.55f, 8.6f), Materials.Roof);
            CreateCube("BackExitGap", parent, new Vector3(-7.6f, 0.1f, 87.8f), new Vector3(1.6f, 0.2f, 5f), Materials.Road);
        }

        private static void CreateShrineLoop(Transform parent)
        {
            CreateCube("BackShrinePath", parent, new Vector3(-8f, 0f, 91f), new Vector3(3.2f, 0.2f, 15f), Materials.Road);
            CreateCube("ShrineFloor", parent, new Vector3(-8f, 0.2f, 99f), new Vector3(5.5f, 0.4f, 4.5f), Materials.Wood);
            CreateCube("ShrineBackWall", parent, new Vector3(-8f, 1.8f, 101.2f), new Vector3(5.5f, 3.2f, 0.3f), Materials.DarkWood);
            CreateCube("ShrineRoof", parent, new Vector3(-8f, 3.6f, 99f), new Vector3(6.3f, 0.45f, 5.4f), Materials.Roof);
            CreateCube("ShrineAltar", parent, new Vector3(-8f, 0.8f, 100.4f), new Vector3(2f, 1.0f, 0.7f), Materials.Altar);
        }

        private static void CreateTree(Transform parent, string name, Vector3 basePosition, float height)
        {
            CreateCylinder(name + "_Trunk", parent, basePosition + new Vector3(0f, height * 0.5f, 0f), new Vector3(0.45f, height * 0.5f, 0.45f), Materials.Bark);
            CreateSphere(name + "_Canopy", parent, basePosition + new Vector3(0f, height + 0.9f, 0f), new Vector3(2.2f, 1.7f, 2.2f), Materials.Canopy);
        }

        private static void CreateJangseung(Transform parent, Vector3 basePosition, string name)
        {
            CreateCylinder(name + "_Post", parent, basePosition + new Vector3(0f, 1.5f, 0f), new Vector3(0.38f, 1.5f, 0.38f), Materials.Bark);
            CreateCube(name + "_Face", parent, basePosition + new Vector3(0f, 2.9f, -0.05f), new Vector3(0.7f, 0.85f, 0.26f), Materials.DarkWood);
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            return CreateCube(name, parent, position, scale, CreateMaterial(name + "_Mat", color));
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;

            var renderer = cube.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return cube;
        }

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent);
            cylinder.transform.position = position;
            cylinder.transform.localScale = scale;
            cylinder.GetComponent<MeshRenderer>().sharedMaterial = material;
            return cylinder;
        }

        private static GameObject CreateSphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent);
            sphere.transform.position = position;
            sphere.transform.localScale = scale;
            sphere.GetComponent<MeshRenderer>().sharedMaterial = material;
            return sphere;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.name = name;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            return material;
        }

        private static void CreateHud(
            Transform parent,
            GameLoopController gameLoop,
            UnityPlayerController player,
            PlayerInteractor interactor)
        {
            var canvasObject = new GameObject("HUDCanvas");
            canvasObject.transform.SetParent(parent);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var statusText = CreateText("StatusText", canvasObject.transform, new Vector2(18f, -18f), TextAnchor.UpperLeft, 18, new Vector2(360f, 150f));
            var promptText = CreateText("CenterPromptText", canvasObject.transform, new Vector2(0f, 72f), TextAnchor.LowerCenter, 22, new Vector2(680f, 64f));

            var staminaBack = CreateUiImage("StaminaBack", canvasObject.transform, new Vector2(0f, 30f), new Vector2(240f, 10f), new Color(0.05f, 0.06f, 0.05f, 0.9f));
            var staminaFill = CreateUiImage("StaminaFill", staminaBack.transform, Vector2.zero, new Vector2(240f, 10f), new Color(0.45f, 0.86f, 0.38f, 0.95f));
            staminaFill.type = UnityEngine.UI.Image.Type.Filled;
            staminaFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;

            var presenter = canvasObject.AddComponent<HudPresenter>();
            SetObject(presenter, "gameLoop", gameLoop);
            SetObject(presenter, "player", player);
            SetObject(presenter, "interactor", interactor);
            SetObject(presenter, "statusText", statusText);
            SetObject(presenter, "centerPromptText", promptText);
            SetObject(presenter, "staminaFill", staminaFill);
        }

        private static UnityEngine.UI.Text CreateText(
            string name,
            Transform parent,
            Vector2 anchoredPosition,
            TextAnchor anchor,
            int fontSize,
            Vector2 size)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent);
            var rect = textObject.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            if (anchor == TextAnchor.UpperLeft)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
            }

            var text = textObject.AddComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = new Color(0.78f, 0.95f, 0.83f, 0.92f);
            text.raycastTarget = false;
            return text;
        }

        private static UnityEngine.UI.Image CreateUiImage(
            string name,
            Transform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            var imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent);
            var rect = imageObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            var image = imageObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void EnsureRenderPipeline()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RenderPipelineRendererPath);
            if (rendererData == null)
            {
                rendererData = CreateUniversalRendererData();
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(RenderPipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, RenderPipelinePath);
            }

            AssignDefaultRenderer(pipeline, rendererData);
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            EditorUtility.SetDirty(pipeline);
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();
        }

        private static ScriptableRendererData CreateUniversalRendererData()
        {
            var createRendererAsset = typeof(UniversalRenderPipelineAsset).GetMethod(
                "CreateRendererAsset",
                BindingFlags.Static | BindingFlags.NonPublic);

            if (createRendererAsset != null)
            {
                return (ScriptableRendererData)createRendererAsset.Invoke(
                    null,
                    new object[] { RenderPipelineRendererPath, RendererType.UniversalRenderer, false, "Renderer" });
            }

            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            rendererData.name = "KHorror_URP_Renderer";
            AssetDatabase.CreateAsset(rendererData, RenderPipelineRendererPath);
            return rendererData;
        }

        private static void AssignDefaultRenderer(UniversalRenderPipelineAsset pipeline, ScriptableRendererData rendererData)
        {
            var serialized = new SerializedObject(pipeline);
            var rendererDataList = serialized.FindProperty("m_RendererDataList");
            if (rendererDataList != null)
            {
                if (rendererDataList.arraySize == 0)
                {
                    rendererDataList.arraySize = 1;
                }

                rendererDataList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            }

            var defaultRendererIndex = serialized.FindProperty("m_DefaultRendererIndex");
            if (defaultRendererIndex != null)
            {
                defaultRendererIndex.intValue = 0;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform CreateMarker(string name, Vector3 position, Quaternion rotation, Transform parent)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent);
            marker.position = position;
            marker.rotation = rotation;
            return marker;
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

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            var folderName = Path.GetFileName(assetFolder);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private readonly struct PlayerBundle
        {
            public PlayerBundle(UnityPlayerController controller, Light cameraLight, PlayerInteractor interactor)
            {
                Controller = controller;
                CameraLight = cameraLight;
                Interactor = interactor;
            }

            public UnityPlayerController Controller { get; }
            public Light CameraLight { get; }
            public PlayerInteractor Interactor { get; }
        }

        private static class Materials
        {
            public static readonly Material Road = CreateMaterial("Mat_MuddyRoad", new Color(0.16f, 0.13f, 0.09f, 1f));
            public static readonly Material Grass = CreateMaterial("Mat_DeadGrass", new Color(0.13f, 0.18f, 0.095f, 1f));
            public static readonly Material Bark = CreateMaterial("Mat_Bark", new Color(0.18f, 0.11f, 0.065f, 1f));
            public static readonly Material Canopy = CreateMaterial("Mat_DarkCanopy", new Color(0.035f, 0.08f, 0.045f, 1f));
            public static readonly Material Stone = CreateMaterial("Mat_WetStone", new Color(0.18f, 0.18f, 0.16f, 1f));
            public static readonly Material StoneWall = CreateMaterial("Mat_OldStoneWall", new Color(0.14f, 0.14f, 0.12f, 1f));
            public static readonly Material DarkWood = CreateMaterial("Mat_DarkWood", new Color(0.13f, 0.07f, 0.04f, 1f));
            public static readonly Material GatePanel = CreateMaterial("Mat_GatePanel", new Color(0.22f, 0.10f, 0.055f, 1f));
            public static readonly Material Roof = CreateMaterial("Mat_BlackTileRoof", new Color(0.025f, 0.028f, 0.027f, 1f));
            public static readonly Material Courtyard = CreateMaterial("Mat_CourtyardEarth", new Color(0.17f, 0.145f, 0.1f, 1f));
            public static readonly Material Wood = CreateMaterial("Mat_AgedWood", new Color(0.22f, 0.14f, 0.08f, 1f));
            public static readonly Material Plaster = CreateMaterial("Mat_StainedPlaster", new Color(0.32f, 0.30f, 0.25f, 1f));
            public static readonly Material DoorPaper = CreateMaterial("Mat_DirtyDoorPaper", new Color(0.46f, 0.43f, 0.34f, 1f));
            public static readonly Material Jar = CreateMaterial("Mat_DarkJar", new Color(0.16f, 0.09f, 0.055f, 1f));
            public static readonly Material Artifact = CreateMaterial("Mat_BrassArtifact", new Color(0.62f, 0.42f, 0.16f, 1f));
            public static readonly Material ShrineToken = CreateMaterial("Mat_ShrineToken", new Color(0.78f, 0.70f, 0.44f, 1f));
            public static readonly Material Extraction = CreateMaterial("Mat_ExtractionZone", new Color(0.03f, 0.28f, 0.12f, 1f));
            public static readonly Material VanFloor = CreateMaterial("Mat_VanFloor", new Color(0.08f, 0.09f, 0.08f, 1f));
            public static readonly Material VanWall = CreateMaterial("Mat_VanWall", new Color(0.03f, 0.035f, 0.035f, 1f));
            public static readonly Material VanRoof = CreateMaterial("Mat_VanRoof", new Color(0.018f, 0.02f, 0.02f, 1f));
            public static readonly Material VanDoor = CreateMaterial("Mat_VanDoor", new Color(0.10f, 0.11f, 0.095f, 1f));
            public static readonly Material Terminal = CreateMaterial("Mat_Terminal", new Color(0.015f, 0.055f, 0.045f, 1f));
            public static readonly Material ScreenGlow = CreateMaterial("Mat_ScreenGlow", new Color(0.1f, 0.85f, 0.55f, 1f));
            public static readonly Material OfficeFloor = CreateMaterial("Mat_OfficeFloor", new Color(0.10f, 0.10f, 0.095f, 1f));
            public static readonly Material Altar = CreateMaterial("Mat_Altar", new Color(0.09f, 0.045f, 0.035f, 1f));
            public static readonly Material Night = CreateMaterial("Mat_TravelNight", new Color(0.025f, 0.03f, 0.028f, 1f));
        }
    }
}
