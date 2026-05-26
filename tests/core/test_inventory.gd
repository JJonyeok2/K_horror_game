extends RefCounted

const TestAssertions = preload("res://tests/test_assertions.gd")
const ArtifactDefinition = preload("res://scripts/core/artifact_definition.gd")
const Inventory = preload("res://scripts/core/inventory.gd")

func run() -> Array[String]:
	var t := TestAssertions.new()
	test_artifact_definition(t)
	test_inventory_adds_small_items_to_each_hand(t)
	test_inventory_large_item_uses_both_hands(t)
	test_inventory_rejects_item_without_free_hand_slots(t)
	test_inventory_rejects_null_items(t)
	test_inventory_can_pop_last_item(t)
	return t.failures

func test_artifact_definition(t: TestAssertions) -> void:
	var bowl := ArtifactDefinition.new("bowl", 120, 1.5, 2, ["ancestor_item"], 3)
	t.assert_equal(bowl.display_name, "bowl", "artifact stores display name")
	t.assert_true(bowl.has_tag("ancestor_item"), "artifact stores taboo tag")
	t.assert_equal(bowl.hand_slots, 2, "artifact clamps hand slots to large")

func test_inventory_adds_small_items_to_each_hand(t: TestAssertions) -> void:
	var inv := Inventory.new(5.0)
	var bowl := ArtifactDefinition.new("bowl", 120, 1.5, 2, [], 1)
	var cup := ArtifactDefinition.new("cup", 80, 0.5, 1, [], 1)
	var spoon := ArtifactDefinition.new("spoon", 40, 0.2, 1, [], 1)
	var charm := ArtifactDefinition.new("charm", 60, 0.2, 1, [], 1)
	t.assert_true(inv.try_add(bowl), "inventory accepts first small item")
	t.assert_true(inv.try_add(cup), "inventory accepts second small item")
	t.assert_true(inv.try_add(spoon), "inventory accepts third small item")
	t.assert_true(inv.try_add(charm), "inventory accepts fourth small item")
	t.assert_equal(inv.used_hand_slots(), 4, "four small items fill all inventory slots")
	t.assert_equal(inv.free_hand_slots(), 0, "no free hands remain")
	t.assert_equal(inv.total_value(), 300, "inventory sums values for held items")
	t.assert_equal(inv.total_resentment_gain(), 5, "inventory sums resentment for held items")
	t.assert_true(abs(inv.total_weight() - 2.4) < 0.001, "inventory sums weight")

func test_inventory_large_item_uses_both_hands(t: TestAssertions) -> void:
	var inv := Inventory.new(2.0)
	var chest := ArtifactDefinition.new("chest", 700, 3.0, 4, [], 2)
	var accepted := inv.try_add(chest)
	t.assert_true(accepted, "inventory accepts large item even when heavy")
	t.assert_equal(inv.used_hand_slots(), 2, "large item fills two inventory slots")
	t.assert_equal(inv.free_hand_slots(), 2, "large item leaves two free slots")
	t.assert_equal(inv.total_weight(), 3.0, "inventory still tracks carried weight")

func test_inventory_rejects_item_without_free_hand_slots(t: TestAssertions) -> void:
	var inv := Inventory.new(10.0)
	var first := ArtifactDefinition.new("first", 10, 1.0, 0, [], 1)
	var second := ArtifactDefinition.new("second", 20, 1.0, 0, [], 1)
	var third := ArtifactDefinition.new("third", 30, 1.0, 0, [], 1)
	var fourth := ArtifactDefinition.new("fourth", 40, 1.0, 0, [], 1)
	var fifth := ArtifactDefinition.new("fifth", 50, 1.0, 0, [], 1)
	t.assert_true(inv.try_add(first), "inventory accepts first hand item")
	t.assert_true(inv.try_add(second), "inventory accepts second hand item")
	t.assert_true(inv.try_add(third), "inventory accepts third hand item")
	t.assert_true(inv.try_add(fourth), "inventory accepts fourth hand item")
	t.assert_true(not inv.try_add(fifth), "inventory rejects item when four slots are full")
	t.assert_equal(inv.total_value(), 100, "rejected item does not add value")

func test_inventory_rejects_null_items(t: TestAssertions) -> void:
	var inv := Inventory.new(10.0)
	t.assert_true(not inv.try_add(null), "inventory safely rejects null items")
	t.assert_equal(inv.items.size(), 0, "null item is not appended")

func test_inventory_can_pop_last_item(t: TestAssertions) -> void:
	var inv := Inventory.new(10.0)
	var first := ArtifactDefinition.new("first", 10, 1.0, 0, [], 1)
	var second := ArtifactDefinition.new("second", 20, 1.0, 0, [], 1)
	inv.try_add(first)
	inv.try_add(second)
	var removed := inv.pop_last_item()
	t.assert_equal(removed.display_name, "second", "inventory drops most recent item first")
	t.assert_equal(inv.used_hand_slots(), 1, "dropping frees one hand slot")
	t.assert_equal(inv.total_value(), 10, "dropped item no longer counts")
