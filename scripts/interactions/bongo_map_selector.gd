extends StaticBody3D
class_name BongoMapSelector

func interaction_label() -> String:
	return "종가 고택으로 이동하기"

func interact(actor: Node) -> void:
	if actor == null:
		return
	var main := actor.get_parent()
	if main != null and main.has_method("travel_to_retrieval_map"):
		main.travel_to_retrieval_map("jongga_estate")
