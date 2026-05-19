extends Area3D
class_name ExtractionZone

signal extracted(total_value: int)

func _ready() -> void:
	body_entered.connect(_on_body_entered)

func extract_inventory(inventory: Variant) -> int:
	var value: int = inventory.total_value()
	inventory.clear()
	extracted.emit(value)
	return value

func _on_body_entered(body: Node3D) -> void:
	if body.has_method("try_collect_artifact"):
		var inventory: Variant = body.get("inventory")
		if inventory != null:
			extract_inventory(inventory)
