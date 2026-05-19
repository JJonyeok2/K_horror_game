extends RefCounted

const TestAssertions = preload("res://tests/test_assertions.gd")
const ArtifactDefinition = preload("res://scripts/core/artifact_definition.gd")
const InventoryScript = preload("res://scripts/core/inventory.gd")

func run() -> Array[String]:
	var t := TestAssertions.new()
	test_artifact_definition(t)
	test_inventory_adds_items_and_weight(t)
	test_inventory_rejects_overweight_item(t)
	return t.failures

func test_artifact_definition(t: TestAssertions) -> void:
	var bowl := ArtifactDefinition.new("놋 제기", 120, 1.5, 2, ["ancestor_item"])
	t.assert_equal(bowl.display_name, "놋 제기", "artifact stores display name")
	t.assert_true(bowl.has_tag("ancestor_item"), "artifact stores taboo tag")

func test_inventory_adds_items_and_weight(t: TestAssertions) -> void:
	var inv = InventoryScript.new(5.0)
	var bowl = ArtifactDefinition.new("놋 제기", 120, 1.5, 2, [])
	t.assert_true(inv.try_add(bowl), "inventory accepts item under weight limit")
	t.assert_equal(inv.total_value(), 120, "inventory sums value")
	t.assert_equal(inv.total_resentment_gain(), 2, "inventory sums resentment gain")
	t.assert_equal(inv.total_weight(), 1.5, "inventory sums weight")

func test_inventory_rejects_overweight_item(t: TestAssertions) -> void:
	var inv = InventoryScript.new(2.0)
	var chest = ArtifactDefinition.new("나전칠기 함", 700, 3.0, 4, [])
	t.assert_true(not inv.try_add(chest), "inventory rejects item over weight limit")
	t.assert_equal(inv.total_value(), 0, "rejected item does not add value")
