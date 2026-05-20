extends SceneTree

const MainScene := preload("res://scenes/Main.tscn")
const ArtifactDefinition := preload("res://scripts/core/artifact_definition.gd")

var _failed := false

func _initialize() -> void:
	var main: Node = MainScene.instantiate()
	root.add_child(main)
	for _i in range(90):
		await physics_frame
	await _travel_to_estate(main)
	if _failed:
		quit(1)
		return

	var player := main.get("player") as Node3D
	if player == null:
		_fail("Main did not create player")
		quit(1)
		return
	player.global_position = Vector3(0.0, 1.34, -94.0)
	main.call("_on_artifact_picked_up", ArtifactDefinition.new("monster escalation relic", 10, 1.0, 12, [], 1))
	for _i in range(12):
		await physics_frame

	_assert_navigation_foundation(main, player)
	_assert_late_monster_roster(main)
	_assert_monsters_use_distinct_spawn_anchors(main)
	_assert_dalgyal_watch_mechanic(main)
	_assert_eoduksini_flashlight_growth(main)
	_assert_jangsanbeom_lure_data(main)
	if _failed:
		quit(1)
		return
	print("KOREAN_MONSTER_AI: low-poly roster, navigation agents, and gimmicks verified")
	quit(0)

func _travel_to_estate(main: Node) -> void:
	if main.has_method("travel_to_retrieval_map"):
		main.call("travel_to_retrieval_map", "jongga_estate")
	for _i in range(90):
		if str(main.get("current_map_id")) == "jongga_estate":
			await physics_frame
			return
		await physics_frame
	_fail("Korean monster AI test could not travel to the estate map")

func _assert_navigation_foundation(main: Node, player: Node3D) -> void:
	if main.find_child("EstateNavigationRegion", true, false) == null:
		_fail("Estate map needs a NavigationRegion3D foundation for monster AI")
	var flashlight := player.find_child("Flashlight", true, false) as SpotLight3D
	if flashlight == null:
		_fail("Player needs a SpotLight3D flashlight for light-reactive monster logic")

func _assert_late_monster_roster(main: Node) -> void:
	for ghost_type in ["dalgyal_gwisin", "eoduksini", "changgwi", "jangsanbeom"]:
		var threat := _threat_by_type(main, ghost_type)
		if threat == null:
			_fail("Missing late-stage Korean monster threat: %s" % ghost_type)
			return
		if not threat.visible:
			_fail("%s should be visible when the player is deep inside the estate at stage 5" % ghost_type)
			return
		if str(threat.get_meta("model_style", "")) != "low_poly_ps1":
			_fail("%s should declare low_poly_ps1 model style" % ghost_type)
			return
		if threat.find_child("NavigationAgent3D", true, false) == null:
			_fail("%s has no NavigationAgent3D" % ghost_type)
			return
		if not threat.has_method("movement_speed_for_context"):
			_fail("%s has no Korean monster AI script" % ghost_type)
			return

func _assert_monsters_use_distinct_spawn_anchors(main: Node) -> void:
	var expected_anchors := {
		"dalgyal_gwisin": "GhostHauntDalgyalGwisin",
		"eoduksini": "GhostHauntEoduksini",
		"changgwi": "GhostHauntChanggwi",
		"jangsanbeom": "GhostHauntJangsanbeom",
	}
	var positions: Array[Vector3] = []
	for ghost_type in expected_anchors.keys():
		var threat := _threat_by_type(main, ghost_type)
		var anchor := main.find_child(str(expected_anchors[ghost_type]), true, false) as Node3D
		if threat == null or anchor == null:
			_fail("Missing threat or spawn anchor for %s" % ghost_type)
			return
		var horizontal_delta := Vector2(threat.global_position.x - anchor.global_position.x, threat.global_position.z - anchor.global_position.z).length()
		if horizontal_delta > 2.0:
			_fail("%s should spawn near %s, delta=%s" % [ghost_type, expected_anchors[ghost_type], horizontal_delta])
			return
		positions.append(threat.global_position)
	for i in range(positions.size()):
		for j in range(i + 1, positions.size()):
			var horizontal_distance := Vector2(positions[i].x - positions[j].x, positions[i].z - positions[j].z).length()
			if horizontal_distance < 5.0:
				_fail("Late-stage monsters should not share the same spawn cluster; distance=%s" % horizontal_distance)
				return

func _assert_dalgyal_watch_mechanic(main: Node) -> void:
	var dalgyal := _threat_by_type(main, "dalgyal_gwisin")
	if dalgyal == null:
		return
	var watched_speed := float(dalgyal.call("movement_speed_for_context", true, false))
	var ignored_speed := float(dalgyal.call("movement_speed_for_context", false, false))
	if ignored_speed <= watched_speed * 1.5:
		_fail("Dalgyal gwisin should surge forward when the player looks away")

func _assert_eoduksini_flashlight_growth(main: Node) -> void:
	var eoduksini := _threat_by_type(main, "eoduksini")
	if eoduksini == null:
		return
	var before := eoduksini.scale.y
	eoduksini.call("apply_flashlight_pressure", 1.0)
	var after := eoduksini.scale.y
	if after <= before:
		_fail("Eoduksini should grow when hit by flashlight pressure")

func _assert_jangsanbeom_lure_data(main: Node) -> void:
	var jangsanbeom := _threat_by_type(main, "jangsanbeom")
	if jangsanbeom == null:
		return
	if not jangsanbeom.has_method("lure_lines"):
		_fail("Jangsanbeom AI should expose lure lines")
		return
	var lines: Array = jangsanbeom.call("lure_lines")
	if not lines.has("여기 비싼 아이템 있어!") or not lines.has("살려줘!"):
		_fail("Jangsanbeom should lure players with item/help voice lines")

func _threat_by_type(main: Node, ghost_type: String) -> Node3D:
	for node in main.get_tree().get_nodes_in_group("threats"):
		var threat := node as Node3D
		if threat != null and str(threat.get_meta("ghost_type", "")) == ghost_type:
			return threat
	return null

func _fail(message: String) -> void:
	_failed = true
	push_error(message)
