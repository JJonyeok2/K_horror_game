extends SceneTree

const TESTS := [
	"res://tests/core/test_quota_tracker.gd",
	"res://tests/core/test_inventory.gd",
	"res://tests/core/test_resentment_tracker.gd",
	"res://tests/core/test_threat_director.gd",
	"res://tests/runtime/test_scene_boot.gd",
]

func _initialize() -> void:
	var failures: Array[String] = []
	for path in TESTS:
		var test_script: Script = load(path)
		var test_instance = test_script.new()
		var result: Array[String] = test_instance.run()
		for failure in result:
			failures.append("%s: %s" % [path, failure])

	if failures.is_empty():
		print("PASS: %d test files" % TESTS.size())
		quit(0)
	else:
		for failure in failures:
			push_error(failure)
		print("FAIL: %d failure(s)" % failures.size())
		quit(1)
