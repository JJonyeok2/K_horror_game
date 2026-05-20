extends SceneTree

const MainScene := preload("res://scenes/Main.tscn")
const ArtifactDefinition := preload("res://scripts/core/artifact_definition.gd")
const BongoVanPlanScript := preload("res://scripts/maps/bongo_van_plan.gd")

func _initialize() -> void:
	var main: Node = MainScene.instantiate()
	root.add_child(main)
	for _i in range(360):
		await physics_frame

	var player := main.get("player") as Node3D
	if player == null:
		_fail("Main did not create player")
		return
	if player.global_position.y < 0.2:
		_fail("Player fell below playable floor: y=%s" % player.global_position.y)
		return
	if player.get("movement_enabled") != true:
		_fail("Player movement was not enabled after startup sequence")
		return
	_assert_health_hud(main, player)

	_assert_required_map_nodes(main)
	_assert_van_camera_clearance(main, player)
	await _assert_hub_exit_locked_until_map_selected(main, player)
	await _travel_to_jongga_estate(main, player)
	await _assert_van_exit_without_jump(player)
	_assert_gate_interaction(main, player)
	await _assert_side_passage_risk(main, player)
	await _assert_extraction_zone_inside_van(main, player)
	_assert_inventory_drop_cycle(main, player)

	print("PLAYABLE_SMOKE: player_y=%s movement_enabled=%s" % [player.global_position.y, player.get("movement_enabled")])
	quit(0)

func _assert_required_map_nodes(main: Node) -> void:
	var required := {
		"VanStartGround": "StaticBody3D",
		"BongoInteriorFloor": "StaticBody3D",
		"BongoRoof": "StaticBody3D",
		"BongoRearStep": "StaticBody3D",
		"BongoLowExitRamp": "StaticBody3D",
		"BongoHubRearDoorBlocker": "StaticBody3D",
		"LongApproachRoad": "StaticBody3D",
		"OuterEstateGate": "Node3D",
		"LeftSwingGatePanel": "StaticBody3D",
		"RightSwingGatePanel": "StaticBody3D",
		"GateLeftPost": "StaticBody3D",
		"GateRightPost": "StaticBody3D",
		"GateThreshold": "StaticBody3D",
		"RiskySidePassage": "StaticBody3D",
		"RiskySidePassageTrigger": "Area3D",
		"LargeInnerCourtyard": "StaticBody3D",
		"LeftSideYard": "StaticBody3D",
		"SideYardChoke": "StaticBody3D",
		"BuildingFrontPad": "StaticBody3D",
		"MainHouseInterior": "StaticBody3D",
		"BackyardLoop": "StaticBody3D",
		"StorehouseLoop": "StaticBody3D",
		"OuthouseYard": "StaticBody3D",
		"DwitganOuthouse": "Node3D",
		"ShrineDeepZone": "StaticBody3D",
		"VanInteriorReturnZone": "Area3D",
	}
	for label: String in required.keys():
		var node := main.find_child(label, true, false)
		if node == null:
			_fail("Map is missing required node: %s" % label)
			return
		if node.get_class() != required[label]:
			_fail("%s has wrong type: %s" % [label, node.get_class()])
			return
		if required[label] == "StaticBody3D" or required[label] == "Area3D":
			if _box_shape_size(node) == Vector3.ZERO and _cylinder_shape_height(node) <= 0.0:
				_fail("%s has no collision shape" % label)
				return

func _assert_van_camera_clearance(main: Node, player: Node3D) -> void:
	var roof := main.find_child("BongoRoof", true, false) as Node3D
	var floor := main.find_child("BongoInteriorFloor", true, false) as Node3D
	if roof == null or floor == null:
		_fail("Van roof/floor missing")
		return
	var camera := player.get_node_or_null("Camera3D") as Camera3D
	if camera == null:
		_fail("Player has no camera")
		return
	var roof_bottom := roof.global_position.y - _box_shape_size(roof).y * 0.5
	if camera.global_position.y >= roof_bottom - 0.08:
		_fail("Camera is too close to van roof: camera_y=%s roof_bottom=%s" % [camera.global_position.y, roof_bottom])
		return
	var floor_size := _box_shape_size(floor)
	var local_delta := player.global_position - floor.global_position
	if abs(local_delta.x) > floor_size.x * 0.5 or abs(local_delta.z) > floor_size.z * 0.5:
		_fail("Player does not start inside van interior footprint")
		return

func _assert_hub_exit_locked_until_map_selected(main: Node, player: Node3D) -> void:
	if str(main.get("current_map_id")) != "bongo_hub":
		_fail("Playable scene should start in the bongo hub before map selection")
		return
	var blocker := main.find_child("BongoHubRearDoorBlocker", true, false) as StaticBody3D
	if blocker == null:
		_fail("Bongo hub needs a closed rear-door blocker before map selection")
		return
	if not blocker.visible:
		_fail("Bongo rear-door blocker should be visible in the hub")
		return
	if _box_shape_disabled(blocker):
		_fail("Bongo rear-door blocker collision should be active in the hub")
		return
	player.global_position = BongoVanPlanScript.PLAYER_START_POSITION
	player.rotation = Vector3.ZERO
	player.set("velocity", Vector3.ZERO)
	player.set("movement_enabled", true)
	Input.action_press("move_forward")
	for _i in range(120):
		await physics_frame
	Input.action_release("move_forward")
	var locked_exit_z: float = BongoVanPlanScript.PLAYER_START_POSITION.z - 1.75
	if player.global_position.z < locked_exit_z:
		_fail("Player exited the bongo before selecting a map: z=%s" % player.global_position.z)
		return

func _travel_to_jongga_estate(main: Node, player: Node3D) -> void:
	var selector := main.find_child("BongoMapSelector", true, false)
	if selector == null or not selector.has_method("interact"):
		_fail("Missing BongoMapSelector for playable scene travel")
		return
	selector.interact(player)
	for _i in range(90):
		if str(main.get("current_map_id")) != "bongo_travel":
			break
		await physics_frame
	if str(main.get("current_map_id")) != "jongga_estate":
		_fail("Playable scene did not arrive at jongga estate after map selection")
		return
	var blocker := main.find_child("BongoHubRearDoorBlocker", true, false) as StaticBody3D
	if blocker != null and (blocker.visible or not _box_shape_disabled(blocker)):
		_fail("Bongo rear-door blocker should open after arriving at the retrieval map")
		return

func _assert_health_hud(main: Node, player: Node3D) -> void:
	var max_health_value: Variant = player.get("max_health")
	var health_value: Variant = player.get("health")
	var health_ratio_value: Variant = player.get("health_ratio")
	if not _is_number(max_health_value) or not _is_number(health_value) or not _is_number(health_ratio_value):
		_fail("Player health fields are missing")
		return
	var max_health: float = max_health_value
	var health: float = health_value
	var health_ratio: float = health_ratio_value
	if max_health < 100.0:
		_fail("Player max health is missing or too low: %s" % max_health)
		return
	if health < max_health:
		_fail("Player should start at full health: %s/%s" % [health, max_health])
		return
	if health_ratio < 0.99:
		_fail("Player health ratio should start full: %s" % health_ratio)
		return
	var hud := main.get("hud") as CanvasLayer
	if hud == null:
		_fail("Main did not create HUD")
		return
	var health_back := hud.find_child("HealthGaugeBack", true, false) as ColorRect
	var health_fill := hud.find_child("HealthGaugeFill", true, false) as ColorRect
	var stamina_back := hud.find_child("StaminaGaugeBack", true, false) as ColorRect
	var stamina_fill := hud.find_child("StaminaGaugeFill", true, false) as ColorRect
	if health_back == null or health_fill == null or stamina_back == null or stamina_fill == null:
		_fail("HUD is missing health/stamina gauge nodes")
		return
	if health_fill.size.x < 200.0:
		_fail("Health gauge should render full width at startup: %s" % health_fill.size.x)
		return
	if health_back.anchor_left < 0.99 or health_back.anchor_right < 0.99:
		_fail("Health gauge should be anchored to the right side of the screen")
		return
	if health_back.offset_right > -16.0:
		_fail("Health gauge should keep a right screen margin: offset_right=%s" % health_back.offset_right)
		return
	if stamina_back.anchor_left != health_back.anchor_left or stamina_back.anchor_right != health_back.anchor_right:
		_fail("Stamina gauge should align with health gauge on the right side")
		return
	if stamina_back.offset_left != health_back.offset_left or stamina_back.offset_right != health_back.offset_right:
		_fail("Stamina gauge should use the same right-side offsets as health gauge")
		return
	var status_label := hud.get("label") as Label
	if status_label != null and status_label.text.find("회수금액") != -1:
		_fail("Recovered quota should not be shown on the player HUD")
		return

func _is_number(value: Variant) -> bool:
	return typeof(value) == TYPE_FLOAT or typeof(value) == TYPE_INT

func _assert_van_exit_without_jump(player: Node3D) -> void:
	player.global_position = BongoVanPlanScript.PLAYER_START_POSITION
	player.rotation = Vector3.ZERO
	player.set("velocity", Vector3.ZERO)
	player.set("movement_enabled", true)
	Input.action_press("move_forward")
	for _i in range(120):
		await physics_frame
	Input.action_release("move_forward")
	var exit_target_z: float = BongoVanPlanScript.PLAYER_START_POSITION.z - 2.35
	if player.global_position.z > exit_target_z:
		_fail("Player could not exit van without jump: z=%s" % player.global_position.z)
		return
	if player.global_position.y < 0.2:
		_fail("Player fell while exiting van: y=%s" % player.global_position.y)
		return

func _assert_gate_interaction(main: Node, player: Node3D) -> void:
	var gate := main.find_child("OuterEstateGate", true, false)
	var left_gate := main.find_child("LeftSwingGatePanel", true, false)
	var right_gate := main.find_child("RightSwingGatePanel", true, false)
	if gate == null or left_gate == null or right_gate == null:
		_fail("Gate nodes are incomplete")
		return
	if not left_gate.has_method("interact") or not right_gate.has_method("interact"):
		_fail("Gate leaves are not directly interactable")
		return
	left_gate.interact(player)
	if gate.get("is_open") != true:
		_fail("Interacting with left gate leaf did not open gate")
		return
	right_gate.interact(player)
	if gate.get("is_open") != false:
		_fail("Interacting with right gate leaf did not close gate")
		return

func _assert_side_passage_risk(main: Node, player: Node3D) -> void:
	var resentment: Variant = main.get("resentment")
	var before := int(resentment.get("current_value"))
	player.global_position = Vector3(-11.7, 1.34, -19.5)
	player.set("velocity", Vector3.ZERO)
	for _i in range(6):
		await physics_frame
	var after_first := int(resentment.get("current_value"))
	if after_first != before + 2:
		_fail("Risky side passage did not add resentment once: before=%d after=%d" % [before, after_first])
		return
	player.global_position = Vector3(-11.7, 1.34, -20.5)
	for _i in range(6):
		await physics_frame
	var after_second := int(resentment.get("current_value"))
	if after_second != after_first:
		_fail("Risky side passage fired more than once: first=%d second=%d" % [after_first, after_second])
		return

func _assert_extraction_zone_inside_van(main: Node, player: Node3D) -> void:
	var zone := main.find_child("VanInteriorReturnZone", true, false) as Area3D
	var floor := main.find_child("BongoInteriorFloor", true, false) as Node3D
	if zone == null or floor == null:
		_fail("Extraction zone or van floor missing")
		return
	var floor_size := _box_shape_size(floor)
	var zone_delta := zone.global_position - floor.global_position
	if abs(zone_delta.x) > floor_size.x * 0.5 or abs(zone_delta.z) > floor_size.z * 0.5:
		_fail("Extraction zone is not inside van footprint")
		return
	if _box_shape_size(zone) == Vector3.ZERO:
		_fail("Extraction zone has no box collision")
		return
	var inventory: Variant = player.get("inventory")
	player.call("try_collect_artifact", ArtifactDefinition.new("zone item", 70, 1.0, 0, [], 1))
	player.global_position = zone.global_position
	player.set("velocity", Vector3.ZERO)
	for _i in range(6):
		await physics_frame
	var quota: Variant = main.get("quota")
	if int(quota.get("recovered_value")) != 0:
		_fail("Van extraction zone should not immediately finalize held value")
		return
	if int(main.get("pending_recovered_value")) != 70:
		_fail("Van extraction zone did not store held value as pending cargo")
		return
	if int(inventory.call("total_value")) != 0:
		_fail("Van extraction zone did not clear inventory")
		return
	if str(main.get("current_map_id")) == "bongo_travel":
		_fail("Loading cargo should not start map travel by itself")
		return
	var return_button := main.find_child("BongoDepartureButton", true, false)
	if return_button == null or not return_button.has_method("interact"):
		_fail("Van has no interactive return button")
		return
	return_button.interact(player)
	for _i in range(90):
		if str(main.get("current_map_id")) != "bongo_travel":
			break
		await physics_frame
	if str(main.get("current_map_id")) != "bongo_hub":
		_fail("Return button did not move player back to the bongo hub before settlement")
		return
	var settlement_selector := main.find_child("BongoSettlementMapSelector", true, false)
	if settlement_selector == null or not settlement_selector.has_method("interact"):
		_fail("Van has no interactive settlement map selector")
		return
	settlement_selector.interact(player)
	for _i in range(90):
		if str(main.get("current_map_id")) != "bongo_travel":
			break
		await physics_frame
	if str(main.get("current_map_id")) != "settlement_office":
		_fail("Settlement selector did not move player to settlement map")
		return
	var settlement := main.find_child("BongoSettlementStation", true, false)
	if settlement == null or not settlement.has_method("interact"):
		_fail("Settlement map has no interactive settlement station")
		return
	settlement.interact(player)
	await process_frame
	if int(quota.get("recovered_value")) != 70:
		_fail("Settlement station did not finalize pending cargo value")
		return

func _assert_inventory_drop_cycle(main: Node, player: Node3D) -> void:
	var inventory: Variant = player.get("inventory")
	var small_item := ArtifactDefinition.new("smoke item", 50, 1.0, 0, [], 1)
	var accepted: bool = bool(player.call("try_collect_artifact", small_item))
	if not accepted:
		_fail("Player could not collect smoke item")
		return
	if int(inventory.call("total_value")) != 50:
		_fail("Inventory did not count collected smoke item")
		return
	var world_artifacts_before: int = _count_direct_artifacts(main)
	var dropped: bool = bool(player.call("drop_current_artifact"))
	if not dropped:
		_fail("Player could not drop held smoke item")
		return
	if int(inventory.call("total_value")) != 0:
		_fail("Dropped item still counted in inventory")
		return
	if _count_direct_artifacts(main) <= world_artifacts_before:
		_fail("Dropping did not create a world artifact")
		return

func _box_shape_size(node: Node) -> Vector3:
	var collision := node.find_child("CollisionShape3D", true, false) as CollisionShape3D
	if collision == null:
		return Vector3.ZERO
	var shape := collision.shape as BoxShape3D
	if shape == null:
		return Vector3.ZERO
	return shape.size

func _box_shape_disabled(node: Node) -> bool:
	var collision := node.find_child("CollisionShape3D", true, false) as CollisionShape3D
	if collision == null:
		return true
	return collision.disabled

func _cylinder_shape_height(node: Node) -> float:
	var collision := node.find_child("CollisionShape3D", true, false) as CollisionShape3D
	if collision == null:
		return 0.0
	var shape := collision.shape as CylinderShape3D
	if shape == null:
		return 0.0
	return shape.height

func _count_direct_artifacts(parent: Node) -> int:
	var count := 0
	for child in parent.get_children():
		if child.has_method("definition"):
			count += 1
	return count

func _fail(message: String) -> void:
	Input.action_release("move_forward")
	push_error(message)
	quit(1)
