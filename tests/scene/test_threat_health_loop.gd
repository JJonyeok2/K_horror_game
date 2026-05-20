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

	await _assert_stage_below_two_cannot_damage(main, player)
	if _failed:
		quit(1)
		return
	await _assert_stage_two_threat_pursues_and_attacks(main, player)
	if _failed:
		quit(1)
		return
	await _assert_health_zero_disables_player(main, player)
	if _failed:
		quit(1)
		return

	print("THREAT_HEALTH_LOOP: spawn pursue fixed cadence damage player_down")
	quit(0)

func _assert_stage_below_two_cannot_damage(main: Node, player: Node3D) -> void:
	var starting_health := float(player.get("health"))
	var fake_threat := Node3D.new()
	fake_threat.name = "ThreatApparition"
	main.add_child(fake_threat)
	fake_threat.global_position = player.global_position + Vector3(0.0, 1.15, -0.8)
	for _i in range(260):
		await physics_frame
	if float(player.get("health")) != starting_health:
		_fail("Threat damaged player before resentment stage 2")
		return
	fake_threat.queue_free()
	await physics_frame

func _assert_stage_two_threat_pursues_and_attacks(main: Node, player: Node3D) -> void:
	var relic := ArtifactDefinition.new("cursed relic", 10, 1.0, 3, [], 1)
	main.call("_on_artifact_picked_up", relic)
	for _i in range(2):
		await physics_frame
	var threat := main.find_child("ThreatApparition", true, false) as Node3D
	if threat == null:
		_fail("ThreatApparition did not spawn at resentment stage 2")
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
		_fail("Threat did not damage player while in range at stage 2")
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
