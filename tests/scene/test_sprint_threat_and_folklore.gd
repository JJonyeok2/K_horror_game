extends SceneTree

const MainScene := preload("res://scenes/Main.tscn")
const ArtifactDefinition := preload("res://scripts/core/artifact_definition.gd")

var _failed := false

func _initialize() -> void:
	var main: Node = MainScene.instantiate()
	root.add_child(main)
	for _i in range(360):
		await physics_frame

	var player := main.get("player") as Node3D
	if player == null:
		_fail("Main did not create player")
		return

	await _travel_to_jongga_estate(main, player)
	await _assert_sprint_stamina(player)
	await _assert_weight_slows_movement_and_drains_sprint(player)
	await _assert_exhausted_walk_recovery(player)
	_assert_folklore_route_props(main)
	await _assert_threat_manifestation(main)
	if _failed:
		quit(1)
		return
	print("SPRINT_THREAT_SMOKE: sprint stamina, jangseung, hidden path cover, threat present")
	quit(0)

func _travel_to_jongga_estate(main: Node, player: Node3D) -> void:
	var terminal := main.find_child("BongoQuotaMonitor", true, false)
	if terminal == null or not terminal.has_method("interact"):
		_fail("Missing BongoQuotaMonitor before sprint test")
		return
	terminal.interact(player)
	for _i in range(90):
		if str(main.get("current_map_id")) != "bongo_travel":
			break
		await physics_frame
	if str(main.get("current_map_id")) != "jongga_estate":
		_fail("Sprint test should run after arriving at the retrieval map")
		return

func _assert_sprint_stamina(player: Node3D) -> void:
	if not InputMap.has_action("sprint"):
		_fail("InputMap is missing sprint action")
		return
	var max_stamina := float(player.get("max_stamina_seconds"))
	if max_stamina != 15.0:
		_fail("Sprint max stamina should be 15 seconds, got %s" % max_stamina)
		return
	var before := float(player.get("stamina_seconds"))
	player.set("movement_enabled", true)
	Input.action_press("move_forward")
	Input.action_press("sprint")
	for _i in range(60):
		await physics_frame
	Input.action_release("sprint")
	Input.action_release("move_forward")
	var after := float(player.get("stamina_seconds"))
	if after >= before:
		_fail("Sprint did not drain stamina: before=%s after=%s" % [before, after])
		return
	if float(player.get("stamina_ratio")) >= 1.0:
		_fail("Stamina ratio did not reflect drain")
		return

func _assert_weight_slows_movement_and_drains_sprint(player: Node3D) -> void:
	var empty_walk_speed := await _measure_forward_walk_speed(player, float(player.get("max_stamina_seconds")))
	var empty_drain := await _measure_sprint_drain(player, 0.0)
	var heavy_walk_speed := await _measure_weighted_walk_speed(player, 10.5)
	var heavy_drain := await _measure_sprint_drain(player, 10.5)
	if not heavy_walk_speed < empty_walk_speed * 0.72:
		_fail("Heavy carried weight should noticeably slow walking: empty=%s heavy=%s" % [empty_walk_speed, heavy_walk_speed])
		return
	if not heavy_drain > empty_drain * 1.25:
		_fail("Heavy carried weight should drain sprint stamina faster: empty=%s heavy=%s" % [empty_drain, heavy_drain])
		return

func _assert_exhausted_walk_recovery(player: Node3D) -> void:
	var max_stamina := float(player.get("max_stamina_seconds"))
	var threshold_value: Variant = player.get("exhausted_recovery_threshold_seconds")
	var recovery_threshold := 1.0 if threshold_value == null else float(threshold_value)
	if recovery_threshold <= 0.0:
		recovery_threshold = 1.0
	var normal_speed := await _measure_forward_walk_speed(player, max_stamina)
	var exhausted_speed := await _measure_forward_walk_speed(player, 0.0)
	if not exhausted_speed < normal_speed * 0.98:
		_fail("Fully depleted stamina should slow walking: normal=%s exhausted=%s" % [normal_speed, exhausted_speed])
		return
	var low_recovery_speed := await _measure_forward_walk_speed(player, recovery_threshold * 0.5)
	if not low_recovery_speed < normal_speed * 0.98:
		_fail("Exhausted walking recovered too early: normal=%s low_recovery=%s" % [normal_speed, low_recovery_speed])
		return
	var recovered_speed := await _measure_forward_walk_speed(player, min(max_stamina, recovery_threshold + 0.5))
	if absf(recovered_speed - normal_speed) > 0.05:
		_fail("Walking speed did not recover after stamina threshold: normal=%s recovered=%s" % [normal_speed, recovered_speed])
		return
	var exhausted_multiplier: Variant = player.get("exhausted_walk_multiplier")
	if exhausted_multiplier == null or float(exhausted_multiplier) <= 0.0 or float(exhausted_multiplier) >= 1.0:
		_fail("Exhausted walk multiplier should be between 0.0 and 1.0, got %s" % exhausted_multiplier)
		return

func _measure_forward_walk_speed(player: Node3D, stamina: float) -> float:
	player.inventory.clear()
	if player.has_method("refresh_held_item_views"):
		player.refresh_held_item_views()
	return await _measure_weighted_walk_speed(player, 0.0, stamina)

func _measure_weighted_walk_speed(player: Node3D, carried_weight: float, stamina: float = -1.0) -> float:
	player.inventory.clear()
	if carried_weight > 0.0:
		player.call("try_collect_artifact", ArtifactDefinition.new("weight test", 10, carried_weight, 0, [], 1))
	player.set("stamina_seconds", stamina)
	if stamina < 0.0:
		player.set("stamina_seconds", float(player.get("max_stamina_seconds")))
	player.set("movement_enabled", true)
	Input.action_release("sprint")
	Input.action_press("move_forward")
	await physics_frame
	Input.action_release("move_forward")
	var player_velocity: Vector3 = player.get("velocity")
	player.inventory.clear()
	if player.has_method("refresh_held_item_views"):
		player.refresh_held_item_views()
	return Vector2(player_velocity.x, player_velocity.z).length()

func _measure_sprint_drain(player: Node3D, carried_weight: float) -> float:
	player.inventory.clear()
	if carried_weight > 0.0:
		player.call("try_collect_artifact", ArtifactDefinition.new("sprint weight test", 10, carried_weight, 0, [], 1))
	var max_stamina := float(player.get("max_stamina_seconds"))
	player.set("stamina_seconds", max_stamina)
	player.set("movement_enabled", true)
	Input.action_press("move_forward")
	Input.action_press("sprint")
	for _i in range(45):
		await physics_frame
	Input.action_release("sprint")
	Input.action_release("move_forward")
	var drained := max_stamina - float(player.get("stamina_seconds"))
	player.inventory.clear()
	if player.has_method("refresh_held_item_views"):
		player.refresh_held_item_views()
	return drained

func _assert_folklore_route_props(main: Node) -> void:
	var required := [
		"JangseungLeft01",
		"JangseungRight01",
		"HiddenPathScreeningBrushA",
		"HiddenPathScreeningBrushB",
	]
	for label in required:
		var node := main.find_child(label, true, false)
		if node == null:
			_fail("Missing folklore/hidden-path prop: %s" % label)
			return
		if node.find_child("CollisionShape3D", true, false) == null:
			_fail("%s has no collision shape" % label)
			return

func _assert_threat_manifestation(main: Node) -> void:
	var player := main.get("player") as Node3D
	if player != null:
		player.global_position = Vector3(0.0, 1.34, -86.0)
	var resentment: Variant = main.get("resentment")
	resentment.call("add_resentment", 8, "test escalation")
	for _i in range(4):
		await process_frame
	var threat := _first_visible_threat(main)
	if threat == null:
		_fail("Threat apparition did not spawn when resentment reached high stage")
		return
	if not threat.visible:
		_fail("Threat apparition exists but is not visible")
		return
	if not bool(threat.get_meta("can_phase_through_walls", false)):
		_fail("High-stage threat should be marked as wall-phasing")
		return
	if str(threat.get_meta("threat_zone", "")) != "inner_building_only":
		_fail("High-stage threat should be marked as building-only")
		return

func _first_visible_threat(main: Node) -> Node3D:
	for node in main.get_tree().get_nodes_in_group("threats"):
		var threat := node as Node3D
		if threat != null and threat.visible:
			return threat
	return null

func _fail(message: String) -> void:
	_failed = true
	Input.action_release("move_forward")
	if InputMap.has_action("sprint"):
		Input.action_release("sprint")
	push_error(message)
