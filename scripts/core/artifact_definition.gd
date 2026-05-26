extends RefCounted
class_name ArtifactDefinition

var display_name: String
var value: int
var weight: float
var resentment_gain: int
var tags: Array[String]
var hand_slots: int

func _init(
	p_display_name: String = "",
	p_value: int = 0,
	p_weight: float = 0.0,
	p_resentment_gain: int = 0,
	p_tags: Array[String] = [],
	p_hand_slots: int = 1
) -> void:
	display_name = p_display_name
	value = max(p_value, 0)
	weight = max(p_weight, 0.0)
	resentment_gain = max(p_resentment_gain, 0)
	tags = p_tags.duplicate()
	hand_slots = clamp(p_hand_slots, 1, 2)

func has_tag(tag: String) -> bool:
	return tags.has(tag)
