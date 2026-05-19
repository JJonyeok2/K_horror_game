extends RefCounted

const TestAssertions = preload("res://tests/test_assertions.gd")
const QuotaTracker = preload("res://scripts/core/quota_tracker.gd")

func run() -> Array[String]:
	var t := TestAssertions.new()
	test_quota_success(t)
	test_quota_failure_adds_debt(t)
	test_contract_ends_after_three_failures(t)
	return t.failures

func test_quota_success(t: TestAssertions) -> void:
	var quota := QuotaTracker.new(1000)
	quota.add_recovered_value(1200)
	t.assert_true(quota.close_quota_check(), "quota check succeeds when recovered value is enough")
	t.assert_equal(quota.debt, 0, "successful quota check does not add debt")

func test_quota_failure_adds_debt(t: TestAssertions) -> void:
	var quota := QuotaTracker.new(1000)
	quota.add_recovered_value(350)
	var quota_met := quota.close_quota_check()
	t.assert_true(not quota_met, "quota check fails when value is short")
	t.assert_equal(quota.debt, 650, "shortfall becomes debt")
	t.assert_equal(quota.failed_quota_checks, 1, "failed quota check increments count")

func test_contract_ends_after_three_failures(t: TestAssertions) -> void:
	var quota := QuotaTracker.new(1000)
	quota.close_quota_check()
	quota.close_quota_check()
	quota.close_quota_check()
	t.assert_true(quota.is_contract_ended(), "three failed quota checks ends prototype contract")
