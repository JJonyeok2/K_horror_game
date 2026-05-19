extends StaticBody3D
class_name GateLeaf

var gate: Node

func interaction_label() -> String:
	if gate != null and gate.has_method("interaction_label"):
		return gate.interaction_label()
	return "대문 열기"

func interact(actor: Node) -> void:
	if gate != null and gate.has_method("interact"):
		gate.interact(actor)
