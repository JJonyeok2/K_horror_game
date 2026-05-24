using System.IO;
using KHorrorGame.Migration;
using KHorrorGame.Migration.Rendering;
using KHorrorGame.Migration.Rendering.Editor;
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
        private const string PostProcessProfilePath = "Assets/KHorrorGame/Rendering/KHorrorPostProcessProfile.asset";
        private const string RenderPipelinePath = "Assets/KHorrorGame/Rendering/KHorror_URP_RenderPipeline.asset";
        private const string RenderPipelineRendererPath = "Assets/KHorrorGame/Rendering/KHorror_URP_Renderer.asset";
        private const string AmbientMaterialRoot = "Assets/KHorrorGame/Materials/AmbientCG";

        [MenuItem("Tools/K Horror Migration/Create Bootstrap Scene")]
        public static void CreateBootstrapScene()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/KHorrorGame/Rendering");
            EnsureRenderPipeline();
            EnsureVisualMaterials();

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

            CreateLighting(systems.transform, player.CameraLight, player.Camera);
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
            camera.fieldOfView = 72f;
            var urpCamera = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            urpCamera.renderPostProcessing = true;
            urpCamera.requiresDepthTexture = true;
            urpCamera.requiresColorTexture = true;
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

            return new PlayerBundle(controller, camera, flashlight, interactor);
        }

        private static void CreateLighting(Transform parent, Light playerFlashlight, Camera playerCamera)
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
            ConfigureLightingProfile(profile);

            var rigObject = new GameObject("LightingRig");
            rigObject.transform.SetParent(parent);
            var rig = rigObject.AddComponent<KHorrorLightingRig>();
            SetObject(rig, "profile", profile);
            SetObject(rig, "moonLight", moon);
            SetObject(rig, "playerFlashlight", playerFlashlight);
            SetObject(rig, "targetCamera", playerCamera);
            rig.ApplyProfile();

            CreatePostProcessVolume(parent);
            CreatePointLight("ApproachPathLowFill", parent, new Vector3(0f, 3.1f, 36f), new Color(0.45f, 0.55f, 0.68f), 2.25f, 42f);
            CreatePointLight("CourtyardMoonBounce", parent, new Vector3(0f, 4.4f, 69f), new Color(0.52f, 0.58f, 0.66f), 1.65f, 36f);
            CreatePointLight("GateWetLamp_Left", parent, new Vector3(-3.7f, 3.05f, 52.9f), new Color(1f, 0.58f, 0.34f), 2.5f, 12f);
            CreatePointLight("GateWetLamp_Right", parent, new Vector3(3.7f, 3.05f, 52.9f), new Color(1f, 0.58f, 0.34f), 2.25f, 11f);
            CreatePointLight("ShrineCandleGlow", parent, new Vector3(-8f, 1.35f, 100f), new Color(1f, 0.46f, 0.25f), 2.5f, 8f);
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
            CreateCube("OuterGateContinuousUnderfloor", parent, new Vector3(0f, -0.08f, 54.45f), new Vector3(9.2f, 0.42f, 4.6f), Materials.Stone);
            CreateCube("OuterGatePackedEarthBridge", parent, new Vector3(0f, 0f, 55.05f), new Vector3(8.3f, 0.2f, 4.4f), Materials.Courtyard);
            var gateInsideSpawn = CreateMarker("EstateGateInsideSpawn", new Vector3(0f, 1f, 59.8f), Quaternion.identity, parent);
            var gateOutsideSpawn = CreateMarker("EstateGateOutsideSpawn", new Vector3(0f, 1f, 51.25f), Quaternion.Euler(0f, 180f, 0f), parent);

            CreateDistantSilhouette(parent);
            CreateForest(parent);
            CreateOuterGate(parent, gateInsideSpawn, gateOutsideSpawn);
            CreateCourtyard(parent);
            CreateMainHouse(parent);
            CreateShrineLoop(parent);

            var artifact = CreateCube("Artifact_BrassBowl", parent, new Vector3(2.2f, 0.5f, 70f), new Vector3(0.45f, 0.3f, 0.45f), Materials.Artifact);
            var pickup = artifact.AddComponent<ArtifactPickup>();
            SetObject(pickup, "gameLoop", gameLoop);

            var shrineArtifact = CreateCube("Artifact_ShrineToken", parent, new Vector3(-7.8f, 0.45f, 95f), new Vector3(0.36f, 0.28f, 0.36f), Materials.ShrineToken);
            var shrinePickup = shrineArtifact.AddComponent<ArtifactPickup>();
            SetObject(shrinePickup, "gameLoop", gameLoop);

            CreateEstateReturnBongo(parent, gameLoop);
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

        private static void CreateEstateReturnBongo(Transform parent, GameLoopController gameLoop)
        {
            var root = new GameObject("EstateReturnBongo");
            root.transform.SetParent(parent);
            root.transform.position = new Vector3(-2.2f, 0f, 20.1f);
            root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            CreateCube("ReturnBongoBody", root.transform, new Vector3(0f, 1.15f, 0f), new Vector3(4.2f, 2.05f, 6.2f), Materials.VanPaint);
            CreateCube("ReturnBongoRoof", root.transform, new Vector3(0f, 2.28f, 0f), new Vector3(4.35f, 0.32f, 6.35f), Materials.VanRoof);
            CreateCube("ReturnBongoFrontCab", root.transform, new Vector3(0f, 1.35f, -3.55f), new Vector3(4.05f, 1.8f, 1.15f), Materials.VanPaint);
            CreateCube("ReturnBongoWindshield", root.transform, new Vector3(0f, 1.72f, -4.14f), new Vector3(2.9f, 0.72f, 0.08f), Materials.VanWindow);
            CreateCube("ReturnBongoSideWindowLeft", root.transform, new Vector3(-2.13f, 1.72f, -2.1f), new Vector3(0.08f, 0.62f, 1.35f), Materials.VanWindow);
            CreateCube("ReturnBongoSideWindowRight", root.transform, new Vector3(2.13f, 1.72f, -2.1f), new Vector3(0.08f, 0.62f, 1.35f), Materials.VanWindow);
            CreateCube("ReturnBongoRearFloor", root.transform, new Vector3(0f, 0.18f, 3.35f), new Vector3(3.55f, 0.16f, 1.75f), Materials.VanFloor);
            CreateCube("ReturnBongoRearDoorLeft", root.transform, new Vector3(-1.25f, 1.15f, 3.48f), new Vector3(1.15f, 1.85f, 0.15f), Materials.VanDoor);
            CreateCube("ReturnBongoRearDoorRight", root.transform, new Vector3(1.25f, 1.15f, 3.48f), new Vector3(1.15f, 1.85f, 0.15f), Materials.VanDoor);
            CreateCube("ReturnBongoRearStep", root.transform, new Vector3(0f, 0.18f, 4.28f), new Vector3(3.3f, 0.2f, 0.75f), Materials.Road);
            CreateCube("ReturnBongoFrontBumper", root.transform, new Vector3(0f, 0.55f, -4.2f), new Vector3(3.7f, 0.32f, 0.18f), Materials.RustedMetal);
            CreateCube("ReturnBongoRearBumper", root.transform, new Vector3(0f, 0.55f, 4.18f), new Vector3(3.7f, 0.32f, 0.18f), Materials.RustedMetal);

            CreateCylinder("ReturnBongoWheel_FL", root.transform, new Vector3(-2.15f, 0.52f, -2.35f), new Vector3(0.42f, 0.22f, 0.42f), Materials.Tire, Quaternion.Euler(0f, 0f, 90f));
            CreateCylinder("ReturnBongoWheel_FR", root.transform, new Vector3(2.15f, 0.52f, -2.35f), new Vector3(0.42f, 0.22f, 0.42f), Materials.Tire, Quaternion.Euler(0f, 0f, 90f));
            CreateCylinder("ReturnBongoWheel_RL", root.transform, new Vector3(-2.15f, 0.52f, 2.35f), new Vector3(0.42f, 0.22f, 0.42f), Materials.Tire, Quaternion.Euler(0f, 0f, 90f));
            CreateCylinder("ReturnBongoWheel_RR", root.transform, new Vector3(2.15f, 0.52f, 2.35f), new Vector3(0.42f, 0.22f, 0.42f), Materials.Tire, Quaternion.Euler(0f, 0f, 90f));

            var extraction = CreateCube("VanInteriorReturnZone", root.transform, new Vector3(0f, 1.0f, 3.35f), new Vector3(3.1f, 1.65f, 1.45f), Materials.Extraction);
            extraction.GetComponent<MeshRenderer>().enabled = false;
            var collider = extraction.GetComponent<Collider>();
            collider.isTrigger = true;
            var extractionZone = extraction.AddComponent<ExtractionZone>();
            SetObject(extractionZone, "gameLoop", gameLoop);

            CreatePointLight("ReturnBongoCabGlow", root.transform, new Vector3(0f, 1.8f, 2.2f), new Color(0.72f, 0.88f, 0.72f), 1.1f, 4.5f);
            CreatePointLight("ReturnBongoTailLampLeft", root.transform, new Vector3(-1.58f, 0.92f, 4.1f), new Color(1f, 0.08f, 0.035f), 0.95f, 3f);
            CreatePointLight("ReturnBongoTailLampRight", root.transform, new Vector3(1.58f, 0.92f, 4.1f), new Color(1f, 0.08f, 0.035f), 0.95f, 3f);
        }

        private static void CreateForest(Transform parent)
        {
            for (var i = 0; i < 64; i++)
            {
                var z = 15f + i * 0.7f;
                var leftOffset = -6.9f - (i % 5) * 0.85f - ((i * 17) % 9) * 0.08f;
                var rightOffset = 6.8f + ((i + 2) % 5) * 0.82f + ((i * 11) % 7) * 0.09f;
                CreateTree(parent, "LeftForestTree_" + i, new Vector3(leftOffset, 0f, z), 6.8f + (i % 6) * 0.5f);
                CreateTree(parent, "RightForestTree_" + i, new Vector3(rightOffset, 0f, z + 0.35f), 6.5f + ((i + 1) % 6) * 0.48f);

                if (i % 4 == 0)
                {
                    CreateBambooCluster(parent, new Vector3(leftOffset + 2.3f, 0f, z + 0.5f), "LeftBamboo_" + i);
                }

                if (i % 5 == 0)
                {
                    CreateGrassClump(parent, new Vector3(rightOffset - 2.0f, 0.08f, z), "RightGrass_" + i);
                }
            }

            for (var i = 0; i < 20; i++)
            {
                var z = 18f + i * 1.75f;
                var x = i % 2 == 0 ? -1.2f : 1.35f;
                var step = CreateCube("BrokenStoneStep_" + i, parent, new Vector3(x, 0.15f, z), new Vector3(1.2f, 0.16f, 0.7f), Materials.Stone);
                step.transform.rotation = Quaternion.Euler(0f, (i % 5 - 2) * 4f, 0f);

                if (i % 3 == 0)
                {
                    CreateRockCluster(parent, new Vector3(x * -1.7f, 0.18f, z + 0.25f), "RoadsideRock_" + i);
                }
            }

            for (var i = 0; i < 5; i++)
            {
                var z = 24f + i * 4.8f;
                var leftX = -3.75f - (i % 2) * 0.45f;
                var rightX = 3.9f + ((i + 1) % 2) * 0.38f;
                CreateJangseung(parent, new Vector3(leftX, 0f, z), "ApproachJangseung_Left_" + i);
                CreateJangseung(parent, new Vector3(rightX, 0f, z + 1.8f), "ApproachJangseung_Right_" + i);
            }

            CreateJangseung(parent, new Vector3(-4.8f, 0f, 48f), "GateJangseung_Left");
            CreateJangseung(parent, new Vector3(4.8f, 0f, 48.4f), "GateJangseung_Right");
            CreateSotdae(parent, new Vector3(-3.2f, 0f, 50.5f), "Sotdae_Left");
            CreateSotdae(parent, new Vector3(3.2f, 0f, 50.7f), "Sotdae_Right");
        }

        private static void CreateOuterGate(Transform parent, Transform gateInsideSpawn, Transform gateOutsideSpawn)
        {
            var gatePortal = new GameObject("OuterGateTraversalPortal");
            gatePortal.transform.SetParent(parent);
            var portal = gatePortal.AddComponent<EstateGatePortal>();
            SetObject(portal, "insideSpawn", gateInsideSpawn);
            SetObject(portal, "outsideSpawn", gateOutsideSpawn);
            SetFloat(portal, "gatePlaneZ", 54f);
            SetString(portal, "enterLabel", "대문 안으로 들어가기 [E]");
            SetString(portal, "exitLabel", "대문 밖으로 나가기 [E]");

            CreateCube("OuterGateLeftPost", gatePortal.transform, new Vector3(-3.7f, 2.1f, 54f), new Vector3(0.75f, 4.2f, 0.75f), Materials.DarkWood);
            CreateCube("OuterGateRightPost", gatePortal.transform, new Vector3(3.7f, 2.1f, 54f), new Vector3(0.75f, 4.2f, 0.75f), Materials.DarkWood);
            CreateCube("OuterGateLintel", gatePortal.transform, new Vector3(0f, 4.15f, 54f), new Vector3(8.2f, 0.65f, 0.8f), Materials.DarkWood);
            CreateCube("OuterGateRoof_LeftSlope", gatePortal.transform, new Vector3(-2.2f, 4.8f, 54f), new Vector3(5.2f, 0.32f, 2.1f), Materials.Roof, Quaternion.Euler(0f, 0f, 9f));
            CreateCube("OuterGateRoof_RightSlope", gatePortal.transform, new Vector3(2.2f, 4.8f, 54f), new Vector3(5.2f, 0.32f, 2.1f), Materials.Roof, Quaternion.Euler(0f, 0f, -9f));
            CreateCube("OuterGateRoofRidge", gatePortal.transform, new Vector3(0f, 5.25f, 54f), new Vector3(0.35f, 0.28f, 2.35f), Materials.RoofRidge);
            CreateRoofTileRibs(gatePortal.transform, new Vector3(0f, 5.0f, 54f), 8.2f, 2.25f, "OuterGateTileRib");
            CreateCube("LeftSwingGatePanel", gatePortal.transform, new Vector3(-1.95f, 1.75f, 54.15f), new Vector3(3.2f, 3f, 0.25f), Materials.GatePanel);
            CreateCube("RightSwingGatePanel", gatePortal.transform, new Vector3(1.95f, 1.75f, 54.15f), new Vector3(3.2f, 3f, 0.25f), Materials.GatePanel);
            CreateCube("OuterGateCenterSeamBlocker", gatePortal.transform, new Vector3(0f, 1.75f, 53.98f), new Vector3(0.82f, 3.05f, 0.18f), Materials.GatePanel);
            CreateCube("OuterGateBottomKickPlate", gatePortal.transform, new Vector3(0f, 0.5f, 53.9f), new Vector3(7.15f, 0.78f, 0.18f), Materials.DarkWood);
            CreateCube("OuterGateBlackoutBacking", gatePortal.transform, new Vector3(0f, 1.8f, 54.42f), new Vector3(7.25f, 3.25f, 0.16f), Materials.Night);
            CreateCube("GatePanelLeftVerticalBatten", gatePortal.transform, new Vector3(-1.95f, 1.75f, 53.95f), new Vector3(0.18f, 3.15f, 0.18f), Materials.DarkWood);
            CreateCube("GatePanelRightVerticalBatten", gatePortal.transform, new Vector3(1.95f, 1.75f, 53.95f), new Vector3(0.18f, 3.15f, 0.18f), Materials.DarkWood);
            CreateCube("GateIronLockPlate", gatePortal.transform, new Vector3(0f, 1.55f, 53.78f), new Vector3(0.8f, 0.48f, 0.08f), Materials.RustedMetal);
            CreatePaperCharm(gatePortal.transform, new Vector3(-0.52f, 2.7f, 53.72f), "GateCharm_Left");
            CreatePaperCharm(gatePortal.transform, new Vector3(0.55f, 2.55f, 53.72f), "GateCharm_Right");
            CreateCube("OuterWallLeft", parent, new Vector3(-10.5f, 1.7f, 58f), new Vector3(12f, 3.4f, 0.35f), Materials.StoneWall);
            CreateCube("OuterWallRight", parent, new Vector3(10.5f, 1.7f, 58f), new Vector3(12f, 3.4f, 0.35f), Materials.StoneWall);
            CreateWallCapStones(parent, -10.5f, 58f, 12f, "LeftWallCap");
            CreateWallCapStones(parent, 10.5f, 58f, 12f, "RightWallCap");
            CreateCube("RiskySidePassage", parent, new Vector3(7.1f, 0.1f, 58.3f), new Vector3(1.5f, 0.2f, 8.5f), Materials.Road);
            CreateCube("SidePassageLowBeam", parent, new Vector3(7.1f, 1.35f, 60f), new Vector3(1.6f, 0.3f, 2.3f), Materials.DarkWood);
            CreateBambooCluster(parent, new Vector3(8.9f, 0f, 60.5f), "SidePassageBamboo_A");
            CreateBambooCluster(parent, new Vector3(5.7f, 0f, 62.8f), "SidePassageBamboo_B");
        }

        private static void CreateCourtyard(Transform parent)
        {
            CreateCube("CourtyardPackedEarth", parent, new Vector3(0f, 0f, 68f), new Vector3(22f, 0.2f, 24f), Materials.Courtyard);
            CreateCube("CourtyardDampPatch_A", parent, new Vector3(-3.5f, 0.12f, 66.5f), new Vector3(4.8f, 0.035f, 3.4f), Materials.WetMud);
            CreateCube("CourtyardDampPatch_B", parent, new Vector3(5.2f, 0.13f, 72.3f), new Vector3(3.8f, 0.035f, 4.6f), Materials.WetMud);
            CreateCube("CourtyardLeftWall", parent, new Vector3(-11.2f, 1.4f, 70f), new Vector3(0.35f, 2.8f, 26f), Materials.StoneWall);
            CreateCube("CourtyardRightWall", parent, new Vector3(11.2f, 1.4f, 70f), new Vector3(0.35f, 2.8f, 26f), Materials.StoneWall);
            CreateCube("SightlineScreen_A", parent, new Vector3(-4.2f, 1.0f, 65f), new Vector3(0.28f, 2f, 5.5f), Materials.DarkWood);
            CreateCube("SightlineScreen_B", parent, new Vector3(4.5f, 1.0f, 72f), new Vector3(0.28f, 2f, 6.2f), Materials.DarkWood);
            CreateCylinder("CourtyardWell", parent, new Vector3(-5.6f, 0.55f, 71f), new Vector3(1.5f, 0.55f, 1.5f), Materials.Stone);
            CreateCylinder("CourtyardWellInnerDark", parent, new Vector3(-5.6f, 0.82f, 71f), new Vector3(1.0f, 0.08f, 1.0f), Materials.Night);
            CreateCube("WellWoodenLid_A", parent, new Vector3(-5.92f, 1.05f, 71f), new Vector3(0.18f, 0.08f, 1.9f), Materials.Wood);
            CreateCube("WellWoodenLid_B", parent, new Vector3(-5.28f, 1.06f, 71f), new Vector3(0.18f, 0.08f, 1.9f), Materials.Wood);

            for (var i = 0; i < 10; i++)
            {
                var x = -8f + (i % 5) * 1.2f;
                var z = 62f + (i / 5) * 1.3f;
                CreateCylinder("JangdokJar_" + i, parent, new Vector3(x, 0.45f, z), new Vector3(0.55f, 0.45f, 0.55f), Materials.Jar);
                CreateCylinder("JangdokJarLid_" + i, parent, new Vector3(x, 0.88f, z), new Vector3(0.42f, 0.08f, 0.42f), Materials.JarLid);
            }

            CreateCube("JangdokPlatform", parent, new Vector3(-5.6f, 0.12f, 62.65f), new Vector3(6.6f, 0.22f, 2.6f), Materials.Stone);
            CreateStoneCairn(parent, new Vector3(7.6f, 0.2f, 64.5f), "CourtyardCairn");
            CreatePaperLantern(parent, new Vector3(-10.8f, 2.3f, 66f), "LeftWallLantern");
            CreatePaperLantern(parent, new Vector3(10.8f, 2.2f, 73f), "RightWallLantern");
        }

        private static void CreateMainHouse(Transform parent)
        {
            CreateCube("MainHouseStoneFoundation", parent, new Vector3(0f, 0.2f, 83f), new Vector3(15.2f, 0.4f, 8.1f), Materials.Stone);
            CreateCube("MainHouseWoodenMaru", parent, new Vector3(0f, 0.55f, 80.2f), new Vector3(14.2f, 0.28f, 1.8f), Materials.Wood);
            CreateCube("MainHouseFloor", parent, new Vector3(0f, 0.55f, 83f), new Vector3(14f, 0.35f, 7f), Materials.Wood);
            CreateCube("MainHouseBackWall", parent, new Vector3(0f, 2f, 86.6f), new Vector3(14f, 3.5f, 0.35f), Materials.Plaster);
            CreateCube("MainHouseLeftWall", parent, new Vector3(-7f, 2f, 83f), new Vector3(0.35f, 3.5f, 7f), Materials.Plaster);
            CreateCube("MainHouseRightWall", parent, new Vector3(7f, 2f, 83f), new Vector3(0.35f, 3.5f, 7f), Materials.Plaster);
            CreateCube("MainHouseFrontBeam", parent, new Vector3(0f, 3.25f, 79.7f), new Vector3(14.8f, 0.35f, 0.35f), Materials.DarkWood);
            CreateCube("MainHouseBackBeam", parent, new Vector3(0f, 3.25f, 86.7f), new Vector3(14.8f, 0.35f, 0.35f), Materials.DarkWood);
            for (var i = 0; i < 7; i++)
            {
                var x = -6f + i * 2f;
                CreateCube("MainHouseFrontColumn_" + i, parent, new Vector3(x, 1.75f, 79.55f), new Vector3(0.32f, 2.45f, 0.32f), Materials.DarkWood);
                CreateCube("MainHousePaperDoor_" + i, parent, new Vector3(x, 1.75f, 79.38f), new Vector3(1.25f, 2.15f, 0.08f), Materials.DoorPaper);
                CreateCube("MainHouseDoorMuntinV_" + i, parent, new Vector3(x, 1.75f, 79.31f), new Vector3(0.08f, 2.25f, 0.08f), Materials.DarkWood);
                CreateCube("MainHouseDoorMuntinH_" + i, parent, new Vector3(x, 1.75f, 79.3f), new Vector3(1.28f, 0.08f, 0.08f), Materials.DarkWood);
            }

            CreateCube("MainHouseRoof_LeftSlope", parent, new Vector3(-3.6f, 4.35f, 83f), new Vector3(9.0f, 0.42f, 9.6f), Materials.Roof, Quaternion.Euler(0f, 0f, 11f));
            CreateCube("MainHouseRoof_RightSlope", parent, new Vector3(3.6f, 4.35f, 83f), new Vector3(9.0f, 0.42f, 9.6f), Materials.Roof, Quaternion.Euler(0f, 0f, -11f));
            CreateCube("MainHouseRoofRidge", parent, new Vector3(0f, 5.05f, 83f), new Vector3(0.45f, 0.35f, 9.9f), Materials.RoofRidge);
            CreateCube("MainHouseDeepEavesFront", parent, new Vector3(0f, 3.95f, 78.2f), new Vector3(15.8f, 0.28f, 0.55f), Materials.Roof);
            CreateRoofTileRibs(parent, new Vector3(0f, 4.55f, 83f), 14.6f, 9.8f, "MainHouseTileRib");
            CreateRafterRow(parent, 79.0f, "FrontRafter");
            CreateRafterRow(parent, 87.05f, "BackRafter");
            CreatePaperCharm(parent, new Vector3(-2.4f, 2.85f, 79.15f), "MainDoorCharm_Left");
            CreatePaperCharm(parent, new Vector3(2.4f, 2.7f, 79.15f), "MainDoorCharm_Right");
            CreateCube("BackExitGap", parent, new Vector3(-7.6f, 0.1f, 87.8f), new Vector3(1.6f, 0.2f, 5f), Materials.Road);
            CreateBambooCluster(parent, new Vector3(-9.1f, 0f, 88.5f), "BackExitBamboo");
        }

        private static void CreateShrineLoop(Transform parent)
        {
            CreateCube("BackShrinePath", parent, new Vector3(-8f, 0f, 91f), new Vector3(3.2f, 0.2f, 15f), Materials.Road);
            CreateCube("ShrineFoundation", parent, new Vector3(-8f, 0.2f, 99f), new Vector3(5.8f, 0.35f, 4.8f), Materials.Stone);
            CreateCube("ShrineFloor", parent, new Vector3(-8f, 0.55f, 99f), new Vector3(5.5f, 0.25f, 4.5f), Materials.Wood);
            CreateCube("ShrineBackWall", parent, new Vector3(-8f, 1.8f, 101.2f), new Vector3(5.5f, 3.2f, 0.3f), Materials.DarkWood);
            CreateCube("ShrineLeftWall", parent, new Vector3(-10.8f, 1.7f, 99f), new Vector3(0.25f, 2.8f, 4.5f), Materials.DarkWood);
            CreateCube("ShrineRightWall", parent, new Vector3(-5.2f, 1.7f, 99f), new Vector3(0.25f, 2.8f, 4.5f), Materials.DarkWood);
            CreateCube("ShrineRoof_LeftSlope", parent, new Vector3(-9.4f, 3.75f, 99f), new Vector3(3.7f, 0.35f, 5.6f), Materials.Roof, Quaternion.Euler(0f, 0f, 10f));
            CreateCube("ShrineRoof_RightSlope", parent, new Vector3(-6.6f, 3.75f, 99f), new Vector3(3.7f, 0.35f, 5.6f), Materials.Roof, Quaternion.Euler(0f, 0f, -10f));
            CreateCube("ShrineRoofRidge", parent, new Vector3(-8f, 4.2f, 99f), new Vector3(0.3f, 0.24f, 5.9f), Materials.RoofRidge);
            CreateCube("ShrineAltar", parent, new Vector3(-8f, 0.8f, 100.4f), new Vector3(2f, 1.0f, 0.7f), Materials.Altar);
            CreateCube("ShrineHangingRope", parent, new Vector3(-8f, 2.75f, 97.05f), new Vector3(4.8f, 0.08f, 0.08f), Materials.Rope);
            for (var i = 0; i < 7; i++)
            {
                var x = -10.1f + i * 0.7f;
                CreateCube("ShrineClothStrip_" + i, parent, new Vector3(x, 2.35f - (i % 2) * 0.15f, 96.95f), new Vector3(0.18f, 0.72f, 0.04f), i % 2 == 0 ? Materials.FadedRedCloth : Materials.FadedWhiteCloth);
            }

            CreateCylinder("ShrineCandle_A", parent, new Vector3(-8.45f, 1.45f, 99.9f), new Vector3(0.08f, 0.22f, 0.08f), Materials.CandleWax);
            CreateCylinder("ShrineCandle_B", parent, new Vector3(-7.55f, 1.45f, 99.9f), new Vector3(0.08f, 0.22f, 0.08f), Materials.CandleWax);
            CreateSphere("ShrineCandleFlame_A", parent, new Vector3(-8.45f, 1.72f, 99.9f), new Vector3(0.12f, 0.18f, 0.12f), Materials.LanternGlow);
            CreateSphere("ShrineCandleFlame_B", parent, new Vector3(-7.55f, 1.72f, 99.9f), new Vector3(0.12f, 0.18f, 0.12f), Materials.LanternGlow);
            CreateStoneCairn(parent, new Vector3(-10.6f, 0.3f, 94.5f), "ShrinePathCairn");
        }

        private static void CreateTree(Transform parent, string name, Vector3 basePosition, float height)
        {
            var lean = new Vector3(((name.GetHashCode() & 7) - 3) * 0.035f, 1f, (((name.GetHashCode() >> 3) & 7) - 3) * 0.025f).normalized;
            var trunkBase = basePosition + Vector3.up * 0.12f;
            var trunkTop = basePosition + lean * height;
            CreateCylinderBetween(name + "_Trunk", parent, trunkBase, trunkTop, 0.28f + height * 0.015f, Materials.Bark);
            CreateCylinderBetween(name + "_RootA", parent, trunkBase + new Vector3(0f, 0.08f, 0f), trunkBase + new Vector3(0.85f, 0.05f, 0.35f), 0.09f, Materials.Bark);
            CreateCylinderBetween(name + "_RootB", parent, trunkBase + new Vector3(0f, 0.08f, 0f), trunkBase + new Vector3(-0.65f, 0.05f, -0.45f), 0.08f, Materials.Bark);
            CreateCylinderBetween(name + "_BranchA", parent, trunkTop - Vector3.up * 1.8f, trunkTop + new Vector3(1.1f, -0.35f, 0.45f), 0.08f, Materials.Bark);
            CreateCylinderBetween(name + "_BranchB", parent, trunkTop - Vector3.up * 2.4f, trunkTop + new Vector3(-0.95f, -0.2f, -0.35f), 0.07f, Materials.Bark);
            CreateSphere(name + "_CanopyTop", parent, trunkTop + new Vector3(0f, 0.9f, 0f), new Vector3(2.0f, 1.45f, 2.0f), Materials.Canopy);
            CreateSphere(name + "_CanopyLowA", parent, trunkTop + new Vector3(0.8f, -0.35f, 0.25f), new Vector3(1.55f, 0.9f, 1.55f), Materials.Canopy);
            CreateSphere(name + "_CanopyLowB", parent, trunkTop + new Vector3(-0.75f, -0.25f, -0.2f), new Vector3(1.45f, 0.85f, 1.45f), Materials.Canopy);
        }

        private static void CreateJangseung(Transform parent, Vector3 basePosition, string name)
        {
            CreateCylinder(name + "_Post", parent, basePosition + new Vector3(0f, 1.5f, 0f), new Vector3(0.38f, 1.5f, 0.38f), Materials.Bark);
            CreateCube(name + "_Face", parent, basePosition + new Vector3(0f, 2.9f, -0.05f), new Vector3(0.7f, 0.85f, 0.26f), Materials.DarkWood);
            CreateCube(name + "_Brow", parent, basePosition + new Vector3(0f, 3.08f, -0.23f), new Vector3(0.55f, 0.08f, 0.08f), Materials.RustedMetal);
            CreateCube(name + "_Mouth", parent, basePosition + new Vector3(0f, 2.72f, -0.23f), new Vector3(0.42f, 0.07f, 0.08f), Materials.RustedMetal);
            CreateCube(name + "_Hat", parent, basePosition + new Vector3(0f, 3.38f, -0.05f), new Vector3(0.9f, 0.22f, 0.38f), Materials.Roof);
        }

        private static void CreateDistantSilhouette(Transform parent)
        {
            CreateCube("LeftMountainSilhouette", parent, new Vector3(-22f, 5f, 58f), new Vector3(15f, 10f, 70f), Materials.DistantHill);
            CreateCube("RightMountainSilhouette", parent, new Vector3(22f, 4.7f, 62f), new Vector3(15f, 9.4f, 76f), Materials.DistantHill);
            CreateCube("BackMountainSilhouette", parent, new Vector3(0f, 5.5f, 112f), new Vector3(48f, 11f, 6f), Materials.DistantHill);
        }

        private static void CreateBambooCluster(Transform parent, Vector3 basePosition, string name)
        {
            for (var i = 0; i < 5; i++)
            {
                var x = ((i * 37) % 9 - 4) * 0.16f;
                var z = ((i * 23) % 7 - 3) * 0.15f;
                var height = 2.8f + i * 0.28f;
                var start = basePosition + new Vector3(x, 0.05f, z);
                var end = start + new Vector3((i - 2) * 0.07f, height, (2 - i) * 0.04f);
                CreateCylinderBetween(name + "_Cane_" + i, parent, start, end, 0.045f, Materials.Bamboo);
                CreateCube(name + "_LeafA_" + i, parent, end + new Vector3(0.22f, -0.2f, 0f), new Vector3(0.55f, 0.04f, 0.12f), Materials.BambooLeaf, Quaternion.Euler(0f, 25f + i * 11f, -22f));
                CreateCube(name + "_LeafB_" + i, parent, end + new Vector3(-0.2f, -0.35f, 0.1f), new Vector3(0.48f, 0.04f, 0.12f), Materials.BambooLeaf, Quaternion.Euler(0f, -35f - i * 7f, 18f));
            }
        }

        private static void CreateGrassClump(Transform parent, Vector3 basePosition, string name)
        {
            for (var i = 0; i < 7; i++)
            {
                var angle = i * 51f;
                var radians = angle * Mathf.Deg2Rad;
                var blade = CreateCube(name + "_Blade_" + i, parent, basePosition + new Vector3(Mathf.Cos(radians) * 0.06f, 0.18f, Mathf.Sin(radians) * 0.06f), new Vector3(0.055f, 0.42f + (i % 3) * 0.08f, 0.025f), Materials.DeadGrassBlade);
                blade.transform.rotation = Quaternion.Euler(0f, angle, (i % 2 == 0 ? 15f : -18f));
                blade.GetComponent<Collider>().enabled = false;
            }
        }

        private static void CreateRockCluster(Transform parent, Vector3 basePosition, string name)
        {
            for (var i = 0; i < 4; i++)
            {
                CreateSphere(
                    name + "_Rock_" + i,
                    parent,
                    basePosition + new Vector3((i - 1.5f) * 0.35f, 0.12f + i * 0.03f, ((i * 7) % 5 - 2) * 0.18f),
                    new Vector3(0.32f + i * 0.04f, 0.18f + i * 0.03f, 0.28f + i * 0.02f),
                    Materials.Stone);
            }
        }

        private static void CreateStoneCairn(Transform parent, Vector3 basePosition, string name)
        {
            for (var i = 0; i < 5; i++)
            {
                var radius = 0.52f - i * 0.055f;
                var stone = CreateSphere(name + "_Stone_" + i, parent, basePosition + new Vector3(0f, i * 0.19f, 0f), new Vector3(radius, 0.15f, radius * 0.75f), Materials.Stone);
                stone.transform.rotation = Quaternion.Euler(0f, i * 23f, 0f);
            }
        }

        private static void CreateSotdae(Transform parent, Vector3 basePosition, string name)
        {
            CreateCylinderBetween(name + "_Pole", parent, basePosition + new Vector3(0f, 0.05f, 0f), basePosition + new Vector3(0f, 3.9f, 0f), 0.07f, Materials.DarkWood);
            CreateCylinderBetween(name + "_Cross", parent, basePosition + new Vector3(-0.75f, 3.55f, 0f), basePosition + new Vector3(0.75f, 3.65f, 0f), 0.05f, Materials.DarkWood);
            CreateCube(name + "_BirdBody", parent, basePosition + new Vector3(0.68f, 3.7f, 0f), new Vector3(0.42f, 0.13f, 0.18f), Materials.DarkWood, Quaternion.Euler(0f, 0f, -8f));
            CreateCube(name + "_BirdHead", parent, basePosition + new Vector3(0.94f, 3.78f, 0f), new Vector3(0.12f, 0.12f, 0.12f), Materials.DarkWood);
        }

        private static void CreatePaperCharm(Transform parent, Vector3 position, string name)
        {
            CreateCube(name + "_Paper", parent, position, new Vector3(0.28f, 0.74f, 0.035f), Materials.TalismanPaper);
            CreateCube(name + "_InkTop", parent, position + new Vector3(0f, 0.16f, -0.035f), new Vector3(0.19f, 0.035f, 0.025f), Materials.TalismanInk);
            CreateCube(name + "_InkMid", parent, position + new Vector3(0f, -0.02f, -0.035f), new Vector3(0.13f, 0.035f, 0.025f), Materials.TalismanInk);
            CreateCube(name + "_InkLow", parent, position + new Vector3(0f, -0.18f, -0.035f), new Vector3(0.21f, 0.035f, 0.025f), Materials.TalismanInk);
        }

        private static void CreatePaperLantern(Transform parent, Vector3 position, string name)
        {
            CreateCube(name + "_Bracket", parent, position + new Vector3(0f, 0.22f, 0f), new Vector3(0.08f, 0.08f, 1.1f), Materials.DarkWood);
            CreateSphere(name + "_Body", parent, position, new Vector3(0.38f, 0.52f, 0.38f), Materials.LanternPaper);
            CreateSphere(name + "_Glow", parent, position, new Vector3(0.26f, 0.38f, 0.26f), Materials.LanternGlow);
            CreatePointLight(name + "_Light", parent, position, new Color(1f, 0.45f, 0.22f), 0.75f, 4.5f);
        }

        private static void CreateRoofTileRibs(Transform parent, Vector3 center, float width, float depth, string prefix)
        {
            for (var i = 0; i < 12; i++)
            {
                var x = -width * 0.46f + i * (width * 0.92f / 11f);
                CreateCube(prefix + "_" + i, parent, center + new Vector3(x, 0.15f, 0f), new Vector3(0.07f, 0.13f, depth), Materials.RoofRidge);
            }
        }

        private static void CreateRafterRow(Transform parent, float z, string prefix)
        {
            for (var i = 0; i < 12; i++)
            {
                var x = -6.6f + i * 1.2f;
                CreateCube(prefix + "_" + i, parent, new Vector3(x, 3.52f, z), new Vector3(0.22f, 0.2f, 1.1f), Materials.DarkWood, Quaternion.Euler(13f, 0f, 0f));
            }
        }

        private static void CreateWallCapStones(Transform parent, float centerX, float z, float length, string prefix)
        {
            for (var i = 0; i < 9; i++)
            {
                var x = centerX - length * 0.42f + i * length * 0.105f;
                var cap = CreateCube(prefix + "_" + i, parent, new Vector3(x, 3.45f, z), new Vector3(1.25f, 0.22f, 0.55f), Materials.Stone);
                cap.transform.rotation = Quaternion.Euler(0f, (i % 3 - 1) * 4f, 0f);
            }
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            return CreateCube(name, parent, position, scale, CreateMaterial(name + "_Mat", color));
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            return CreateCube(name, parent, position, scale, material, Quaternion.identity);
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Quaternion rotation)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.rotation = rotation;
            cube.transform.localScale = scale;

            var renderer = cube.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return cube;
        }

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            return CreateCylinder(name, parent, position, scale, material, Quaternion.identity);
        }

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Quaternion rotation)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent);
            cylinder.transform.position = position;
            cylinder.transform.rotation = rotation;
            cylinder.transform.localScale = scale;
            cylinder.GetComponent<MeshRenderer>().sharedMaterial = material;
            return cylinder;
        }

        private static GameObject CreateCylinderBetween(string name, Transform parent, Vector3 start, Vector3 end, float radius, Material material)
        {
            var direction = end - start;
            var length = direction.magnitude;
            var rotation = length <= 0.001f ? Quaternion.identity : Quaternion.FromToRotation(Vector3.up, direction.normalized);
            return CreateCylinder(name, parent, (start + end) * 0.5f, new Vector3(radius, length * 0.5f, radius), material, rotation);
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

        private static Light CreatePointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = position;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
            return light;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            return CreateMaterial(name, color, 0.35f);
        }

        private static Material CreateMaterial(string name, Color color, float smoothness)
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
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            return material;
        }

        private static Material CreateEmissionMaterial(string name, Color color, float intensity)
        {
            var material = CreateMaterial(name, color, 0.7f);
            var emission = color * intensity;
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", emission);
                material.EnableKeyword("_EMISSION");
            }

            return material;
        }

        private static Material LoadAmbientMaterial(string materialName, string displayName, Color tint, Vector2 tiling, float smoothness)
        {
            var materialPath = $"{AmbientMaterialRoot}/{materialName}.mat";
            var source = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (source == null)
            {
                return CreateMaterial(displayName, tint, smoothness);
            }

            var material = new Material(source)
            {
                name = displayName
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tint);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", tint);
            }
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            SetTextureScale(material, "_BaseMap", tiling);
            SetTextureScale(material, "_MainTex", tiling);
            SetTextureScale(material, "_BumpMap", tiling);
            SetTextureScale(material, "_MetallicGlossMap", tiling);
            SetTextureScale(material, "_OcclusionMap", tiling);
            SetTextureScale(material, "_ParallaxMap", tiling);
            return material;
        }

        private static void SetTextureScale(Material material, string propertyName, Vector2 scale)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTextureScale(propertyName, scale);
            }
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

        private static void EnsureVisualMaterials()
        {
            if (AssetDatabase.IsValidFolder("Assets/External/ambientcg/materials"))
            {
                AmbientCgMaterialBuilder.BuildAllMaterials();
            }
            else
            {
                Debug.LogWarning("ambientCG source materials are not inside the Unity Assets folder. Falling back to procedural tint materials.");
            }
        }

        private static void ConfigureLightingProfile(KHorrorLightingProfile profile)
        {
            var serialized = new SerializedObject(profile);
            SetSerializedBool(serialized, "fogEnabled", true);
            SetSerializedFloat(serialized, "fogDensity", 0.021f);
            SetSerializedColor(serialized, "fogColor", new Color(0.045f, 0.052f, 0.052f, 1f));
            SetSerializedColor(serialized, "ambientColor", new Color(0.062f, 0.066f, 0.058f, 1f));
            SetSerializedFloat(serialized, "reflectionIntensity", 0.16f);
            SetSerializedColor(serialized, "moonColor", new Color(0.58f, 0.67f, 0.78f, 1f));
            SetSerializedFloat(serialized, "moonIntensity", 0.42f);
            SetSerializedVector3(serialized, "moonEulerAngles", new Vector3(54f, -41f, 0f));
            SetSerializedColor(serialized, "flashlightColor", new Color(1.0f, 0.94f, 0.76f, 1f));
            SetSerializedFloat(serialized, "flashlightIntensity", 14f);
            SetSerializedFloat(serialized, "flashlightRange", 34f);
            SetSerializedFloat(serialized, "flashlightSpotAngle", 54f);
            SetSerializedFloat(serialized, "exposureCompensation", 0.35f);
            SetSerializedFloat(serialized, "vignetteIntensity", 0.16f);
            SetSerializedFloat(serialized, "saturation", -8f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
        }

        private static void CreatePostProcessVolume(Transform parent)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostProcessProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, PostProcessProfilePath);
            }

            ConfigurePostProcessProfile(profile);

            var volumeObject = new GameObject("GlobalPostProcessVolume");
            volumeObject.transform.SetParent(parent);
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
        }

        private static void ConfigurePostProcessProfile(VolumeProfile profile)
        {
            var profilePath = AssetDatabase.GetAssetPath(profile);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(profilePath))
            {
                if (asset != profile && asset is VolumeComponent)
                {
                    Object.DestroyImmediate(asset, true);
                }
            }

            profile.components.Clear();

            var color = AddVolumeComponent<ColorAdjustments>(profile);
            color.postExposure.Override(0.45f);
            color.contrast.Override(6f);
            color.saturation.Override(-8f);
            color.colorFilter.Override(new Color(0.98f, 1.0f, 0.94f, 1f));

            var whiteBalance = AddVolumeComponent<WhiteBalance>(profile);
            whiteBalance.temperature.Override(-4f);
            whiteBalance.tint.Override(-2f);

            var vignette = AddVolumeComponent<Vignette>(profile);
            vignette.intensity.Override(0.16f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(new Color(0f, 0.006f, 0.003f, 1f));

            var bloom = AddVolumeComponent<Bloom>(profile);
            bloom.threshold.Override(1.15f);
            bloom.intensity.Override(0.18f);
            bloom.scatter.Override(0.55f);

            var filmGrain = AddVolumeComponent<FilmGrain>(profile);
            filmGrain.type.Override(FilmGrainLookup.Thin1);
            filmGrain.intensity.Override(0.08f);
            filmGrain.response.Override(0.68f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        private static T AddVolumeComponent<T>(VolumeProfile profile)
            where T : VolumeComponent
        {
            var component = profile.Add<T>(true);
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(component);
            return component;
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
            ConfigureRenderPipeline(pipeline);
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

        private static void ConfigureRenderPipeline(UniversalRenderPipelineAsset pipeline)
        {
            var serialized = new SerializedObject(pipeline);
            SetSerializedBool(serialized, "m_RequireDepthTexture", true);
            SetSerializedBool(serialized, "m_RequireOpaqueTexture", true);
            SetSerializedBool(serialized, "m_SupportsHDR", true);
            SetSerializedFloat(serialized, "m_RenderScale", 1f);
            SetSerializedBool(serialized, "m_MainLightShadowsSupported", true);
            SetSerializedInt(serialized, "m_MainLightShadowmapResolution", 2048);
            SetSerializedBool(serialized, "m_AdditionalLightShadowsSupported", true);
            SetSerializedInt(serialized, "m_AdditionalLightsPerObjectLimit", 6);
            SetSerializedFloat(serialized, "m_ShadowDistance", 72f);
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

        private static void SetString(Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedBool(SerializedObject serialized, string propertyName, bool value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetSerializedInt(SerializedObject serialized, string propertyName, int value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetSerializedFloat(SerializedObject serialized, string propertyName, float value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetSerializedColor(SerializedObject serialized, string propertyName, Color value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.colorValue = value;
            }
        }

        private static void SetSerializedVector3(SerializedObject serialized, string propertyName, Vector3 value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.vector3Value = value;
            }
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
            public PlayerBundle(UnityPlayerController controller, Camera camera, Light cameraLight, PlayerInteractor interactor)
            {
                Controller = controller;
                Camera = camera;
                CameraLight = cameraLight;
                Interactor = interactor;
            }

            public UnityPlayerController Controller { get; }
            public Camera Camera { get; }
            public Light CameraLight { get; }
            public PlayerInteractor Interactor { get; }
        }

        private static class Materials
        {
            public static readonly Material Road = LoadAmbientMaterial("Ground037", "Mat_MuddyRoad_PBR", new Color(0.34f, 0.29f, 0.22f, 1f), new Vector2(5f, 18f), 0.22f);
            public static readonly Material WetMud = LoadAmbientMaterial("Ground103", "Mat_WetMud_PBR", new Color(0.18f, 0.15f, 0.12f, 1f), new Vector2(3.5f, 3.5f), 0.62f);
            public static readonly Material Grass = LoadAmbientMaterial("Grass007", "Mat_DeadGrass_PBR", new Color(0.36f, 0.44f, 0.25f, 1f), new Vector2(7f, 18f), 0.18f);
            public static readonly Material DeadGrassBlade = CreateMaterial("Mat_DryGrassBlade", new Color(0.32f, 0.38f, 0.19f, 1f), 0.18f);
            public static readonly Material Bark = LoadAmbientMaterial("Bark014", "Mat_PineBark_PBR", new Color(0.72f, 0.55f, 0.38f, 1f), new Vector2(1.2f, 5f), 0.24f);
            public static readonly Material Canopy = CreateMaterial("Mat_WetPineNeedles", new Color(0.035f, 0.105f, 0.052f, 1f), 0.34f);
            public static readonly Material Bamboo = CreateMaterial("Mat_DarkBamboo", new Color(0.19f, 0.28f, 0.12f, 1f), 0.28f);
            public static readonly Material BambooLeaf = CreateMaterial("Mat_BambooLeaf", new Color(0.035f, 0.15f, 0.055f, 1f), 0.25f);
            public static readonly Material Stone = LoadAmbientMaterial("Rock064", "Mat_WetStone_PBR", new Color(0.48f, 0.48f, 0.42f, 1f), new Vector2(2.2f, 2.2f), 0.36f);
            public static readonly Material StoneWall = LoadAmbientMaterial("PavingStones150", "Mat_OldStoneWall_PBR", new Color(0.42f, 0.42f, 0.36f, 1f), new Vector2(5f, 2f), 0.32f);
            public static readonly Material DarkWood = LoadAmbientMaterial("Wood095", "Mat_AgedBlackWood_PBR", new Color(0.39f, 0.24f, 0.14f, 1f), new Vector2(3.2f, 1.1f), 0.26f);
            public static readonly Material GatePanel = LoadAmbientMaterial("Wood095", "Mat_RedBrownGateWood_PBR", new Color(0.50f, 0.22f, 0.11f, 1f), new Vector2(2.4f, 2.4f), 0.28f);
            public static readonly Material Roof = LoadAmbientMaterial("Rock064", "Mat_BlackGiwaTile_PBR", new Color(0.13f, 0.14f, 0.135f, 1f), new Vector2(6.5f, 3f), 0.45f);
            public static readonly Material RoofRidge = LoadAmbientMaterial("Rock064", "Mat_WetRoofRidge_PBR", new Color(0.095f, 0.10f, 0.095f, 1f), new Vector2(4f, 1f), 0.5f);
            public static readonly Material Courtyard = LoadAmbientMaterial("Ground103", "Mat_CourtyardPackedEarth_PBR", new Color(0.39f, 0.32f, 0.23f, 1f), new Vector2(8f, 8f), 0.25f);
            public static readonly Material Wood = LoadAmbientMaterial("Wood095", "Mat_AgedWood_PBR", new Color(0.55f, 0.34f, 0.19f, 1f), new Vector2(4f, 2f), 0.25f);
            public static readonly Material Plaster = LoadAmbientMaterial("Plaster001", "Mat_StainedPlaster_PBR", new Color(0.63f, 0.58f, 0.48f, 1f), new Vector2(3.5f, 3f), 0.22f);
            public static readonly Material DoorPaper = CreateMaterial("Mat_DirtyHanjiPaper", new Color(0.62f, 0.56f, 0.42f, 1f), 0.18f);
            public static readonly Material TalismanPaper = CreateMaterial("Mat_FadedTalismanPaper", new Color(0.78f, 0.62f, 0.23f, 1f), 0.16f);
            public static readonly Material TalismanInk = CreateMaterial("Mat_DriedRedInk", new Color(0.42f, 0.03f, 0.02f, 1f), 0.2f);
            public static readonly Material Rope = CreateMaterial("Mat_StrawRope", new Color(0.48f, 0.39f, 0.20f, 1f), 0.18f);
            public static readonly Material FadedRedCloth = CreateMaterial("Mat_FadedRedCloth", new Color(0.42f, 0.04f, 0.035f, 1f), 0.12f);
            public static readonly Material FadedWhiteCloth = CreateMaterial("Mat_FadedWhiteCloth", new Color(0.62f, 0.58f, 0.48f, 1f), 0.12f);
            public static readonly Material Jar = CreateMaterial("Mat_DarkOnggiJar", new Color(0.22f, 0.10f, 0.055f, 1f), 0.58f);
            public static readonly Material JarLid = CreateMaterial("Mat_OnggiLid", new Color(0.12f, 0.065f, 0.045f, 1f), 0.48f);
            public static readonly Material Artifact = CreateMaterial("Mat_BrassArtifact", new Color(0.68f, 0.42f, 0.13f, 1f), 0.54f);
            public static readonly Material ShrineToken = CreateEmissionMaterial("Mat_ShrineToken", new Color(0.85f, 0.72f, 0.38f, 1f), 0.35f);
            public static readonly Material Extraction = CreateEmissionMaterial("Mat_ExtractionZone", new Color(0.03f, 0.28f, 0.12f, 1f), 0.45f);
            public static readonly Material VanFloor = CreateMaterial("Mat_VanRubberFloor", new Color(0.06f, 0.065f, 0.058f, 1f), 0.42f);
            public static readonly Material VanPaint = CreateMaterial("Mat_WeatheredBongoPaint", new Color(0.28f, 0.36f, 0.38f, 1f), 0.38f);
            public static readonly Material VanWall = CreateMaterial("Mat_VanDarkInteriorPaint", new Color(0.075f, 0.083f, 0.076f, 1f), 0.32f);
            public static readonly Material VanRoof = CreateMaterial("Mat_VanDullRoof", new Color(0.20f, 0.24f, 0.245f, 1f), 0.30f);
            public static readonly Material VanDoor = CreateMaterial("Mat_VanRearDoor", new Color(0.24f, 0.31f, 0.31f, 1f), 0.38f);
            public static readonly Material VanWindow = CreateMaterial("Mat_DirtyVanWindow", new Color(0.055f, 0.085f, 0.095f, 1f), 0.62f);
            public static readonly Material Tire = CreateMaterial("Mat_WornTireRubber", new Color(0.018f, 0.018f, 0.016f, 1f), 0.34f);
            public static readonly Material Terminal = CreateMaterial("Mat_TerminalCase", new Color(0.012f, 0.045f, 0.036f, 1f), 0.46f);
            public static readonly Material ScreenGlow = CreateEmissionMaterial("Mat_ScreenGlow", new Color(0.1f, 0.85f, 0.55f, 1f), 1.6f);
            public static readonly Material LanternPaper = CreateMaterial("Mat_AgedLanternPaper", new Color(0.86f, 0.50f, 0.28f, 1f), 0.24f);
            public static readonly Material LanternGlow = CreateEmissionMaterial("Mat_LanternGlow", new Color(1f, 0.43f, 0.18f, 1f), 1.9f);
            public static readonly Material CandleWax = CreateMaterial("Mat_CandleWax", new Color(0.82f, 0.73f, 0.55f, 1f), 0.28f);
            public static readonly Material RustedMetal = CreateMaterial("Mat_RustedIron", new Color(0.18f, 0.12f, 0.08f, 1f), 0.38f);
            public static readonly Material OfficeFloor = CreateMaterial("Mat_OfficeFloor", new Color(0.095f, 0.095f, 0.085f, 1f), 0.32f);
            public static readonly Material Altar = LoadAmbientMaterial("Wood095", "Mat_DarkAltarWood_PBR", new Color(0.25f, 0.095f, 0.055f, 1f), new Vector2(1.8f, 1.2f), 0.32f);
            public static readonly Material Night = CreateMaterial("Mat_TravelNight", new Color(0.012f, 0.018f, 0.016f, 1f), 0.2f);
            public static readonly Material DistantHill = CreateMaterial("Mat_DistantTreeSilhouette", new Color(0.006f, 0.017f, 0.011f, 1f), 0.22f);
        }
    }
}
