extends Area3D
class_name ExtractionZone

const Inventory = preload("res://scripts/core/inventory.gd")

signal extracted(total_value: int)

func _ready() -> void:
	body_entered.connect(_on_body_entered)

func interaction_label() -> String:
	return "봉고차에 물품 싣기"

func interact(actor: Node) -> void:
	if actor != null and actor.get_parent() != null and actor.get_parent().has_method("extract_player_inventory"):
		actor.get_parent().extract_player_inventory()

func extract_inventory(inventory: Inventory) -> int:
	var value := inventory.total_value()
	inventory.clear()
	extracted.emit(value)
	return value

func _on_body_entered(body: Node) -> void:
	if body != null and body.get_parent() != null and body.get_parent().has_method("extract_player_inventory"):
		body.get_parent().extract_player_inventory()
