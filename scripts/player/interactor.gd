extends RayCast3D
class_name PlayerInteractor

@export var actor_path: NodePath
var actor: Node
var current_label: String = ""

func _ready() -> void:
	actor = get_node(actor_path)

func _process(_delta: float) -> void:
	current_label = ""
	if is_colliding():
		var hit := get_collider()
		if hit != null and hit.has_method("interaction_label"):
			current_label = hit.interaction_label()
			if Input.is_action_just_pressed("interact"):
				hit.interact(actor)
