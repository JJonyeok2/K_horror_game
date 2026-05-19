extends Node3D
class_name JonggaEstateBuilder

const ArtifactScene := preload("res://scenes/props/Artifact.tscn")
const ExtractionScene := preload("res://scenes/zones/ExtractionZone.tscn")

func build(main: Node) -> void:
	var walls := Node3D.new()
	walls.name = "Walls"
	add_child(walls)
	var doorways := Node3D.new()
	doorways.name = "Doorways"
	add_child(doorways)

	_create_floor("대문", Vector3(0, 0, -3.8), Vector3(4, 0.2, 2.4), Color.DARK_OLIVE_GREEN)
	_create_floor("바깥마당", Vector3(0, 0, -8), Vector3(14, 0.2, 10), Color.DARK_GREEN)
	_create_floor("사랑채", Vector3(-5, 0, -15), Vector3(8, 0.2, 7), Color.DIM_GRAY)
	_create_floor("안채", Vector3(5, 0, -22), Vector3(9, 0.2, 8), Color.SADDLE_BROWN)
	_create_floor("곳간", Vector3(-5.5, 0, -27), Vector3(6, 0.2, 6), Color.DARK_SLATE_GRAY)
	_create_floor("뒤뜰", Vector3(-2, 0, -31), Vector3(10, 0.2, 4), Color.DARK_GREEN)
	_create_floor("사당", Vector3(0, 0, -36), Vector3(7, 0.2, 7), Color.MAROON)
	_create_floor("사랑채-안채 연결마루", Vector3(0, 0, -18.25), Vector3(8, 0.2, 1.2), Color.DARK_GRAY)
	_create_floor("안채-곳간 연결마루", Vector3(-1.25, 0, -25), Vector3(8, 0.2, 1.2), Color.DARK_GRAY)
	_create_floor("뒤뜰-사당 연결마루", Vector3(-1, 0, -33.25), Vector3(5, 0.2, 1.2), Color.DARK_GRAY)
	_create_collision_floor("낙하방지바닥", Vector3(0, -0.35, -20), Vector3(24, 0.5, 42))

	_create_wall(walls, "담장_서쪽", Vector3(-7.2, 1.3, -8), Vector3(0.3, 2.6, 10), Color.DARK_SLATE_GRAY)
	_create_wall(walls, "담장_동쪽", Vector3(7.2, 1.3, -8), Vector3(0.3, 2.6, 10), Color.DARK_SLATE_GRAY)
	_create_wall(walls, "대문_서쪽담", Vector3(-4.6, 1.3, -3), Vector3(5, 2.6, 0.3), Color.DARK_SLATE_GRAY)
	_create_wall(walls, "대문_동쪽담", Vector3(4.6, 1.3, -3), Vector3(5, 2.6, 0.3), Color.DARK_SLATE_GRAY)
	_create_wall(walls, "마당_북서담", Vector3(-6.1, 1.3, -13), Vector3(2.2, 2.6, 0.3), Color.DARK_SLATE_GRAY)
	_create_wall(walls, "마당_북동담", Vector3(4.2, 1.3, -13), Vector3(6.0, 2.6, 0.3), Color.DARK_SLATE_GRAY)

	_create_room_walls(walls, "사랑채", Vector3(-5, 0, -15), Vector3(8, 0.2, 7), Color.SADDLE_BROWN)
	_create_room_walls(walls, "안채", Vector3(5, 0, -22), Vector3(9, 0.2, 8), Color.SIENNA)
	_create_room_walls(walls, "곳간", Vector3(-5.5, 0, -27), Vector3(6, 0.2, 6), Color.DIM_GRAY)
	_create_room_walls(walls, "사당", Vector3(0, 0, -36), Vector3(7, 0.2, 7), Color.MAROON)

	_create_doorway(doorways, "문_대문", Vector3(0, 0.1, -3))
	_create_doorway(doorways, "문_사랑채", Vector3(-5, 0.1, -11.5))
	_create_doorway(doorways, "문_안채", Vector3(5, 0.1, -18))
	_create_doorway(doorways, "문_곳간", Vector3(-5.5, 0.1, -24))
	_create_doorway(doorways, "문_사당", Vector3(0, 0.1, -32.5))

	_spawn_artifact(main, "놋 제기", 120, 1.5, 2, Vector3(-5, 0.4, -15), ["ancestor_item"])
	_spawn_artifact(main, "서예 족자", 280, 1.0, 3, Vector3(5, 0.4, -22), ["document_item"])
	_spawn_artifact(main, "족보", 220, 1.0, 3, Vector3(-4.5, 0.4, -27), ["document_item"])
	_spawn_artifact(main, "사당 방울", 700, 2.0, 5, Vector3(0, 0.4, -36), ["shrine_item"])
	var extraction := ExtractionScene.instantiate()
	add_child(extraction)
	extraction.position = Vector3(0, 0.5, -4)
	if main.has_method("register_extraction_zone"):
		main.register_extraction_zone(extraction)

func _create_floor(label: String, pos: Vector3, scale: Vector3, color: Color) -> void:
	var body := StaticBody3D.new()
	body.name = label
	add_child(body)
	body.position = pos
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

func _create_room_walls(parent: Node, label: String, center: Vector3, floor_size: Vector3, color: Color) -> void:
	var width: float = floor_size.x
	var depth: float = floor_size.z
	var half_width: float = width * 0.5
	var half_depth: float = depth * 0.5
	_create_wall(parent, "%s_서쪽벽" % label, Vector3(center.x - half_width, 1.4, center.z), Vector3(0.25, 2.8, depth), color)
	_create_wall(parent, "%s_동쪽벽" % label, Vector3(center.x + half_width, 1.4, center.z), Vector3(0.25, 2.8, depth), color)
	_create_wall(parent, "%s_뒤벽" % label, Vector3(center.x, 1.4, center.z - half_depth), Vector3(width, 2.8, 0.25), color)
	_create_wall(parent, "%s_입구좌벽" % label, Vector3(center.x - half_width * 0.62, 1.4, center.z + half_depth), Vector3(width * 0.38, 2.8, 0.25), color)
	_create_wall(parent, "%s_입구우벽" % label, Vector3(center.x + half_width * 0.62, 1.4, center.z + half_depth), Vector3(width * 0.38, 2.8, 0.25), color)

func _create_wall(parent: Node, label: String, pos: Vector3, scale: Vector3, color: Color) -> void:
	var body := StaticBody3D.new()
	body.name = label
	parent.add_child(body)
	body.position = pos
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

func _create_doorway(parent: Node, label: String, pos: Vector3) -> void:
	var marker := Node3D.new()
	marker.name = label
	parent.add_child(marker)
	marker.position = pos
	_create_wall(parent, "%s_문턱" % label, pos + Vector3(0, 0.05, 0), Vector3(1.7, 0.1, 0.18), Color.DARK_GOLDENROD)

func _create_collision_floor(label: String, pos: Vector3, scale: Vector3) -> void:
	var body := StaticBody3D.new()
	body.name = label
	add_child(body)
	body.position = pos
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
	artifact.position = pos
	if main.has_method("register_artifact"):
		main.register_artifact(artifact)
