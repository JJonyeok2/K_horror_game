extends "res://scripts/interactions/interactable.gd"
class_name Artifact

const ArtifactDefinition = preload("res://scripts/core/artifact_definition.gd")

signal picked_up(definition: ArtifactDefinition)

@export var display_name: String = "회수품"
@export var value: int = 100
@export var weight: float = 1.0
@export var resentment_gain: int = 1
@export var tags: Array[String] = []
@export_range(1, 2, 1) var hand_slots: int = 1

func _ready() -> void:
	configure_visuals()

func definition() -> ArtifactDefinition:
	return ArtifactDefinition.new(display_name, value, weight, resentment_gain, tags, hand_slots)

func interaction_label() -> String:
	return "%s 회수" % display_name

func interact(actor: Node) -> void:
	var item := definition()
	if actor.has_method("try_collect_artifact") and actor.try_collect_artifact(item):
		picked_up.emit(item)
		queue_free()

func configure_visuals() -> void:
	_clear_visual_meshes()
	var visual_key := _visual_key()
	var bounds := _artifact_bounds(visual_key)
	_update_collision(bounds)
	_add_collision_proxy(bounds)
	_add_outline(bounds)
	var model_root := Node3D.new()
	model_root.name = "ArtifactModelRoot"
	add_child(model_root)
	match visual_key:
		"brass_bowl":
			_build_brass_bowl(model_root)
		"wood_goose":
			_build_wood_goose(model_root)
		"jegi":
			_build_jegi(model_root)
		"large_pottery":
			_build_large_pottery(model_root)
		_:
			_build_default_relic(model_root, bounds)
	set_meta("artifact_visual_key", visual_key)
	set_meta("carry_class", "two_hand_heavy" if hand_slots >= 2 else "one_hand_light")

func _clear_visual_meshes() -> void:
	for child in get_children():
		if child is MeshInstance3D or child.name == "ArtifactModelRoot":
			remove_child(child)
			child.free()

func _visual_key() -> String:
	for tag in tags:
		match tag:
			"brass_bowl", "wood_goose", "jegi", "large_pottery":
				return tag
	if display_name.contains("놋그릇") or display_name.contains("놋수저"):
		return "brass_bowl"
	if display_name.contains("기러기"):
		return "wood_goose"
	if display_name.contains("제기"):
		return "jegi"
	if display_name.contains("항아리") or display_name.contains("도자기"):
		return "large_pottery"
	return "default_relic"

func _artifact_bounds(visual_key: String) -> Vector3:
	match visual_key:
		"brass_bowl":
			return Vector3(0.58, 0.34, 0.58)
		"wood_goose":
			return Vector3(0.72, 0.42, 0.34)
		"jegi":
			return Vector3(0.5, 0.62, 0.5)
		"large_pottery":
			return Vector3(1.18, 1.22, 1.18)
		_:
			return Vector3(0.76, 0.46, 0.52) if hand_slots >= 2 else Vector3(0.45, 0.3, 0.45)

func _update_collision(bounds: Vector3) -> void:
	var collision := get_node_or_null("CollisionShape3D") as CollisionShape3D
	if collision == null:
		collision = CollisionShape3D.new()
		collision.name = "CollisionShape3D"
		add_child(collision)
	var shape := BoxShape3D.new()
	shape.size = bounds
	collision.shape = shape

func _add_collision_proxy(bounds: Vector3) -> void:
	var proxy := MeshInstance3D.new()
	proxy.name = "ArtifactCollisionProxy"
	proxy.visible = false
	var mesh := BoxMesh.new()
	mesh.size = bounds
	proxy.mesh = mesh
	add_child(proxy)

func _add_outline(bounds: Vector3) -> void:
	var outline := MeshInstance3D.new()
	outline.name = "ArtifactOutline"
	var mesh := BoxMesh.new()
	mesh.size = bounds * 1.08
	mesh.material = _artifact_material(Color(0.18, 0.24, 0.22, 0.28), true)
	outline.mesh = mesh
	add_child(outline)

func _build_brass_bowl(root: Node3D) -> void:
	_add_cylinder(root, "BrassBowlFoot", Vector3(0.0, -0.1, 0.0), 0.22, 0.1, Color(0.55, 0.42, 0.16))
	_add_cylinder(root, "BrassBowlBody", Vector3(0.0, 0.02, 0.0), 0.34, 0.22, Color(0.72, 0.55, 0.22))
	_add_cylinder(root, "BrassBowlDarkMouth", Vector3(0.0, 0.16, 0.0), 0.29, 0.04, Color(0.19, 0.13, 0.05))

func _build_wood_goose(root: Node3D) -> void:
	_add_box(root, "WoodGooseBody", Vector3(0.0, 0.0, 0.0), Vector3(0.54, 0.24, 0.24), Color(0.24, 0.13, 0.06))
	_add_box(root, "WoodGooseChest", Vector3(0.25, 0.05, 0.0), Vector3(0.24, 0.28, 0.22), Color(0.29, 0.17, 0.08), Vector3(0.0, 0.0, -12.0))
	_add_box(root, "WoodGooseNeck", Vector3(0.43, 0.25, 0.0), Vector3(0.11, 0.36, 0.12), Color(0.22, 0.11, 0.045), Vector3(0.0, 0.0, -18.0))
	_add_box(root, "WoodGooseHead", Vector3(0.55, 0.45, 0.0), Vector3(0.2, 0.16, 0.14), Color(0.24, 0.13, 0.055))
	_add_box(root, "WoodGooseWingLeft", Vector3(-0.05, 0.03, -0.15), Vector3(0.42, 0.08, 0.05), Color(0.16, 0.08, 0.035), Vector3(0.0, 12.0, 0.0))
	_add_box(root, "WoodGooseWingRight", Vector3(-0.05, 0.03, 0.15), Vector3(0.42, 0.08, 0.05), Color(0.16, 0.08, 0.035), Vector3(0.0, -12.0, 0.0))

func _build_jegi(root: Node3D) -> void:
	_add_cylinder(root, "JegiStem", Vector3(0.0, -0.05, 0.0), 0.1, 0.34, Color(0.3, 0.17, 0.07))
	_add_cylinder(root, "JegiBowl", Vector3(0.0, 0.17, 0.0), 0.3, 0.18, Color(0.57, 0.42, 0.17))
	_add_cylinder(root, "JegiLip", Vector3(0.0, 0.29, 0.0), 0.34, 0.04, Color(0.74, 0.55, 0.22))
	_add_box(root, "JegiCrackMark", Vector3(0.11, 0.29, -0.28), Vector3(0.04, 0.16, 0.035), Color(0.08, 0.04, 0.02), Vector3(0.0, 0.0, 18.0))

func _build_large_pottery(root: Node3D) -> void:
	_add_cylinder(root, "LargePotteryBody", Vector3(0.0, 0.0, 0.0), 0.43, 0.86, Color(0.72, 0.68, 0.58))
	_add_cylinder(root, "LargePotteryNeck", Vector3(0.0, 0.52, 0.0), 0.22, 0.28, Color(0.78, 0.75, 0.66))
	_add_cylinder(root, "LargePotteryMouth", Vector3(0.0, 0.69, 0.0), 0.28, 0.08, Color(0.2, 0.18, 0.15))
	_add_box(root, "LargePotteryCrackA", Vector3(0.22, 0.1, -0.38), Vector3(0.05, 0.42, 0.04), Color(0.1, 0.08, 0.06), Vector3(0.0, 0.0, -14.0))
	_add_box(root, "LargePotteryCrackB", Vector3(-0.16, -0.2, -0.4), Vector3(0.04, 0.28, 0.04), Color(0.1, 0.08, 0.06), Vector3(0.0, 0.0, 22.0))

func _build_default_relic(root: Node3D, bounds: Vector3) -> void:
	_add_box(root, "DefaultRelicBody", Vector3.ZERO, bounds * 0.82, Color(0.5, 0.38, 0.22))
	_add_box(root, "DefaultRelicMark", Vector3(0.0, bounds.y * 0.12, -bounds.z * 0.42), Vector3(bounds.x * 0.5, bounds.y * 0.08, 0.035), Color(0.18, 0.08, 0.035))

func _add_box(root: Node3D, label: String, local_position: Vector3, size: Vector3, color: Color, rotation_degrees_value: Vector3 = Vector3.ZERO) -> MeshInstance3D:
	var mesh_instance := MeshInstance3D.new()
	mesh_instance.name = label
	mesh_instance.position = local_position
	mesh_instance.rotation_degrees = rotation_degrees_value
	var mesh := BoxMesh.new()
	mesh.size = size
	mesh.material = _artifact_material(color)
	mesh_instance.mesh = mesh
	root.add_child(mesh_instance)
	return mesh_instance

func _add_cylinder(root: Node3D, label: String, local_position: Vector3, radius: float, height: float, color: Color) -> MeshInstance3D:
	var mesh_instance := MeshInstance3D.new()
	mesh_instance.name = label
	mesh_instance.position = local_position
	var mesh := CylinderMesh.new()
	mesh.top_radius = radius
	mesh.bottom_radius = radius * 0.88
	mesh.height = height
	mesh.radial_segments = 8
	mesh.rings = 1
	mesh.material = _artifact_material(color)
	mesh_instance.mesh = mesh
	root.add_child(mesh_instance)
	return mesh_instance

func _artifact_material(color: Color, outline: bool = false) -> StandardMaterial3D:
	var material := StandardMaterial3D.new()
	material.albedo_color = color
	material.roughness = 0.9
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	material.emission_enabled = true
	material.emission = Color(0.08, 0.095, 0.07) if outline else Color(color.r * 0.08, color.g * 0.08, color.b * 0.08)
	material.emission_energy_multiplier = 0.42 if outline else 0.16
	if color.a < 1.0:
		material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	return material
