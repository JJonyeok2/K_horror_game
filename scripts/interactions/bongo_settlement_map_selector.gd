extends StaticBody3D
class_name BongoSettlementMapSelector

func interaction_label() -> String:
	return "정산소로 이동하기"

func interact(actor: Node) -> void:
	if actor == null:
		return
	var main := actor.get_parent()
	if main != null and main.has_method("travel_to_settlement_map"):
		main.travel_to_settlement_map()
