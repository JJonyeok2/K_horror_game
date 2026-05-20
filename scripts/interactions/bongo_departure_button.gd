extends StaticBody3D
class_name BongoDepartureButton

func interaction_label() -> String:
	return "봉고차 출발하기"

func interact(actor: Node) -> void:
	if actor == null:
		return
	var main := actor.get_parent()
	if main != null and main.has_method("depart_bongo"):
		main.depart_bongo()
