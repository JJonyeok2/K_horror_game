extends RefCounted
class_name Inventory

const ArtifactDefinition = preload("res://scripts/core/artifact_definition.gd")

var max_weight: float
var items: Array[ArtifactDefinition] = []
var max_hand_slots: int = 2

func _init(p_max_weight: float = 10.0) -> void:
	max_weight = max(p_max_weight, 0.0)

func try_add(item: ArtifactDefinition) -> bool:
	if item.hand_slots > free_hand_slots():
		return false
	items.append(item)
	return true

func clear() -> void:
	items.clear()

func pop_last_item() -> ArtifactDefinition:
	if items.is_empty():
		return null
	return items.pop_back()

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

func used_hand_slots() -> int:
	var sum := 0
	for item in items:
		sum += item.hand_slots
	return sum

func free_hand_slots() -> int:
	return max(max_hand_slots - used_hand_slots(), 0)

func hand_status() -> String:
	if items.is_empty():
		return "손: 비어 있음"
	if items.size() == 1 and items[0].hand_slots == 2:
		return "양손: %s" % items[0].display_name
	var labels: Array[String] = []
	for item in items:
		labels.append(item.display_name)
	return "손: %s" % " / ".join(labels)
