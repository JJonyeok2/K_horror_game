extends RefCounted
class_name ResentmentTracker

signal resentment_changed(value: int, stage: int, reason: String)

var current_value: int = 0
var history: Array[String] = []

func add_resentment(amount: int, reason: String) -> void:
	current_value += max(amount, 0)
	history.append(reason)
	resentment_changed.emit(current_value, stage(), reason)

func stage() -> int:
	if current_value <= 0:
		return 0
	if current_value <= 2:
		return 1
	if current_value <= 4:
		return 2
	if current_value <= 7:
		return 3
	if current_value <= 10:
		return 4
	return 5
