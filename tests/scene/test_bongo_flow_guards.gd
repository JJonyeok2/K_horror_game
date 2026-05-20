extends SceneTree

const MainScene := preload("res://scenes/Main.tscn")
const ArtifactDefinition := preload("res://scripts/core/artifact_definition.gd")

const TRAVEL_MAP_ID := "bongo_travel"

var _failed := false

func _initialize() -> void:
	var main: Node = MainScene.instantiate()
	root.add_child(main)
	for _i in range(90):
		await physics_frame

	await _assert_invalid_hub_actions_do_not_mutate_run(main)
	if _failed:
		quit(1)
		return
	await _assert_retrieval_cargo_must_return_to_hub_before_settlement(main)
	if _failed:
		quit(1)
		return
	await _assert_settlement_is_idempotent(main)
	if _failed:
		quit(1)
		return
	print("BONGO_FLOW_GUARDS: invalid map and cargo actions are safely ignored")
	quit(0)

func _assert_invalid_hub_actions_do_not_mutate_run(main: Node) -> void:
	var player := main.get("player") as Node
	if player == null:
		_fail("Main did not expose player")
		return
	player.call("try_collect_artifact", ArtifactDefinition.new("hub item", 25, 0.5, 0, [], 1))
	var extracted: bool = main.call("extract_player_inventory") == true
	if extracted:
		_fail("Extracting cargo should fail while still in the bongo hub")
		return
	if int(main.get("pending_recovered_value")) != 0:
		_fail("Hub extraction should not create pending cargo")
		return
	if player.get("inventory").items.size() != 1:
		_fail("Hub extraction should leave the player's carried item alone")
		return
	var settlement_started: bool = main.call("travel_to_settlement_map") == true
	if settlement_started:
		_fail("Settlement travel should not start without pending cargo")
		return
	if str(main.get("current_map_id")) != "bongo_hub":
		_fail("Invalid settlement travel mutated current_map_id: %s" % str(main.get("current_map_id")))
		return
	player.get("inventory").clear()
	player.call("refresh_held_item_views")

func _assert_retrieval_cargo_must_return_to_hub_before_settlement(main: Node) -> void:
	var player := main.get("player") as Node3D
	if player == null:
		_fail("Main did not expose player")
		return
	var retrieval_started: bool = main.call("travel_to_retrieval_map", "jongga_estate") == true
	if not retrieval_started:
		_fail("Retrieval travel should start from the bongo hub")
		return
	await _wait_for_travel_complete(main)
	if str(main.get("current_map_id")) != "jongga_estate":
		_fail("Expected jongga_estate after retrieval travel")
		return
	player.call("try_collect_artifact", ArtifactDefinition.new("estate item", 70, 1.0, 0, [], 1))
	var extracted: bool = main.call("extract_player_inventory") == true
	if not extracted:
		_fail("Extraction should succeed in the estate after carrying an item")
		return
	if int(main.get("pending_recovered_value")) != 70:
		_fail("Estate extraction should create pending cargo")
		return
	var settlement_started: bool = main.call("travel_to_settlement_map") == true
	if settlement_started:
		_fail("Settlement travel should require returning to the bongo hub first")
		return
	if str(main.get("current_map_id")) != "jongga_estate":
		_fail("Invalid settlement travel from estate changed map: %s" % str(main.get("current_map_id")))
		return
	var returned: bool = main.call("return_to_bongo_hub") == true
	if not returned:
		_fail("Return to hub should start from the estate")
		return
	await _wait_for_travel_complete(main)
	if str(main.get("current_map_id")) != "bongo_hub":
		_fail("Expected bongo_hub after returning from estate")
		return
	settlement_started = main.call("travel_to_settlement_map") == true
	if not settlement_started:
		_fail("Settlement travel should start from hub when pending cargo exists")
		return
	await _wait_for_travel_complete(main)
	if str(main.get("current_map_id")) != "settlement_office":
		_fail("Expected settlement_office after settlement travel")
		return

func _assert_settlement_is_idempotent(main: Node) -> void:
	var settled: bool = main.call("settle_stored_cargo") == true
	if not settled:
		_fail("First settlement should finalize pending cargo")
		return
	if int(main.get("pending_recovered_value")) != 0:
		_fail("Settlement should clear pending cargo")
		return
	var quota: Variant = main.get("quota")
	if int(quota.get("recovered_value")) != 70:
		_fail("Settlement should add pending cargo to quota")
		return
	settled = main.call("settle_stored_cargo") == true
	if settled:
		_fail("Second settlement with no cargo should safely return false")
		return
	if int(quota.get("recovered_value")) != 70:
		_fail("Second settlement should not double count quota")
		return

func _wait_for_travel_complete(main: Node) -> void:
	for _i in range(90):
		if str(main.get("current_map_id")) != TRAVEL_MAP_ID:
			return
		await physics_frame

func _fail(message: String) -> void:
	_failed = true
	push_error(message)
