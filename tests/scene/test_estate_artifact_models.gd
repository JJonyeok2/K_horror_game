extends SceneTree

const MainScene := preload("res://scenes/Main.tscn")

var _failed := false

func _initialize() -> void:
	var main: Node = MainScene.instantiate()
	root.add_child(main)
	for _i in range(90):
		await physics_frame

	_assert_traditional_artifact_roster(main)
	_assert_artifact_visibility_treatment(main)
	_assert_light_and_heavy_artifact_scale(main)
	if _failed:
		quit(1)
		return
	print("ESTATE_ARTIFACT_MODELS: traditional props, outlines, and carry scale verified")
	quit(0)

func _assert_traditional_artifact_roster(main: Node) -> void:
	for display_name in ["낡은 놋그릇", "나무 기러기", "제기", "큰 백자 항아리"]:
		var artifact := _artifact_by_name(main, display_name)
		if artifact == null:
			_fail("Missing traditional estate artifact: %s" % display_name)
			return
		var model_root := artifact.find_child("ArtifactModelRoot", true, false)
		if model_root == null or model_root.get_child_count() < 2:
			_fail("%s should be modeled as a low-poly prop, not a single box" % display_name)
			return

func _assert_artifact_visibility_treatment(main: Node) -> void:
	var artifact := _artifact_by_name(main, "낡은 놋그릇")
	if artifact == null:
		return
	var outline := artifact.find_child("ArtifactOutline", true, false) as MeshInstance3D
	if outline == null:
		_fail("Artifacts need an ArtifactOutline mesh for dark-map readability")
		return
	var material := _standard_material(outline)
	if material == null:
		_fail("ArtifactOutline has no StandardMaterial3D")
		return
	if not material.emission_enabled:
		_fail("ArtifactOutline should use subtle emission so it reads in the dark")

func _assert_light_and_heavy_artifact_scale(main: Node) -> void:
	var light := _artifact_by_name(main, "나무 기러기")
	var heavy := _artifact_by_name(main, "큰 백자 항아리")
	if light == null or heavy == null:
		return
	if int(light.get("hand_slots")) != 1:
		_fail("나무 기러기 should be a one-hand artifact")
		return
	if int(heavy.get("hand_slots")) != 2:
		_fail("큰 백자 항아리 should be a two-hand artifact")
		return
	if float(heavy.get("weight")) < 4.0:
		_fail("큰 백자 항아리 should be heavy enough to slow movement")
		return
	var light_size := _box_collision_size(light)
	var heavy_size := _box_collision_size(heavy)
	if heavy_size.x <= light_size.x * 1.5 or heavy_size.y <= light_size.y * 1.5:
		_fail("Heavy artifact collision should be visibly larger than one-hand props")

func _artifact_by_name(main: Node, display_name: String) -> Node3D:
	for node in main.find_children("*", "StaticBody3D", true, false):
		if node.has_method("definition") and str(node.get("display_name")) == display_name:
			return node as Node3D
	return null

func _box_collision_size(node: Node) -> Vector3:
	var collision := node.find_child("CollisionShape3D", true, false) as CollisionShape3D
	if collision == null:
		return Vector3.ZERO
	var shape := collision.shape as BoxShape3D
	if shape == null:
		return Vector3.ZERO
	return shape.size

func _standard_material(mesh_instance: MeshInstance3D) -> StandardMaterial3D:
	var primitive := mesh_instance.mesh as PrimitiveMesh
	if primitive == null:
		return null
	return primitive.material as StandardMaterial3D

func _fail(message: String) -> void:
	_failed = true
	push_error(message)
