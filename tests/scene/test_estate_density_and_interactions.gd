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
	_assert_forest_canopy_does_not_cover_center_path(main)
	_assert_long_approach_distance(main)
	_assert_approach_side_wall_gap_closed(main)
	_assert_gate_bypass_blocked(main)
	_assert_single_gate_readability(main)
	_assert_courtyard_density(main)
	_assert_courtyard_compression(main)
	_assert_courtyard_route_is_obscured(main)
	_assert_visible_route_branches_are_sealed(main)
	_assert_return_route_is_not_straight(main)
	_assert_courtyard_salgut_installation(main)
	_assert_roofed_buildings(main)
	_assert_larger_main_house_and_hidden_interior(main)
	_assert_courtyard_building_silhouettes(main)
	_assert_korean_ghost_haunts(main)
	_assert_taller_walls_and_posts(main)
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

func _assert_approach_side_wall_gap_closed(main: Node) -> void:
	_assert_approach_wall_seal(main, "ApproachWallLeftMiddleSeal", true)
	_assert_approach_wall_seal(main, "ApproachWallRightMiddleSeal", false)

func _assert_approach_wall_seal(main: Node, label: String, should_be_left: bool) -> void:
	var seal := main.find_child(label, true, false) as Node3D
	if seal == null:
		_fail("%s is missing before the main gate" % label)
		return
	_assert_node_with_collision(main, label)
	var size := _box_shape_size(seal)
	var min_z := seal.global_position.z - size.z * 0.5
	var max_z := seal.global_position.z + size.z * 0.5
	if min_z > -5.0 or max_z < 7.0:
		_fail("%s does not cover the pre-gate gap: %s..%s" % [label, min_z, max_z])
		return
	if should_be_left and seal.global_position.x > -4.5:
		_fail("%s should stay on the left wall, not block the center route" % label)
		return
	if not should_be_left and seal.global_position.x < 4.5:
		_fail("%s should stay on the right wall, not block the center route" % label)
		return

func _assert_gate_bypass_blocked(main: Node) -> void:
	var required := [
		"GateBypassBlockLeft",
		"GateBypassBlockRight",
		"GateSideSeamLeft",
		"GateSideSeamRight",
	]
	for label in required:
		_assert_node_with_collision(main, label)

func _assert_single_gate_readability(main: Node) -> void:
	var required := [
		"SingleGateLeftReturnWall",
		"SingleGateRightReturnWall",
		"SidePassageBoardedGate",
		"SidePassageBrushScreenA",
		"SidePassageBrushScreenB",
		"OuterGateTalismanA",
		"OuterGateTalismanB",
		"OuterGateGeumjulRope",
	]
	for label in required:
		_assert_node_with_collision(main, label)
	var gate := main.find_child("OuterEstateGate", true, false) as Node3D
	var board := main.find_child("SidePassageBoardedGate", true, false) as Node3D
	if gate == null or board == null:
		return
	if board.global_position.z > gate.global_position.z + 2.5:
		_fail("Side passage blocker is too far behind the gate to read as closed")

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

func _assert_courtyard_route_is_obscured(main: Node) -> void:
	var required := [
		"CourtyardSightlineScreenA",
		"CourtyardSightlineScreenB",
		"CourtyardPathBaffleA",
		"CourtyardPathBaffleB",
	]
	for label in required:
		_assert_node_with_collision(main, label)
		var node := main.find_child(label, true, false)
		var size := _box_shape_size(node)
		if size.y < 3.0:
			_fail("%s is too low to break the visible straight courtyard route" % label)

func _assert_return_route_is_not_straight(main: Node) -> void:
	var required := [
		"ReturnRouteBaffleLeft01",
		"ReturnRouteBaffleRight01",
		"ReturnRouteBaffleLeft02",
		"ReturnRouteHillRamp01",
		"ReturnRouteHillRamp02",
		"ReturnRouteHillRamp03",
	]
	for label in required:
		_assert_node_with_collision(main, label)
	var left := main.find_child("ReturnRouteBaffleLeft01", true, false) as Node3D
	var right := main.find_child("ReturnRouteBaffleRight01", true, false) as Node3D
	if left == null or right == null:
		return
	if left.global_position.x >= 0.0 or right.global_position.x <= 0.0:
		_fail("Return route baffles should alternate left/right to break the straight path")

func _assert_visible_route_branches_are_sealed(main: Node) -> void:
	var required := [
		"RouteBranchSealLeftLoop",
		"RouteBranchSealRightLoop",
		"StorehouseBrokenGapSeal",
		"BackyardBrokenGapSeal",
	]
	for label in required:
		_assert_node_with_collision(main, label)

func _assert_courtyard_salgut_installation(main: Node) -> void:
	var required := [
		"CourtyardSalgutPoleNorth",
		"CourtyardSalgutPoleSouth",
		"CourtyardSalgutRopeA",
		"CourtyardSalgutRopeB",
		"CourtyardSalgutClothA",
		"CourtyardSalgutAltar",
	]
	for label in required:
		_assert_node_with_collision(main, label)
	var cloth := main.find_child("CourtyardSalgutClothA", true, false) as Node3D
	if cloth != null and cloth.global_position.y < 2.2:
		_fail("Courtyard salgut cloth should hang overhead, not sit on the floor")

func _assert_roofed_buildings(main: Node) -> void:
	var required_roofs := [
		"MainHouseRoof",
		"StorehouseShedRoof",
		"DwitganOuthouseRoof",
		"ServantQuartersRoof",
		"CollapsedKitchenRoof",
		"SideShrinePavilionRoof",
		"FrontSarangchaeRoof",
		"FrontStorehouseAnnexRoof",
	]
	for label in required_roofs:
		_assert_node_with_collision(main, label)

func _assert_forest_canopy_does_not_cover_center_path(main: Node) -> void:
	for node in main.find_children("DeepForestCanopy*", "StaticBody3D", true, false):
		var size := _box_shape_size(node)
		var body := node as Node3D
		if body == null:
			continue
		var covers_center: bool = abs(body.global_position.x) < 3.2 and size.x > 6.0
		if covers_center:
			_fail("%s covers the center approach like a ceiling" % node.name)
			return

func _assert_larger_main_house_and_hidden_interior(main: Node) -> void:
	var roof := main.find_child("MainHouseRoof", true, false)
	var roof_size := _box_shape_size(roof)
	if roof_size.x < 44.0 or roof_size.z < 38.0:
		_fail("Main house should read as larger than the current courtyard-facing block: %s" % roof_size)
		return
	var required := [
		"MainHouseHiddenFrontChamber",
		"MainHouseHiddenMiddleChamber",
		"MainHouseHiddenDeepChamber",
		"MainHouseHiddenFalseWall",
		"MainHouseHiddenBackWall",
	]
	for label in required:
		_assert_node_with_collision(main, label)
	var front := main.find_child("MainHouseHiddenFrontChamber", true, false) as Node3D
	var middle := main.find_child("MainHouseHiddenMiddleChamber", true, false) as Node3D
	var deep := main.find_child("MainHouseHiddenDeepChamber", true, false) as Node3D
	if front == null or middle == null or deep == null:
		return
	var front_size := _box_shape_size(front)
	var middle_size := _box_shape_size(middle)
	var deep_size := _box_shape_size(deep)
	if not (front.global_position.z > middle.global_position.z and middle.global_position.z > deep.global_position.z):
		_fail("Hidden interior chambers should pull deeper into the house")
		return
	if not (front_size.x > middle_size.x and middle_size.x > deep_size.x):
		_fail("Hidden interior chambers should narrow as the player goes deeper")
		return

func _assert_courtyard_building_silhouettes(main: Node) -> void:
	var required := [
		"SarangchaeSilhouette",
		"SarangchaeSilhouetteRoof",
		"HaengrangchaeSilhouette",
		"HaengrangchaeSilhouetteRoof",
		"SmallBarnSilhouette",
		"SmallBarnSilhouetteRoof",
	]
	for label in required:
		_assert_node_with_collision(main, label)

func _assert_korean_ghost_haunts(main: Node) -> void:
	var required := [
		"GhostHauntSangbok",
		"GhostHauntDalgyalGwisin",
		"GhostHauntDokkaebi",
		"GhostHauntEoduksini",
		"GhostHauntWellSpirit",
	]
	for label in required:
		var node := main.find_child(label, true, false) as Node3D
		if node == null:
			_fail("Missing Korean ghost haunt marker: %s" % label)
			return
		if node.get_child_count() <= 0:
			_fail("%s has no visible haunt marker children" % label)
			return
	var gate := main.find_child("OuterEstateGate", true, false) as Node3D
	var dokkaebi := main.find_child("GhostHauntDokkaebi", true, false) as Node3D
	if gate != null and dokkaebi != null and dokkaebi.global_position.z <= gate.global_position.z:
		_fail("Dokkaebi haunt should sit outside the main gate")

func _assert_taller_walls_and_posts(main: Node) -> void:
	var minimum_heights := {
		"ApproachWallLeftMiddleSeal": 5.2,
		"ApproachWallRightMiddleSeal": 5.2,
		"GateLeftPost": 5.2,
		"GateRightPost": 5.2,
		"CourtyardOuterWallLeft": 5.2,
		"CourtyardOuterWallRight": 5.2,
		"MainHouseFrontWallLeft": 5.2,
		"MainHouseFrontWallRight": 5.2,
		"FrontSarangchaeBackWall": 5.0,
		"FrontStorehouseAnnexBackWall": 5.0,
		"ServantQuartersBackWall": 5.0,
		"CollapsedKitchenBackWall": 5.0,
	}
	for label: String in minimum_heights.keys():
		var node := main.find_child(label, true, false)
		if node == null:
			_fail("Missing tall-wall candidate: %s" % label)
			return
		var size := _box_shape_size(node)
		var min_height: float = minimum_heights[label]
		if size.y < min_height:
			_fail("%s is too low: %s" % [label, size.y])
			return
	var minimum_roof_tops := {
		"MainHouseRoof": 5.4,
		"FrontSarangchaeRoof": 5.3,
		"FrontStorehouseAnnexRoof": 5.3,
		"ServantQuartersRoof": 5.3,
		"CollapsedKitchenRoof": 5.3,
		"DwitganOuthouseRoof": 5.3,
	}
	for label: String in minimum_roof_tops.keys():
		var node := main.find_child(label, true, false) as Node3D
		if node == null:
			_fail("Missing taller roof candidate: %s" % label)
			return
		var size := _box_shape_size(node)
		var top_y := node.global_position.y + size.y * 0.5
		if top_y < minimum_roof_tops[label]:
			_fail("%s roof top is too low: %s" % [label, top_y])
			return
	var front_wall := main.find_child("MainHouseFrontWallLeft", true, false)
	var material := _first_standard_material(front_wall)
	if material != null and material.albedo_texture == null and material.albedo_color.r > 0.34:
		_fail("Main house wall tone is too bright for the night map: %s" % material.albedo_color)

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

func _first_standard_material(node: Node) -> StandardMaterial3D:
	if node == null:
		return null
	var mesh_instance := _first_mesh_instance(node)
	if mesh_instance == null:
		return null
	var primitive_mesh := mesh_instance.mesh as PrimitiveMesh
	if primitive_mesh == null:
		return null
	return primitive_mesh.material as StandardMaterial3D

func _first_mesh_instance(node: Node) -> MeshInstance3D:
	for child in node.get_children():
		if child is MeshInstance3D:
			return child as MeshInstance3D
		var nested := _first_mesh_instance(child)
		if nested != null:
			return nested
	return null

func _fail(message: String) -> void:
	_failed = true
	push_error(message)
