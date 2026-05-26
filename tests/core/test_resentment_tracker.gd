extends RefCounted

const TestAssertions = preload("res://tests/test_assertions.gd")
const ResentmentTracker = preload("res://scripts/core/resentment_tracker.gd")
const TabooRule = preload("res://scripts/core/taboo_rule.gd")

func run() -> Array[String]:
	var t := TestAssertions.new()
	test_stage_thresholds(t)
	test_taboo_rule_adds_resentment(t)
	return t.failures

func test_stage_thresholds(t: TestAssertions) -> void:
	var tracker := ResentmentTracker.new()
	t.assert_equal(tracker.stage(), 0, "resentment starts dormant")
	tracker.add_resentment(1, "값싼 물건 회수")
	t.assert_equal(tracker.stage(), 1, "stage 1 starts at resentment 1")
	tracker.add_resentment(4, "사당 물건 회수")
	t.assert_equal(tracker.stage(), 3, "stage 3 starts at resentment 5")
	tracker.add_resentment(7, "금기 연속 위반")
	t.assert_equal(tracker.stage(), 5, "stage caps at 5")

func test_taboo_rule_adds_resentment(t: TestAssertions) -> void:
	var tracker := ResentmentTracker.new()
	var rule := TabooRule.new("문턱 밟기", 2)
	rule.apply_to(tracker)
	t.assert_equal(tracker.current_value, 2, "taboo rule adds resentment")
