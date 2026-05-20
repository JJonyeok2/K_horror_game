extends StaticBody3D
class_name BongoDepartureButton

func interaction_label() -> String:
	return "봉고차로 복귀하기"

func interact(actor: Node) -> void:
	if actor == null:
		return
	var main := actor.get_parent()
	if main != null and main.has_method("return_to_bongo_hub"):
		main.return_to_bongo_hub()
