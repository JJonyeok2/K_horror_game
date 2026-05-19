extends RefCounted

const TestAssertions = preload("res://tests/test_assertions.gd")
const QuotaTracker = preload("res://scripts/core/quota_tracker.gd")

func run() -> Array[String]:
	var t := TestAssertions.new()
	var quota := QuotaTracker.new(1000)
	t.assert_equal(quota.required_value, 1000, "quota stores required value")
	t.assert_equal(quota.recovered_value, 0, "quota starts with no recovered value")
	return t.failures
