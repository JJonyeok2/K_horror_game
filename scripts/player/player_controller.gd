extends CharacterBody3D
class_name PlayerController

const InventoryScript := preload("res://scripts/core/inventory.gd")

@export var base_speed: float = 4.5
@export var mouse_sensitivity: float = 0.0025
@export var gravity: float = 9.8
@export var jump_velocity: float = 5.0

var inventory: Variant = InventoryScript.new(2)
var camera: Camera3D

func _ready() -> void:
	camera = $Camera3D
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		rotate_y(-event.relative.x * mouse_sensitivity)
		camera.rotate_x(-event.relative.y * mouse_sensitivity)
		camera.rotation.x = clamp(camera.rotation.x, deg_to_rad(-80), deg_to_rad(80))

func _physics_process(delta: float) -> void:
	var input_dir := Input.get_vector("move_left", "move_right", "move_forward", "move_back")
	var direction := (transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	var speed: float = base_speed
	velocity.x = direction.x * speed
	velocity.z = direction.z * speed
	if Input.is_action_just_pressed("jump") and is_on_floor():
		velocity.y = jump_velocity
	if not is_on_floor():
		velocity.y -= gravity * delta
	move_and_slide()

func try_collect_artifact(item: Variant) -> bool:
	return inventory.try_add(item)
