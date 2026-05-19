extends RefCounted
class_name Inventory

var max_slots: int
var items: Array = []

func _init(p_max_slots: int = 2) -> void:
	max_slots = max(p_max_slots, 0)

func try_add(item: Variant) -> bool:
	if used_slots() >= max_slots:
		return false
	items.append(item)
	return true

func used_slots() -> int:
	return items.size()

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

func has_space() -> bool:
	return used_slots() < max_slots
