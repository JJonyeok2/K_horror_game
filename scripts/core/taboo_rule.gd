extends RefCounted
class_name TabooRule

const ResentmentTracker = preload("res://scripts/core/resentment_tracker.gd")

var display_name: String
var resentment_gain: int

func _init(p_display_name: String = "", p_resentment_gain: int = 0) -> void:
	display_name = p_display_name
	resentment_gain = max(p_resentment_gain, 0)

func apply_to(tracker: ResentmentTracker) -> void:
	tracker.add_resentment(resentment_gain, display_name)
