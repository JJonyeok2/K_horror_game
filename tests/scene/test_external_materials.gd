extends SceneTree

const MainScene := preload("res://scenes/Main.tscn")

var _failed := false

func _initialize() -> void:
	var main: Node = MainScene.instantiate()
	root.add_child(main)
	for _i in range(90):
		await physics_frame

	_assert_surface_has_pbr_material(main, "LongForestApproachRoad", "forest approach road")
	_assert_surface_has_pbr_material(main, "EstateContinuousGround", "continuous estate ground")
	_assert_surface_has_pbr_material(main, "ApproachForestTreeLeft01", "approach tree bark")
	_assert_surface_has_pbr_material(main, "MainHouseFrontWallLeft", "main house plaster")
	_assert_surface_has_pbr_material(main, "CourtyardRubble01", "courtyard rubble stone")

	if _failed:
		quit(1)
		return
	print("EXTERNAL_MATERIALS_SMOKE: key estate surfaces use imported PBR textures")
	quit(0)

func _assert_surface_has_pbr_material(main: Node, label: String, description: String) -> void:
	var node := main.find_child(label, true, false)
	if node == null:
		_fail("Missing %s node: %s" % [description, label])
		return
	var mesh_instance := _first_mesh_instance(node)
	if mesh_instance == null:
		_fail("%s has no mesh instance" % label)
		return
	var primitive_mesh := mesh_instance.mesh as PrimitiveMesh
	if primitive_mesh == null:
		_fail("%s mesh is not a primitive mesh" % label)
		return
	var material := primitive_mesh.material as StandardMaterial3D
	if material == null:
		_fail("%s has no StandardMaterial3D" % label)
		return
	if material.albedo_texture == null:
		_fail("%s should use an imported albedo texture" % label)
		return
	if not material.normal_enabled or material.normal_texture == null:
		_fail("%s should use an imported normal texture" % label)
		return

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
