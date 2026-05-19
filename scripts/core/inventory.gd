extends RefCounted
class_name Inventory

var max_weight: float
var items: Array = []

func _init(p_max_weight: float = 10.0) -> void:
	max_weight = max(p_max_weight, 0.0)

func try_add(item: Variant) -> bool:
	if total_weight() + item.weight > max_weight:
		return false
	items.append(item)
	return true

func clear() -> void:
	items.clear()

func total_value() -> int:
	var sum := 0
	for item in items:
		sum += item.value
	return sum

func total_resentment_gain() -> int:
	var sum := 0
	for item in items:
		sum += item.resentment_gain
	return sum

func total_weight() -> float:
	var sum := 0.0
	for item in items:
		sum += item.weight
	return sum
