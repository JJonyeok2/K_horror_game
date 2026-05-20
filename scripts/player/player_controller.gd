extends CharacterBody3D
class_name PlayerController

const ArtifactDefinition = preload("res://scripts/core/artifact_definition.gd")
const Inventory = preload("res://scripts/core/inventory.gd")
const ArtifactScene := preload("res://scenes/props/Artifact.tscn")

@export var base_speed: float = 4.5
@export var sprint_speed_multiplier: float = 1.65
@export var exhausted_walk_multiplier: float = 0.65
@export var exhausted_recovery_threshold_seconds: float = 1.0
@export var max_stamina_seconds: float = 15.0
@export var stamina_recovery_seconds: float = 11.0
@export var mouse_sensitivity: float = 0.0025
@export var gravity: float = 21.0
@export var jump_velocity: float = 6.4
@export var movement_enabled: bool = true
@export var max_health: float = 100.0

var inventory := Inventory.new(12.0)
var stamina_seconds: float = 15.0
var stamina_ratio: float = 1.0
var health: float = 100.0
var health_ratio: float = 1.0
var is_sprinting: bool = false
var camera: Camera3D
var _held_mounts: Node3D
var _is_exhausted: bool = false

func _ready() -> void:
	camera = $Camera3D
	_held_mounts = $Camera3D/HeldItemMounts
	_ensure_four_inventory_mounts()
	stamina_seconds = max_stamina_seconds
	health = max_health
	_update_stamina_ratio()
	_update_health_ratio()
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and event.physical_keycode == KEY_ESCAPE:
		Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
		return
	if event is InputEventMouseButton and event.pressed and Input.mouse_mode != Input.MOUSE_MODE_CAPTURED:
		Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
		return
	if movement_enabled and event.is_action_pressed("drop_item"):
		drop_current_artifact()
		return
	if movement_enabled and event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		rotate_y(-event.relative.x * mouse_sensitivity)
		camera.rotate_x(-event.relative.y * mouse_sensitivity)
		camera.rotation.x = clamp(camera.rotation.x, deg_to_rad(-80), deg_to_rad(80))

func _physics_process(delta: float) -> void:
	var input_dir := Vector2.ZERO
	if movement_enabled:
		input_dir = Input.get_vector("move_left", "move_right", "move_forward", "move_back")
	var direction: Vector3 = (transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	var weight_ratio: float = clamp(inventory.total_weight() / inventory.max_weight, 0.0, 1.0)
	var speed: float = lerp(base_speed, base_speed * 0.55, weight_ratio)
	var wants_sprint := movement_enabled and Input.is_action_pressed("sprint") and direction.length() > 0.0
	if stamina_seconds <= 0.0:
		_is_exhausted = true
	elif _is_exhausted and stamina_seconds >= exhausted_recovery_threshold_seconds:
		_is_exhausted = false
	is_sprinting = wants_sprint and not _is_exhausted and stamina_seconds > 0.0
	if is_sprinting:
		speed *= sprint_speed_multiplier
		stamina_seconds = max(stamina_seconds - delta, 0.0)
		if stamina_seconds <= 0.0:
			_is_exhausted = true
	else:
		if _is_exhausted:
			speed *= exhausted_walk_multiplier
		var recovery_rate := max_stamina_seconds / stamina_recovery_seconds
		stamina_seconds = min(stamina_seconds + recovery_rate * delta, max_stamina_seconds)
		if _is_exhausted and stamina_seconds >= exhausted_recovery_threshold_seconds:
			_is_exhausted = false
	_update_stamina_ratio()
	velocity.x = direction.x * speed
	velocity.z = direction.z * speed
	if not is_on_floor():
		velocity.y -= gravity * delta
	elif velocity.y < 0.0:
		velocity.y = 0.0
	if movement_enabled and is_on_floor() and Input.is_action_just_pressed("jump"):
		velocity.y = jump_velocity
	move_and_slide()

func _update_stamina_ratio() -> void:
	stamina_ratio = clamp(stamina_seconds / max_stamina_seconds, 0.0, 1.0)

func apply_damage(amount: float) -> void:
	health = clamp(health - max(amount, 0.0), 0.0, max_health)
	_update_health_ratio()

func heal(amount: float) -> void:
	health = clamp(health + max(amount, 0.0), 0.0, max_health)
	_update_health_ratio()

func _update_health_ratio() -> void:
	health_ratio = clamp(health / max_health, 0.0, 1.0)

func try_collect_artifact(item: ArtifactDefinition) -> bool:
	var accepted: bool = inventory.try_add(item)
	if accepted:
		refresh_held_item_views()
	return accepted

func drop_current_artifact() -> bool:
	var item: ArtifactDefinition = inventory.pop_last_item()
	if item == null:
		return false
	var artifact: Node3D = ArtifactScene.instantiate()
	artifact.display_name = item.display_name
	artifact.value = item.value
	artifact.weight = item.weight
	artifact.resentment_gain = item.resentment_gain
	artifact.tags = item.tags.duplicate()
	artifact.hand_slots = item.hand_slots
	var forward: Vector3 = -camera.global_transform.basis.z.normalized()
	var drop_position: Vector3 = global_position + forward * 1.45
	drop_position.y = max(global_position.y - 0.55, 0.45)
	var parent_node: Node = get_parent()
	parent_node.add_child(artifact)
	artifact.global_position = drop_position
	if parent_node.has_method("register_artifact"):
		parent_node.register_artifact(artifact)
	refresh_held_item_views()
	return true

func refresh_held_item_views() -> void:
	for child in _held_mounts.get_children():
		for held_child in child.get_children():
			held_child.queue_free()
	if inventory.items.is_empty():
		return
	var mounts := _inventory_slot_mounts()
	var slot_index := 0
	for item in inventory.items:
		if slot_index >= mounts.size():
			return
		var is_large := item.hand_slots >= 2
		_add_held_item_box(mounts[slot_index], item, is_large)
		slot_index += max(item.hand_slots, 1)

func _ensure_four_inventory_mounts() -> void:
	_ensure_mount("LowerLeftHandMount", Vector3(-0.42, -0.43, -0.66), Vector3(0.0, deg_to_rad(-12.0), 0.0))
	_ensure_mount("LowerRightHandMount", Vector3(0.42, -0.43, -0.66), Vector3(0.0, deg_to_rad(12.0), 0.0))

func _ensure_mount(label: String, local_position: Vector3, local_rotation: Vector3) -> Node3D:
	var existing := _held_mounts.get_node_or_null(label) as Node3D
	if existing != null:
		return existing
	var marker := Marker3D.new()
	marker.name = label
	_held_mounts.add_child(marker)
	marker.position = local_position
	marker.rotation = local_rotation
	return marker

func _inventory_slot_mounts() -> Array[Node3D]:
	return [
		$Camera3D/HeldItemMounts/LeftHandMount,
		$Camera3D/HeldItemMounts/RightHandMount,
		$Camera3D/HeldItemMounts/LowerLeftHandMount,
		$Camera3D/HeldItemMounts/LowerRightHandMount,
	]

func _add_held_item_box(mount: Node3D, item: ArtifactDefinition, is_large: bool) -> void:
	var mesh_instance := MeshInstance3D.new()
	mesh_instance.name = "Held_%s" % item.display_name
	var mesh := BoxMesh.new()
	mesh.size = Vector3(0.75, 0.42, 0.32) if is_large else Vector3(0.34, 0.28, 0.24)
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.72, 0.58, 0.35) if is_large else Color(0.68, 0.62, 0.46)
	mesh.material = mat
	mesh_instance.mesh = mesh
	mount.add_child(mesh_instance)
