extends SceneTree

const MainScene := preload("res://scenes/Main.tscn")
const ArtifactDefinition := preload("res://scripts/core/artifact_definition.gd")

var _failed := false

func _initialize() -> void:
	var main: Node = MainScene.instantiate()
	root.add_child(main)
	for _i in range(12):
		await physics_frame

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
	await _assert_stage_five_swaps_to_dalgyal_pattern(main, player)
	if _failed:
		quit(1)
		return
	await _assert_health_zero_disables_player(main, player)
	if _failed:
		quit(1)
		return

	print("THREAT_HEALTH_LOOP: zone gated ghost patterns and player_down")
	quit(0)

func _assert_stage_three_dokkaebi_stays_outside_gate(main: Node, player: Node3D) -> void:
	player.global_position = Vector3(0.0, 1.34, 120.0)
	var starting_health := float(player.get("health"))
	var relic := ArtifactDefinition.new("warning relic", 10, 1.0, 5, [], 1)
	main.call("_on_artifact_picked_up", relic)
	for _i in range(8):
		await physics_frame
	var threat := main.find_child("ThreatApparition", true, false) as Node3D
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
	var threat := main.find_child("ThreatApparition", true, false) as Node3D
	if threat == null:
		_fail("ThreatApparition node missing after stage escalation")
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

func _assert_stage_five_swaps_to_dalgyal_pattern(main: Node, player: Node3D) -> void:
	player.global_position = Vector3(0.0, 1.34, -98.0)
	var relic := ArtifactDefinition.new("deep relic", 10, 1.0, 4, [], 1)
	main.call("_on_artifact_picked_up", relic)
	for _i in range(6):
		await physics_frame
	var threat := main.find_child("ThreatApparition", true, false) as Node3D
	if threat == null:
		_fail("ThreatApparition missing after stage 5 escalation")
		return
	if str(threat.get_meta("ghost_type", "")) != "dalgyal_gwisin":
		_fail("Stage 5 threat should swap to dalgyal gwisin")
		return
	if str(threat.get_meta("attack_pattern", "")) != "dalgyal_blind_lunge":
		_fail("Stage 5 threat should use blind lunge pattern")
		return
	if str(threat.get_meta("threat_zone", "")) != "inner_building_only":
		_fail("Stage 5 threat should remain building-only")
		return

func _assert_health_zero_disables_player(main: Node, player: Node3D) -> void:
	player.set("health", 1.0)
	player.set("health_ratio", 0.01)
	player.set("movement_enabled", true)
	var threat := main.find_child("ThreatApparition", true, false) as Node3D
	if threat == null:
		_fail("ThreatApparition missing before down-state check")
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

func _fail(message: String) -> void:
	_failed = true
	push_error(message)
