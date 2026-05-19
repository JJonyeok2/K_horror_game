extends SceneTree

const MainScene := preload("res://scenes/Main.tscn")
const BongoVanPlanScript := preload("res://scripts/maps/bongo_van_plan.gd")

var _failed := false

func _initialize() -> void:
	var main: Node = MainScene.instantiate()
	root.add_child(main)
	for _i in range(90):
		await physics_frame

	_assert_forest_approach(main)
	_assert_long_approach_distance(main)
	_assert_gate_bypass_blocked(main)
	_assert_courtyard_density(main)
	_assert_courtyard_compression(main)
	_assert_roofed_buildings(main)
	_assert_interaction_density(main)
	if _failed:
		quit(1)
		return
	print("ESTATE_DENSITY_SMOKE: forest path, dense courtyard, roofed buildings, interactions present")
	quit(0)

func _assert_forest_approach(main: Node) -> void:
	var required := [
		"ApproachForestTreeLeft01",
		"ApproachForestTreeRight01",
		"DeepForestCanopyA",
		"ApproachLowRootStepA",
		"ApproachBrokenStoneStepA",
		"ApproachBrushClusterA",
		"ApproachToriiLikeBeam",
	]
	for label in required:
		_assert_node_with_collision(main, label)
	var low_root := main.find_child("ApproachLowRootStepA", true, false) as Node3D
	var root_size := _box_shape_size(low_root)
	if root_size.y > 0.32:
		_fail("Approach low root step is too tall and likely to snag movement: %s" % root_size.y)
	var tree := main.find_child("ApproachForestTreeLeft01", true, false)
	var tree_height := _cylinder_shape_height(tree)
	if tree_height < 7.0:
		_fail("Approach forest trees are too short for an oppressive canopy: %s" % tree_height)

func _assert_long_approach_distance(main: Node) -> void:
	var gate := main.find_child("OuterEstateGate", true, false) as Node3D
	if gate == null:
		_fail("Missing gate for approach distance check")
		return
	var player_start: Vector3 = BongoVanPlanScript.PLAYER_START_POSITION
	var distance: float = abs(player_start.z - gate.global_position.z)
	if distance < 270.0:
		_fail("Bongo-to-gate approach is too short for a one-minute walk: %s" % distance)

func _assert_gate_bypass_blocked(main: Node) -> void:
	var required := [
		"GateBypassBlockLeft",
		"GateBypassBlockRight",
		"GateSideSeamLeft",
		"GateSideSeamRight",
	]
	for label in required:
		_assert_node_with_collision(main, label)

func _assert_courtyard_density(main: Node) -> void:
	var prefixes := [
		"CourtyardClutterJar",
		"CourtyardLaundryPost",
		"CourtyardCart",
		"CourtyardRubble",
		"CourtyardLowWall",
	]
	var count := 0
	for prefix in prefixes:
		count += _count_nodes_with_prefix(main, prefix)
	if count < 20:
		_fail("Courtyard is too empty, found only %d density nodes" % count)

func _assert_courtyard_compression(main: Node) -> void:
	var required := [
		"CourtyardPartitionWallA",
		"CourtyardPartitionWallB",
		"CourtyardPartitionWallC",
		"CourtyardCanopyDeadTreeA",
		"CourtyardCanopyDeadTreeB",
	]
	for label in required:
		_assert_node_with_collision(main, label)

func _assert_roofed_buildings(main: Node) -> void:
	var required_roofs := [
		"MainHouseRoof",
		"StorehouseShedRoof",
		"DwitganOuthouseRoof",
		"ServantQuartersRoof",
		"CollapsedKitchenRoof",
		"SideShrinePavilionRoof",
	]
	for label in required_roofs:
		_assert_node_with_collision(main, label)

func _assert_interaction_density(main: Node) -> void:
	var required_interactables := [
		"MainHouseSlidingDoor",
		"StorehouseSlidingDoor",
		"ServantQuartersDoor",
		"CollapsedKitchenCabinet",
		"ShrineOfferingBox",
		"ShrineBellPull",
		"CourtyardToolChest",
	]
	for label in required_interactables:
		var node := main.find_child(label, true, false)
		if node == null:
			_fail("Missing interactable: %s" % label)
			return
		if not node.has_method("interaction_label") or not node.has_method("interact"):
			_fail("%s is not interactable" % label)
			return
		var before := bool(node.get("is_active"))
		node.interact(main.get("player"))
		var after := bool(node.get("is_active"))
		if before == after:
			_fail("%s did not change state on interact" % label)
			return
		if label == "MainHouseSlidingDoor" and node.global_position.x < 4.6:
			_fail("Main house sliding door did not clear the doorway after opening")
			return

func _assert_node_with_collision(main: Node, label: String) -> void:
	var node := main.find_child(label, true, false)
	if node == null:
		_fail("Missing node: %s" % label)
		return
	if node.find_child("CollisionShape3D", true, false) == null:
		_fail("%s has no collision" % label)

func _count_nodes_with_prefix(root: Node, prefix: String) -> int:
	var count := 0
	for child in root.find_children("%s*" % prefix, "", true, false):
		count += 1
	return count

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

func _cylinder_shape_height(node: Node) -> float:
	if node == null:
		return 0.0
	var collision := node.find_child("CollisionShape3D", true, false) as CollisionShape3D
	if collision == null:
		return 0.0
	var shape := collision.shape as CylinderShape3D
	if shape == null:
		return 0.0
	return shape.height

func _fail(message: String) -> void:
	_failed = true
	push_error(message)
