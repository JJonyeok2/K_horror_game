extends SceneTree

const MainScene := preload("res://scenes/Main.tscn")
const PerformanceSettingsScript := preload("res://scripts/game/performance_settings.gd")

var _failed := false
var _previous_low_spec: Variant

func _initialize() -> void:
	_previous_low_spec = ProjectSettings.get_setting(PerformanceSettingsScript.LOW_SPEC_SETTING, true)
	ProjectSettings.set_setting(PerformanceSettingsScript.LOW_SPEC_SETTING, false)
	var main: Node = MainScene.instantiate()
	root.add_child(main)
	for _i in range(90):
		await physics_frame

	_assert_default_prefers_high_spec()
	_assert_environment_uses_high_spec_atmosphere(main)
	_assert_dynamic_lights_cast_selected_shadows(main)

	ProjectSettings.set_setting(PerformanceSettingsScript.LOW_SPEC_SETTING, _previous_low_spec)
	if _failed:
		quit(1)
		return
	print("HIGH_SPEC_VISUALS: PBR atmosphere, volumetric fog, GI, selected dynamic shadows enabled")
	quit(0)

func _assert_default_prefers_high_spec() -> void:
	if bool(ProjectSettings.get_setting(PerformanceSettingsScript.LOW_SPEC_SETTING, true)):
		_fail("Project default should keep low_spec_mode disabled for local development")
		return
	if PerformanceSettingsScript.is_low_spec_mode():
		_fail("PerformanceSettings fallback should default to high-spec mode")

func _assert_environment_uses_high_spec_atmosphere(main: Node) -> void:
	var world_environment := main.find_child("NightFogEnvironment", true, false) as WorldEnvironment
	if world_environment == null or world_environment.environment == null:
		_fail("Missing NightFogEnvironment")
		return
	var environment := world_environment.environment
	if not environment.fog_enabled:
		_fail("High-spec mode should enable distance fog")
		return
	if environment.get("volumetric_fog_enabled") != true:
		_fail("High-spec mode should enable volumetric fog")
		return
	if environment.get("sdfgi_enabled") != true:
		_fail("High-spec mode should enable SDFGI runtime global illumination")

func _assert_dynamic_lights_cast_selected_shadows(main: Node) -> void:
	var moon := main.find_child("ColdMoonLight", true, false) as DirectionalLight3D
	if moon == null:
		_fail("Missing ColdMoonLight")
		return
	if not moon.shadow_enabled:
		_fail("High-spec moon light should cast shadows")
		return
	var gate_lamp := main.find_child("GateWarningLamp", true, false) as OmniLight3D
	if gate_lamp == null or not gate_lamp.shadow_enabled:
		_fail("Gate warning lamp should cast a selected high-spec shadow")
		return
	var shrine_lamp := main.find_child("ShrineRedLamp", true, false) as OmniLight3D
	if shrine_lamp == null or not shrine_lamp.shadow_enabled:
		_fail("Shrine red lamp should cast a selected high-spec shadow")

func _fail(message: String) -> void:
	_failed = true
	push_error(message)
