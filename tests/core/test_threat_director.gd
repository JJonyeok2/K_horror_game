extends RefCounted

const TestAssertions = preload("res://tests/test_assertions.gd")
const ThreatDirectorScript = preload("res://scripts/core/threat_director.gd")

func run() -> Array[String]:
	var t := TestAssertions.new()
	test_stage_to_threat_state(t)
	test_audio_cues_by_stage(t)
	return t.failures

func test_stage_to_threat_state(t: TestAssertions) -> void:
	var director = ThreatDirectorScript.new()
	t.assert_equal(director.state_for_stage(0), "dormant", "stage 0 is dormant")
	t.assert_equal(director.state_for_stage(2), "visible", "stage 2 shows apparition")
	t.assert_equal(director.state_for_stage(4), "pursuit", "stage 4 starts pursuit")
	t.assert_equal(director.state_for_stage(5), "contested_extraction", "stage 5 contests extraction")

func test_audio_cues_by_stage(t: TestAssertions) -> void:
	var director = ThreatDirectorScript.new()
	t.assert_true(director.audio_cues_for_stage(1).has("distant_floor_creak"), "stage 1 has subtle sound")
	t.assert_true(director.audio_cues_for_stage(3).has("door_slide_false_route"), "stage 3 has route interference cue")
	t.assert_true(director.audio_cues_for_stage(5).has("close_funeral_wail"), "stage 5 has close threat cue")
