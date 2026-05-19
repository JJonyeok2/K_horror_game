extends StaticBody3D
class_name StatefulInteractable

@export var prompt: String = "상호작용"
@export var active_prompt: String = "되돌리기"
@export var active_offset := Vector3.ZERO
@export var active_rotation_degrees := Vector3.ZERO

var is_active := false
var _rest_position := Vector3.ZERO
var _rest_rotation := Vector3.ZERO
var _rest_initialized := false

func _ready() -> void:
	_capture_rest_state()

func _capture_rest_state() -> void:
	_rest_position = position
	_rest_rotation = rotation_degrees
	_rest_initialized = true

func interaction_label() -> String:
	return active_prompt if is_active else prompt

func interact(_actor: Node) -> void:
	if not _rest_initialized:
		_capture_rest_state()
	is_active = not is_active
	if is_active:
		position = _rest_position + active_offset
		rotation_degrees = _rest_rotation + active_rotation_degrees
	else:
		position = _rest_position
		rotation_degrees = _rest_rotation
