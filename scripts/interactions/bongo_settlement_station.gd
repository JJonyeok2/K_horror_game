extends StaticBody3D
class_name BongoSettlementStation

func interaction_label() -> String:
	return "미정산 물품 정산받기"

func interact(actor: Node) -> void:
	if actor == null:
		return
	var main := actor.get_parent()
	if main != null and main.has_method("settle_stored_cargo"):
		main.settle_stored_cargo()
