extends RefCounted
class_name QuotaTracker

var required_value: int
var recovered_value: int = 0
var debt: int = 0
var failed_quota_checks: int = 0

func _init(starting_required_value: int = 1000) -> void:
	required_value = starting_required_value

func add_recovered_value(value: int) -> void:
	recovered_value += max(value, 0)

func is_quota_met() -> bool:
	return recovered_value >= required_value

func close_quota_check() -> bool:
	if is_quota_met():
		return true
	var shortfall := required_value - recovered_value
	debt += shortfall
	failed_quota_checks += 1
	return false

func is_contract_ended() -> bool:
	return failed_quota_checks >= 3
