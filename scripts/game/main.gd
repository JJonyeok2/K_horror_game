extends Node3D

const PlayerScene := preload("res://scenes/player/Player.tscn")
const AudioDirectorScript := preload("res://scripts/audio/audio_director.gd")
const JonggaEstateBuilder := preload("res://scripts/maps/jongga_estate_builder.gd")
const BongoVanPlanScript := preload("res://scripts/maps/bongo_van_plan.gd")
const ArtifactDefinition = preload("res://scripts/core/artifact_definition.gd")
const QuotaTracker := preload("res://scripts/core/quota_tracker.gd")
const ResentmentTracker := preload("res://scripts/core/resentment_tracker.gd")
const ThreatDirector := preload("res://scripts/core/threat_director.gd")
const HUDScript := preload("res://scripts/ui/hud.gd")
const StartupSequence := preload("res://scripts/game/startup_sequence.gd")
const PerformanceSettingsScript := preload("res://scripts/game/performance_settings.gd")
const FALLBACK_THREAT_DAMAGE: float = 18.0
const FALLBACK_THREAT_ATTACK_RANGE: float = 1.65
const FALLBACK_THREAT_ATTACK_CADENCE: float = 1.0
const FALLBACK_THREAT_PURSUIT_SPEED: float = 2.85
const THREAT_PLAYER_OFFSET := Vector3(0.0, 1.15, 0.0)

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
var _threat_attack_elapsed: float = 0.0
var _player_down: bool = false
var player_down: bool = false
var pending_recovered_value: int = 0
var pending_cargo_items: Array[ArtifactDefinition] = []
var _stored_cargo_visual_index: int = 0

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
	_update_bongo_quota_monitor()

func _physics_process(delta: float) -> void:
	_update_threat_health_loop(delta)

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
		if not _player_down:
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
	_ensure_threat_manifestation(stage)

func _ensure_threat_manifestation(stage: int) -> Node3D:
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
		threat.global_position = player_position + THREAT_PLAYER_OFFSET + Vector3(0.0, 0.0, -18.0 + float(stage) * -1.5)
		threat.add_to_group("threats")
		threat.set_meta("entity_type", "ghost")
		threat.set_meta("is_threat_entity", true)
	threat.visible = true
	return threat

func _update_threat_health_loop(delta: float) -> void:
	if _player_down or player == null:
		return
	var stage := resentment.stage()
	if stage < 2:
		_threat_attack_elapsed = 0.0
		return
	var player_node: Node3D = player as Node3D
	if player_node == null:
		return
	var threat := _ensure_threat_manifestation(stage)
	var target_position := player_node.global_position + THREAT_PLAYER_OFFSET
	var distance_to_player := threat.global_position.distance_to(target_position)
	var attack_range := _threat_attack_range(stage)
	if distance_to_player > max(attack_range * 0.45, 0.2):
		var step := _threat_pursuit_speed(stage) * delta
		threat.global_position = threat.global_position.move_toward(target_position, step)
		distance_to_player = threat.global_position.distance_to(target_position)
	if distance_to_player <= attack_range:
		_threat_attack_elapsed += delta
		var attack_cadence := _threat_attack_cadence(stage)
		while _threat_attack_elapsed >= attack_cadence and not _player_down:
			_threat_attack_elapsed -= attack_cadence
			_apply_threat_damage(stage)
	else:
		_threat_attack_elapsed = 0.0

func _apply_threat_damage(stage: int) -> void:
	var damage := _threat_damage(stage)
	if damage <= 0.0:
		return
	if player.has_method("apply_damage"):
		player.apply_damage(damage)
	else:
		var current_health := float(player.get("health"))
		var max_health: float = max(float(player.get("max_health")), 1.0)
		var next_health: float = clamp(current_health - damage, 0.0, max_health)
		player.set("health", next_health)
		player.set("health_ratio", clamp(next_health / max_health, 0.0, 1.0))
	if float(player.get("health")) <= 0.0:
		_mark_player_down()

func _mark_player_down() -> void:
	if _player_down:
		return
	_player_down = true
	player_down = true
	player.set("movement_enabled", false)
	print("PLAYER_DOWN: health depleted by ThreatApparition")

func _threat_damage(stage: int) -> float:
	return _threat_profile_float(
		stage,
		["damage", "damage_per_attack", "attack_damage", "damage_per_hit"],
		["damage_per_hit", "damage_for_stage", "attack_damage_for_stage", "damage_per_attack_for_stage", "threat_damage_for_stage"],
		FALLBACK_THREAT_DAMAGE
	)

func _threat_attack_range(stage: int) -> float:
	return _threat_profile_float(
		stage,
		["attack_range", "range", "damage_range"],
		["attack_range", "attack_range_for_stage", "threat_attack_range_for_stage", "damage_range_for_stage"],
		FALLBACK_THREAT_ATTACK_RANGE
	)

func _threat_attack_cadence(stage: int) -> float:
	return max(
		_threat_profile_float(
			stage,
			["attack_cadence", "cadence", "attack_interval", "attack_interval_seconds"],
			["attack_interval_seconds", "attack_cadence_for_stage", "threat_attack_cadence_for_stage", "attack_interval_for_stage"],
			FALLBACK_THREAT_ATTACK_CADENCE
		),
		0.05
	)

func _threat_pursuit_speed(stage: int) -> float:
	return _threat_profile_float(
		stage,
		["pursuit_speed", "speed", "move_speed"],
		["pursuit_speed", "pursuit_speed_for_stage", "threat_pursuit_speed_for_stage", "move_speed_for_stage"],
		FALLBACK_THREAT_PURSUIT_SPEED
	)

func _threat_profile_float(stage: int, profile_keys: Array, method_names: Array, fallback: float) -> float:
	var profile: Variant = _threat_damage_profile(stage)
	if typeof(profile) == TYPE_DICTIONARY:
		for key in profile_keys:
			if profile.has(key) and _is_number(profile[key]):
				return max(float(profile[key]), 0.0)
	for method_name in method_names:
		var value: Variant = _call_threat_director(method_name, stage)
		if _is_number(value):
			return max(float(value), 0.0)
	return fallback

func _threat_damage_profile(stage: int) -> Variant:
	for method_name in ["damage_profile_for_stage", "threat_damage_profile_for_stage", "damage_profile"]:
		var profile: Variant = _call_threat_director(method_name, stage)
		if typeof(profile) == TYPE_DICTIONARY:
			return profile
	return null

func _call_threat_director(method_name: String, stage: int) -> Variant:
	if not threat_director.has_method(method_name):
		return null
	for method in threat_director.get_method_list():
		if str(method.get("name", "")) == method_name:
			var args: Array = method.get("args", [])
			if args.is_empty():
				return threat_director.call(method_name)
			return threat_director.call(method_name, stage)
	return threat_director.call(method_name, stage)

func _is_number(value: Variant) -> bool:
	return typeof(value) == TYPE_FLOAT or typeof(value) == TYPE_INT

func _update_bongo_quota_monitor() -> void:
	var label := find_child("BongoQuotaMonitorText", true, false) as Label3D
	if label == null:
		return
	label.text = "최종 ₩%d / ₩%d\n미정산 ₩%d" % [quota.recovered_value, quota.required_value, pending_recovered_value]

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
	if player.inventory.items.is_empty():
		return
	var deposited_items: Array = player.inventory.items.duplicate()
	var value: int = player.inventory.total_value()
	for item: ArtifactDefinition in deposited_items:
		pending_cargo_items.append(item)
		_create_stored_cargo_visual(item)
	pending_recovered_value += value
	player.inventory.clear()
	if player.has_method("refresh_held_item_views"):
		player.refresh_held_item_views()
	_update_bongo_quota_monitor()
	print("보관:%d 미정산:%d / 할당량:%d" % [value, pending_recovered_value, quota.required_value])

func settle_stored_cargo() -> void:
	if pending_recovered_value <= 0:
		print("정산할 물품이 없습니다.")
		return
	var settled_value := pending_recovered_value
	quota.add_recovered_value(settled_value)
	pending_recovered_value = 0
	pending_cargo_items.clear()
	_clear_pending_cargo_visuals()
	_update_bongo_quota_monitor()
	print("정산:%d / 할당량:%d" % [quota.recovered_value, quota.required_value])

func _create_stored_cargo_visual(item: ArtifactDefinition) -> void:
	_stored_cargo_visual_index += 1
	var body := StaticBody3D.new()
	body.name = "StoredCargoItem%02d" % _stored_cargo_visual_index
	body.add_to_group("pending_cargo_visuals")
	body.set_meta("display_name", item.display_name)
	body.set_meta("value", item.value)
	add_child(body)
	var row := float((_stored_cargo_visual_index - 1) / 3)
	var column := float((_stored_cargo_visual_index - 1) % 3)
	body.global_position = BongoVanPlanScript.STORED_CARGO_START_POSITION + Vector3(column * 0.55, row * 0.22, row * 0.36)
	var mesh_instance := MeshInstance3D.new()
	var box_mesh := BoxMesh.new()
	box_mesh.size = Vector3(0.46, 0.28, 0.34) if item.hand_slots < 2 else Vector3(0.72, 0.36, 0.44)
	var material := StandardMaterial3D.new()
	material.albedo_color = Color(0.58, 0.46, 0.28)
	material.roughness = 0.85
	box_mesh.material = material
	mesh_instance.mesh = box_mesh
	body.add_child(mesh_instance)
	var collision := CollisionShape3D.new()
	collision.name = "CollisionShape3D"
	var shape := BoxShape3D.new()
	shape.size = box_mesh.size
	collision.shape = shape
	body.add_child(collision)

func _clear_pending_cargo_visuals() -> void:
	for node in get_tree().get_nodes_in_group("pending_cargo_visuals"):
		node.queue_free()

func _on_risky_side_passage_entered(body: Node) -> void:
	if _side_passage_triggered or body != player:
		return
	_side_passage_triggered = true
	resentment.add_resentment(2, "대문 옆 샛길 침입")
	print("샛길의 깨진 항아리가 울립니다. 원한이 올라갑니다.")
