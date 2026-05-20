extends SceneTree

const MainScene := preload("res://scenes/Main.tscn")

const CLEARANCE := 0.015
const SEAM_TOLERANCE := 0.02

var _failed := false

func _initialize() -> void:
	var main: Node = MainScene.instantiate()
	root.add_child(main)
	for _i in range(90):
		await physics_frame
	await _travel_to_estate(main)

	_assert_gate_wall_seams_are_closed(main)
	_assert_gate_side_passages_are_blocked(main)
	_assert_route_floor_surfaces_are_separated(main)
	_assert_main_house_corner_seams_are_closed(main)
	_assert_thin_front_decals_are_clear_of_backing_walls(main)
	_assert_thin_surfaces_are_double_sided(main)
	_assert_collision_matches_box_meshes(main)
	if _failed:
		quit(1)
		return
	print("ESTATE_MESH_INTEGRITY_SMOKE: snapped seams, decal offsets, and box collisions verified")
	quit(0)

func _travel_to_estate(main: Node) -> void:
	if main.has_method("travel_to_retrieval_map"):
		main.call("travel_to_retrieval_map", "jongga_estate")
	for _i in range(90):
		if str(main.get("current_map_id")) == "jongga_estate":
			await physics_frame
			return
		await physics_frame
	_fail("Mesh integrity test could not travel to the estate map")

func _assert_gate_wall_seams_are_closed(main: Node) -> void:
	var left_wall := _required_box(main, "OuterEstateGateLeftWall")
	var right_wall := _required_box(main, "OuterEstateGateRightWall")
	if left_wall == null or right_wall == null:
		return
	_assert_axis_coverage(left_wall, -15.2, -6.0, "OuterEstateGateLeftWall")
	_assert_axis_coverage(right_wall, 6.0, 15.2, "OuterEstateGateRightWall")

	var left_bounds := _bounds(left_wall)
	var right_bounds := _bounds(right_wall)
	if absf(float(left_bounds["min"].z) - float(right_bounds["min"].z)) > SEAM_TOLERANCE:
		_fail("Gate side walls are not snapped to the same front z plane")
	if absf(float(left_bounds["max"].z) - float(right_bounds["max"].z)) > SEAM_TOLERANCE:
		_fail("Gate side walls are not snapped to the same back z plane")

func _assert_gate_side_passages_are_blocked(main: Node) -> void:
	_assert_blocked_lateral_passage(main, Vector3(-4.7, 1.6, -14.4), Vector3(-16.5, 1.6, -14.4), "left gate-side courtyard opening")
	_assert_blocked_lateral_passage(main, Vector3(4.7, 1.6, -14.4), Vector3(16.5, 1.6, -14.4), "right gate-side courtyard opening")
	_assert_blocked_lateral_passage(main, Vector3(-32.0, 1.35, -24.0), Vector3(-32.0, 1.35, -9.0), "left front courtyard outer opening")
	_assert_blocked_lateral_passage(main, Vector3(32.0, 1.35, -24.0), Vector3(32.0, 1.35, -9.0), "right front courtyard outer opening")

func _assert_blocked_lateral_passage(main: Node, start: Vector3, end: Vector3, label: String) -> void:
	var space_state := main.get_viewport().world_3d.direct_space_state
	var query := PhysicsRayQueryParameters3D.create(start, end)
	query.collide_with_areas = false
	query.collide_with_bodies = true
	var result := space_state.intersect_ray(query)
	if result.is_empty():
		_fail("Unsealed %s lets the player bypass the main gate" % label)

func _assert_main_house_corner_seams_are_closed(main: Node) -> void:
	_assert_wall_reaches_back(main, "MainHouseLeftOuterWall", "MainHouseBackWall")
	_assert_wall_reaches_back(main, "MainHouseRightOuterWall", "MainHouseBackWall")
	_assert_wall_reaches_front(main, "MainHouseLeftOuterWall", "MainHouseFrontWallLeft")
	_assert_wall_reaches_front(main, "MainHouseRightOuterWall", "MainHouseFrontWallRight")
	_assert_wall_reaches_back(main, "MainHouseHiddenLeftWall", "MainHouseHiddenBackWall")
	_assert_wall_reaches_back(main, "MainHouseHiddenRightWall", "MainHouseHiddenBackWall")

func _assert_route_floor_surfaces_are_separated(main: Node) -> void:
	_assert_floor_overlay_clearance(main, "LongForestApproachRoad", "LongApproachRoad")
	_assert_floor_overlay_clearance(main, "LeftSideYard", "SideYardChoke")

func _assert_floor_overlay_clearance(main: Node, lower_label: String, upper_label: String) -> void:
	var lower := _required_box(main, lower_label)
	var upper := _required_box(main, upper_label)
	if lower == null or upper == null:
		return
	var lower_bounds := _bounds(lower)
	var upper_bounds := _bounds(upper)
	if float(upper_bounds["min"].z) > float(lower_bounds["max"].z) or float(upper_bounds["max"].z) < float(lower_bounds["min"].z):
		return
	var surface_delta := float(upper_bounds["max"].y) - float(lower_bounds["max"].y)
	if surface_delta < 0.04:
		_fail("%s sits too close above %s and can z-fight: %s" % [upper_label, lower_label, surface_delta])

func _assert_thin_front_decals_are_clear_of_backing_walls(main: Node) -> void:
	for label in ["MainHousePaperDoorA", "MainHousePaperDoorB", "MainHousePaperDoorC"]:
		_assert_in_front_of_positive_z_face(main, label, "MainHouseFrontWallLeft")
	for label in ["OuterGateTalismanA", "OuterGateTalismanB", "OuterGateGeumjulRope"]:
		_assert_in_front_of_positive_z_face(main, label, "LeftSwingGatePanel")
	for label in ["ServantQuartersPaperPanel", "ServantQuartersHangingCloth"]:
		_assert_in_front_of_positive_z_face(main, label, "ServantQuartersFrontLeft")

	var beam := _required_box(main, "ShrinePaperCharmsBeam")
	if beam == null:
		return
	var beam_bounds := _bounds(beam)
	for charm in main.find_children("ShrinePaperCharmsPaper*", "MeshInstance3D", true, false):
		var charm_bounds := _bounds(charm as Node3D)
		if float(charm_bounds["min"].z) <= float(beam_bounds["max"].z) + CLEARANCE:
			_fail("%s is embedded in ShrinePaperCharmsBeam and can z-fight" % charm.name)

func _assert_thin_surfaces_are_double_sided(main: Node) -> void:
	var labels := [
		"MainHousePaperDoorA",
		"OuterGateTalismanA",
		"ServantQuartersPaperPanel",
		"ShrinePaperCharmsPaper1",
	]
	for label in labels:
		var node := _required_box(main, label)
		if node == null:
			continue
		var material := _first_material(node)
		if material == null:
			_fail("%s has no material to verify culling mode" % label)
			continue
		if material.cull_mode != BaseMaterial3D.CULL_DISABLED:
			_fail("%s should render double-sided to avoid angle-dependent holes" % label)

func _assert_collision_matches_box_meshes(main: Node) -> void:
	for body in main.find_children("*", "StaticBody3D", true, false):
		var collision := body.find_child("CollisionShape3D", true, false) as CollisionShape3D
		var mesh_instance := _first_mesh_instance(body)
		if collision == null or mesh_instance == null:
			continue
		var shape := collision.shape as BoxShape3D
		var box_mesh := mesh_instance.mesh as BoxMesh
		if shape == null or box_mesh == null:
			continue
		var delta := shape.size - box_mesh.size
		if absf(delta.x) > 0.001 or absf(delta.y) > 0.001 or absf(delta.z) > 0.001:
			_fail("%s visual mesh and collision shape are out of sync" % body.name)
			return

func _assert_axis_coverage(node: Node3D, expected_min_x: float, expected_max_x: float, label: String) -> void:
	var bounds := _bounds(node)
	if float(bounds["min"].x) > expected_min_x + SEAM_TOLERANCE:
		_fail("%s leaves a left/right x gap at min edge: %s" % [label, bounds["min"].x])
	if float(bounds["max"].x) < expected_max_x - SEAM_TOLERANCE:
		_fail("%s leaves a left/right x gap at max edge: %s" % [label, bounds["max"].x])

func _assert_wall_reaches_back(main: Node, side_label: String, back_label: String) -> void:
	var side := _required_box(main, side_label)
	var back := _required_box(main, back_label)
	if side == null or back == null:
		return
	var side_bounds := _bounds(side)
	var back_bounds := _bounds(back)
	if float(side_bounds["min"].z) > float(back_bounds["max"].z) + SEAM_TOLERANCE:
		_fail("%s does not reach %s; visible corner gap remains" % [side_label, back_label])

func _assert_wall_reaches_front(main: Node, side_label: String, front_label: String) -> void:
	var side := _required_box(main, side_label)
	var front := _required_box(main, front_label)
	if side == null or front == null:
		return
	var side_bounds := _bounds(side)
	var front_bounds := _bounds(front)
	if float(side_bounds["max"].z) < float(front_bounds["min"].z) - SEAM_TOLERANCE:
		_fail("%s does not reach %s; visible corner gap remains" % [side_label, front_label])

func _assert_in_front_of_positive_z_face(main: Node, decor_label: String, backing_label: String) -> void:
	var decor := _required_box(main, decor_label)
	var backing := _required_box(main, backing_label)
	if decor == null or backing == null:
		return
	var decor_bounds := _bounds(decor)
	var backing_bounds := _bounds(backing)
	if float(decor_bounds["min"].z) <= float(backing_bounds["max"].z) + CLEARANCE:
		_fail("%s is embedded in %s and can z-fight" % [decor_label, backing_label])

func _required_box(main: Node, label: String) -> Node3D:
	var node := main.find_child(label, true, false) as Node3D
	if node == null:
		_fail("Missing mesh integrity node: %s" % label)
		return null
	if _box_size(node) == Vector3.ZERO:
		_fail("%s has no box mesh or box collision to inspect" % label)
		return null
	return node

func _bounds(node: Node3D) -> Dictionary:
	var size := _box_size(node)
	return {
		"min": node.global_position - size * 0.5,
		"max": node.global_position + size * 0.5,
	}

func _box_size(node: Node) -> Vector3:
	var collision := node.find_child("CollisionShape3D", true, false) as CollisionShape3D
	if collision != null and collision.shape is BoxShape3D:
		return (collision.shape as BoxShape3D).size
	if node is MeshInstance3D:
		var mesh := (node as MeshInstance3D).mesh as BoxMesh
		if mesh != null:
			return mesh.size
	return Vector3.ZERO

func _first_mesh_instance(node: Node) -> MeshInstance3D:
	for child in node.get_children():
		if child is MeshInstance3D:
			return child as MeshInstance3D
		var nested := _first_mesh_instance(child)
		if nested != null:
			return nested
	return null

func _first_material(node: Node) -> StandardMaterial3D:
	var mesh_instance := node as MeshInstance3D
	if mesh_instance == null:
		mesh_instance = _first_mesh_instance(node)
	if mesh_instance == null:
		return null
	var primitive := mesh_instance.mesh as PrimitiveMesh
	if primitive == null:
		return null
	return primitive.material as StandardMaterial3D

func _fail(message: String) -> void:
	_failed = true
	push_error(message)
