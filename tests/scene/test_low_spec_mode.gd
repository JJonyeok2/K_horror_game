extends SceneTree

const MainScene := preload("res://scenes/Main.tscn")
const PerformanceSettingsScript := preload("res://scripts/game/performance_settings.gd")

var _failed := false
var _previous_low_spec: Variant

func _initialize() -> void:
	_previous_low_spec = ProjectSettings.get_setting(PerformanceSettingsScript.LOW_SPEC_SETTING, true)
	ProjectSettings.set_setting(PerformanceSettingsScript.LOW_SPEC_SETTING, true)
	var main: Node = MainScene.instantiate()
	root.add_child(main)
	for _i in range(90):
		await physics_frame

	_assert_surface_uses_flat_material(main, "LongForestApproachRoad")
	_assert_surface_uses_flat_material(main, "MainHouseFrontWallLeft")
	_assert_fog_disabled(main)
	_assert_reduced_runtime_lights(main)
	_assert_deep_red_lamp_retained(main)

	ProjectSettings.set_setting(PerformanceSettingsScript.LOW_SPEC_SETTING, _previous_low_spec)
	if _failed:
		quit(1)
		return
	print("LOW_SPEC_SMOKE: flat materials, fog disabled, reduced lights")
	quit(0)

func _assert_surface_uses_flat_material(main: Node, label: String) -> void:
	var node := main.find_child(label, true, false)
	if node == null:
		_fail("Missing node: %s" % label)
		return
	var material := _first_standard_material(node)
	if material == null:
		_fail("%s has no StandardMaterial3D" % label)
		return
	if material.albedo_texture != null:
		_fail("%s should not load an albedo texture in low-spec mode" % label)
		return
	if material.normal_enabled or material.normal_texture != null:
		_fail("%s should not load a normal texture in low-spec mode" % label)
		return

func _assert_fog_disabled(main: Node) -> void:
	var world_environment := main.find_child("NightFogEnvironment", true, false) as WorldEnvironment
	if world_environment == null or world_environment.environment == null:
		_fail("Missing world environment")
		return
	if world_environment.environment.fog_enabled:
		_fail("Fog should be disabled in low-spec mode")

func _assert_reduced_runtime_lights(main: Node) -> void:
	var omni_count := 0
	for node in main.find_children("*", "OmniLight3D", true, false):
		omni_count += 1
	if omni_count > 3:
		_fail("Low-spec mode should keep only critical omni lights, found %d" % omni_count)

func _assert_deep_red_lamp_retained(main: Node) -> void:
	var lamp := main.find_child("ShrineRedLamp", true, false) as OmniLight3D
	if lamp == null:
		_fail("Low-spec mode must retain the deepest red shrine lamp")
		return
	if lamp.light_color.r < 0.85 or lamp.light_color.g > 0.22:
		_fail("Shrine red lamp color is not red enough: %s" % lamp.light_color)

func _first_standard_material(node: Node) -> StandardMaterial3D:
	var mesh_instance := _first_mesh_instance(node)
	if mesh_instance == null:
		return null
	var primitive_mesh := mesh_instance.mesh as PrimitiveMesh
	if primitive_mesh == null:
		return null
	return primitive_mesh.material as StandardMaterial3D

func _first_mesh_instance(node: Node) -> MeshInstance3D:
	for child in node.get_children():
		if child is MeshInstance3D:
			return child as MeshInstance3D
		var nested := _first_mesh_instance(child)
		if nested != null:
			return nested
	return null

func _fail(message: String) -> void:
	_failed = true
	push_error(message)
