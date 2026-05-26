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

	_assert_primary_route_clearance(main)
	_assert_approach_reads_as_forest(main)
	_assert_main_house_door_seals_front_gap(main)
	_assert_rear_route_to_shrine_is_open(main)
	_assert_shrine_artifacts_sit_on_altar(main)
	await _assert_shrine_pickup_delays_visible_threat(main)

	if _failed:
		quit(1)
		return
	print("ESTATE_QA_REGRESSIONS: route clearance, forest density, door seal, shrine route, artifact height, and shrine threat grace verified")
	quit(0)

func _travel_to_estate(main: Node) -> void:
	if main.has_method("travel_to_retrieval_map"):
		main.call("travel_to_retrieval_map", "jongga_estate")
	for _i in range(90):
		if str(main.get("current_map_id")) == "jongga_estate":
			await physics_frame
			return
		await physics_frame
	_fail("Could not travel to the estate map")

func _assert_primary_route_clearance(main: Node) -> void:
	for z in [218.0, 171.0, 116.0, 61.0]:
		var blocker := _first_blocking_body(main, Vector3(0.0, 1.15, z), Vector3(1.1, 1.45, 1.1))
		if blocker != null:
			_fail("Primary forest route has a chest-high blocker at z=%s: %s" % [z, blocker.name])
			return

func _assert_approach_reads_as_forest(main: Node) -> void:
	var trunk_count := 0
	var brush_count := 0
	var canopy_count := 0
	for node in main.find_children("*", "StaticBody3D", true, false):
		var label := str(node.name)
		if label.begins_with("ApproachForestTree") and not label.contains("Branch") and not label.contains("Canopy") and not label.contains("Brush"):
			trunk_count += 1
		if label.begins_with("ApproachBrushCluster"):
			brush_count += 1
		if label.begins_with("DeepForestCanopy"):
			canopy_count += 1
	if trunk_count < 64:
		_fail("Approach forest still reads like roadside trees; trunk_count=%d" % trunk_count)
		return
	if brush_count < 34:
		_fail("Approach forest lacks dense low brush; brush_count=%d" % brush_count)
		return
	if canopy_count < 10:
		_fail("Approach forest lacks overhead canopy mass; canopy_count=%d" % canopy_count)
		return

func _assert_main_house_door_seals_front_gap(main: Node) -> void:
	var door := main.find_child("MainHouseSlidingDoor", true, false) as Node3D
	if door == null:
		_fail("Missing MainHouseSlidingDoor")
		return
	var size := _box_shape_size(door)
	if size.x < 9.5:
		_fail("Main house front door is too narrow and leaves pass-through side gaps: %s" % size)
		return

func _assert_rear_route_to_shrine_is_open(main: Node) -> void:
	for z in [-112.8, -115.2, -119.2]:
		var blocker := _first_blocking_body(main, Vector3(-4.0, 1.15, z), Vector3(0.9, 1.55, 0.9))
		if blocker != null:
			_fail("Rear route from anchae to shrine is blocked at z=%s by %s" % [z, blocker.name])
			return

func _assert_shrine_artifacts_sit_on_altar(main: Node) -> void:
	for display_name in ["위패", "제기"]:
		for artifact in _artifacts_by_name(main, display_name):
			if artifact.global_position.y < 1.65:
				_fail("%s is embedded too low for the shrine altar: y=%s" % [display_name, artifact.global_position.y])
				return

func _assert_shrine_pickup_delays_visible_threat(main: Node) -> void:
	var player := main.get("player") as Node3D
	if player == null:
		_fail("Missing player")
		return
	player.global_position = Vector3(0.0, 1.34, -136.0)
	main.call("_on_artifact_picked_up", ArtifactDefinition.new("위패", 760, 2.0, 12, ["shrine_item"], 2))
	for _i in range(12):
		await physics_frame
	if _visible_threat_count(main) > 0:
		_fail("Shrine artifact pickup spawned a visible ghost immediately")
		return
	for _i in range(540):
		await physics_frame
	if _visible_threat_count(main) <= 0:
		_fail("Shrine threat grace never released the delayed apparition")
		return

func _first_blocking_body(main: Node, center: Vector3, size: Vector3) -> Node:
	var shape := BoxShape3D.new()
	shape.size = size
	var query := PhysicsShapeQueryParameters3D.new()
	query.shape = shape
	query.transform = Transform3D(Basis(), center)
	query.collide_with_areas = false
	query.collide_with_bodies = true
	for result in main.get_viewport().world_3d.direct_space_state.intersect_shape(query, 16):
		var collider := result.get("collider") as Node
		if collider == null:
			continue
		var label := str(collider.name)
		if label.contains("Floor") or label.contains("Ground") or label.contains("Road"):
			continue
		return collider
	return null

func _artifacts_by_name(main: Node, display_name: String) -> Array[Node3D]:
	var result: Array[Node3D] = []
	for node in main.find_children("*", "StaticBody3D", true, false):
		if node.get_script() != null and str(node.get("display_name")) == display_name:
			result.append(node as Node3D)
	return result

func _visible_threat_count(main: Node) -> int:
	var count := 0
	for node in main.get_tree().get_nodes_in_group("threats"):
		var threat := node as Node3D
		if threat != null and threat.visible:
			count += 1
	return count

func _box_shape_size(node: Node) -> Vector3:
	if node == null:
		return Vector3.ZERO
	var collision := node.find_child("CollisionShape3D", true, false) as CollisionShape3D
	if collision != null and collision.shape is BoxShape3D:
		return (collision.shape as BoxShape3D).size
	return Vector3.ZERO

func _fail(message: String) -> void:
	_failed = true
	push_error(message)
