extends Node3D
class_name EstateGate

@export var open_angle_degrees: float = 82.0

var is_open := false
var _left_hinge: Node3D
var _right_hinge: Node3D

func setup(left_hinge: Node3D, right_hinge: Node3D) -> void:
	_left_hinge = left_hinge
	_right_hinge = right_hinge

func interaction_label() -> String:
	if is_open:
		return "대문 닫기"
	return "대문 열기"

func interact(_actor: Node) -> void:
	toggle_gate()

func toggle_gate() -> void:
	set_open(not is_open)

func open_gate() -> void:
	set_open(true)

func close_gate() -> void:
	set_open(false)

func set_open(value: bool) -> void:
	is_open = value
	var angle := deg_to_rad(open_angle_degrees)
	if _left_hinge != null:
		_left_hinge.rotation.y = -angle if is_open else 0.0
	if _right_hinge != null:
		_right_hinge.rotation.y = angle if is_open else 0.0
