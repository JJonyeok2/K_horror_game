extends SceneTree

const MainScene := preload("res://scenes/Main.tscn")
const ArtifactDefinition := preload("res://scripts/core/artifact_definition.gd")
const BongoVanPlanScript := preload("res://scripts/maps/bongo_van_plan.gd")
const SETTLEMENT_OFFICE_TEST_POSITION := BongoVanPlanScript.SETTLEMENT_OFFICE_PLAYER_POSITION
const TRAVEL_MAP_ID := "bongo_travel"

var _failed := false

func _initialize() -> void:
	var main: Node = MainScene.instantiate()
	root.add_child(main)
	for _i in range(90):
		await physics_frame

	_assert_bongo_quota_monitor(main)
	_assert_estate_world_hidden_in_bongo_hub(main)
	await _assert_bongo_map_selector_starts_retrieval_map(main)
	if _failed:
		quit(1)
		return
	await _assert_bongo_deposit_waits_for_manual_settlement(main)
	if _failed:
		quit(1)
		return
	print("BONGO_MONITOR_SMOKE: in-world quota monitor present inside van")
	quit(0)

func _assert_bongo_quota_monitor(main: Node) -> void:
	var monitor := main.find_child("BongoQuotaMonitor", true, false) as Node3D
	if monitor == null:
		_fail("Missing BongoQuotaMonitor")
		return
	var floor := main.find_child("BongoInteriorFloor", true, false) as Node3D
	if floor == null:
		_fail("Missing BongoInteriorFloor")
		return
	var floor_size := _box_shape_size(floor)
	var local_delta := monitor.global_position - floor.global_position
	if abs(local_delta.x) > floor_size.x * 0.5 or abs(local_delta.z) > floor_size.z * 0.5:
		_fail("BongoQuotaMonitor is outside van footprint: delta=%s floor_size=%s" % [local_delta, floor_size])
		return
	var quota_text := _quota_monitor_text(monitor)
	if quota_text == "":
		_fail("BongoQuotaMonitor has no Label3D or TextMesh text")
		return
	if quota_text.find("최종") == -1 or quota_text.find("미정산") == -1:
		_fail("BongoQuotaMonitor text does not show final and pending values: %s" % quota_text)
		return

func _assert_bongo_map_selector_starts_retrieval_map(main: Node) -> void:
	if str(main.get("current_map_id")) != "bongo_hub":
		_fail("Game should start inside the bongo map-selection hub")
		return
	var player := main.get("player") as Node3D
	var selector := main.find_child("BongoMapSelector", true, false)
	if player == null or selector == null or not selector.has_method("interact"):
		_fail("Missing player or interactive BongoMapSelector")
		return
	selector.interact(player)
	await process_frame
	if str(main.get("current_map_id")) != TRAVEL_MAP_ID:
		_fail("Map selector should start a bongo travel animation before arrival")
		return
	if str(main.get("bongo_travel_destination_id")) != "jongga_estate":
		_fail("Map selector should set jongga estate as travel destination")
		return
	if not bool(main.get("bongo_traveling")):
		_fail("Map selector should mark bongo as traveling")
		return
	if bool(player.get("movement_enabled")):
		_fail("Player movement should pause during bongo travel animation")
		return
	await _wait_for_travel_complete(main)
	if str(main.get("current_map_id")) != "jongga_estate":
		_fail("Map selector should move the run to the selected retrieval map")
		return
	if int(main.get("map_travel_count")) != 1:
		_fail("Map selector should count the retrieval-map travel")
		return
	if player.global_position.distance_to(BongoVanPlanScript.PLAYER_START_POSITION) > 0.2:
		_fail("Selected retrieval map should use the current bongo start point")
		return
	_assert_estate_world_visible_after_arrival(main)
	if not bool(player.get("movement_enabled")):
		_fail("Player should be able to move after selecting a retrieval map")
		return

func _assert_bongo_deposit_waits_for_manual_settlement(main: Node) -> void:
	var player := main.get("player") as Node3D
	var zone := main.find_child("VanInteriorReturnZone", true, false) as Area3D
	if player == null or zone == null:
		_fail("Missing player or van extraction zone")
		return
	player.set("movement_enabled", true)
	player.call("try_collect_artifact", ArtifactDefinition.new("monitor test", 70, 1.0, 0, [], 1))
	player.global_position = zone.global_position
	player.set("velocity", Vector3.ZERO)
	main.call("extract_player_inventory")
	await process_frame
	var quota: Variant = main.get("quota")
	if int(quota.get("recovered_value")) != 0:
		_fail("Depositing in the van should not immediately finalize quota")
		return
	if int(main.get("pending_recovered_value")) != 70:
		_fail("Depositing in the van should create pending cargo value")
		return
	if str(main.get("current_map_id")) != "jongga_estate":
		_fail("Loading cargo should not leave the active retrieval map before pressing the return button")
		return
	if int(main.get("map_travel_count")) != 1:
		_fail("Loading cargo should not count as map travel")
		return
	if main.find_child("StoredCargoItem01", true, false) == null:
		_fail("Deposited item should remain visible in the van before settlement")
		return
	var monitor := main.find_child("BongoQuotaMonitor", true, false)
	var quota_text := _quota_monitor_text(monitor)
	if quota_text.find("미정산") == -1 or quota_text.find("70") == -1:
		_fail("BongoQuotaMonitor did not show pending value after deposit: %s" % quota_text)
		return
	var departure_button := main.find_child("BongoDepartureButton", true, false)
	if departure_button == null or not departure_button.has_method("interact"):
		_fail("Missing interactive BongoDepartureButton")
		return
	departure_button.interact(player)
	await process_frame
	if str(main.get("current_map_id")) != TRAVEL_MAP_ID or str(main.get("bongo_travel_destination_id")) != "bongo_hub":
		_fail("Return button should start travel back to the bongo hub")
		return
	await _wait_for_travel_complete(main)
	if str(main.get("current_map_id")) != "bongo_hub":
		_fail("Return button should finish in the bongo interior hub")
		return
	if int(main.get("map_travel_count")) != 2:
		_fail("Returning to the bongo hub should count as the second map travel")
		return
	if int(main.get("pending_recovered_value")) != 70:
		_fail("Pending cargo should stay loaded after returning to the bongo hub")
		return
	_assert_estate_world_hidden_in_bongo_hub(main)

	var settlement_selector := main.find_child("BongoSettlementMapSelector", true, false)
	if settlement_selector == null or not settlement_selector.has_method("interact"):
		_fail("Missing interactive BongoSettlementMapSelector")
		return
	settlement_selector.interact(player)
	await process_frame
	if str(main.get("current_map_id")) != TRAVEL_MAP_ID or str(main.get("bongo_travel_destination_id")) != "settlement_office":
		_fail("Settlement map selector should start travel to the settlement map")
		return
	await _wait_for_travel_complete(main)
	if str(main.get("current_map_id")) != "settlement_office":
		_fail("Settlement map selector should finish in the settlement map")
		return
	if int(main.get("map_travel_count")) != 3:
		_fail("Settlement map travel should be the third map travel")
		return
	if player.global_position.distance_to(SETTLEMENT_OFFICE_TEST_POSITION) > 0.2:
		_fail("Settlement map should place the player in the settlement office")
		return
	_assert_settlement_office_is_large_enough(main, player)

	var settlement := main.find_child("BongoSettlementStation", true, false)
	if settlement == null or not settlement.has_method("interact"):
		_fail("Missing interactive BongoSettlementStation")
		return
	settlement.interact(player)
	await process_frame
	if int(quota.get("recovered_value")) != 70:
		_fail("Manual settlement did not finalize quota")
		return
	if not bool(player.get("movement_enabled")):
		_fail("Manual settlement should not disable player movement")
		return
	if int(main.get("pending_recovered_value")) != 0:
		_fail("Pending cargo value was not cleared after settlement")
		return
	if str(main.get("current_map_id")) != "settlement_office":
		_fail("Manual settlement should happen inside the settlement map")
		return
	if int(main.get("map_travel_count")) != 3:
		_fail("Manual settlement should not add another map travel after arriving at the office")
		return
	quota_text = _quota_monitor_text(monitor)
	if quota_text.find("최종") == -1 or quota_text.find("70") == -1:
		_fail("BongoQuotaMonitor did not show finalized quota after settlement: %s" % quota_text)
		return
	await _assert_settlement_bongo_returns_to_hub(main, player)

func _assert_settlement_bongo_returns_to_hub(main: Node, player: Node3D) -> void:
	var settlement_bongo_floor := main.find_child("SettlementOfficeBongoInteriorFloor", true, false) as Node3D
	if settlement_bongo_floor == null:
		_fail("Settlement map needs its own parked bongo van")
		return
	if settlement_bongo_floor.global_position.distance_to(BongoVanPlanScript.SETTLEMENT_OFFICE_ORIGIN) > 36.0:
		_fail("Settlement bongo should be placed at the settlement map, not reuse the hub van")
		return
	var settlement_return_button := main.find_child("SettlementOfficeBongoReturnButton", true, false)
	if settlement_return_button == null or not settlement_return_button.has_method("interact"):
		_fail("Settlement bongo needs an interactive return button")
		return
	settlement_return_button.interact(player)
	await process_frame
	if str(main.get("current_map_id")) != TRAVEL_MAP_ID or str(main.get("bongo_travel_destination_id")) != "bongo_hub":
		_fail("Settlement bongo return button should start travel back to the bongo hub")
		return
	await _wait_for_travel_complete(main)
	if str(main.get("current_map_id")) != "bongo_hub":
		_fail("Settlement bongo should return the player to the bongo interior hub")
		return
	if int(main.get("map_travel_count")) != 4:
		_fail("Returning from settlement bongo should count as the fourth map travel")
		return
	if player.global_position.distance_to(BongoVanPlanScript.PLAYER_START_POSITION) > 0.2:
		_fail("Settlement bongo return should place the player back inside the bongo hub")
		return

func _assert_settlement_office_is_large_enough(main: Node, player: Node3D) -> void:
	var floor := main.find_child("SettlementOfficeFloor", true, false) as Node3D
	if floor == null:
		_fail("Settlement map is missing SettlementOfficeFloor")
		return
	var floor_size := _box_shape_size(floor)
	if floor_size.x < 44.0 or floor_size.z < 32.0:
		_fail("Settlement office is too small for the requested quarter-estate scale: %s" % floor_size)
		return
	var local_delta := player.global_position - floor.global_position
	if abs(local_delta.x) > floor_size.x * 0.5 or abs(local_delta.z) > floor_size.z * 0.5:
		_fail("Settlement office spawn is outside the enlarged floor")
		return

func _assert_estate_world_hidden_in_bongo_hub(main: Node) -> void:
	if str(main.get("current_map_id")) != "bongo_hub":
		return
	for label in ["LongForestApproachRoad", "OuterEstateGate", "LargeInnerCourtyard"]:
		var node := main.find_child(label, true, false) as Node3D
		if node == null:
			_fail("Missing estate visibility test node: %s" % label)
			return
		if node.visible:
			_fail("%s should be hidden while the player is only inside the bongo hub" % label)
			return
		if _has_enabled_collision(node):
			_fail("%s collision should be disabled while hidden in the bongo hub" % label)
			return

func _assert_estate_world_visible_after_arrival(main: Node) -> void:
	if str(main.get("current_map_id")) != "jongga_estate":
		return
	for label in ["LongForestApproachRoad", "OuterEstateGate", "LargeInnerCourtyard"]:
		var node := main.find_child(label, true, false) as Node3D
		if node == null:
			_fail("Missing estate arrival visibility test node: %s" % label)
			return
		if not node.visible:
			_fail("%s should become visible after the bongo arrives at the estate" % label)
			return
		if not _has_enabled_collision(node):
			_fail("%s collision should be enabled after arriving at the estate" % label)
			return

func _quota_monitor_text(root: Node) -> String:
	for child in root.get_children():
		if child is Label3D:
			return str((child as Label3D).text)
		if child is MeshInstance3D:
			var mesh_instance := child as MeshInstance3D
			if mesh_instance.mesh is TextMesh:
				return str((mesh_instance.mesh as TextMesh).text)
		var nested := _quota_monitor_text(child)
		if nested != "":
			return nested
	return ""

func _wait_for_travel_complete(main: Node) -> void:
	for _i in range(90):
		if str(main.get("current_map_id")) != TRAVEL_MAP_ID:
			return
		await physics_frame

func _box_shape_size(node: Node) -> Vector3:
	if node == null:
		return Vector3.ZERO
	var collision := node.find_child("CollisionShape3D", true, false) as CollisionShape3D
	if collision == null:
		return Vector3.ZERO
	var shape := collision.shape as BoxShape3D
	if shape == null:
		return Vector3.ZERO
	return shape.size

func _has_enabled_collision(node: Node) -> bool:
	for child in node.find_children("*", "CollisionShape3D", true, false):
		var collision := child as CollisionShape3D
		if collision != null and not collision.disabled:
			return true
	return false

func _fail(message: String) -> void:
	_failed = true
	push_error(message)
