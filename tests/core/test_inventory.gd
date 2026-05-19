extends RefCounted

const TestAssertions = preload("res://tests/test_assertions.gd")
const ArtifactDefinition = preload("res://scripts/core/artifact_definition.gd")
const InventoryScript = preload("res://scripts/core/inventory.gd")

func run() -> Array[String]:
	var t := TestAssertions.new()
	test_artifact_definition(t)
	test_inventory_adds_items_to_slots(t)
	test_inventory_rejects_when_slots_are_full(t)
	return t.failures

func test_artifact_definition(t: TestAssertions) -> void:
	var bowl := ArtifactDefinition.new("놋 제기", 120, 1.5, 2, ["ancestor_item"])
	t.assert_equal(bowl.display_name, "놋 제기", "artifact stores display name")
	t.assert_true(bowl.has_tag("ancestor_item"), "artifact stores taboo tag")

func test_inventory_adds_items_to_slots(t: TestAssertions) -> void:
	var inv = InventoryScript.new(2)
	var bowl = ArtifactDefinition.new("놋 제기", 120, 1.5, 2, [])
	t.assert_true(inv.try_add(bowl), "inventory accepts item while a slot is open")
	t.assert_equal(inv.used_slots(), 1, "inventory tracks used slots")
	t.assert_equal(inv.max_slots, 2, "inventory exposes slot capacity")
	t.assert_equal(inv.total_value(), 120, "inventory sums value")
	t.assert_equal(inv.total_resentment_gain(), 2, "inventory sums resentment gain")

func test_inventory_rejects_when_slots_are_full(t: TestAssertions) -> void:
	var inv = InventoryScript.new(2)
	var bowl = ArtifactDefinition.new("놋 제기", 120, 1.5, 2, [])
	var scroll = ArtifactDefinition.new("서예 족자", 280, 1.0, 3, [])
	var chest = ArtifactDefinition.new("나전칠기 함", 700, 3.0, 4, [])
	t.assert_true(inv.try_add(bowl), "inventory accepts first item")
	t.assert_true(inv.try_add(scroll), "inventory accepts second item")
	t.assert_true(not inv.try_add(chest), "inventory rejects third item")
	t.assert_equal(inv.total_value(), 400, "rejected item does not add value")
