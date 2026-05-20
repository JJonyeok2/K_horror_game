extends SceneTree

const MainScene := preload("res://scenes/Main.tscn")
const BongoVanPlanScript := preload("res://scripts/maps/bongo_van_plan.gd")

var _failed := false

func _initialize() -> void:
	var main: Node = MainScene.instantiate()
	root.add_child(main)
	for _i in range(90):
		await physics_frame
	await _travel_to_estate(main)
	if _failed:
		quit(1)
		return

	_assert_route_floor_coverage(main)
	if _failed:
		quit(1)
		return
	_assert_wall_and_building_contract(main)
	if _failed:
		quit(1)
		return
	print("ESTATE_STRUCTURE_SMOKE: route floors, walls, and buildings present")
	quit(0)

func _travel_to_estate(main: Node) -> void:
	if main.has_method("travel_to_retrieval_map"):
		main.call("travel_to_retrieval_map", "jongga_estate")
	for _i in range(90):
		if str(main.get("current_map_id")) == "jongga_estate":
			await physics_frame
			return
		await physics_frame
	_fail("Estate route structure test could not travel to the estate map")

func _assert_route_floor_coverage(main: Node) -> void:
	var samples := {
		"van interior": BongoVanPlanScript.PLAYER_START_POSITION + Vector3(0.0, 8.0, 0.0),
		"van exit": Vector3(0.0, 8.0, BongoVanPlanScript.PLAYER_START_POSITION.z - 3.0),
		"deep forest approach": Vector3(0.0, 8.0, 180.0),
		"mid forest approach": Vector3(0.0, 8.0, 90.0),
		"approach road": Vector3(0.0, 8.0, 4.0),
		"gate threshold": Vector3(0.0, 8.0, -12.5),
		"courtyard center": Vector3(0.0, 8.0, -40.0),
		"left side route": Vector3(-24.0, 8.0, -42.0),
		"building front": Vector3(0.0, 8.0, -69.0),
		"main house interior": Vector3(0.0, 8.0, -86.0),
		"storehouse loop": Vector3(-25.0, 8.0, -101.0),
		"back kitchen connector": Vector3(-8.0, 8.0, -108.0),
		"backyard loop": Vector3(25.0, 8.0, -108.0),
		"outhouse yard": Vector3(36.0, 8.0, -107.0),
		"shrine approach": Vector3(0.0, 8.0, -119.0),
		"shrine deep": Vector3(0.0, 8.0, -136.0),
	}
	for label: String in samples.keys():
		if not _has_floor_below(main, samples[label]):
			_fail("No floor under route sample: %s at %s" % [label, samples[label]])
			return

func _assert_wall_and_building_contract(main: Node) -> void:
	var required_static := [
		"EstateContinuousGround",
		"CourtyardFrontLeftGuideWall",
		"CourtyardFrontRightGuideWall",
		"MainHouseFrontWallLeft",
		"MainHouseFrontWallRight",
		"MainHouseBackWall",
		"MainHouseLeftOuterWall",
		"MainHouseRightOuterWall",
		"MainHouseCenterPartition",
		"MainHouseRearPartitionLeft",
		"MainHouseRearPartitionRight",
		"MainHouseRoof",
		"MainHouseDoorThreshold",
		"StorehouseShedBackWall",
		"StorehouseShedLeftWall",
		"StorehouseShedRightWall",
		"StorehouseShedRoof",
		"BackKitchenLeftWall",
		"BackKitchenRightWall",
		"BackKitchenRearGateLeft",
		"BackKitchenRearGateRight",
		"BackyardLoopBaffleA",
		"BackyardLoopBaffleB",
		"StorehouseLoopBaffleA",
		"StorehouseLoopBaffleB",
		"ShrineApproachLeftWall",
		"ShrineApproachRightWall",
	]
	for label in required_static:
		var node := main.find_child(label, true, false)
		if node == null:
			_fail("Missing structural node: %s" % label)
			return
		if node.get_class() != "StaticBody3D":
			_fail("%s is not StaticBody3D: %s" % [label, node.get_class()])
			return
		if node.find_child("CollisionShape3D", true, false) == null:
			_fail("%s has no collision shape" % label)
			return

	var required_visual := [
		"MainHousePaperDoorA",
		"MainHousePaperDoorB",
		"MainHousePaperDoorC",
	]
	for label in required_visual:
		if main.find_child(label, true, false) == null:
			_fail("Missing readable building visual: %s" % label)
			return

func _has_floor_below(main: Node, origin: Vector3) -> bool:
	var space_state := main.get_viewport().world_3d.direct_space_state
	var query := PhysicsRayQueryParameters3D.create(origin, origin + Vector3(0.0, -12.0, 0.0))
	query.collide_with_areas = false
	query.collide_with_bodies = true
	var result := space_state.intersect_ray(query)
	if result.is_empty():
		return false
	var collider := result.get("collider") as Node
	if collider == null:
		return false
	return collider.get_class() == "StaticBody3D"

func _fail(message: String) -> void:
	_failed = true
	push_error(message)
