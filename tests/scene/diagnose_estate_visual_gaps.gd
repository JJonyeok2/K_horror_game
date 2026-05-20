extends SceneTree

const MainScene := preload("res://scenes/Main.tscn")

const SAMPLE_Y := 1.35
const EDGE_TOLERANCE := 0.08
const GAP_TOLERANCE := 0.18
const SCREENSHOT_DIR := "/private/tmp/k_horror_estate_diag"

var _failures: Array[String] = []
var _warnings: Array[String] = []

func _initialize() -> void:
	root.size = Vector2i(1280, 720)
	var main: Node = MainScene.instantiate()
	root.add_child(main)
	await _wait_process_frames(2)
	await _wait_physics_frames(90)
	await _travel_to_estate(main)
	await _wait_physics_frames(12)

	_print_header(main)
	_scan_wall_boundary_coverage(main)
	_scan_escape_rays(main)
	_scan_floor_support(main)
	_scan_box_collision_sync(main)
	_scan_material_culling(main)
	_scan_floor_surface_z_fighting(main)
	await _capture_estate_viewpoints(main)

	print("")
	print("=== ESTATE VISUAL GAP DIAGNOSIS SUMMARY ===")
	print("failures=%d warnings=%d" % [_failures.size(), _warnings.size()])
	for message in _failures:
		print("FAIL: %s" % message)
	for message in _warnings:
		print("WARN: %s" % message)
	quit(1 if not _failures.is_empty() else 0)

func _travel_to_estate(main: Node) -> void:
	if main.has_method("travel_to_retrieval_map"):
		main.call("travel_to_retrieval_map", "jongga_estate")
	for _i in range(120):
		if str(main.get("current_map_id")) == "jongga_estate":
			await physics_frame
			return
		await physics_frame
	_fail("could not travel to jongga_estate for diagnostics")

func _print_header(main: Node) -> void:
	print("=== ESTATE VISUAL GAP DIAGNOSIS ===")
	print("current_map_id=%s low_spec_mode=%s" % [
		str(main.get("current_map_id")),
		str(ProjectSettings.get_setting("k_horror/low_spec_mode", false)),
	])

func _scan_wall_boundary_coverage(main: Node) -> void:
	print("")
	print("=== Wall Boundary Coverage ===")
	var checks: Array[Dictionary] = [
		{
			"label": "gate facade, only the two swing-door panels may open",
			"axis": "z",
			"line": -12.0,
			"ranges": [Vector2(-15.25, -4.55), Vector2(4.55, 15.25)],
		},
		{
			"label": "courtyard front wall left/right of the single main gate",
			"axis": "z",
			"line": -16.70,
			"ranges": [Vector2(-36.90, -7.00), Vector2(7.00, 36.90)],
		},
		{
			"label": "main house front wall, excluding the wide central doorway",
			"axis": "z",
			"line": -73.40,
			"ranges": [Vector2(-22.35, -5.40), Vector2(5.40, 22.35)],
		},
		{
			"label": "main house rear wall",
			"axis": "z",
			"line": -113.0,
			"ranges": [Vector2(-22.50, 22.50)],
		},
		{
			"label": "courtyard left outer wall",
			"axis": "x",
			"line": -36.50,
			"ranges": [Vector2(-82.0, -12.0)],
		},
		{
			"label": "courtyard right outer wall",
			"axis": "x",
			"line": 36.50,
			"ranges": [Vector2(-88.0, -12.0)],
		},
	]
	for check in checks:
		_scan_boundary(main, check)

func _scan_boundary(main: Node, check: Dictionary) -> void:
	var label := str(check["label"])
	var axis := str(check["axis"])
	var line := float(check["line"])
	var expected_ranges: Array = check["ranges"]
	var intervals: Array[Vector2] = []
	for body in _active_box_bodies(main):
		var bounds := _bounds(body)
		var size := _box_size(body)
		if size.y < 1.45:
			continue
		if float(bounds["min"].y) > SAMPLE_Y or float(bounds["max"].y) < SAMPLE_Y:
			continue
		if axis == "z":
			if float(bounds["min"].z) <= line + EDGE_TOLERANCE and float(bounds["max"].z) >= line - EDGE_TOLERANCE:
				intervals.append(Vector2(float(bounds["min"].x), float(bounds["max"].x)))
		else:
			if float(bounds["min"].x) <= line + EDGE_TOLERANCE and float(bounds["max"].x) >= line - EDGE_TOLERANCE:
				intervals.append(Vector2(float(bounds["min"].z), float(bounds["max"].z)))
	var merged := _merge_intervals(intervals)
	var gaps: Array[Vector2] = []
	for range_value in expected_ranges:
		var expected := range_value as Vector2
		gaps.append_array(_subtract_coverage(expected, merged))
	if gaps.is_empty():
		print("OK %s line=%s %.2f" % [label, axis, line])
		return
	for gap in gaps:
		var message := "%s has visible/collision gap along %s=%.2f from %.2f to %.2f" % [
			label,
			axis,
			line,
			gap.x,
			gap.y,
		]
		_fail(message)

func _scan_escape_rays(main: Node) -> void:
	print("")
	print("=== Player Escape Ray Checks ===")
	var rays := [
		{
			"label": "left front courtyard leak beside the wall",
			"start": Vector3(-32.0, 1.35, -24.0),
			"end": Vector3(-32.0, 1.35, -9.0),
		},
		{
			"label": "right front courtyard leak beside the wall",
			"start": Vector3(32.0, 1.35, -24.0),
			"end": Vector3(32.0, 1.35, -9.0),
		},
		{
			"label": "left gate-side seal near the main gate",
			"start": Vector3(-10.6, 1.35, -18.0),
			"end": Vector3(-10.6, 1.35, -8.0),
		},
		{
			"label": "right gate-side seal near the main gate",
			"start": Vector3(10.6, 1.35, -18.0),
			"end": Vector3(10.6, 1.35, -8.0),
		},
	]
	var space_state := main.get_viewport().world_3d.direct_space_state
	for ray_data in rays:
		var start: Vector3 = ray_data["start"]
		var end: Vector3 = ray_data["end"]
		var query := PhysicsRayQueryParameters3D.create(start, end)
		query.collide_with_areas = false
		query.collide_with_bodies = true
		var result: Dictionary = space_state.intersect_ray(query)
		if result.is_empty():
			_fail("%s is open from %s to %s" % [str(ray_data["label"]), str(start), str(end)])
			continue
		var collider := result.get("collider") as Node
		print("OK %s blocked_by=%s at=%s" % [
			str(ray_data["label"]),
			collider.name if collider != null else "<unknown>",
			str(result.get("position", Vector3.ZERO)),
		])

func _scan_floor_support(main: Node) -> void:
	print("")
	print("=== Floor Support Samples ===")
	var samples := [
		Vector3(0.0, 2.0, 26.0),
		Vector3(0.0, 2.0, 8.0),
		Vector3(0.0, 2.0, -12.0),
		Vector3(0.0, 2.0, -40.0),
		Vector3(-25.0, 2.0, -92.0),
		Vector3(25.0, 2.0, -96.0),
		Vector3(0.0, 2.0, -93.0),
		Vector3(0.0, 2.0, -136.0),
		Vector3(32.0, 2.0, -24.0),
		Vector3(-32.0, 2.0, -24.0),
	]
	var space_state := main.get_viewport().world_3d.direct_space_state
	for sample in samples:
		var query := PhysicsRayQueryParameters3D.create(sample, sample + Vector3.DOWN * 6.0)
		query.collide_with_areas = false
		query.collide_with_bodies = true
		var result: Dictionary = space_state.intersect_ray(query)
		if result.is_empty():
			_fail("no floor collision below sample %s" % str(sample))
			continue
		var collider := result.get("collider") as Node
		print("OK floor sample %s hit=%s y=%.2f" % [
			str(sample),
			collider.name if collider != null else "<unknown>",
			float((result.get("position", Vector3.ZERO) as Vector3).y),
		])

func _scan_box_collision_sync(main: Node) -> void:
	print("")
	print("=== Mesh / Collision Box Sync ===")
	var mismatch_count := 0
	for body in _active_box_bodies(main):
		var mesh_instance := _first_mesh_instance(body)
		if mesh_instance == null:
			continue
		var box_mesh := mesh_instance.mesh as BoxMesh
		if box_mesh == null:
			continue
		var size := _box_size(body)
		var delta := size - box_mesh.size
		if absf(delta.x) > 0.001 or absf(delta.y) > 0.001 or absf(delta.z) > 0.001:
			mismatch_count += 1
			_fail("%s visual mesh and collision differ: collision=%s mesh=%s" % [
				body.name,
				str(size),
				str(box_mesh.size),
			])
	if mismatch_count == 0:
		print("OK all active BoxMesh bodies have matching BoxShape3D sizes")

func _scan_material_culling(main: Node) -> void:
	print("")
	print("=== Material Culling / Normal Risk ===")
	var cull_count := 0
	for mesh_node in main.find_children("*", "MeshInstance3D", true, false):
		var mesh_instance := mesh_node as MeshInstance3D
		if mesh_instance == null or not mesh_instance.visible:
			continue
		var primitive := mesh_instance.mesh as PrimitiveMesh
		if primitive == null:
			continue
		var material := primitive.material as StandardMaterial3D
		if material == null:
			continue
		if material.cull_mode != BaseMaterial3D.CULL_DISABLED:
			cull_count += 1
			_warn("%s uses culling and may disappear from some angles" % mesh_instance.name)
	if cull_count == 0:
		print("OK active primitive materials are double-sided")

func _scan_floor_surface_z_fighting(main: Node) -> void:
	print("")
	print("=== Near-Coplanar Floor Surface Scan ===")
	var floors: Array[Dictionary] = []
	for body in _active_box_bodies(main):
		var label := str(body.name)
		var size := _box_size(body)
		if size.y > 0.38:
			continue
		if not (label.contains("Floor") or label.contains("Ground") or label.contains("Road") or label.contains("Yard") or label.contains("Pad") or label.contains("Stitch")):
			continue
		floors.append({"body": body, "bounds": _bounds(body), "top": float(_bounds(body)["max"].y)})
	for i in range(floors.size()):
		for j in range(i + 1, floors.size()):
			var a: Dictionary = floors[i]
			var b: Dictionary = floors[j]
			if absf(float(a["top"]) - float(b["top"])) > 0.025:
				continue
			var bounds_a: Dictionary = a["bounds"]
			var bounds_b: Dictionary = b["bounds"]
			var overlap_x := minf(float(bounds_a["max"].x), float(bounds_b["max"].x)) - maxf(float(bounds_a["min"].x), float(bounds_b["min"].x))
			var overlap_z := minf(float(bounds_a["max"].z), float(bounds_b["max"].z)) - maxf(float(bounds_a["min"].z), float(bounds_b["min"].z))
			if overlap_x > 0.4 and overlap_z > 0.4:
				var body_a := a["body"] as Node
				var body_b := b["body"] as Node
				_warn("possible floor z-fighting: %s top=%.3f overlaps %s top=%.3f" % [
					body_a.name if body_a != null else "<unknown>",
					float(a["top"]),
					body_b.name if body_b != null else "<unknown>",
					float(b["top"]),
				])

func _capture_estate_viewpoints(main: Node) -> void:
	print("")
	print("=== Diagnostic Screenshots ===")
	if DisplayServer.get_name() == "headless":
		print("SKIP screenshots: headless display driver has no rendered viewport texture")
		return
	var error := DirAccess.make_dir_recursive_absolute(SCREENSHOT_DIR)
	if error != OK:
		_warn("could not create screenshot directory: %s error=%d" % [SCREENSHOT_DIR, error])
		return
	var camera := Camera3D.new()
	camera.name = "DiagnosticEstateCamera"
	camera.fov = 68.0
	main.add_child(camera)
	camera.current = true
	var shots := [
		{"name": "01_gate_front", "position": Vector3(0.0, 3.0, 3.0), "target": Vector3(0.0, 2.0, -13.0)},
		{"name": "02_gate_inner_left_gap", "position": Vector3(-24.0, 3.0, -26.0), "target": Vector3(-35.0, 1.8, -15.0)},
		{"name": "03_gate_inner_right_gap", "position": Vector3(24.0, 3.0, -26.0), "target": Vector3(35.0, 1.8, -15.0)},
		{"name": "04_main_house_front", "position": Vector3(0.0, 3.2, -58.0), "target": Vector3(0.0, 2.2, -74.0)},
		{"name": "05_deep_house", "position": Vector3(0.0, 2.7, -89.0), "target": Vector3(0.0, 1.7, -112.0)},
	]
	for shot_data in shots:
		camera.global_position = shot_data["position"]
		camera.look_at(shot_data["target"], Vector3.UP)
		await _wait_process_frames(8)
		var image := root.get_texture().get_image()
		if image == null or image.is_empty():
			_warn("screenshot %s returned an empty image" % str(shot_data["name"]))
			continue
		var path := "%s/%s.png" % [SCREENSHOT_DIR, str(shot_data["name"])]
		var save_error := image.save_png(path)
		if save_error != OK:
			_warn("could not save screenshot %s error=%d" % [path, save_error])
			continue
		print("SAVED %s" % path)
	camera.queue_free()

func _active_box_bodies(main: Node) -> Array[StaticBody3D]:
	var bodies: Array[StaticBody3D] = []
	for node in main.find_children("*", "StaticBody3D", true, false):
		var body := node as StaticBody3D
		if body == null:
			continue
		var collision := body.find_child("CollisionShape3D", true, false) as CollisionShape3D
		if collision == null or collision.disabled:
			continue
		if not collision.shape is BoxShape3D:
			continue
		if not _is_branch_visible(body):
			continue
		bodies.append(body)
	return bodies

func _is_branch_visible(node: Node) -> bool:
	var current := node
	while current != null:
		var spatial := current as Node3D
		if spatial != null and not spatial.visible:
			return false
		current = current.get_parent()
	return true

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
	return Vector3.ZERO

func _first_mesh_instance(node: Node) -> MeshInstance3D:
	for child in node.get_children():
		if child is MeshInstance3D:
			return child as MeshInstance3D
		var nested := _first_mesh_instance(child)
		if nested != null:
			return nested
	return null

func _merge_intervals(intervals: Array[Vector2]) -> Array[Vector2]:
	if intervals.is_empty():
		return []
	intervals.sort_custom(func(a: Vector2, b: Vector2) -> bool:
		return a.x < b.x
	)
	var merged: Array[Vector2] = []
	var current := intervals[0]
	for i in range(1, intervals.size()):
		var interval := intervals[i]
		if interval.x <= current.y + GAP_TOLERANCE:
			current.y = maxf(current.y, interval.y)
		else:
			merged.append(current)
			current = interval
	merged.append(current)
	return merged

func _subtract_coverage(expected: Vector2, coverage: Array[Vector2]) -> Array[Vector2]:
	var gaps: Array[Vector2] = []
	var cursor := expected.x
	for interval in coverage:
		if interval.y < cursor + GAP_TOLERANCE:
			continue
		if interval.x > expected.y - GAP_TOLERANCE:
			break
		if interval.x > cursor + GAP_TOLERANCE:
			gaps.append(Vector2(cursor, minf(interval.x, expected.y)))
		cursor = maxf(cursor, interval.y)
		if cursor >= expected.y - GAP_TOLERANCE:
			return gaps
	if cursor < expected.y - GAP_TOLERANCE:
		gaps.append(Vector2(cursor, expected.y))
	return gaps

func _wait_process_frames(count: int) -> void:
	for _i in range(count):
		await process_frame

func _wait_physics_frames(count: int) -> void:
	for _i in range(count):
		await physics_frame

func _fail(message: String) -> void:
	_failures.append(message)
	push_error(message)

func _warn(message: String) -> void:
	_warnings.append(message)
	push_warning(message)
