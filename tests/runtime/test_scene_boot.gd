extends RefCounted

const TestAssertions = preload("res://tests/test_assertions.gd")
const RUNTIME_SCRIPTS := [
	"res://scripts/game/main.gd",
	"res://scripts/player/player_controller.gd",
	"res://scripts/player/interactor.gd",
	"res://scripts/props/artifact.gd",
	"res://scripts/zones/extraction_zone.gd",
	"res://scripts/maps/jongga_estate_builder.gd",
	"res://scripts/ui/hud.gd",
	"res://scripts/audio/audio_director.gd",
]

func run() -> Array[String]:
	var t := TestAssertions.new()
	test_runtime_scripts_parse(t)
	test_main_scene_loads(t)
	test_player_scene_loads(t)
	test_player_camera_is_current(t)
	return t.failures

func test_runtime_scripts_parse(t: TestAssertions) -> void:
	for path in RUNTIME_SCRIPTS:
		var script: Script = load(path)
		t.assert_true(script != null and script.can_instantiate(), "%s parses before scene load" % path)

func test_main_scene_loads(t: TestAssertions) -> void:
	var scene = load("res://scenes/Main.tscn")
	t.assert_true(scene != null, "main scene loads without script parse errors")

func test_player_scene_loads(t: TestAssertions) -> void:
	var scene = load("res://scenes/player/Player.tscn")
	t.assert_true(scene != null, "player scene loads without script parse errors")

func test_player_camera_is_current(t: TestAssertions) -> void:
	var scene: PackedScene = load("res://scenes/player/Player.tscn")
	var player: Node = scene.instantiate()
	var camera: Camera3D = player.get_node("Camera3D")
	t.assert_true(camera.current, "player camera is explicitly current")
	player.free()
