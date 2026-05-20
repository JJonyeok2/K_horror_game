using System.IO;
using KHorrorGame.Migration;
using KHorrorGame.Migration.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KHorrorGame.Editor
{
    public static class KHorrorBootstrapSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/KHorror_Main.unity";
        private const string LightingProfilePath = "Assets/KHorrorGame/Rendering/KHorrorLightingProfile.asset";

        [MenuItem("Tools/K Horror Migration/Create Bootstrap Scene")]
        public static void CreateBootstrapScene()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/KHorrorGame/Rendering");

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
            var settlementSpawn = CreateMarker("SettlementSpawn", new Vector3(0f, 1f, 42f), Quaternion.identity, systems.transform);

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

            CreateLighting(systems.transform, player.CameraLight);
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

            return new PlayerBundle(controller, flashlight);
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
            CreateCube("BongoInteriorFloor", parent, new Vector3(0f, 0f, -3f), new Vector3(4f, 0.2f, 5f), new Color(0.08f, 0.09f, 0.08f));
            CreateCube("BongoCargoWallLeft", parent, new Vector3(-2.1f, 1.2f, -3f), new Vector3(0.2f, 2.4f, 5f), new Color(0.03f, 0.035f, 0.035f));
            CreateCube("BongoCargoWallRight", parent, new Vector3(2.1f, 1.2f, -3f), new Vector3(0.2f, 2.4f, 5f), new Color(0.03f, 0.035f, 0.035f));
            CreateCube("BongoCargoRoof", parent, new Vector3(0f, 2.45f, -3f), new Vector3(4.3f, 0.18f, 5f), new Color(0.02f, 0.025f, 0.025f));
            var terminal = CreateCube("BongoTerminal", parent, new Vector3(0f, 1.1f, -0.7f), new Vector3(0.9f, 0.55f, 0.12f), new Color(0.02f, 0.08f, 0.065f));
            var bongoTerminal = terminal.AddComponent<BongoTerminal>();
            SetObject(bongoTerminal, "gameLoop", gameLoop);
        }

        private static void CreateEstateProxy(Transform parent, GameLoopController gameLoop)
        {
            CreateCube("ApproachRoad", parent, new Vector3(0f, 0f, 18f), new Vector3(7f, 0.2f, 32f), new Color(0.13f, 0.12f, 0.095f));
            CreateCube("OuterGateProxy", parent, new Vector3(0f, 2f, 32f), new Vector3(5f, 4f, 0.35f), new Color(0.16f, 0.09f, 0.055f));
            CreateCube("CourtyardProxy", parent, new Vector3(0f, 0f, 44f), new Vector3(16f, 0.2f, 18f), new Color(0.12f, 0.105f, 0.08f));
            CreateCube("MainHouseProxy", parent, new Vector3(0f, 2f, 54f), new Vector3(10f, 4f, 0.6f), new Color(0.18f, 0.17f, 0.14f));

            var artifact = CreateCube("TestArtifact_BrassBowl", parent, new Vector3(1.5f, 0.5f, 43f), new Vector3(0.45f, 0.3f, 0.45f), new Color(0.45f, 0.32f, 0.16f));
            var pickup = artifact.AddComponent<ArtifactPickup>();
            SetObject(pickup, "gameLoop", gameLoop);

            var extraction = CreateCube("VanInteriorReturnZone", parent, new Vector3(-2f, 0.7f, 38f), new Vector3(2.8f, 1.4f, 2.4f), new Color(0.05f, 0.22f, 0.12f));
            var collider = extraction.GetComponent<Collider>();
            collider.isTrigger = true;
            var extractionZone = extraction.AddComponent<ExtractionZone>();
            SetObject(extractionZone, "gameLoop", gameLoop);
        }

        private static void CreateSettlementProxy(Transform parent, GameLoopController gameLoop)
        {
            CreateCube("SettlementFloor", parent, new Vector3(0f, 0f, 42f), new Vector3(7f, 0.2f, 7f), new Color(0.1f, 0.1f, 0.1f));
            CreateCube("SettlementBackWall", parent, new Vector3(0f, 1.8f, 45.4f), new Vector3(7f, 3.6f, 0.25f), new Color(0.12f, 0.12f, 0.105f));
            var station = CreateCube("SettlementStation", parent, new Vector3(0f, 1.0f, 44.1f), new Vector3(1.4f, 1f, 0.45f), new Color(0.05f, 0.07f, 0.08f));
            var settlementStation = station.AddComponent<SettlementStation>();
            SetObject(settlementStation, "gameLoop", gameLoop);
        }

        private static void CreateTravelProxy(Transform parent)
        {
            CreateCube("TravelMotionBackdrop", parent, new Vector3(0f, 1.2f, 8f), new Vector3(8f, 2.5f, 0.2f), new Color(0.025f, 0.03f, 0.028f));
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;

            var renderer = cube.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateMaterial(name + "_Mat", color);
            return cube;
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
            public PlayerBundle(UnityPlayerController controller, Light cameraLight)
            {
                Controller = controller;
                CameraLight = cameraLight;
            }

            public UnityPlayerController Controller { get; }
            public Light CameraLight { get; }
        }
    }
}
