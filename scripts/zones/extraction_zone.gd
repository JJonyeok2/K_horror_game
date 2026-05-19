extends Area3D
class_name ExtractionZone

signal extracted(total_value: int)

func extract_inventory(inventory: Variant) -> int:
	var value := inventory.total_value()
	inventory.clear()
	extracted.emit(value)
	return value
