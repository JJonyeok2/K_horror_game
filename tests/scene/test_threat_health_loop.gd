extends SceneTree

const MainScene := preload("res://scenes/Main.tscn")
const ArtifactDefinition := preload("res://scripts/core/artifact_definition.gd")

var _failed := false

func _initialize() -> void:
	var main: Node = MainScene.instantiate()
	root.add_child(main)
	for _i in range(12):
		await physics_frame
	await _travel_to_estate(main)
	if _failed:
		quit(1)
		return

	var player := main.get("player") as Node3D
	if player == null:
		_fail("Main did not create player")
		return
	player.set("movement_enabled", true)
	player.set("velocity", Vector3.ZERO)

	await _assert_stage_three_dokkaebi_stays_outside_gate(main, player)
	if _failed:
		quit(1)
		return
	await _assert_stage_four_ignores_courtyard_until_building(main, player)
	if _failed:
		quit(1)
		return
	await _assert_stage_five_accumulates_prior_threats(main, player)
	if _failed:
		quit(1)
		return
	await _assert_health_zero_disables_player(main, player)
	if _failed:
		quit(1)
		return

	print("THREAT_HEALTH_LOOP: zone gated ghost patterns and player_down")
	quit(0)

func _travel_to_estate(main: Node) -> void:
	if main.has_method("travel_to_retrieval_map"):
		main.call("travel_to_retrieval_map", "jongga_estate")
	for _i in range(90):
		if str(main.get("current_map_id")) == "jongga_estate":
			await physics_frame
			return
		await physics_frame
	_fail("Threat health loop test could not travel to the estate map")

func _assert_stage_three_dokkaebi_stays_outside_gate(main: Node, player: Node3D) -> void:
	player.global_position = Vector3(0.0, 1.34, 120.0)
	var starting_health := float(player.get("health"))
	var relic := ArtifactDefinition.new("warning relic", 10, 1.0, 5, [], 1)
	main.call("_on_artifact_picked_up", relic)
	for _i in range(8):
		await physics_frame
	var threat := _threat_by_type(main, "dokkaebi")
	if threat == null:
		_fail("Stage 3 dokkaebi did not spawn outside the gate")
		return
	if str(threat.get_meta("ghost_type", "")) != "dokkaebi":
		_fail("Stage 3 threat should be dokkaebi")
		return
	if str(threat.get_meta("threat_zone", "")) != "outside_gate_forest":
		_fail("Dokkaebi should be marked as outside-gate forest threat")
		return
	if str(threat.get_meta("attack_pattern", "")) != "dokkaebi_forest_trickster":
		_fail("Dokkaebi should use forest trickster attack pattern")
		return
	var gate := main.find_child("OuterEstateGate", true, false) as Node3D
	if gate != null and threat.global_position.z <= gate.global_position.z:
		_fail("Dokkaebi spawned inside the gate instead of outside/forest")
		return

	player.global_position = Vector3(0.0, 1.34, -40.0)
	for _i in range(60):
		await physics_frame
	if threat.visible:
		_fail("Dokkaebi should disappear once player is inside the gate")
		return
	if float(player.get("health")) != starting_health:
		_fail("Dokkaebi damaged player after leaving its outside-gate zone")
		return

func _assert_stage_four_ignores_courtyard_until_building(main: Node, player: Node3D) -> void:
	player.global_position = Vector3(0.0, 1.34, -42.0)
	var relic := ArtifactDefinition.new("cursed relic", 10, 1.0, 3, [], 1)
	main.call("_on_artifact_picked_up", relic)
	for _i in range(30):
		await physics_frame
	var threat := _threat_by_type(main, "sangbok_ghost")
	if threat == null:
		_fail("Sangbok threat node missing after stage escalation")
		return
	if threat.visible:
		_fail("Stage 4 threat should not appear in the open courtyard")
		return

	player.global_position = Vector3(0.0, 1.34, -86.0)
	for _i in range(4):
		await physics_frame
	if not threat.visible:
		_fail("Stage 4 threat did not appear after entering the building")
		return
	if not bool(threat.get_meta("can_phase_through_walls", false)):
		_fail("High-stage ThreatApparition should explicitly be wall-phasing")
		return
	if str(threat.get_meta("ghost_type", "")) != "sangbok_ghost":
		_fail("Stage 4 threat should be sangbok ghost")
		return
	if str(threat.get_meta("threat_zone", "")) != "inner_building_only":
		_fail("Stage 4 threat should be building-only")
		return
	if str(threat.get_meta("attack_pattern", "")) != "sangbok_steady_pursuit":
		_fail("Stage 4 threat should use steady pursuit")
		return
	var before_distance := threat.global_position.distance_to(player.global_position)
	for _i in range(90):
		await physics_frame
	var after_distance := threat.global_position.distance_to(player.global_position)
	if after_distance >= before_distance - 0.2:
		_fail("ThreatApparition did not pursue player: before=%s after=%s" % [before_distance, after_distance])
		return

	threat.global_position = player.global_position + Vector3(0.0, 1.15, -0.9)
	var health_before_attack := float(player.get("health"))
	for _i in range(15):
		await physics_frame
	if float(player.get("health")) != health_before_attack:
		_fail("Threat should wait for fixed attack cadence before damaging")
		return
	for _i in range(130):
		await physics_frame
	if float(player.get("health")) >= health_before_attack:
		_fail("Threat did not damage player while in range at stage 4")
		return

func _assert_stage_five_accumulates_prior_threats(main: Node, player: Node3D) -> void:
	player.global_position = Vector3(0.0, 1.34, -98.0)
	var relic := ArtifactDefinition.new("deep relic", 10, 1.0, 4, [], 1)
	main.call("_on_artifact_picked_up", relic)
	for _i in range(6):
		await physics_frame
	var active_types := _visible_threat_types(main)
	if not active_types.has("sangbok_ghost"):
		_fail("Stage 5 should keep the stage 4 sangbok threat active")
		return
	if not active_types.has("dalgyal_gwisin"):
		_fail("Stage 5 should add dalgyal gwisin without replacing earlier threats")
		return
	var dalgyal := _threat_by_type(main, "dalgyal_gwisin")
	if dalgyal == null:
		_fail("Stage 5 dalgyal threat node missing")
		return
	if str(dalgyal.get_meta("attack_pattern", "")) != "dalgyal_blind_lunge":
		_fail("Stage 5 dalgyal should use blind lunge pattern")
		return
	if str(dalgyal.get_meta("threat_zone", "")) != "inner_building_only":
		_fail("Stage 5 dalgyal should remain building-only")
		return

func _assert_health_zero_disables_player(main: Node, player: Node3D) -> void:
	player.set("health", 1.0)
	player.set("health_ratio", 0.01)
	player.set("movement_enabled", true)
	var threat := _threat_by_type(main, "dalgyal_gwisin")
	if threat == null:
		threat = _threat_by_type(main, "sangbok_ghost")
	if threat == null:
		_fail("Threat node missing before down-state check")
		return
	threat.global_position = player.global_position + Vector3(0.0, 1.15, -0.9)
	for _i in range(260):
		await physics_frame
	if float(player.get("health")) > 0.0:
		_fail("Threat did not reduce low health to zero")
		return
	if bool(player.get("movement_enabled")):
		_fail("Player movement stayed enabled after health reached zero")
		return
	if not bool(main.get("_player_down")):
		_fail("Main did not set _player_down after health reached zero")
		return
	var hud := main.get("hud") as CanvasLayer
	var restart_button := hud.find_child("RestartRunButton", true, false) as Button if hud != null else null
	if restart_button == null or not restart_button.visible:
		_fail("Downed player should see a restart button")
		return
	if not InputMap.has_action("restart_run"):
		_fail("InputMap is missing restart_run action for R retry")
		return
	Input.action_press("restart_run")
	await process_frame
	await process_frame
	Input.action_release("restart_run")
	if bool(main.get("_player_down")):
		_fail("R retry did not clear player down state")
		return
	if float(player.get("health")) < float(player.get("max_health")):
		_fail("R retry did not restore player health")
		return
	if not bool(player.get("movement_enabled")):
		_fail("R retry did not re-enable movement")
		return

func _visible_threat_types(main: Node) -> Array[String]:
	var result: Array[String] = []
	for node in main.get_tree().get_nodes_in_group("threats"):
		var threat := node as Node3D
		if threat != null and threat.visible:
			result.append(str(threat.get_meta("ghost_type", "")))
	return result

func _threat_by_type(main: Node, ghost_type: String) -> Node3D:
	for node in main.get_tree().get_nodes_in_group("threats"):
		var threat := node as Node3D
		if threat != null and str(threat.get_meta("ghost_type", "")) == ghost_type:
			return threat
	return null

func _fail(message: String) -> void:
	_failed = true
	Input.action_release("restart_run")
	push_error(message)
