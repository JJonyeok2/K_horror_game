extends Node3D
class_name JonggaEstateBuilder

const ArtifactScene := preload("res://scenes/props/Artifact.tscn")
const ExtractionScene := preload("res://scenes/zones/ExtractionZone.tscn")

func build(main: Node) -> void:
	_create_floor("바깥마당", Vector3(0, 0, -8), Vector3(12, 0.2, 10), Color.DARK_GREEN)
	_create_floor("사랑채", Vector3(-7, 0, -15), Vector3(8, 0.2, 7), Color.DIM_GRAY)
	_create_floor("안채", Vector3(7, 0, -22), Vector3(9, 0.2, 8), Color.SADDLE_BROWN)
	_create_floor("곳간", Vector3(-8, 0, -27), Vector3(6, 0.2, 6), Color.DARK_SLATE_GRAY)
	_create_floor("사당", Vector3(0, 0, -36), Vector3(7, 0.2, 7), Color.MAROON)
	_spawn_artifact(main, "놋 제기", 120, 1.5, 2, Vector3(-5, 0.4, -15), ["ancestor_item"])
	_spawn_artifact(main, "서예 족자", 280, 1.0, 3, Vector3(7, 0.4, -22), ["document_item"])
	_spawn_artifact(main, "사당 방울", 700, 2.0, 5, Vector3(0, 0.4, -36), ["shrine_item"])
	var extraction := ExtractionScene.instantiate()
	add_child(extraction)
	extraction.global_position = Vector3(0, 0.5, 2)
	if main.has_method("register_extraction_zone"):
		main.register_extraction_zone(extraction)

func _create_floor(label: String, pos: Vector3, scale: Vector3, color: Color) -> void:
	var body := StaticBody3D.new()
	body.name = label
	add_child(body)
	body.global_position = pos
	var mesh_instance := MeshInstance3D.new()
	var box_mesh := BoxMesh.new()
	box_mesh.size = scale
	var mat := StandardMaterial3D.new()
	mat.albedo_color = color
	box_mesh.material = mat
	mesh_instance.mesh = box_mesh
	body.add_child(mesh_instance)
	var collision := CollisionShape3D.new()
	var shape := BoxShape3D.new()
	shape.size = scale
	collision.shape = shape
	body.add_child(collision)

func _spawn_artifact(main: Node, display_name: String, value: int, weight: float, resentment_gain: int, pos: Vector3, tags: Array[String]) -> void:
	var artifact := ArtifactScene.instantiate()
	add_child(artifact)
	artifact.display_name = display_name
	artifact.value = value
	artifact.weight = weight
	artifact.resentment_gain = resentment_gain
	artifact.tags = tags
	artifact.global_position = pos
	if main.has_method("register_artifact"):
		main.register_artifact(artifact)
