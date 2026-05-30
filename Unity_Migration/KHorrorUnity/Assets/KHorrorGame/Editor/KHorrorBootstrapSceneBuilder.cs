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
        private const float ShrineX = -8f;
        private const float ShrineZ = 141f;

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
            systems.AddComponent<TerritoryResolver>();

            CreateLighting(systems.transform, player.CameraLight, player.Camera);
            CreateHud(systems.transform, controller, player.Controller, player.Interactor);
            var hubCargoHold = CreateBongoHub(bongoHub.transform, controller);
            var estateCargoHold = CreateEstateProxy(estate.transform, controller, player.Controller);
            SetObject(controller, "hubCargoHold", hubCargoHold);
            SetObject(controller, "estateCargoHold", estateCargoHold);
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
            player.AddComponent<PlayerDamageReceiver>();

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
            var left = CreateMarker("LeftHandHeldMount", new Vector3(-0.30f, -0.22f, 0.43f), Quaternion.identity, mounts.transform);
            var right = CreateMarker("RightHandHeldMount", new Vector3(0.30f, -0.22f, 0.43f), Quaternion.identity, mounts.transform);
            var both = CreateMarker("TwoHandHeldMount", new Vector3(0f, -0.24f, 0.42f), Quaternion.identity, mounts.transform);

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
            CreatePointLight("ShrineCandleGlow", parent, new Vector3(ShrineX, 1.35f, ShrineZ + 1f), new Color(1f, 0.46f, 0.25f), 2.5f, 8f);
        }

        private static VanCargoHold CreateBongoHub(Transform parent, GameLoopController gameLoop)
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

            var cargoRoot = new GameObject("BongoHubCargoHold");
            cargoRoot.transform.SetParent(parent);
            cargoRoot.transform.localPosition = Vector3.zero;
            cargoRoot.transform.localRotation = Quaternion.identity;
            var cargoHold = cargoRoot.AddComponent<VanCargoHold>();
            RegisterCargoSlot(cargoRoot.transform, cargoHold, "BongoHubCargoSlot_0", new Vector3(-0.82f, 0.62f, -3.35f));
            RegisterCargoSlot(cargoRoot.transform, cargoHold, "BongoHubCargoSlot_1", new Vector3(0f, 0.62f, -3.35f));
            RegisterCargoSlot(cargoRoot.transform, cargoHold, "BongoHubCargoSlot_2", new Vector3(0.82f, 0.62f, -3.35f));
            RegisterFallbackCargoSlot(cargoRoot.transform, cargoHold, "BongoHubCargoFallback", new Vector3(-0.82f, 0.62f, -2.78f));
            CreateCube("BongoHubCargoPad", parent, new Vector3(0f, 0.32f, -3.28f), new Vector3(2.8f, 0.08f, 1.25f), Materials.Extraction).GetComponent<Collider>().enabled = false;
            return cargoHold;
        }

        private static VanCargoHold CreateEstateProxy(Transform parent, GameLoopController gameLoop, UnityPlayerController player)
        {
            var forestRoot = CreateZoneRoot(parent, "ForestApproach");
            var gateRoot = CreateZoneRoot(parent, "FrontGateBoundary");
            var courtyardRoot = CreateZoneRoot(parent, "Courtyard");
            var mainHouseRoot = CreateZoneRoot(parent, "MainHouse");
            var backRouteRoot = CreateZoneRoot(parent, "BackRoute");
            var shrineRoot = CreateZoneRoot(parent, "Shrine");

            CreateTerritoryVolume("ForestApproachTerritoryVolume", forestRoot, TerritoryKind.ForestApproach, new Vector3(0f, 1.5f, 31f), new Vector3(24f, 5f, 46f), 10);
            CreateTerritoryVolume("EstateInteriorTerritoryVolume", parent, TerritoryKind.EstateInterior, new Vector3(0f, 1.5f, 101.5f), new Vector3(24f, 5f, 93f), 0);
            CreateTerritoryVolume("ShrineTerritoryVolume", shrineRoot, TerritoryKind.EstateInterior, new Vector3(ShrineX, 1.5f, ShrineZ), new Vector3(8f, 5f, 14f), 5);
            CreateColliderOnlyBox("FrontGateAIBoundaryBlocker", gateRoot, new Vector3(0f, 1.5f, 54.05f), new Vector3(7.2f, 3f, 0.35f), false);

            CreateCube("ApproachRoad_MuddyCenter", forestRoot, new Vector3(0f, 0f, 34f), new Vector3(7f, 0.2f, 36f), Materials.Road);
            CreateCube("ApproachRoad_GrassLeft", forestRoot, new Vector3(-5.5f, 0.02f, 34f), new Vector3(4f, 0.16f, 36f), Materials.Grass);
            CreateCube("ApproachRoad_GrassRight", forestRoot, new Vector3(5.5f, 0.02f, 34f), new Vector3(4f, 0.16f, 36f), Materials.Grass);
            CreateCube("OuterGateThresholdStone", gateRoot, new Vector3(0f, 0.08f, 53f), new Vector3(8f, 0.24f, 1.2f), Materials.Stone);
            CreateCube("OuterGateContinuousUnderfloor", gateRoot, new Vector3(0f, -0.08f, 54.45f), new Vector3(9.2f, 0.42f, 4.6f), Materials.Stone);
            CreateCube("OuterGatePackedEarthBridge", gateRoot, new Vector3(0f, 0f, 55.05f), new Vector3(8.3f, 0.2f, 4.4f), Materials.Courtyard);
            CreateEstateGroundContinuity(parent);
            var gateInsideSpawn = CreateMarker("EstateGateInsideSpawn", new Vector3(0f, 1f, 59.8f), Quaternion.identity, gateRoot);
            var gateOutsideSpawn = CreateMarker("EstateGateOutsideSpawn", new Vector3(0f, 1f, 51.25f), Quaternion.Euler(0f, 180f, 0f), gateRoot);

            CreateDistantSilhouette(parent);
            CreateForest(forestRoot);
            CreateOuterGate(gateRoot, gateInsideSpawn, gateOutsideSpawn);
            CreateCourtyard(courtyardRoot);
            CreateMainHouse(mainHouseRoot);
            CreateSarangchae(courtyardRoot);
            CreateRearCompound(backRouteRoot);
            CreateShrineLoop(shrineRoot);

            CreateEstateArtifacts(parent, gameLoop);
            CreateRuntimeThreatSpawner(parent, gameLoop, player);
            return CreateEstateReturnBongo(parent, gameLoop);
        }

        private static void CreateEstateArtifacts(Transform parent, GameLoopController gameLoop)
        {
            CreateArtifact("Artifact_BrassBowl", parent, gameLoop, new Vector3(2.2f, 0.5f, 70f), new Vector3(0.45f, 0.3f, 0.45f), Materials.Artifact, "Brass bowl", 260, 2.1f, 1, 2);
            CreateArtifact("Artifact_CourtyardLedger", parent, gameLoop, new Vector3(-4.7f, 0.48f, 63.05f), new Vector3(0.46f, 0.12f, 0.34f), Materials.TalismanPaper, "Family ledger", 180, 0.8f, 1, 1);
            CreateArtifact("Artifact_MainHouseScroll", parent, gameLoop, new Vector3(-1.6f, 0.85f, 82.35f), new Vector3(0.22f, 0.22f, 0.64f), Materials.Rope, "Ancestral scroll", 320, 1.0f, 2, 1);
            CreateArtifact("Artifact_SarangchaeComb", parent, gameLoop, new Vector3(8.85f, 0.72f, 76.7f), new Vector3(0.36f, 0.08f, 0.22f), Materials.Artifact, "Horn comb", 140, 0.4f, 1, 1);
            CreateArtifact("Artifact_KitchenCharm", parent, gameLoop, new Vector3(-3.4f, 0.72f, 90.4f), new Vector3(0.28f, 0.38f, 0.06f), Materials.TalismanPaper, "Kitchen talisman", 120, 0.2f, 1, 1, new[] { PaperDoorInteraction.TalismanTag });
            CreateArtifact("Artifact_RearGardenKnife", parent, gameLoop, new Vector3(4.2f, 0.62f, 112.1f), new Vector3(0.58f, 0.08f, 0.16f), Materials.RustedMetal, "Ritual knife", 280, 0.7f, 2, 1);
            CreateArtifact("Artifact_ShrineBell", parent, gameLoop, new Vector3(ShrineX - 0.65f, 0.88f, ShrineZ - 0.8f), new Vector3(0.28f, 0.32f, 0.28f), Materials.ShrineToken, "Shrine bell", 420, 0.7f, 3, 1, new[] { "shrine_item" });
            CreateArtifact("Artifact_ShrineToken", parent, gameLoop, new Vector3(ShrineX + 0.45f, 0.88f, ShrineZ - 0.75f), new Vector3(0.36f, 0.28f, 0.36f), Materials.ShrineToken, "Shrine token", 500, 1.0f, 4, 1, new[] { "shrine_item" });
            CreateArtifact("Artifact_JonggaSpiritTablet", parent, gameLoop, new Vector3(ShrineX, 1.48f, ShrineZ + 1.12f), new Vector3(0.64f, 0.92f, 0.18f), Materials.ShrineToken, "Jongga spirit tablet", 760, 1.6f, 5, 2, new[] { "shrine_item", "jongga_relic" });
        }

        private static void CreateArtifact(
            string name,
            Transform parent,
            GameLoopController gameLoop,
            Vector3 position,
            Vector3 scale,
            Material material,
            string displayName,
            int value,
            float weight,
            int resentmentGain,
            int handSlots,
            string[] tags = null)
        {
            var artifact = CreateCube(name, parent, position, scale, material);
            var pickup = artifact.AddComponent<ArtifactPickup>();
            pickup.ApplyDefinition(new ArtifactDefinition(displayName, value, weight, resentmentGain, tags, handSlots));
            SetObject(pickup, "gameLoop", gameLoop);
            EditorUtility.SetDirty(pickup);
        }

        private static void AttachPaperDoorInteraction(GameObject doorObject, string displayName)
        {
            var door = doorObject.AddComponent<PaperDoorInteraction>();
            door.Configure(displayName);
            EditorUtility.SetDirty(door);
        }

        private static void CreateRuntimeThreatSpawner(Transform parent, GameLoopController gameLoop, UnityPlayerController player)
        {
            var root = new GameObject("RuntimeThreatSpawner");
            root.transform.SetParent(parent);
            var spawner = root.AddComponent<RuntimeThreatSpawner>();
            SetObject(spawner, "gameLoop", gameLoop);
            SetObject(spawner, "playerTarget", player.transform);

            var ghostAnchors = new[]
            {
                CreateMarker("GhostSpawnAnchor_ShrineThreshold", new Vector3(-4.9f, 0.95f, 136.2f), Quaternion.Euler(0f, 150f, 0f), root.transform),
                CreateMarker("GhostSpawnAnchor_AnchaeInterior", new Vector3(-8.15f, 0.95f, 128.8f), Quaternion.Euler(0f, 180f, 0f), root.transform),
                CreateMarker("GhostSpawnAnchor_BackStorehouse", new Vector3(3.4f, 0.95f, 116.8f), Quaternion.Euler(0f, -165f, 0f), root.transform),
            };
            var dokkaebiAnchors = new[]
            {
                CreateMarker("DokkaebiSpawnAnchor_ForestApproach", new Vector3(4.3f, 0.92f, 36.5f), Quaternion.Euler(0f, -35f, 0f), root.transform),
                CreateMarker("DokkaebiSpawnAnchor_ForestJangseung", new Vector3(-4.8f, 0.92f, 44.2f), Quaternion.Euler(0f, 40f, 0f), root.transform),
            };

            var ghosts = new[]
            {
                CreateGhostActor(root.transform, "RuntimeGhostActor", ghostAnchors[0].position, ghostAnchors[0].rotation, player.transform),
                CreateGhostActor(root.transform, "RuntimeGhostActor_02", ghostAnchors[1].position, ghostAnchors[1].rotation, player.transform),
                CreateGhostActor(root.transform, "RuntimeGhostActor_03", ghostAnchors[2].position, ghostAnchors[2].rotation, player.transform),
            };
            var dokkaebi = new[]
            {
                CreateDokkaebiActor(root.transform, "RuntimeDokkaebiActor", dokkaebiAnchors[0].position, dokkaebiAnchors[0].rotation, player.transform),
                CreateDokkaebiActor(root.transform, "RuntimeDokkaebiActor_02", dokkaebiAnchors[1].position, dokkaebiAnchors[1].rotation, player.transform),
            };

            var cue = CreatePointLight("ThreatSpawnCueLight", root.transform, new Vector3(-4.9f, 2.3f, 136.2f), new Color(1f, 0.2f, 0.1f), 2.4f, 11f);
            cue.enabled = false;

            foreach (var ghost in ghosts)
            {
                AttachThreatAudio(ghost, player.transform);
            }

            foreach (var forestActor in dokkaebi)
            {
                AttachThreatAudio(forestActor, player.transform);
            }

            var atmosphere = root.AddComponent<ThreatAtmosphereCue>();
            SetObject(atmosphere, "gameLoop", gameLoop);
            SetObjectArray(
                atmosphere,
                "threatLights",
                cue,
                FindSceneLight("DeepShrineLanternGlow"),
                FindSceneLight("RearRouteLanternPool_Second"),
                FindSceneLight("RearRouteLanternPool_Third"));

            SetObject(spawner, "ghostActor", ghosts[0]);
            SetObject(spawner, "dokkaebiActor", dokkaebi[0]);
            SetObject(spawner, "ghostSpawnAnchor", ghostAnchors[0]);
            SetObject(spawner, "dokkaebiSpawnAnchor", dokkaebiAnchors[0]);
            SetObjectArray(spawner, "ghostActors", ghosts);
            SetObjectArray(spawner, "dokkaebiActors", dokkaebi);
            SetObjectArray(spawner, "ghostSpawnAnchors", ghostAnchors);
            SetObjectArray(spawner, "dokkaebiSpawnAnchors", dokkaebiAnchors);
            SetObject(spawner, "spawnCueLight", cue);
        }

        private static void AttachThreatAudio(EnemyBrain brain, Transform listener)
        {
            var actor = brain.gameObject;
            var source = actor.AddComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.playOnAwake = false;
            source.loop = true;
            source.volume = 0.15f;
            source.minDistance = 1.5f;
            source.maxDistance = 18f;

            var filter = actor.AddComponent<AudioLowPassFilter>();
            filter.cutoffFrequency = 22000f;

            var occlusion = actor.AddComponent<ThreatAudioOcclusion>();
            occlusion.Configure(brain, listener, source, filter);
            EditorUtility.SetDirty(source);
            EditorUtility.SetDirty(filter);
            EditorUtility.SetDirty(occlusion);
        }

        private static Light FindSceneLight(string name)
        {
            var found = GameObject.Find(name);
            return found != null ? found.GetComponent<Light>() : null;
        }

        private static EnemyBrain CreateGhostActor(
            Transform parent,
            string actorName,
            Vector3 position,
            Quaternion rotation,
            Transform playerTarget)
        {
            var actor = new GameObject(actorName);
            actor.transform.SetParent(parent);
            actor.transform.SetPositionAndRotation(position, rotation);
            var brain = actor.AddComponent<EnemyBrain>();
            brain.Configure(EnemyKind.Ghost, ThreatStageProfile.ForStage(4), playerTarget, TerritoryKind.EstateInterior, position);
            brain.SetAutomaticTick(false);

            CreateLocalPrimitive(PrimitiveType.Cylinder, "RuntimeGhostActor_Body", actor.transform, new Vector3(0f, 0.68f, 0f), new Vector3(0.33f, 0.68f, 0.33f), Materials.GhostBody);
            CreateLocalPrimitive(PrimitiveType.Sphere, "RuntimeGhostActor_Head", actor.transform, new Vector3(0f, 1.5f, 0f), new Vector3(0.48f, 0.52f, 0.48f), Materials.GhostBody);
            CreateLocalPrimitive(PrimitiveType.Cube, "RuntimeGhostActor_HairVeil", actor.transform, new Vector3(0f, 1.22f, -0.2f), new Vector3(0.78f, 1.25f, 0.055f), Materials.Night);
            CreateLocalPrimitive(PrimitiveType.Cube, "RuntimeGhostActor_SleeveLeft", actor.transform, new Vector3(-0.42f, 0.96f, 0.02f), new Vector3(0.12f, 0.7f, 0.12f), Materials.GhostBody);
            CreateLocalPrimitive(PrimitiveType.Cube, "RuntimeGhostActor_SleeveRight", actor.transform, new Vector3(0.42f, 0.96f, 0.02f), new Vector3(0.12f, 0.7f, 0.12f), Materials.GhostBody);

            actor.SetActive(false);
            return brain;
        }

        private static EnemyBrain CreateDokkaebiActor(
            Transform parent,
            string actorName,
            Vector3 position,
            Quaternion rotation,
            Transform playerTarget)
        {
            var actor = new GameObject(actorName);
            actor.transform.SetParent(parent);
            actor.transform.SetPositionAndRotation(position, rotation);
            var brain = actor.AddComponent<EnemyBrain>();
            brain.Configure(EnemyKind.Dokkaebi, ThreatStageProfile.ForStage(3), playerTarget, TerritoryKind.ForestApproach, position);
            brain.SetAutomaticTick(false);

            CreateLocalPrimitive(PrimitiveType.Cylinder, "RuntimeDokkaebiActor_Body", actor.transform, new Vector3(0f, 0.58f, 0f), new Vector3(0.42f, 0.58f, 0.42f), Materials.DokkaebiBody);
            CreateLocalPrimitive(PrimitiveType.Sphere, "RuntimeDokkaebiActor_Head", actor.transform, new Vector3(0f, 1.34f, 0f), new Vector3(0.58f, 0.5f, 0.58f), Materials.DokkaebiBody);
            CreateLocalPrimitive(PrimitiveType.Cube, "RuntimeDokkaebiActor_HornLeft", actor.transform, new Vector3(-0.26f, 1.77f, 0f), new Vector3(0.09f, 0.42f, 0.09f), Materials.RustedMetal, Quaternion.Euler(0f, 0f, -25f));
            CreateLocalPrimitive(PrimitiveType.Cube, "RuntimeDokkaebiActor_HornRight", actor.transform, new Vector3(0.26f, 1.77f, 0f), new Vector3(0.09f, 0.42f, 0.09f), Materials.RustedMetal, Quaternion.Euler(0f, 0f, 25f));
            CreateLocalPrimitive(PrimitiveType.Cube, "RuntimeDokkaebiActor_Club", actor.transform, new Vector3(0.5f, 0.76f, -0.12f), new Vector3(0.16f, 0.9f, 0.16f), Materials.DarkWood, Quaternion.Euler(0f, 0f, -18f));

            actor.SetActive(false);
            return brain;
        }

        private static void CreateEstateGroundContinuity(Transform parent)
        {
            CreateCube("EstateContinuousEarthUnderlay", parent, new Vector3(0f, -0.055f, 76f), new Vector3(24f, 0.11f, 152f), Materials.Courtyard);
            CreateCube("BongoToForestApron", parent, new Vector3(0f, 0.02f, 8.35f), new Vector3(8.4f, 0.16f, 15.5f), Materials.Road);
            CreateCube("BongoApronGrassLeft", parent, new Vector3(-5.9f, 0f, 8.35f), new Vector3(3.3f, 0.12f, 15.5f), Materials.Grass);
            CreateCube("BongoApronGrassRight", parent, new Vector3(5.9f, 0f, 8.35f), new Vector3(3.3f, 0.12f, 15.5f), Materials.Grass);
            CreateCube("ApproachRoadSeamPatch", parent, new Vector3(0f, 0.045f, 16.15f), new Vector3(7.2f, 0.13f, 1.1f), Materials.Road);
            CreateCube("InsideGateLandingPad", parent, new Vector3(0f, 0.03f, 58.2f), new Vector3(8.2f, 0.14f, 3.2f), Materials.Courtyard);
            CreateCube("CourtyardToMainHousePad", parent, new Vector3(0f, 0.04f, 78.2f), new Vector3(8.6f, 0.14f, 2.2f), Materials.Courtyard);
            CreateCube("RearCompoundUnderlayPad", parent, new Vector3(0f, 0.035f, 111f), new Vector3(22.2f, 0.13f, 45f), Materials.Courtyard);
            CreateCube("ShrineEntryPackedEarthPad", parent, new Vector3(ShrineX, 0.04f, ShrineZ - 2.85f), new Vector3(4.4f, 0.14f, 1.5f), Materials.Road);
            CreateCube("SouthMapBoundaryDitch", parent, new Vector3(0f, 1.9f, 0.5f), new Vector3(25f, 3.8f, 1.2f), Materials.DistantHill);
            CreateCube("WestMapBoundaryRidge", parent, new Vector3(-12.35f, 2.1f, 76f), new Vector3(0.9f, 4.2f, 152f), Materials.DistantHill);
            CreateCube("EastMapBoundaryRidge", parent, new Vector3(12.35f, 2.1f, 76f), new Vector3(0.9f, 4.2f, 152f), Materials.DistantHill);
            CreateCube("NorthMapBoundaryRidge", parent, new Vector3(0f, 2.4f, 152.5f), new Vector3(25f, 4.8f, 1.4f), Materials.DistantHill);
        }

        private static void CreateSettlementProxy(Transform parent, GameLoopController gameLoop)
        {
            CreateCube("SettlementFloor", parent, new Vector3(0f, 0f, -34f), new Vector3(16f, 0.2f, 18f), Materials.OfficeFloor);
            CreateCube("SettlementBackWall", parent, new Vector3(0f, 1.8f, -25.1f), new Vector3(16f, 3.6f, 0.25f), Materials.Plaster);
            CreateCube("SettlementFrontWall_Left", parent, new Vector3(-5.4f, 1.8f, -42.9f), new Vector3(5.2f, 3.6f, 0.25f), Materials.Plaster);
            CreateCube("SettlementFrontWall_Right", parent, new Vector3(5.4f, 1.8f, -42.9f), new Vector3(5.2f, 3.6f, 0.25f), Materials.Plaster);
            CreateCube("SettlementLeftWall", parent, new Vector3(-8.1f, 1.8f, -34f), new Vector3(0.25f, 3.6f, 18f), Materials.Plaster);
            CreateCube("SettlementRightWall", parent, new Vector3(8.1f, 1.8f, -34f), new Vector3(0.25f, 3.6f, 18f), Materials.Plaster);
            CreateCube("SettlementCeiling", parent, new Vector3(0f, 3.6f, -34f), new Vector3(16.4f, 0.25f, 18.4f), Materials.Wood);
            CreateCube("SettlementServiceCounter", parent, new Vector3(0f, 0.78f, -28.3f), new Vector3(8.8f, 1.2f, 1.05f), Materials.DarkWood);
            CreateCube("SettlementCounterGrate", parent, new Vector3(0f, 1.92f, -28.05f), new Vector3(8.4f, 1.45f, 0.08f), Materials.RustedMetal);
            CreateCube("SettlementQueueRail_Left", parent, new Vector3(-2.3f, 0.75f, -35.2f), new Vector3(0.16f, 0.9f, 7.8f), Materials.RustedMetal);
            CreateCube("SettlementQueueRail_Right", parent, new Vector3(2.3f, 0.75f, -35.2f), new Vector3(0.16f, 0.9f, 7.8f), Materials.RustedMetal);
            CreateCube("SettlementCargoScale", parent, new Vector3(-3.5f, 1.22f, -27.45f), new Vector3(1.55f, 0.25f, 1.0f), Materials.RustedMetal);
            CreateCube("SettlementWarningBoard", parent, new Vector3(5.2f, 1.65f, -25.35f), new Vector3(2.1f, 1.1f, 0.08f), Materials.TalismanPaper);

            var station = CreateCube("SettlementStation", parent, new Vector3(0f, 1.15f, -27.45f), new Vector3(1.6f, 1.1f, 0.5f), Materials.Terminal);
            var settlementStation = station.AddComponent<SettlementStation>();
            SetObject(settlementStation, "gameLoop", gameLoop);

            var returnTablet = CreateCube("SettlementReturnTablet", parent, new Vector3(0f, 1.2f, -40.15f), new Vector3(1.45f, 0.78f, 0.12f), Materials.Terminal);
            var terminal = returnTablet.AddComponent<BongoTerminal>();
            SetObject(terminal, "gameLoop", gameLoop);
            CreateCube("SettlementReturnScreenGlow", parent, new Vector3(0f, 1.2f, -40.08f), new Vector3(1.18f, 0.52f, 0.03f), Materials.ScreenGlow).GetComponent<Collider>().enabled = false;

            CreateCube("SettlementCollectorShadow", parent, new Vector3(-5.2f, 1.25f, -28.85f), new Vector3(1.0f, 2.25f, 0.55f), Materials.Night).GetComponent<Collider>().enabled = false;
            CreateCube("SettlementCollectorHead", parent, new Vector3(-5.2f, 2.65f, -28.85f), new Vector3(0.72f, 0.55f, 0.45f), Materials.GhostBody).GetComponent<Collider>().enabled = false;
            var threatZone = CreateCube("SettlementCollectorThreatZone", parent, new Vector3(-4.7f, 1.05f, -31.35f), new Vector3(3.2f, 2.1f, 4.2f), Materials.Night);
            threatZone.GetComponent<Collider>().isTrigger = true;
            threatZone.AddComponent<SettlementThreatZone>();
            CreatePointLight("SettlementCollectorWarningGlow", parent, new Vector3(-4.7f, 2.2f, -30.7f), new Color(1f, 0.05f, 0.03f), 1.4f, 7f);
            CreatePointLight("SettlementOfficeDimFill", parent, new Vector3(1.2f, 2.8f, -36.5f), new Color(0.46f, 0.54f, 0.48f), 1.2f, 10f);
        }

        private static void CreateTravelProxy(Transform parent)
        {
            CreateCube("TravelMotionBackdrop", parent, new Vector3(0f, 1.2f, 8f), new Vector3(8f, 2.5f, 0.2f), Materials.Night);
        }

        private static VanCargoHold CreateEstateReturnBongo(Transform parent, GameLoopController gameLoop)
        {
            var root = new GameObject("EstateReturnBongo");
            root.transform.SetParent(parent);
            root.transform.position = new Vector3(-2.2f, 0f, 20.1f);
            root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var cargoHold = root.AddComponent<VanCargoHold>();
            RegisterCargoSlot(root.transform, cargoHold, "ReturnBongoCargoSlot_0", new Vector3(-0.82f, 0.62f, 3.25f));
            RegisterCargoSlot(root.transform, cargoHold, "ReturnBongoCargoSlot_1", new Vector3(0f, 0.62f, 3.25f));
            RegisterCargoSlot(root.transform, cargoHold, "ReturnBongoCargoSlot_2", new Vector3(0.82f, 0.62f, 3.25f));
            RegisterFallbackCargoSlot(root.transform, cargoHold, "ReturnBongoCargoFallback", new Vector3(-0.82f, 0.62f, 2.68f));

            CreateCube("ReturnBongoSideWallLeft", root.transform, new Vector3(-2.08f, 1.16f, 0.36f), new Vector3(0.22f, 1.95f, 5.72f), Materials.VanPaint);
            CreateCube("ReturnBongoSideWallRight", root.transform, new Vector3(2.08f, 1.16f, 0.36f), new Vector3(0.22f, 1.95f, 5.72f), Materials.VanPaint);
            CreateCube("ReturnBongoFrontBulkhead", root.transform, new Vector3(0f, 1.28f, -2.68f), new Vector3(3.95f, 1.85f, 0.22f), Materials.VanPaint);
            CreateCube("ReturnBongoRoof", root.transform, new Vector3(0f, 2.28f, 0f), new Vector3(4.35f, 0.32f, 6.35f), Materials.VanRoof);
            CreateCube("ReturnBongoFrontCab", root.transform, new Vector3(0f, 1.35f, -3.55f), new Vector3(4.05f, 1.8f, 1.15f), Materials.VanPaint);
            CreateCube("ReturnBongoWindshield", root.transform, new Vector3(0f, 1.72f, -4.14f), new Vector3(2.9f, 0.72f, 0.08f), Materials.VanWindow);
            CreateCube("ReturnBongoSideWindowLeft", root.transform, new Vector3(-2.13f, 1.72f, -2.1f), new Vector3(0.08f, 0.62f, 1.35f), Materials.VanWindow);
            CreateCube("ReturnBongoSideWindowRight", root.transform, new Vector3(2.13f, 1.72f, -2.1f), new Vector3(0.08f, 0.62f, 1.35f), Materials.VanWindow);
            CreateCube("ReturnBongoRearFloor", root.transform, new Vector3(0f, 0.18f, 3.35f), new Vector3(3.55f, 0.16f, 1.75f), Materials.VanFloor);
            var rearDoorLeft = CreateCube("ReturnBongoRearDoorLeft", root.transform, new Vector3(-2.32f, 1.12f, 3.28f), new Vector3(0.14f, 1.82f, 1.12f), Materials.VanDoor);
            rearDoorLeft.GetComponent<Collider>().enabled = false;
            rearDoorLeft.transform.rotation = Quaternion.Euler(0f, -22f, 0f);
            var rearDoorRight = CreateCube("ReturnBongoRearDoorRight", root.transform, new Vector3(2.32f, 1.12f, 3.28f), new Vector3(0.14f, 1.82f, 1.12f), Materials.VanDoor);
            rearDoorRight.GetComponent<Collider>().enabled = false;
            rearDoorRight.transform.rotation = Quaternion.Euler(0f, 22f, 0f);
            CreateCube("ReturnBongoRearStep", root.transform, new Vector3(0f, 0.18f, 4.28f), new Vector3(3.3f, 0.2f, 0.75f), Materials.Road);
            var returnLever = CreateCube("ReturnBongoReturnLever", root.transform, new Vector3(-1.42f, 0.92f, 2.52f), new Vector3(0.42f, 0.62f, 0.28f), Materials.Terminal);
            var returnTerminal = returnLever.AddComponent<BongoReturnTerminal>();
            SetObject(returnTerminal, "gameLoop", gameLoop);
            var cargoDepositZone = CreateColliderOnlyBox("ReturnBongoCargoDepositZone", root.transform, new Vector3(0f, 1.05f, 3.25f), new Vector3(3.35f, 2.15f, 2.15f), true);
            var cargoDeposit = cargoDepositZone.AddComponent<VanCargoDepositZone>();
            SetObject(cargoDeposit, "gameLoop", gameLoop);
            SetObject(cargoDeposit, "cargoHold", cargoHold);
            CreateCube("ReturnBongoCargoPad_G", root.transform, new Vector3(0f, 0.32f, 3.38f), new Vector3(2.8f, 0.08f, 1.25f), Materials.Extraction).GetComponent<Collider>().enabled = false;
            CreateCube("ReturnBongoFrontBumper", root.transform, new Vector3(0f, 0.55f, -4.2f), new Vector3(3.7f, 0.32f, 0.18f), Materials.RustedMetal);
            CreateCube("ReturnBongoRearBumper", root.transform, new Vector3(0f, 0.55f, 4.18f), new Vector3(3.7f, 0.32f, 0.18f), Materials.RustedMetal);

            CreateCylinder("ReturnBongoWheel_FL", root.transform, new Vector3(-2.15f, 0.52f, -2.35f), new Vector3(0.42f, 0.22f, 0.42f), Materials.Tire, Quaternion.Euler(0f, 0f, 90f));
            CreateCylinder("ReturnBongoWheel_FR", root.transform, new Vector3(2.15f, 0.52f, -2.35f), new Vector3(0.42f, 0.22f, 0.42f), Materials.Tire, Quaternion.Euler(0f, 0f, 90f));
            CreateCylinder("ReturnBongoWheel_RL", root.transform, new Vector3(-2.15f, 0.52f, 2.35f), new Vector3(0.42f, 0.22f, 0.42f), Materials.Tire, Quaternion.Euler(0f, 0f, 90f));
            CreateCylinder("ReturnBongoWheel_RR", root.transform, new Vector3(2.15f, 0.52f, 2.35f), new Vector3(0.42f, 0.22f, 0.42f), Materials.Tire, Quaternion.Euler(0f, 0f, 90f));

            CreatePointLight("ReturnBongoCabGlow", root.transform, new Vector3(0f, 1.8f, 2.2f), new Color(0.72f, 0.88f, 0.72f), 1.1f, 4.5f);
            CreatePointLight("ReturnBongoTailLampLeft", root.transform, new Vector3(-1.58f, 0.92f, 4.1f), new Color(1f, 0.08f, 0.035f), 0.95f, 3f);
            CreatePointLight("ReturnBongoTailLampRight", root.transform, new Vector3(1.58f, 0.92f, 4.1f), new Color(1f, 0.08f, 0.035f), 0.95f, 3f);
            return cargoHold;
        }

        private static void RegisterCargoSlot(Transform parent, VanCargoHold cargoHold, string name, Vector3 localPosition)
        {
            var slot = new GameObject(name).transform;
            slot.SetParent(parent, false);
            slot.localPosition = localPosition;
            cargoHold.RegisterSlot(slot);
        }

        private static void RegisterFallbackCargoSlot(Transform parent, VanCargoHold cargoHold, string name, Vector3 localPosition)
        {
            var slot = new GameObject(name).transform;
            slot.SetParent(parent, false);
            slot.localPosition = localPosition;
            cargoHold.RegisterFallbackSlot(slot);
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
            CreateCube("LeftGateWallConnector", parent, new Vector3(-4.35f, 1.7f, 56.15f), new Vector3(0.75f, 3.4f, 4.6f), Materials.StoneWall);
            CreateCube("RightGateWallConnector", parent, new Vector3(4.35f, 1.7f, 56.15f), new Vector3(0.75f, 3.4f, 4.6f), Materials.StoneWall);
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
            CreateCube("MainHouseFrontLowStep", parent, new Vector3(0f, 0.16f, 78.55f), new Vector3(6.2f, 0.22f, 0.9f), Materials.Stone);
            CreateCube("MainHouseFrontHighStep", parent, new Vector3(0f, 0.34f, 79.1f), new Vector3(4.4f, 0.22f, 0.75f), Materials.Stone);
            CreateCube("MainHouseBackWall_Right", parent, new Vector3(1.0f, 2f, 86.6f), new Vector3(11.6f, 3.5f, 0.35f), Materials.Plaster);
            CreateCube("MainHouseBackWall_LeftStub", parent, new Vector3(-6.95f, 2f, 86.6f), new Vector3(0.25f, 3.5f, 0.35f), Materials.Plaster);
            CreateCube("MainHouseLeftWall", parent, new Vector3(-7f, 2f, 83f), new Vector3(0.35f, 3.5f, 7f), Materials.Plaster);
            CreateCube("MainHouseRightWall", parent, new Vector3(7f, 2f, 83f), new Vector3(0.35f, 3.5f, 7f), Materials.Plaster);
            CreateCube("MainHouseFrontBeam", parent, new Vector3(0f, 3.25f, 79.7f), new Vector3(14.8f, 0.35f, 0.35f), Materials.DarkWood);
            CreateCube("MainHouseBackBeam", parent, new Vector3(0f, 3.25f, 86.7f), new Vector3(14.8f, 0.35f, 0.35f), Materials.DarkWood);
            for (var i = 0; i < 7; i++)
            {
                var x = -6f + i * 2f;
                if (i == 3)
                {
                    CreateCube("MainHouseOpenDoor_LeftPanel", parent, new Vector3(-0.92f, 1.75f, 79.34f), new Vector3(0.52f, 2.15f, 0.08f), Materials.DoorPaper);
                    CreateCube("MainHouseOpenDoor_RightPanel", parent, new Vector3(0.92f, 1.75f, 79.34f), new Vector3(0.52f, 2.15f, 0.08f), Materials.DoorPaper);
                    continue;
                }

                CreateCube("MainHouseFrontColumn_" + i, parent, new Vector3(x, 1.75f, 79.55f), new Vector3(0.32f, 2.45f, 0.32f), Materials.DarkWood);
                var paperDoor = CreateCube("MainHousePaperDoor_" + i, parent, new Vector3(x, 1.75f, 79.38f), new Vector3(1.25f, 2.15f, 0.08f), Materials.DoorPaper);
                if (i == 2)
                {
                    AttachPaperDoorInteraction(paperDoor, "main house paper door");
                }

                CreateCube("MainHouseDoorMuntinV_" + i, parent, new Vector3(x, 1.75f, 79.31f), new Vector3(0.08f, 2.25f, 0.08f), Materials.DarkWood);
                CreateCube("MainHouseDoorMuntinH_" + i, parent, new Vector3(x, 1.75f, 79.3f), new Vector3(1.28f, 0.08f, 0.08f), Materials.DarkWood);
            }

            CreateCube("MainHouseInteriorLeftRoomWall", parent, new Vector3(-3.1f, 1.85f, 83.2f), new Vector3(0.22f, 2.6f, 5.2f), Materials.Plaster);
            CreateCube("MainHouseInteriorRightRoomWall", parent, new Vector3(3.1f, 1.85f, 83.2f), new Vector3(0.22f, 2.6f, 5.2f), Materials.Plaster);
            CreateCube("MainHouseInnerBackRoomScreen", parent, new Vector3(0f, 1.6f, 84.95f), new Vector3(4.4f, 2.0f, 0.18f), Materials.DoorPaper);
            CreateCube("MainHouseRearLanding", parent, new Vector3(-5.8f, 0.61f, 86.85f), new Vector3(1.8f, 0.18f, 0.7f), Materials.Wood);
            CreateCube("MainHouseRearMiddleStep", parent, new Vector3(-6.2f, 0.42f, 87.55f), new Vector3(2.3f, 0.18f, 0.9f), Materials.Stone);
            CreateCube("MainHouseRearLowStep", parent, new Vector3(-7.15f, 0.24f, 88.35f), new Vector3(2.6f, 0.18f, 1.15f), Materials.Stone);
            CreateCube("MainHouseBackExitConnectorFloor", parent, new Vector3(-6.8f, 0.12f, 89.15f), new Vector3(3.1f, 0.2f, 4.9f), Materials.Road);

            CreateCube("MainHouseRoof_LeftSlope", parent, new Vector3(-3.6f, 4.35f, 83f), new Vector3(9.0f, 0.42f, 9.6f), Materials.Roof, Quaternion.Euler(0f, 0f, 11f));
            CreateCube("MainHouseRoof_RightSlope", parent, new Vector3(3.6f, 4.35f, 83f), new Vector3(9.0f, 0.42f, 9.6f), Materials.Roof, Quaternion.Euler(0f, 0f, -11f));
            CreateCube("MainHouseRoofRidge", parent, new Vector3(0f, 5.05f, 83f), new Vector3(0.45f, 0.35f, 9.9f), Materials.RoofRidge);
            CreateCube("MainHouseDeepEavesFront", parent, new Vector3(0f, 3.95f, 78.2f), new Vector3(15.8f, 0.28f, 0.55f), Materials.Roof);
            CreateRoofTileRibs(parent, new Vector3(0f, 4.55f, 83f), 14.6f, 9.8f, "MainHouseTileRib");
            CreateRafterRow(parent, 79.0f, "FrontRafter");
            CreateRafterRow(parent, 87.05f, "BackRafter");
            CreatePaperCharm(parent, new Vector3(-2.4f, 2.85f, 79.15f), "MainDoorCharm_Left");
            CreatePaperCharm(parent, new Vector3(2.4f, 2.7f, 79.15f), "MainDoorCharm_Right");
            CreateCube("BackExitGap", parent, new Vector3(-7.6f, 0.1f, 88.5f), new Vector3(2.2f, 0.2f, 6.4f), Materials.Road);
            CreateBambooCluster(parent, new Vector3(-9.1f, 0f, 88.5f), "BackExitBamboo");
        }

        private static void CreateSarangchae(Transform parent)
        {
            CreateCube("SarangchaeStoneFoundation", parent, new Vector3(9.2f, 0.18f, 75.5f), new Vector3(3.9f, 0.36f, 8.4f), Materials.Stone);
            CreateCube("SarangchaeWoodFloor", parent, new Vector3(9.15f, 0.48f, 75.5f), new Vector3(3.55f, 0.25f, 7.8f), Materials.Wood);
            CreateCube("SarangchaeFrontStep", parent, new Vector3(6.95f, 0.2f, 75.5f), new Vector3(1.0f, 0.22f, 5.4f), Materials.Stone);
            CreateCube("SarangchaeBackWall", parent, new Vector3(11.05f, 1.85f, 75.5f), new Vector3(0.28f, 2.9f, 7.8f), Materials.Plaster);
            CreateCube("SarangchaeNorthWall", parent, new Vector3(9.2f, 1.85f, 79.5f), new Vector3(3.65f, 2.9f, 0.28f), Materials.Plaster);
            CreateCube("SarangchaeSouthWall", parent, new Vector3(9.2f, 1.85f, 71.5f), new Vector3(3.65f, 2.9f, 0.28f), Materials.Plaster);

            for (var i = 0; i < 4; i++)
            {
                var z = 72.4f + i * 2.05f;
                CreateCube("SarangchaeFrontColumn_" + i, parent, new Vector3(7.3f, 1.65f, z), new Vector3(0.28f, 2.35f, 0.28f), Materials.DarkWood);
            }

            AttachPaperDoorInteraction(
                CreateCube("SarangchaeOpenPaperDoor_A", parent, new Vector3(7.22f, 1.55f, 74.3f), new Vector3(0.08f, 1.9f, 0.72f), Materials.DoorPaper),
                "sarangchae paper door");
            CreateCube("SarangchaeOpenPaperDoor_B", parent, new Vector3(7.22f, 1.55f, 76.6f), new Vector3(0.08f, 1.9f, 0.72f), Materials.DoorPaper);
            CreateCube("SarangchaeInteriorScreen", parent, new Vector3(9.15f, 1.55f, 75.5f), new Vector3(0.12f, 2.0f, 3.2f), Materials.DoorPaper);
            CreateCube("SarangchaeFrontBeam", parent, new Vector3(7.32f, 3.1f, 75.5f), new Vector3(0.36f, 0.32f, 8.2f), Materials.DarkWood);
            CreateCube("SarangchaeBackBeam", parent, new Vector3(11.04f, 3.1f, 75.5f), new Vector3(0.36f, 0.32f, 8.2f), Materials.DarkWood);
            CreateCube("SarangchaeRoof_LeftSlope", parent, new Vector3(8.25f, 4.0f, 75.5f), new Vector3(2.4f, 0.36f, 9.0f), Materials.Roof, Quaternion.Euler(0f, 0f, 10f));
            CreateCube("SarangchaeRoof_RightSlope", parent, new Vector3(10.05f, 4.0f, 75.5f), new Vector3(2.4f, 0.36f, 9.0f), Materials.Roof, Quaternion.Euler(0f, 0f, -10f));
            CreateCube("SarangchaeRoofRidge", parent, new Vector3(9.15f, 4.45f, 75.5f), new Vector3(0.32f, 0.26f, 9.3f), Materials.RoofRidge);
            CreateRoofTileRibs(parent, new Vector3(9.15f, 4.18f, 75.5f), 3.8f, 8.8f, "SarangchaeTileRib");
            CreatePaperLantern(parent, new Vector3(7.0f, 2.35f, 78.3f), "SarangchaeLantern");

            CreateCube("KitchenShedFoundation", parent, new Vector3(-3.0f, 0.15f, 91.5f), new Vector3(5.8f, 0.3f, 3.8f), Materials.Stone);
            CreateCube("KitchenShedFloor", parent, new Vector3(-3.0f, 0.43f, 91.5f), new Vector3(5.4f, 0.25f, 3.4f), Materials.Wood);
            CreateCube("KitchenShedBackWall", parent, new Vector3(-3.0f, 1.65f, 93.25f), new Vector3(5.4f, 2.4f, 0.24f), Materials.Plaster);
            CreateCube("KitchenShedLeftWall", parent, new Vector3(-5.8f, 1.65f, 91.5f), new Vector3(0.24f, 2.4f, 3.4f), Materials.Plaster);
            CreateCube("KitchenShedRightWall", parent, new Vector3(-0.2f, 1.65f, 91.5f), new Vector3(0.24f, 2.4f, 3.4f), Materials.Plaster);
            CreateCube("KitchenShedRoof", parent, new Vector3(-3.0f, 3.0f, 91.5f), new Vector3(6.1f, 0.35f, 4.1f), Materials.Roof, Quaternion.Euler(0f, 0f, -4f));
            CreateCylinder("KitchenOnggiJar", parent, new Vector3(-4.3f, 0.75f, 89.9f), new Vector3(0.55f, 0.6f, 0.55f), Materials.Jar);
        }

        private static void CreateRearCompound(Transform parent)
        {
            CreateCube("MainHouseSideShortcutBlocker_Left", parent, new Vector3(-10.1f, 1.45f, 89.25f), new Vector3(1.65f, 2.9f, 0.7f), Materials.StoneWall);
            CreateCube("MainHouseSideShortcutBlocker_Right", parent, new Vector3(8.3f, 1.45f, 89.25f), new Vector3(1.9f, 2.9f, 0.7f), Materials.StoneWall);

            CreateCube("RearCompoundPackedEarth_South", parent, new Vector3(0f, 0.02f, 99.5f), new Vector3(22f, 0.16f, 13.5f), Materials.Courtyard);
            CreateCube("RearCompoundPackedEarth_Middle", parent, new Vector3(0f, 0.02f, 114.5f), new Vector3(22f, 0.16f, 16.5f), Materials.Courtyard);
            CreateCube("RearCompoundPackedEarth_North", parent, new Vector3(0f, 0.02f, 129f), new Vector3(22f, 0.16f, 14f), Materials.Courtyard);
            CreateCube("RearCompoundWall_Left", parent, new Vector3(-11.2f, 1.55f, 114.5f), new Vector3(0.35f, 3.1f, 51f), Materials.StoneWall);
            CreateCube("RearCompoundWall_Right", parent, new Vector3(11.2f, 1.55f, 114.5f), new Vector3(0.35f, 3.1f, 51f), Materials.StoneWall);

            CreateCube("RearRoutePath_WestEntry", parent, new Vector3(-8.9f, 0.09f, 96.2f), new Vector3(2.8f, 0.18f, 12f), Materials.Road);
            CreateCube("RearRoutePath_FirstCrossing", parent, new Vector3(-2.7f, 0.09f, 105.2f), new Vector3(12f, 0.18f, 2.4f), Materials.Road);
            CreateCube("RearRoutePath_EastRun", parent, new Vector3(4.0f, 0.09f, 114.5f), new Vector3(2.8f, 0.18f, 17f), Materials.Road);
            CreateCube("RearRoutePath_SecondCrossing", parent, new Vector3(-2.0f, 0.09f, 123.6f), new Vector3(12f, 0.18f, 2.4f), Materials.Road);
            CreateCube("RearRoutePath_WestRun", parent, new Vector3(-8.0f, 0.09f, 132.8f), new Vector3(3.0f, 0.18f, 16.5f), Materials.Road);

            CreateCube("RearRouteScreen_FirstTurn", parent, new Vector3(-0.4f, 1.45f, 99.5f), new Vector3(14.8f, 2.9f, 0.34f), Materials.StoneWall);
            CreateCube("RearRouteScreen_SecondTurn", parent, new Vector3(-4.1f, 1.45f, 112.6f), new Vector3(14.2f, 2.9f, 0.34f), Materials.StoneWall);
            CreateCube("RearRouteScreen_ThirdTurn", parent, new Vector3(2.2f, 1.45f, 125.8f), new Vector3(14.2f, 2.9f, 0.34f), Materials.StoneWall);
            CreateRearRouteHanokAtmosphere(parent);

            CreateCube("JonggaStorehouseFoundation", parent, new Vector3(5.2f, 0.18f, 103.6f), new Vector3(4.8f, 0.36f, 5.2f), Materials.Stone);
            CreateCube("JonggaStorehouseFloor", parent, new Vector3(5.2f, 0.5f, 103.6f), new Vector3(4.4f, 0.26f, 4.8f), Materials.Wood);
            CreateCube("JonggaStorehouseBackWall", parent, new Vector3(5.2f, 1.7f, 106.1f), new Vector3(4.4f, 2.6f, 0.28f), Materials.Plaster);
            CreateCube("JonggaStorehouseRoof", parent, new Vector3(5.2f, 3.1f, 103.6f), new Vector3(5.2f, 0.35f, 5.6f), Materials.Roof, Quaternion.Euler(0f, 0f, -4f));

            CreateCube("AncestralSideRoomFoundation", parent, new Vector3(-6.3f, 0.18f, 116.4f), new Vector3(4.6f, 0.36f, 5.8f), Materials.Stone);
            CreateCube("AncestralSideRoomFloor", parent, new Vector3(-6.3f, 0.5f, 116.4f), new Vector3(4.2f, 0.26f, 5.4f), Materials.Wood);
            CreateCube("AncestralSideRoomWall", parent, new Vector3(-8.55f, 1.8f, 116.4f), new Vector3(0.28f, 2.8f, 5.4f), Materials.Plaster);
            CreateCube("AncestralSideRoomRoof", parent, new Vector3(-6.3f, 3.18f, 116.4f), new Vector3(5.0f, 0.35f, 6.4f), Materials.Roof, Quaternion.Euler(0f, 0f, 5f));

            CreateCube("RearGardenStoneBasin", parent, new Vector3(3.2f, 0.45f, 121.2f), new Vector3(1.7f, 0.45f, 1.2f), Materials.Stone);
            CreateCube("RearGardenCollapsedPlank_A", parent, new Vector3(-2.7f, 0.22f, 129.2f), new Vector3(2.7f, 0.12f, 0.28f), Materials.DarkWood, Quaternion.Euler(0f, 17f, 0f));
            CreateCube("RearGardenCollapsedPlank_B", parent, new Vector3(-1.2f, 0.25f, 130.1f), new Vector3(2.3f, 0.12f, 0.28f), Materials.DarkWood, Quaternion.Euler(0f, -12f, 0f));
            CreateStoneCairn(parent, new Vector3(-9.6f, 0.25f, 105.5f), "RearGardenCairn_A");
            CreateStoneCairn(parent, new Vector3(6.8f, 0.25f, 118.2f), "RearGardenCairn_B");
            CreateJangseung(parent, new Vector3(-9.7f, 0f, 128.2f), "DeepShrineJangseung_Left");
            CreateJangseung(parent, new Vector3(-5.9f, 0f, 128.7f), "DeepShrineJangseung_Right");
            CreateSotdae(parent, new Vector3(7.4f, 0f, 108.7f), "RearCompoundSotdae_A");
            CreateSotdae(parent, new Vector3(-9.3f, 0f, 120.8f), "RearCompoundSotdae_B");
            CreateBambooCluster(parent, new Vector3(-10.0f, 0f, 101.8f), "RearGardenBamboo_WestA");
            CreateBambooCluster(parent, new Vector3(8.2f, 0f, 111.4f), "RearGardenBamboo_EastA");
            CreateBambooCluster(parent, new Vector3(-10.1f, 0f, 123.0f), "RearGardenBamboo_WestB");
            CreateBambooCluster(parent, new Vector3(7.5f, 0f, 130.0f), "RearGardenBamboo_EastB");
            CreateTree(parent, "RearGardenOldPine_A", new Vector3(-9.2f, 0f, 110.0f), 8.5f);
            CreateTree(parent, "RearGardenOldPine_B", new Vector3(8.9f, 0f, 126.5f), 8.1f);
            CreatePaperLantern(parent, new Vector3(-10.9f, 2.35f, 111.2f), "RearCompoundLantern_West");
            CreatePaperLantern(parent, new Vector3(10.8f, 2.35f, 122.5f), "RearCompoundLantern_East");
            CreatePointLight("RearGardenColdLamp", parent, new Vector3(3.4f, 2.1f, 121.2f), new Color(0.55f, 0.68f, 0.75f), 0.9f, 8f);
        }

        private static void CreateRearRouteHanokAtmosphere(Transform parent)
        {
            CreateRearHanokGate(parent, "First", new Vector3(-8.9f, 0f, 100.3f));
            CreateRearHanokGate(parent, "Second", new Vector3(4.0f, 0f, 116.6f));
            CreateRearHanokGate(parent, "Third", new Vector3(-8.0f, 0f, 133.4f));

            CreateCube("RearRouteSightlineBreak_First", parent, new Vector3(-0.4f, 1.65f, 102.2f), new Vector3(12.6f, 3.3f, 0.32f), Materials.StoneWall);
            CreateCube("RearRouteSightlineBreak_Second", parent, new Vector3(-4.1f, 1.65f, 116.0f), new Vector3(11.4f, 3.3f, 0.32f), Materials.StoneWall);
            CreateCube("RearRouteSightlineBreak_Third", parent, new Vector3(1.4f, 1.65f, 129.1f), new Vector3(12.4f, 3.3f, 0.32f), Materials.StoneWall);

            CreateCube("RearRouteEaves_First", parent, new Vector3(-8.9f, 3.45f, 100.3f), new Vector3(4.4f, 0.28f, 1.4f), Materials.Roof, Quaternion.Euler(0f, 0f, 5f));
            CreateCube("RearRouteEaves_Second", parent, new Vector3(4.0f, 3.45f, 116.6f), new Vector3(4.4f, 0.28f, 1.4f), Materials.Roof, Quaternion.Euler(0f, 0f, -5f));
            CreateCube("RearRouteEaves_Third", parent, new Vector3(-8.0f, 3.45f, 133.4f), new Vector3(4.4f, 0.28f, 1.4f), Materials.Roof, Quaternion.Euler(0f, 0f, 5f));

            CreatePaperLantern(parent, new Vector3(-10.2f, 2.25f, 100.9f), "RearRouteLantern_First");
            CreatePaperLantern(parent, new Vector3(2.65f, 2.25f, 116.0f), "RearRouteLantern_Second");
            CreatePaperLantern(parent, new Vector3(-9.35f, 2.25f, 134.0f), "RearRouteLantern_Third");
            CreatePointLight("RearRouteLanternPool_First", parent, new Vector3(-8.9f, 1.75f, 101.0f), new Color(0.95f, 0.46f, 0.24f), 1.25f, 7.2f);
            CreatePointLight("RearRouteLanternPool_Second", parent, new Vector3(4.0f, 1.75f, 116.0f), new Color(0.9f, 0.42f, 0.22f), 1.1f, 6.8f);
            CreatePointLight("RearRouteLanternPool_Third", parent, new Vector3(-8.0f, 1.75f, 134.0f), new Color(0.86f, 0.38f, 0.2f), 1.15f, 7.4f);

            CreateNonBlockingPaperCharm(parent, new Vector3(-8.9f, 2.25f, 99.55f), "RearHanokGateCharm_First");
            CreateNonBlockingPaperCharm(parent, new Vector3(4.0f, 2.25f, 115.85f), "RearHanokGateCharm_Second");
            CreateNonBlockingPaperCharm(parent, new Vector3(-8.0f, 2.25f, 132.65f), "RearHanokGateCharm_Third");
            CreateNonBlockingPaperCharm(parent, new Vector3(-0.2f, 2.05f, 102.0f), "RearRoutePaperCharm_FirstTurn");
            CreateNonBlockingPaperCharm(parent, new Vector3(-4.2f, 2.05f, 115.8f), "RearRoutePaperCharm_SecondTurn");
            CreateNonBlockingPaperCharm(parent, new Vector3(1.5f, 2.05f, 128.9f), "RearRoutePaperCharm_ThirdTurn");
        }

        private static void CreateRearHanokGate(Transform parent, string suffix, Vector3 basePosition)
        {
            var root = new GameObject("RearHanokGate_" + suffix);
            root.transform.SetParent(parent);
            root.transform.position = basePosition;

            CreateCube("RearHanokGate_" + suffix + "_LeftPost", parent, basePosition + new Vector3(-1.7f, 1.45f, 0f), new Vector3(0.34f, 2.9f, 0.34f), Materials.DarkWood);
            CreateCube("RearHanokGate_" + suffix + "_RightPost", parent, basePosition + new Vector3(1.7f, 1.45f, 0f), new Vector3(0.34f, 2.9f, 0.34f), Materials.DarkWood);
            CreateCube("RearHanokGate_" + suffix + "_Lintel", parent, basePosition + new Vector3(0f, 3.0f, 0f), new Vector3(3.9f, 0.32f, 0.36f), Materials.DarkWood);
            CreateCube("RearHanokGate_" + suffix + "_PaperSideLeft", parent, basePosition + new Vector3(-2.05f, 1.6f, -0.08f), new Vector3(0.42f, 2.2f, 0.08f), Materials.DoorPaper);
            CreateCube("RearHanokGate_" + suffix + "_PaperSideRight", parent, basePosition + new Vector3(2.05f, 1.6f, -0.08f), new Vector3(0.42f, 2.2f, 0.08f), Materials.DoorPaper);
            CreateCube("RearHanokGate_" + suffix + "_RoofLeft", parent, basePosition + new Vector3(-0.85f, 3.38f, 0f), new Vector3(2.4f, 0.28f, 1.3f), Materials.Roof, Quaternion.Euler(0f, 0f, 8f));
            CreateCube("RearHanokGate_" + suffix + "_RoofRight", parent, basePosition + new Vector3(0.85f, 3.38f, 0f), new Vector3(2.4f, 0.28f, 1.3f), Materials.Roof, Quaternion.Euler(0f, 0f, -8f));
            CreateCube("RearHanokGate_" + suffix + "_RoofRidge", parent, basePosition + new Vector3(0f, 3.76f, 0f), new Vector3(0.24f, 0.22f, 1.45f), Materials.RoofRidge);
        }

        private static void CreateShrineLoop(Transform parent)
        {
            CreateCube("BackShrinePath", parent, new Vector3(ShrineX, 0f, ShrineZ - 8.5f), new Vector3(3.2f, 0.2f, 17f), Materials.Road);
            CreateShrineRouteBoundaries(parent);
            CreateCube("ShrineFoundation", parent, new Vector3(ShrineX, 0.2f, ShrineZ), new Vector3(5.8f, 0.35f, 4.8f), Materials.Stone);
            CreateCube("ShrineFloor", parent, new Vector3(ShrineX, 0.55f, ShrineZ), new Vector3(5.5f, 0.25f, 4.5f), Materials.Wood);
            CreateCube("ShrineBackWall", parent, new Vector3(ShrineX, 1.8f, ShrineZ + 2.2f), new Vector3(5.5f, 3.2f, 0.3f), Materials.DarkWood);
            CreateCube("ShrineLeftWall", parent, new Vector3(ShrineX - 2.8f, 1.7f, ShrineZ), new Vector3(0.25f, 2.8f, 4.5f), Materials.DarkWood);
            CreateCube("ShrineRightWall", parent, new Vector3(ShrineX + 2.8f, 1.7f, ShrineZ), new Vector3(0.25f, 2.8f, 4.5f), Materials.DarkWood);
            CreateCube("ShrineRoof_LeftSlope", parent, new Vector3(ShrineX - 1.4f, 3.75f, ShrineZ), new Vector3(3.7f, 0.35f, 5.6f), Materials.Roof, Quaternion.Euler(0f, 0f, 10f));
            CreateCube("ShrineRoof_RightSlope", parent, new Vector3(ShrineX + 1.4f, 3.75f, ShrineZ), new Vector3(3.7f, 0.35f, 5.6f), Materials.Roof, Quaternion.Euler(0f, 0f, -10f));
            CreateCube("ShrineRoofRidge", parent, new Vector3(ShrineX, 4.2f, ShrineZ), new Vector3(0.3f, 0.24f, 5.9f), Materials.RoofRidge);
            CreateCube("ShrineAltar", parent, new Vector3(ShrineX, 0.8f, ShrineZ + 1.4f), new Vector3(2f, 1.0f, 0.7f), Materials.Altar);
            var shrineRope = CreateCube("ShrineHangingRope", parent, new Vector3(ShrineX, 2.75f, ShrineZ - 1.95f), new Vector3(4.8f, 0.08f, 0.08f), Materials.Rope);
            shrineRope.GetComponent<Collider>().enabled = false;
            for (var i = 0; i < 7; i++)
            {
                var x = ShrineX - 2.1f + i * 0.7f;
                var strip = CreateCube("ShrineClothStrip_" + i, parent, new Vector3(x, 2.35f - (i % 2) * 0.15f, ShrineZ - 2.05f), new Vector3(0.18f, 0.72f, 0.04f), i % 2 == 0 ? Materials.FadedRedCloth : Materials.FadedWhiteCloth);
                strip.GetComponent<Collider>().enabled = false;
            }

            CreateCylinder("ShrineCandle_A", parent, new Vector3(ShrineX - 0.45f, 1.45f, ShrineZ + 0.9f), new Vector3(0.08f, 0.22f, 0.08f), Materials.CandleWax);
            CreateCylinder("ShrineCandle_B", parent, new Vector3(ShrineX + 0.45f, 1.45f, ShrineZ + 0.9f), new Vector3(0.08f, 0.22f, 0.08f), Materials.CandleWax);
            CreateSphere("ShrineCandleFlame_A", parent, new Vector3(ShrineX - 0.45f, 1.72f, ShrineZ + 0.9f), new Vector3(0.12f, 0.18f, 0.12f), Materials.LanternGlow);
            CreateSphere("ShrineCandleFlame_B", parent, new Vector3(ShrineX + 0.45f, 1.72f, ShrineZ + 0.9f), new Vector3(0.12f, 0.18f, 0.12f), Materials.LanternGlow);
            CreatePointLight("DeepShrineLanternGlow", parent, new Vector3(ShrineX, 2.1f, ShrineZ - 1.15f), new Color(0.9f, 0.44f, 0.22f), 1.45f, 9f);
            CreateStoneCairn(parent, new Vector3(ShrineX - 2.6f, 0.3f, ShrineZ - 4.5f), "ShrinePathCairn");
        }

        private static void CreateShrineRouteBoundaries(Transform parent)
        {
            CreateCube("ShrineRouteBoundary_WestStone", parent, new Vector3(ShrineX - 2.45f, 1.25f, ShrineZ - 4.8f), new Vector3(0.45f, 2.5f, 12.4f), Materials.StoneWall);
            CreateCube("ShrineRouteBoundary_EastStone", parent, new Vector3(ShrineX + 2.45f, 1.25f, ShrineZ - 4.8f), new Vector3(0.45f, 2.5f, 12.4f), Materials.StoneWall);
            CreateBambooCluster(parent, new Vector3(ShrineX - 2.15f, 0f, ShrineZ - 8.8f), "ShrineRouteWestBamboo_A");
            CreateBambooCluster(parent, new Vector3(ShrineX - 2.15f, 0f, ShrineZ - 3.5f), "ShrineRouteWestBamboo_B");
            CreateBambooCluster(parent, new Vector3(ShrineX - 2.15f, 0f, ShrineZ + 0.2f), "ShrineRouteWestBamboo_C");
            CreateBambooCluster(parent, new Vector3(ShrineX + 2.15f, 0f, ShrineZ - 8.9f), "ShrineRouteEastBamboo_A");
            CreateBambooCluster(parent, new Vector3(ShrineX + 2.15f, 0f, ShrineZ - 3.6f), "ShrineRouteEastBamboo_B");
            CreateBambooCluster(parent, new Vector3(ShrineX + 2.15f, 0f, ShrineZ + 0.1f), "ShrineRouteEastBamboo_C");
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
            CreateCube("BackMountainSilhouette", parent, new Vector3(0f, 5.5f, 158f), new Vector3(48f, 11f, 6f), Materials.DistantHill);
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

        private static void CreateNonBlockingPaperCharm(Transform parent, Vector3 position, string name)
        {
            DisableCollider(CreateCube(name + "_Paper", parent, position, new Vector3(0.28f, 0.74f, 0.035f), Materials.TalismanPaper));
            DisableCollider(CreateCube(name + "_InkTop", parent, position + new Vector3(0f, 0.16f, -0.035f), new Vector3(0.19f, 0.035f, 0.025f), Materials.TalismanInk));
            DisableCollider(CreateCube(name + "_InkMid", parent, position + new Vector3(0f, -0.02f, -0.035f), new Vector3(0.13f, 0.035f, 0.025f), Materials.TalismanInk));
            DisableCollider(CreateCube(name + "_InkLow", parent, position + new Vector3(0f, -0.18f, -0.035f), new Vector3(0.21f, 0.035f, 0.025f), Materials.TalismanInk));
        }

        private static void DisableCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
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

        private static GameObject CreateLocalPrimitive(
            PrimitiveType primitiveType,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Quaternion? localRotation = null)
        {
            var primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localRotation = localRotation ?? Quaternion.identity;
            primitive.transform.localScale = localScale;
            primitive.GetComponent<MeshRenderer>().sharedMaterial = material;

            var collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            return primitive;
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

        private static Transform CreateZoneRoot(Transform parent, string name)
        {
            var zone = new GameObject(name).transform;
            zone.SetParent(parent);
            zone.localPosition = Vector3.zero;
            zone.localRotation = Quaternion.identity;
            zone.localScale = Vector3.one;
            return zone;
        }

        private static TerritoryVolume CreateTerritoryVolume(
            string name,
            Transform parent,
            TerritoryKind territory,
            Vector3 position,
            Vector3 size,
            int priority)
        {
            var volumeObject = CreateColliderOnlyBox(name, parent, position, size, true);
            var volume = volumeObject.AddComponent<TerritoryVolume>();
            volume.Configure(territory, priority);
            return volume;
        }

        private static GameObject CreateColliderOnlyBox(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 size,
            bool isTrigger)
        {
            var boxObject = new GameObject(name);
            boxObject.transform.SetParent(parent);
            boxObject.transform.position = position;
            var collider = boxObject.AddComponent<BoxCollider>();
            collider.size = size;
            collider.isTrigger = isTrigger;
            return boxObject;
        }

        private static void SetObject(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(Object target, string propertyName, params Object[] values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

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
            public static readonly Material GhostBody = CreateEmissionMaterial("Mat_ThreatGhostProxy", new Color(0.46f, 0.9f, 0.78f, 0.82f), 0.55f);
            public static readonly Material DokkaebiBody = CreateEmissionMaterial("Mat_ThreatDokkaebiProxy", new Color(0.78f, 0.13f, 0.09f, 1f), 0.45f);
        }
    }
}
