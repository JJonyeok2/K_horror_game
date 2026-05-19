extends Area3D
class_name ExtractionZone

signal extracted(total_value: int)

func _ready() -> void:
	body_entered.connect(_on_body_entered)

func extract_inventory(inventory: Inventory) -> int:
	var value := inventory.total_value()
	inventory.clear()
	extracted.emit(value)
	return value

func _on_body_entered(body: Node3D) -> void:
	if body is PlayerController:
		extract_inventory(body.inventory)
