extends RefCounted

const TestAssertions = preload("res://tests/test_assertions.gd")
const JonggaEstateBuilderScript = preload("res://scripts/maps/jongga_estate_builder.gd")

func run() -> Array[String]:
	var t := TestAssertions.new()
	test_map_builds_named_rooms(t)
	test_map_builds_wall_and_doorway_structure(t)
	test_map_builds_fallthrough_guard(t)
	return t.failures

func test_map_builds_named_rooms(t: TestAssertions) -> void:
	var main := Node.new()
	var map: Node3D = JonggaEstateBuilderScript.new()
	Engine.get_main_loop().root.add_child(map)
	map.build(main)
	for room_name in ["대문", "바깥마당", "사랑채", "안채", "곳간", "사당"]:
		t.assert_true(map.find_child(room_name, true, false) != null, "%s section exists" % room_name)
	map.free()
	main.free()

func test_map_builds_wall_and_doorway_structure(t: TestAssertions) -> void:
	var main := Node.new()
	var map: Node3D = JonggaEstateBuilderScript.new()
	Engine.get_main_loop().root.add_child(map)
	map.build(main)
	var walls: Node = map.get_node_or_null("Walls")
	var doorways: Node = map.get_node_or_null("Doorways")
	t.assert_true(walls != null and walls.get_child_count() >= 18, "map has enough wall segments to read as rooms")
	t.assert_true(doorways != null and doorways.get_child_count() >= 5, "map has named doorways between key rooms")
	map.free()
	main.free()

func test_map_builds_fallthrough_guard(t: TestAssertions) -> void:
	var main := Node.new()
	var map: Node3D = JonggaEstateBuilderScript.new()
	Engine.get_main_loop().root.add_child(map)
	map.build(main)
	t.assert_true(map.find_child("낙하방지바닥", true, false) != null, "map has hidden fallthrough guard collider")
	map.free()
	main.free()
