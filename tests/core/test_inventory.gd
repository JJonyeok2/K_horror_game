extends RefCounted

const TestAssertions = preload("res://tests/test_assertions.gd")
const ArtifactDefinition = preload("res://scripts/core/artifact_definition.gd")

func run() -> Array[String]:
	var t := TestAssertions.new()
	var bowl := ArtifactDefinition.new("놋 제기", 120, 1.5, 2, ["ancestor_item"])
	t.assert_equal(bowl.display_name, "놋 제기", "artifact stores display name")
	t.assert_equal(bowl.value, 120, "artifact stores value")
	t.assert_equal(bowl.weight, 1.5, "artifact stores weight")
	t.assert_equal(bowl.resentment_gain, 2, "artifact stores resentment gain")
	t.assert_true(bowl.has_tag("ancestor_item"), "artifact stores taboo tag")
	return t.failures
