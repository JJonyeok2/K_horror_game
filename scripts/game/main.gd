extends Node3D

const PlayerScene := preload("res://scenes/player/Player.tscn")
const AudioDirectorScript := preload("res://scripts/audio/audio_director.gd")
const JonggaEstateBuilder := preload("res://scripts/maps/jongga_estate_builder.gd")
const ArtifactDefinition = preload("res://scripts/core/artifact_definition.gd")
const QuotaTracker := preload("res://scripts/core/quota_tracker.gd")
const ResentmentTracker := preload("res://scripts/core/resentment_tracker.gd")
const ThreatDirector := preload("res://scripts/core/threat_director.gd")
const HUDScript := preload("res://scripts/ui/hud.gd")
const StartupSequence := preload("res://scripts/game/startup_sequence.gd")
const PerformanceSettingsScript := preload("res://scripts/game/performance_settings.gd")

var player: Node
var quota := QuotaTracker.new(800)
var resentment := ResentmentTracker.new()
var threat_director := ThreatDirector.new()
var audio_director: Node
var hud: CanvasLayer
var startup_sequence := StartupSequence.new()
var _startup_time: float = 0.0
var _startup_done: bool = false
var _side_passage_triggered: bool = false

func _ready() -> void:
	audio_director = AudioDirectorScript.new()
	add_child(audio_director)
	_configure_world()
	_create_scene_lighting()
	var map := JonggaEstateBuilder.new()
	add_child(map)
	map.build(self)
	player = PlayerScene.instantiate()
	add_child(player)
	startup_sequence.apply_player_start(player)
	player.movement_enabled = false
	hud = HUDScript.new()
	add_child(hud)
	resentment.resentment_changed.connect(_on_resentment_changed)
	print("BONGO_VAN: 오래된 봉고차가 비포장 길을 덜컹이며 들어옵니다.")
	print("K Horror Retrieval Prototype booted")

func _configure_world() -> void:
	var low_spec_mode := PerformanceSettingsScript.is_low_spec_mode()
	RenderingServer.set_default_clear_color(Color(0.012, 0.015, 0.018))
	var world_environment := WorldEnvironment.new()
	world_environment.name = "NightFogEnvironment"
	var environment := Environment.new()
	environment.background_mode = Environment.BG_COLOR
	environment.background_color = Color(0.012, 0.015, 0.018)
	environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	environment.ambient_light_color = Color(0.05, 0.062, 0.07)
	environment.ambient_light_energy = 0.32 if low_spec_mode else 0.22
	environment.fog_enabled = not low_spec_mode
	environment.fog_light_color = Color(0.11, 0.14, 0.15)
	environment.fog_light_energy = 0.35
	environment.fog_density = 0.018
	world_environment.environment = environment
	add_child(world_environment)

func _create_scene_lighting() -> void:
	var low_spec_mode := PerformanceSettingsScript.is_low_spec_mode()
	var light: DirectionalLight3D = DirectionalLight3D.new()
	light.name = "ColdMoonLight"
	light.rotation_degrees = Vector3(-58, -32, 0)
	light.light_color = Color(0.72, 0.82, 1.0)
	light.light_energy = 0.65 if low_spec_mode else 0.95
	light.shadow_enabled = false
	add_child(light)
	_add_lantern_light("VanInteriorLamp", Vector3(0.0, 2.45, 13.0), Color(1.0, 0.62, 0.34), 1.1 if low_spec_mode else 1.8, 5.0)
	_add_lantern_light("GateWarningLamp", Vector3(-6.7, 2.1, -8.5), Color(1.0, 0.42, 0.25), 1.3 if low_spec_mode else 2.2, 6.0)
	if low_spec_mode:
		_add_lantern_light("ShrineRedLamp", Vector3(0.0, 2.35, -141.0), Color(1.0, 0.12, 0.08), 1.35, 7.5)
		print("LOW_SPEC_MODE: PBR textures and fog disabled")
		return
	_add_lantern_light("CourtyardWellLamp", Vector3(-15.0, 1.7, -41.0), Color(0.7, 0.95, 0.8), 1.2, 8.0)
	_add_lantern_light("ShrineRedLamp", Vector3(0.0, 2.2, -141.0), Color(1.0, 0.12, 0.08), 2.8, 9.0)

func _add_lantern_light(label: String, position: Vector3, color: Color, energy: float, light_range: float) -> void:
	var lamp := OmniLight3D.new()
	lamp.name = label
	lamp.light_color = color
	lamp.light_energy = energy
	lamp.omni_range = light_range
	lamp.shadow_enabled = false
	add_child(lamp)
	lamp.global_position = position

func _process(delta: float) -> void:
	_update_startup_sequence(delta)
	var interaction_label: String = ""
	var interactor: Node = player.get_node_or_null("Camera3D/Interactor")
	if interactor != null:
		interaction_label = interactor.current_label
	hud.update_status(
		quota.recovered_value,
		quota.required_value,
		player.inventory.total_weight(),
		player.inventory.max_weight,
		resentment.stage(),
		interaction_label,
		player.inventory.hand_status(),
		player.health_ratio,
		player.stamina_ratio,
		player.is_sprinting
	)

func _update_startup_sequence(delta: float) -> void:
	if _startup_done:
		return
	_startup_time += delta
	var camera: Node3D = player.get_node_or_null("Camera3D")
	if camera != null:
		camera.position.x = sin(_startup_time * 12.0) * 0.025
		camera.position.y = 1.55 + sin(_startup_time * 8.0) * 0.018
	if _startup_time >= 5.0:
		_startup_done = true
		player.movement_enabled = true
		if camera != null:
			camera.position = Vector3(0, 1.55, 0)
		print("BONGO_VAN_DOOR: 철컥, 낡은 봉고차 문이 열립니다.")
		print("도착했습니다. 대문까지 걸어가세요.")

func register_artifact(artifact: Node) -> void:
	artifact.picked_up.connect(_on_artifact_picked_up)

func _on_artifact_picked_up(definition: ArtifactDefinition) -> void:
	resentment.add_resentment(definition.resentment_gain, "%s 회수" % definition.display_name)

func _on_resentment_changed(value: int, stage: int, reason: String) -> void:
	print("원한:%d 단계:%d 이유:%s" % [value, stage, reason])
	audio_director.play_stage_cues(threat_director.audio_cues_for_stage(stage))
	_update_threat_manifestation(stage)

func _update_threat_manifestation(stage: int) -> void:
	if stage < 2:
		return
	var threat := find_child("ThreatApparition", true, false) as Node3D
	if threat == null:
		threat = Node3D.new()
		threat.name = "ThreatApparition"
		add_child(threat)
		_create_threat_visual(threat)
	var player_node: Node3D = player as Node3D
	var player_position: Vector3 = Vector3.ZERO
	if player_node != null:
		player_position = player_node.global_position
	threat.global_position = player_position + Vector3(0.0, 1.15, -18.0 + float(stage) * -1.5)
	threat.visible = true

func _create_threat_visual(root: Node3D) -> void:
	var body := MeshInstance3D.new()
	body.name = "ThreatBody"
	var body_mesh := CapsuleMesh.new()
	body_mesh.radius = 0.34
	body_mesh.height = 2.3
	var body_mat := StandardMaterial3D.new()
	body_mat.albedo_color = Color(0.08, 0.075, 0.07, 0.88)
	body_mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	body_mesh.material = body_mat
	body.mesh = body_mesh
	root.add_child(body)
	var face := MeshInstance3D.new()
	face.name = "ThreatPaleFace"
	face.position = Vector3(0.0, 0.55, -0.18)
	var face_mesh := BoxMesh.new()
	face_mesh.size = Vector3(0.42, 0.55, 0.05)
	var face_mat := StandardMaterial3D.new()
	face_mat.albedo_color = Color(0.82, 0.82, 0.74, 0.72)
	face_mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	face_mesh.material = face_mat
	face.mesh = face_mesh
	root.add_child(face)
	var marker_light := OmniLight3D.new()
	marker_light.name = "ThreatColdGlow"
	marker_light.light_color = Color(0.52, 0.7, 0.75)
	marker_light.light_energy = 0.8
	marker_light.omni_range = 5.0
	root.add_child(marker_light)

func extract_player_inventory() -> void:
	var value: int = player.inventory.total_value()
	if value <= 0:
		return
	player.inventory.clear()
	if player.has_method("refresh_held_item_views"):
		player.refresh_held_item_views()
	quota.add_recovered_value(value)
	print("정산:%d / 할당량:%d" % [quota.recovered_value, quota.required_value])

func _on_risky_side_passage_entered(body: Node) -> void:
	if _side_passage_triggered or body != player:
		return
	_side_passage_triggered = true
	resentment.add_resentment(2, "대문 옆 샛길 침입")
	print("샛길의 깨진 항아리가 울립니다. 원한이 올라갑니다.")
