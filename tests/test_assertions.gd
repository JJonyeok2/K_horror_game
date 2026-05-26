extends RefCounted
class_name TestAssertions

var failures: Array[String] = []

func assert_equal(actual: Variant, expected: Variant, message: String) -> void:
	if actual != expected:
		failures.append("%s | expected=%s actual=%s" % [message, str(expected), str(actual)])

func assert_true(value: bool, message: String) -> void:
	if not value:
		failures.append(message)
