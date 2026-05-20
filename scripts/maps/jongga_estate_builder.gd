extends Node3D
class_name JonggaEstateBuilder

const ArtifactScene := preload("res://scenes/props/Artifact.tscn")
const ExtractionScene := preload("res://scenes/zones/ExtractionZone.tscn")
const EstateGateScript := preload("res://scripts/maps/estate_gate.gd")
const GateLeafScript := preload("res://scripts/maps/gate_leaf.gd")
const EstateLayoutPlanScript := preload("res://scripts/maps/estate_layout_plan.gd")
const BongoVanPlanScript := preload("res://scripts/maps/bongo_van_plan.gd")
const VisualPaletteScript := preload("res://scripts/maps/visual_palette.gd")
const StatefulInteractableScript := preload("res://scripts/interactions/stateful_interactable.gd")
const BongoSettlementStationScript := preload("res://scripts/interactions/bongo_settlement_station.gd")
const BongoDepartureButtonScript := preload("res://scripts/interactions/bongo_departure_button.gd")
const BongoMapSelectorScript := preload("res://scripts/interactions/bongo_map_selector.gd")
const BongoSettlementMapSelectorScript := preload("res://scripts/interactions/bongo_settlement_map_selector.gd")
const BongoHubRearDoorLockScript := preload("res://scripts/interactions/bongo_hub_rear_door_lock.gd")
const BongoQuotaMonitorScript := preload("res://scripts/interactions/bongo_quota_monitor.gd")
const PerformanceSettingsScript := preload("res://scripts/game/performance_settings.gd")

const SPAWN_POINT := BongoVanPlanScript.PLAYER_START_POSITION
const GATE_NODE_NAME := "OuterEstateGate"
const GATE_POSITION := Vector3(0.0, 0.0, -12.0)
const ESTATE_BOX_SNAP: float = 0.05
const ESTATE_WALL_MIN_HEIGHT: float = 5.6
const ESTATE_ROOF_MIN_CENTER_Y: float = 5.45

const MATERIAL_SURFACES := {
	"packed_earth": "packed_courtyard_dirt",
	"mud": "wet_moss",
	"stone": "stone_threshold",
	"dark_stone": "dark_wood",
	"old_plaster": "aged_plaster",
	"aged_wood": "dark_wood",
	"black_wood": "dark_wood",
	"roof_tile": "tile_roof",
	"straw": "straw_toilet_wood",
	"wet_moss": "wet_moss",
	"shrine_red": "ritual_red",
	"shadow": "wet_moss",
	"van_paint": "van_paint",
	"glass": "glass",
	"metal": "metal",
}

const EXTERNAL_MATERIALS_BY_KEY := {
	"packed_earth": {"asset": "Ground103", "uv_scale": 18.0},
	"mud": {"asset": "Ground037", "uv_scale": 18.0},
	"wet_moss": {"asset": "Grass007", "uv_scale": 12.0},
	"dead_tree": {"asset": "Bark014", "uv_scale": 4.0},
	"aged_wood": {"asset": "Wood095", "uv_scale": 5.0},
	"black_wood": {"asset": "Wood095", "uv_scale": 5.0},
	"straw": {"asset": "Wood095", "uv_scale": 7.0},
	"stone": {"asset": "Rock064", "uv_scale": 7.0},
	"dark_stone": {"asset": "PavingStones150", "uv_scale": 8.0},
	"old_plaster": {"asset": "Plaster001", "uv_scale": 8.0},
	"roof_tile": {"asset": "PavingStones150", "uv_scale": 8.0},
}

var _material_cache: Dictionary = {}
var _texture_cache: Dictionary = {}

func build(main: Node) -> void:
	_create_continuous_ground_and_route_stitches()
	_create_estate_navigation_region()
	_create_planned_floors()
	_create_planned_walls()
	_create_gate()
	_create_side_passage_risk_trigger(main)
	_create_planned_landmarks()
	_create_route_guidance_walls()
	_create_main_house_building()
	_create_approach_forest()
	_create_korean_ghost_haunts()
	_create_courtyard_density_props()
	_create_secondary_roofed_buildings()
	_create_bongo_van()
	_create_settlement_office_map()
	_spawn_planned_artifacts(main)
	_create_extraction_zone()

func _create_continuous_ground_and_route_stitches() -> void:
	_create_floor("EstateContinuousGround", Vector3(0.0, -0.04, 62.0), Vector3(82.0, 0.12, 426.0), _fallback_color("packed_earth"), "packed_earth")
	_create_floor("LongForestApproachRoad", Vector3(0.0, -0.03, 130.0), Vector3(8.5, 0.16, 288.0), _fallback_color("mud"), "mud")
	var stitches := [
		{"name": "GateToCourtyardFloorStitch", "position": Vector3(0.0, 0.025, -16.0), "size": Vector3(12.0, 0.16, 2.4), "material_key": "packed_earth"},
		{"name": "CourtyardToBuildingFloorStitch", "position": Vector3(0.0, 0.025, -64.0), "size": Vector3(18.0, 0.16, 3.0), "material_key": "mud"},
		{"name": "MainHouseToBackKitchenFloorStitch", "position": Vector3(-4.0, 0.025, -99.0), "size": Vector3(28.0, 0.16, 4.0), "material_key": "packed_earth"},
		{"name": "BackKitchenToShrineFloorStitch", "position": Vector3(0.0, 0.025, -110.5), "size": Vector3(22.0, 0.16, 8.0), "material_key": "dark_stone"},
		{"name": "BackyardToOuthouseFloorStitch", "position": Vector3(31.5, 0.025, -107.0), "size": Vector3(12.0, 0.16, 14.0), "material_key": "mud"},
		{"name": "LeftLoopFloorStitch", "position": Vector3(-25.0, 0.025, -68.0), "size": Vector3(13.0, 0.16, 12.0), "material_key": "mud"},
	]
	for data: Dictionary in stitches:
		_create_floor(str(data["name"]), data["position"], data["size"], _fallback_color(str(data["material_key"])), str(data["material_key"]))

func _create_planned_floors() -> void:
	for floor_data: Dictionary in EstateLayoutPlanScript.get_floor_zones():
		var label := str(floor_data["name"])
		var position: Vector3 = floor_data["position"]
		var size: Vector3 = floor_data["size"]
		var material_key := str(floor_data["material_key"])
		if label == "ToenmaruPorch":
			position.y = 0.18
			size.y = 0.24
		if label == "BackKitchenYard":
			size.z = 21.0
		_create_floor(label, position, size, _fallback_color(material_key), material_key)

func _create_planned_walls() -> void:
	for wall_data: Dictionary in EstateLayoutPlanScript.get_wall_segments():
		var label := str(wall_data["name"])
		var position: Vector3 = wall_data["position"]
		var size: Vector3 = wall_data["size"]
		var material_key := str(wall_data["material_key"])
		if material_key == "old_plaster" or material_key == "shrine_red":
			var raised_size := _heightened_size(size, 4.2)
			position = _keep_bottom_position(position, size, raised_size)
			size = raised_size
		if label == "ApproachWallLeft":
			_create_box("ApproachWallLeftMiddleSeal", position, size, _fallback_color(material_key), material_key)
			continue
		if label == "ApproachWallRight":
			_create_box("ApproachWallRightMiddleSeal", position, size, _fallback_color(material_key), material_key)
			continue
		_create_box(label, position, size, _fallback_color(material_key), material_key)

func _create_route_guidance_walls() -> void:
	_create_tall_wall_box("CourtyardFrontLeftGuideWall", Vector3(-17.5, 1.35, -16.7), Vector3(21.0, 2.7, 0.55), "old_plaster")
	_create_tall_wall_box("CourtyardFrontRightGuideWall", Vector3(17.5, 1.35, -16.7), Vector3(21.0, 2.7, 0.55), "old_plaster")
	_create_tall_wall_box("CourtyardLeftInnerReturnWall", Vector3(-24.8, 1.25, -29.0), Vector3(0.55, 2.5, 16.0), "old_plaster")
	_create_tall_wall_box("CourtyardRightInnerReturnWall", Vector3(24.8, 1.25, -31.0), Vector3(0.55, 2.5, 20.0), "old_plaster")
	_create_tall_wall_box("CourtyardBuildingApproachLeft", Vector3(-12.5, 1.2, -64.0), Vector3(8.0, 2.4, 0.45), "old_plaster")
	_create_tall_wall_box("CourtyardBuildingApproachRight", Vector3(12.5, 1.2, -64.0), Vector3(8.0, 2.4, 0.45), "old_plaster")

	_create_tall_wall_box("SingleGateLeftReturnWall", Vector3(-7.2, 1.7, -11.2), Vector3(2.4, 3.4, 1.1), "old_plaster")
	_create_tall_wall_box("SingleGateRightReturnWall", Vector3(7.2, 1.7, -11.2), Vector3(2.4, 3.4, 1.1), "old_plaster")
	_create_tall_wall_box("GateLeftCourtyardSideSealWall", Vector3(-10.6, 1.8, -14.45), Vector3(9.2, 3.6, 4.9), "old_plaster")
	_create_tall_wall_box("GateRightCourtyardSideSealWall", Vector3(10.6, 1.8, -14.45), Vector3(9.2, 3.6, 4.9), "old_plaster")
	_create_tall_wall_box("CourtyardFrontLeftOuterSealWall", Vector3(-32.15, 1.35, -16.7), Vector3(8.7, 2.7, 0.55), "old_plaster")
	_create_tall_wall_box("CourtyardFrontRightOuterSealWall", Vector3(32.15, 1.35, -16.7), Vector3(8.7, 2.7, 0.55), "old_plaster")
	_create_box("SidePassageBoardedGate", Vector3(-8.7, 1.85, -10.8), Vector3(3.3, 3.7, 0.55), _fallback_color("aged_wood"), "aged_wood")
	_create_box("SidePassageBrushScreenA", Vector3(-9.4, 1.35, -5.4), Vector3(3.8, 2.7, 5.2), _fallback_color("wet_moss"), "mud")
	_create_box("SidePassageBrushScreenB", Vector3(-12.0, 1.45, -14.8), Vector3(3.8, 2.9, 4.4), _fallback_color("wet_moss"), "mud")

	_create_tall_wall_box("CourtyardSightlineScreenA", Vector3(-8.5, 1.65, -37.5), Vector3(17.0, 3.3, 0.55), "old_plaster")
	_create_tall_wall_box("CourtyardSightlineScreenB", Vector3(8.5, 1.65, -49.0), Vector3(17.0, 3.3, 0.55), "old_plaster")
	_create_tall_wall_box("CourtyardPathBaffleA", Vector3(-2.8, 1.6, -32.5), Vector3(0.55, 3.2, 12.0), "old_plaster")
	_create_tall_wall_box("CourtyardPathBaffleB", Vector3(4.2, 1.6, -57.0), Vector3(0.55, 3.2, 13.0), "old_plaster")

	_create_tall_wall_box("BackKitchenLeftWall", Vector3(-20.4, 1.25, -108.0), Vector3(0.6, 2.5, 21.0), "old_plaster")
	_create_tall_wall_box("BackKitchenRightWall", Vector3(4.4, 1.25, -108.0), Vector3(0.6, 2.5, 21.0), "old_plaster")
	_create_tall_wall_box("BackKitchenRearGateLeft", Vector3(-14.0, 1.25, -119.2), Vector3(10.5, 2.5, 0.55), "old_plaster")
	_create_tall_wall_box("BackKitchenRearGateRight", Vector3(1.5, 1.25, -119.2), Vector3(5.7, 2.5, 0.55), "old_plaster")

	_create_tall_wall_box("BackyardLoopBaffleA", Vector3(25.0, 1.0, -89.0), Vector3(12.0, 2.0, 0.45), "old_plaster")
	_create_tall_wall_box("BackyardLoopBaffleB", Vector3(27.0, 1.0, -105.5), Vector3(12.0, 2.0, 0.45), "old_plaster")
	_create_tall_wall_box("StorehouseLoopBaffleA", Vector3(-26.0, 1.0, -82.5), Vector3(11.0, 2.0, 0.45), "old_plaster")
	_create_tall_wall_box("StorehouseLoopBaffleB", Vector3(-24.0, 1.0, -97.5), Vector3(11.0, 2.0, 0.45), "old_plaster")
	_create_tall_wall_box("RouteBranchSealLeftLoop", Vector3(-16.4, 1.6, -88.5), Vector3(0.75, 3.2, 9.5), "old_plaster")
	_create_tall_wall_box("RouteBranchSealRightLoop", Vector3(16.4, 1.6, -89.0), Vector3(0.75, 3.2, 9.5), "old_plaster")
	_create_tall_wall_box("StorehouseBrokenGapSeal", Vector3(-18.0, 1.45, -94.0), Vector3(0.85, 2.9, 6.4), "old_plaster")
	_create_tall_wall_box("BackyardBrokenGapSeal", Vector3(17.0, 1.45, -96.0), Vector3(0.85, 2.9, 6.6), "old_plaster")

	_create_tall_wall_box("ShrineApproachLeftWall", Vector3(-9.5, 1.25, -119.0), Vector3(0.65, 2.5, 24.0), "shrine_red")
	_create_tall_wall_box("ShrineApproachRightWall", Vector3(9.5, 1.25, -119.0), Vector3(0.65, 2.5, 24.0), "shrine_red")

func _create_approach_forest() -> void:
	for i in range(17):
		var z := 260.0 - float(i) * 16.5
		var left_height := 8.5 + float(i % 4) * 0.9
		var right_height := 9.0 + float((i + 2) % 4) * 0.8
		_create_forest_tree("ApproachForestTreeLeft%02d" % [i + 1], Vector3(-7.2 - float(i % 3) * 1.1, 0.0, z), left_height)
		_create_forest_tree("ApproachForestTreeRight%02d" % [i + 1], Vector3(7.2 + float((i + 1) % 3) * 1.05, 0.0, z - 7.5), right_height)
		if i % 2 == 0:
			_create_box("ApproachBrushCluster%02d" % [i + 1], Vector3(-3.7, 0.55, z - 3.0), Vector3(1.9, 0.85, 2.2), _fallback_color("wet_moss"), "mud")
			_create_box("ApproachBrushClusterRight%02d" % [i + 1], Vector3(3.7, 0.55, z - 8.0), Vector3(1.8, 0.82, 2.1), _fallback_color("wet_moss"), "mud")
		if i % 4 == 0:
			var canopy_x := -6.6 if i % 8 == 0 else 6.6
			_create_box("DeepForestCanopy%02d" % [i + 1], Vector3(canopy_x, 7.2, z - 5.0), Vector3(3.2, 0.45, 8.6), _fallback_color("dead_tree"), "dead_tree")

	_create_box("DeepForestCanopyA", Vector3(-6.8, 7.45, 188.0), Vector3(3.4, 0.5, 11.0), _fallback_color("dead_tree"), "dead_tree")
	_create_box("DeepForestCanopyB", Vector3(6.8, 7.25, 92.0), Vector3(3.4, 0.48, 10.0), _fallback_color("dead_tree"), "dead_tree")
	_create_box("ApproachLowRootStepA", Vector3(-3.35, 0.25, 210.0), Vector3(1.8, 0.1, 0.42), _fallback_color("dead_tree"), "dead_tree")
	_create_box("ApproachLowRootStepB", Vector3(3.15, 0.25, 128.0), Vector3(1.8, 0.1, 0.38), _fallback_color("dead_tree"), "dead_tree")
	_create_box("ApproachBrokenStoneStepA", Vector3(-2.8, 0.24, 174.0), Vector3(1.15, 0.1, 0.75), _fallback_color("stone"), "stone")
	_create_box("ApproachBrokenStoneStepB", Vector3(2.6, 0.24, 48.0), Vector3(0.9, 0.1, 0.72), _fallback_color("stone"), "stone")
	_create_box("ApproachBrushClusterA", Vector3(-3.2, 0.55, 235.0), Vector3(1.9, 0.85, 2.1), _fallback_color("wet_moss"), "mud")
	_create_box("ApproachBrushClusterB", Vector3(3.1, 0.55, 72.0), Vector3(1.9, 0.82, 2.2), _fallback_color("wet_moss"), "mud")
	_create_box("ApproachToriiLikeLeftPost", Vector3(-3.9, 1.5, -7.8), Vector3(0.38, 3.0, 0.38), _fallback_color("black_wood"), "black_wood")
	_create_box("ApproachToriiLikeRightPost", Vector3(3.9, 1.5, -7.8), Vector3(0.38, 3.0, 0.38), _fallback_color("black_wood"), "black_wood")
	_create_box("ApproachToriiLikeBeam", Vector3(0.0, 2.9, -7.8), Vector3(8.5, 0.34, 0.45), _fallback_color("black_wood"), "black_wood")
	_create_jangseung("JangseungLeft01", Vector3(-4.9, 0.0, 18.0), "天下大將軍")
	_create_jangseung("JangseungRight01", Vector3(4.9, 0.0, 18.0), "地下女將軍")
	_create_jangseung("JangseungLeft02", Vector3(-5.4, 0.0, 104.0), "禁入")
	_create_jangseung("JangseungRight02", Vector3(5.2, 0.0, 104.0), "回頭")
	_create_box("HiddenPathScreeningBrushA", Vector3(-15.7, 0.95, -24.0), Vector3(5.0, 1.7, 7.0), _fallback_color("wet_moss"), "mud")
	_create_box("HiddenPathScreeningBrushB", Vector3(-14.6, 1.15, -31.0), Vector3(4.5, 2.0, 5.5), _fallback_color("wet_moss"), "mud")
	_create_box("HiddenPathSightlineFenceA", Vector3(-12.7, 1.1, -27.5), Vector3(0.45, 2.2, 9.5), _fallback_color("aged_wood"), "aged_wood")
	_create_return_route_obstacles()

func _create_return_route_obstacles() -> void:
	_create_box("ReturnRouteBaffleLeft01", Vector3(-1.9, 0.74, 218.0), Vector3(5.0, 1.2, 0.55), _fallback_color("wet_moss"), "mud")
	_create_box("ReturnRouteBaffleRight01", Vector3(1.9, 0.74, 171.0), Vector3(5.0, 1.2, 0.55), _fallback_color("wet_moss"), "mud")
	_create_box("ReturnRouteBaffleLeft02", Vector3(-1.85, 0.74, 116.0), Vector3(5.1, 1.2, 0.55), _fallback_color("wet_moss"), "mud")

	var ramp_a := _create_box("ReturnRouteHillRamp01", Vector3(0.0, 0.16, 234.0), Vector3(7.6, 0.22, 17.0), _fallback_color("mud"), "mud")
	ramp_a.rotation_degrees.x = -3.5
	var ramp_b := _create_box("ReturnRouteHillRamp02", Vector3(0.0, 0.18, 151.0), Vector3(7.6, 0.24, 18.0), _fallback_color("mud"), "mud")
	ramp_b.rotation_degrees.x = 4.0
	var ramp_c := _create_box("ReturnRouteHillRamp03", Vector3(0.0, 0.17, 61.0), Vector3(7.6, 0.22, 16.0), _fallback_color("mud"), "mud")
	ramp_c.rotation_degrees.x = -3.0

func _create_korean_ghost_haunts() -> void:
	_create_ghost_haunt("GhostHauntDokkaebi", Vector3(-5.7, 0.0, 11.5), Color(0.28, 0.08, 0.05), true)
	_create_ghost_haunt("GhostHauntSangbok", Vector3(-8.8, 0.0, -78.0), Color(0.08, 0.075, 0.07), false)
	_create_ghost_haunt("GhostHauntDalgyalGwisin", Vector3(8.8, 0.0, -118.0), Color(0.78, 0.76, 0.66), false)
	_create_ghost_haunt("GhostHauntEoduksini", Vector3(-16.0, 0.0, -104.0), Color(0.025, 0.025, 0.035), false)
	_create_ghost_haunt("GhostHauntChanggwi", Vector3(5.8, 0.0, -102.0), Color(0.11, 0.085, 0.07), false)
	_create_ghost_haunt("GhostHauntJangsanbeom", Vector3(2.5, 0.0, -131.0), Color(0.76, 0.74, 0.68), false)
	_create_ghost_haunt("GhostHauntWellSpirit", Vector3(-15.0, 0.0, -42.5), Color(0.08, 0.15, 0.15), false)

func _create_estate_navigation_region() -> void:
	var region := NavigationRegion3D.new()
	region.name = "EstateNavigationRegion"
	add_child(region)
	var nav_mesh := NavigationMesh.new()
	nav_mesh.agent_radius = 0.35
	nav_mesh.agent_height = 1.8
	nav_mesh.agent_max_climb = 0.45
	nav_mesh.agent_max_slope = 48.0
	var vertices := PackedVector3Array([
		Vector3(-38.0, 0.02, 278.0),
		Vector3(38.0, 0.02, 278.0),
		Vector3(38.0, 0.02, -150.0),
		Vector3(-38.0, 0.02, -150.0),
	])
	nav_mesh.set_vertices(vertices)
	nav_mesh.add_polygon(PackedInt32Array([0, 1, 2, 3]))
	region.navigation_mesh = nav_mesh

func _create_ghost_haunt(label: String, position: Vector3, body_color: Color, add_horns: bool) -> void:
	var root := Node3D.new()
	root.name = label
	add_child(root)
	root.global_position = position

	_add_visual_box(root, "%sBody" % label, Vector3(0.0, 1.0, 0.0), Vector3(0.55, 1.8, 0.22), body_color, "")
	_add_visual_box(root, "%sFace" % label, Vector3(0.0, 1.75, -0.16), Vector3(0.42, 0.48, 0.05), Color(0.78, 0.76, 0.65), "")
	if add_horns:
		_add_visual_box(root, "%sHornLeft" % label, Vector3(-0.22, 2.12, -0.02), Vector3(0.14, 0.34, 0.12), Color(0.72, 0.62, 0.36), "")
		_add_visual_box(root, "%sHornRight" % label, Vector3(0.22, 2.12, -0.02), Vector3(0.14, 0.34, 0.12), Color(0.72, 0.62, 0.36), "")
		_add_visual_box(root, "%sClub" % label, Vector3(0.52, 0.96, 0.0), Vector3(0.16, 1.2, 0.16), _fallback_color("dead_tree"), "dead_tree")

func _create_jangseung(label: String, position: Vector3, _text_hint: String) -> void:
	_create_cylinder(label, Vector3(position.x, 2.1, position.z), 0.26, 4.2, _fallback_color("dead_tree"), "dead_tree")
	_create_box("%sFace" % label, Vector3(position.x, 3.35, position.z - 0.22), Vector3(0.55, 0.72, 0.08), Color(0.72, 0.62, 0.44), "straw")
	_create_box("%sHat" % label, Vector3(position.x, 4.42, position.z), Vector3(0.86, 0.25, 0.62), _fallback_color("black_wood"), "black_wood")
	_create_box("%sMouth" % label, Vector3(position.x, 3.15, position.z - 0.28), Vector3(0.34, 0.06, 0.05), Color(0.08, 0.035, 0.025), "black_wood")
	_create_box("%sTextPlate" % label, Vector3(position.x, 2.35, position.z - 0.28), Vector3(0.42, 0.9, 0.04), Color(0.18, 0.09, 0.05), "black_wood")

func _create_forest_tree(label: String, position: Vector3, height: float) -> void:
	var base_y := 0.17
	var trunk_center := Vector3(position.x, base_y + height * 0.5, position.z)
	_create_cylinder(label, trunk_center, 0.32, height, _fallback_color("dead_tree"), "dead_tree")
	_create_box("%sBranchA" % label, trunk_center + Vector3(0.62, height * 0.2, 0.15), Vector3(1.8, 0.16, 0.22), _fallback_color("dead_tree"), "dead_tree")
	_create_box("%sBranchB" % label, trunk_center + Vector3(-0.55, height * 0.32, -0.25), Vector3(1.5, 0.14, 0.2), _fallback_color("dead_tree"), "dead_tree")
	_create_box("%sCanopy" % label, trunk_center + Vector3(0.0, height * 0.34, 0.0), Vector3(2.4, 1.4, 2.2), _fallback_color("wet_moss"), "mud")
	_create_box("%sBrush" % label, Vector3(position.x, 0.62, position.z), Vector3(1.8, 0.9, 1.8), _fallback_color("wet_moss"), "mud")

func _create_courtyard_density_props() -> void:
	var jar_positions := [
		Vector3(-21.5, 0.48, -30.5), Vector3(-19.8, 0.48, -31.6), Vector3(-18.0, 0.48, -30.2),
		Vector3(19.0, 0.48, -38.5), Vector3(20.6, 0.48, -39.8), Vector3(22.4, 0.48, -37.6),
		Vector3(-17.5, 0.48, -52.0), Vector3(-15.6, 0.48, -53.2), Vector3(15.6, 0.48, -51.5),
		Vector3(17.5, 0.48, -53.0),
	]
	for i in range(jar_positions.size()):
		_create_cylinder("CourtyardClutterJar%02d" % [i + 1], jar_positions[i], 0.34 + float(i % 3) * 0.06, 0.72, Color(0.28, 0.14, 0.075), "clay_pot")

	_create_box("CourtyardLaundryPost01", Vector3(-23.5, 1.15, -43.5), Vector3(0.2, 2.3, 0.2), _fallback_color("aged_wood"), "aged_wood")
	_create_box("CourtyardLaundryPost02", Vector3(-17.5, 1.15, -43.5), Vector3(0.2, 2.3, 0.2), _fallback_color("aged_wood"), "aged_wood")
	_create_box("CourtyardLaundryLine01", Vector3(-20.5, 2.15, -43.5), Vector3(6.2, 0.05, 0.05), _fallback_color("paper"), "paper")
	_create_box("CourtyardLaundryCloth01", Vector3(-22.0, 1.65, -43.45), Vector3(0.8, 0.9, 0.04), _fallback_color("paper"), "paper")
	_create_box("CourtyardLaundryCloth02", Vector3(-19.9, 1.6, -43.45), Vector3(0.75, 0.8, 0.04), Color(0.38, 0.12, 0.11), "shrine_red")

	_create_box("CourtyardCartBase", Vector3(12.0, 0.45, -45.0), Vector3(2.4, 0.28, 1.35), _fallback_color("aged_wood"), "aged_wood")
	_create_cylinder("CourtyardCartWheel01", Vector3(10.75, 0.35, -44.3), 0.28, 0.16, _fallback_color("black_wood"), "black_wood")
	_create_cylinder("CourtyardCartWheel02", Vector3(13.25, 0.35, -44.3), 0.28, 0.16, _fallback_color("black_wood"), "black_wood")
	_create_box("CourtyardCartHandle", Vector3(12.0, 0.64, -46.0), Vector3(0.22, 0.18, 1.4), _fallback_color("aged_wood"), "aged_wood")

	_create_box("CourtyardRubble01", Vector3(-6.0, 0.28, -34.5), Vector3(2.4, 0.32, 0.9), _fallback_color("stone"), "stone")
	_create_box("CourtyardRubble02", Vector3(7.0, 0.28, -30.8), Vector3(1.8, 0.3, 0.7), _fallback_color("stone"), "stone")
	_create_box("CourtyardRubble03", Vector3(5.5, 0.28, -56.5), Vector3(2.0, 0.28, 0.8), _fallback_color("old_plaster"), "old_plaster")
	_create_box("CourtyardLowWall01", Vector3(-12.5, 0.62, -28.5), Vector3(6.0, 0.8, 0.42), _fallback_color("old_plaster"), "old_plaster")
	_create_box("CourtyardLowWall02", Vector3(10.8, 0.62, -58.0), Vector3(5.5, 0.8, 0.42), _fallback_color("old_plaster"), "old_plaster")
	_create_box("CourtyardLowWall03", Vector3(24.0, 0.62, -47.0), Vector3(0.42, 0.8, 6.5), _fallback_color("old_plaster"), "old_plaster")
	_create_box("CourtyardPartitionWallA", Vector3(-7.0, 0.9, -41.0), Vector3(9.0, 1.35, 0.45), _fallback_color("old_plaster"), "old_plaster")
	_create_box("CourtyardPartitionWallB", Vector3(8.5, 0.9, -47.0), Vector3(8.0, 1.35, 0.45), _fallback_color("old_plaster"), "old_plaster")
	_create_box("CourtyardPartitionWallC", Vector3(0.0, 0.9, -54.5), Vector3(7.0, 1.35, 0.45), _fallback_color("old_plaster"), "old_plaster")
	_create_courtyard_salgut_installation()
	_create_forest_tree("CourtyardCanopyDeadTreeA", Vector3(-21.0, 0.0, -47.0), 8.2)
	_create_forest_tree("CourtyardCanopyDeadTreeB", Vector3(22.0, 0.0, -33.0), 8.6)
	_create_interactable_box("CourtyardToolChest", Vector3(-14.5, 0.45, -57.0), Vector3(1.25, 0.55, 0.85), _fallback_color("black_wood"), "black_wood", "공구함 열기", "공구함 닫기", Vector3(0.0, 0.08, 0.0), Vector3(-12.0, 0.0, 0.0))

func _create_courtyard_salgut_installation() -> void:
	_create_box("CourtyardSalgutPoleNorth", Vector3(-5.8, 1.8, -33.5), Vector3(0.18, 3.6, 0.18), _fallback_color("black_wood"), "black_wood")
	_create_box("CourtyardSalgutPoleSouth", Vector3(5.8, 1.8, -52.0), Vector3(0.18, 3.6, 0.18), _fallback_color("black_wood"), "black_wood")
	_create_box("CourtyardSalgutRopeA", Vector3(0.0, 3.0, -38.4), Vector3(12.8, 0.07, 0.07), _fallback_color("straw"), "straw")
	_create_box("CourtyardSalgutRopeB", Vector3(0.0, 2.86, -47.0), Vector3(12.8, 0.07, 0.07), _fallback_color("straw"), "straw")
	_create_box("CourtyardSalgutClothA", Vector3(-2.4, 2.55, -38.4), Vector3(0.95, 1.1, 0.05), Color(0.78, 0.72, 0.62), "paper")
	_create_box("CourtyardSalgutClothB", Vector3(2.3, 2.44, -47.0), Vector3(0.9, 0.95, 0.05), Color(0.36, 0.08, 0.07), "shrine_red")
	_create_box("CourtyardSalgutAltar", Vector3(0.0, 0.55, -43.5), Vector3(3.4, 0.72, 1.25), _fallback_color("black_wood"), "black_wood")
	_create_box("CourtyardSalgutBowl", Vector3(-0.9, 1.02, -43.5), Vector3(0.52, 0.22, 0.52), _fallback_color("clay_pot"), "clay_pot")
	_create_box("CourtyardSalgutKnife", Vector3(0.82, 1.0, -43.4), Vector3(0.78, 0.06, 0.16), _fallback_color("metal"), "metal")

func _create_main_house_building() -> void:
	_create_tall_wall_box("MainHouseFrontWallLeft", Vector3(-13.8, 1.7, -73.4), Vector3(16.8, 3.4, 0.55), "old_plaster")
	_create_tall_wall_box("MainHouseFrontWallRight", Vector3(13.8, 1.7, -73.4), Vector3(16.8, 3.4, 0.55), "old_plaster")
	_create_box("MainHouseDoorThreshold", Vector3(0.0, 0.22, -73.15), Vector3(8.2, 0.22, 1.2), _fallback_color("aged_wood"), "aged_wood")
	_create_tall_wall_box("MainHouseLeftOuterWall", Vector3(-22.0, 1.65, -93.25), Vector3(0.65, 3.3, 40.5), "old_plaster")
	_create_tall_wall_box("MainHouseRightOuterWall", Vector3(22.0, 1.65, -93.25), Vector3(0.65, 3.3, 40.5), "old_plaster")
	_create_box("MainHouseCenterPartition", Vector3(-9.2, 1.35, -86.0), Vector3(11.6, 2.7, 0.4), _fallback_color("aged_wood"), "aged_wood")
	_create_box("MainHouseCenterPartitionRight", Vector3(9.2, 1.35, -86.0), Vector3(11.6, 2.7, 0.4), _fallback_color("aged_wood"), "aged_wood")
	_create_box("MainHouseRearPartitionLeft", Vector3(-9.8, 1.35, -96.0), Vector3(10.8, 2.7, 0.4), _fallback_color("aged_wood"), "aged_wood")
	_create_box("MainHouseRearPartitionRight", Vector3(9.8, 1.35, -96.0), Vector3(10.8, 2.7, 0.4), _fallback_color("aged_wood"), "aged_wood")
	_create_box("MainHouseRoof", Vector3(0.0, 4.9, -93.0), Vector3(48.5, 0.45, 43.0), _fallback_color("roof_tile"), "roof_tile")
	_create_box("MainHouseRoofFrontLip", Vector3(0.0, 4.55, -72.0), Vector3(50.0, 0.35, 1.4), _fallback_color("roof_tile"), "roof_tile")
	_add_visual_box_world("MainHousePaperDoorA", Vector3(-12.4, 1.45, -73.05), Vector3(1.4, 2.15, 0.05), _fallback_color("paper"), "paper")
	_add_visual_box_world("MainHousePaperDoorB", Vector3(8.2, 1.45, -73.05), Vector3(1.4, 2.15, 0.05), _fallback_color("paper"), "paper")
	_add_visual_box_world("MainHousePaperDoorC", Vector3(12.4, 1.45, -73.05), Vector3(1.4, 2.15, 0.05), _fallback_color("paper"), "paper")
	_create_interactable_box("MainHouseSlidingDoor", Vector3(0.0, 1.35, -73.5), Vector3(2.4, 2.15, 0.14), _fallback_color("aged_wood"), "aged_wood", "안채 문 열기", "안채 문 닫기", Vector3(5.4, 0.0, 0.0), Vector3.ZERO)
	_add_visual_box_world("MainHouseLeftRoomPaperWall", Vector3(-5.2, 1.5, -86.0), Vector3(0.06, 2.2, 8.8), _fallback_color("paper"), "paper")
	_add_visual_box_world("MainHouseRightRoomPaperWall", Vector3(5.2, 1.5, -86.0), Vector3(0.06, 2.2, 8.8), _fallback_color("paper"), "paper")
	_create_box("MainHouseLowTable", Vector3(-4.0, 0.42, -89.5), Vector3(2.2, 0.32, 1.1), _fallback_color("black_wood"), "black_wood")
	_create_box("MainHouseRolledMatA", Vector3(4.2, 0.27, -89.0), Vector3(2.4, 0.18, 0.7), Color(0.32, 0.27, 0.18), "straw")
	_create_box("MainHouseRolledMatB", Vector3(6.6, 0.27, -91.2), Vector3(1.9, 0.18, 0.65), Color(0.32, 0.27, 0.18), "straw")
	_create_hidden_main_house_interior()

func _create_hidden_main_house_interior() -> void:
	_create_box("MainHouseHiddenFrontChamber", Vector3(0.0, 0.22, -91.0), Vector3(14.0, 0.24, 7.0), _fallback_color("old_plaster"), "old_plaster")
	_create_box("MainHouseHiddenMiddleChamber", Vector3(0.0, 0.24, -102.0), Vector3(8.6, 0.24, 7.8), _fallback_color("old_plaster"), "old_plaster")
	_create_box("MainHouseHiddenDeepChamber", Vector3(0.0, 0.26, -111.0), Vector3(4.6, 0.24, 7.8), _fallback_color("shadow"), "shadow")
	_create_tall_wall_box("MainHouseHiddenFalseWall", Vector3(-5.6, 1.55, -98.0), Vector3(7.8, 3.1, 0.45), "aged_wood")
	_create_tall_wall_box("MainHouseHiddenScreenRight", Vector3(5.2, 1.55, -103.0), Vector3(5.6, 3.1, 0.45), "aged_wood")
	_create_tall_wall_box("MainHouseHiddenBackWall", Vector3(0.0, 1.65, -115.2), Vector3(9.0, 3.3, 0.55), "old_plaster")
	_create_tall_wall_box("MainHouseHiddenLeftWall", Vector3(-7.2, 1.45, -103.85), Vector3(0.45, 2.9, 23.2), "old_plaster")
	_create_tall_wall_box("MainHouseHiddenRightWall", Vector3(7.2, 1.45, -103.85), Vector3(0.45, 2.9, 23.2), "old_plaster")
	_create_box("MainHouseHiddenAncestralChest", Vector3(0.0, 0.55, -111.8), Vector3(2.0, 0.75, 0.9), _fallback_color("black_wood"), "black_wood")

func _create_secondary_roofed_buildings() -> void:
	_create_front_courtyard_outbuildings()
	_create_courtyard_readable_silhouettes()
	_create_servant_quarters()
	_create_collapsed_kitchen()
	_create_side_shrine_pavilion()

func _create_front_courtyard_outbuildings() -> void:
	var left_origin := Vector3(-25.5, 0.0, -36.0)
	_create_box("FrontSarangchaeFloor", left_origin + Vector3(0.0, 0.14, 0.0), Vector3(12.0, 0.28, 10.0), _fallback_color("packed_earth"), "packed_earth")
	_create_box("FrontSarangchaeBackWall", left_origin + Vector3(0.0, 1.35, 4.8), Vector3(12.0, 2.7, 0.45), _fallback_color("old_plaster"), "old_plaster")
	_create_box("FrontSarangchaeLeftWall", left_origin + Vector3(-5.8, 1.35, 0.0), Vector3(0.45, 2.7, 9.5), _fallback_color("old_plaster"), "old_plaster")
	_create_box("FrontSarangchaeRightPost", left_origin + Vector3(5.8, 1.35, -3.2), Vector3(0.45, 2.7, 3.2), _fallback_color("old_plaster"), "old_plaster")
	_create_box("FrontSarangchaeFrontRail", left_origin + Vector3(0.0, 0.82, -5.0), Vector3(11.8, 0.5, 0.35), _fallback_color("aged_wood"), "aged_wood")
	_create_box("FrontSarangchaeRoof", left_origin + Vector3(0.0, 3.08, 0.0), Vector3(13.4, 0.38, 11.2), _fallback_color("roof_tile"), "roof_tile")
	_add_visual_box_world("FrontSarangchaePaperDoorA", left_origin + Vector3(-2.2, 1.55, -4.75), Vector3(1.2, 1.8, 0.05), _fallback_color("paper"), "paper")
	_add_visual_box_world("FrontSarangchaePaperDoorB", left_origin + Vector3(1.8, 1.55, -4.75), Vector3(1.2, 1.8, 0.05), _fallback_color("paper"), "paper")

	var right_origin := Vector3(25.0, 0.0, -43.0)
	_create_box("FrontStorehouseAnnexFloor", right_origin + Vector3(0.0, 0.14, 0.0), Vector3(10.5, 0.28, 8.5), _fallback_color("packed_earth"), "packed_earth")
	_create_box("FrontStorehouseAnnexBackWall", right_origin + Vector3(0.0, 1.25, 4.0), Vector3(10.5, 2.5, 0.42), _fallback_color("old_plaster"), "old_plaster")
	_create_box("FrontStorehouseAnnexLeftWall", right_origin + Vector3(-5.0, 1.25, 0.0), Vector3(0.42, 2.5, 8.5), _fallback_color("old_plaster"), "old_plaster")
	_create_box("FrontStorehouseAnnexRightWall", right_origin + Vector3(5.0, 1.25, 0.0), Vector3(0.42, 2.5, 8.5), _fallback_color("old_plaster"), "old_plaster")
	_create_box("FrontStorehouseAnnexFrontWall", right_origin + Vector3(0.0, 1.25, -4.0), Vector3(10.5, 2.5, 0.42), _fallback_color("old_plaster"), "old_plaster")
	_create_box("FrontStorehouseAnnexDoor", right_origin + Vector3(-1.2, 1.05, -4.25), Vector3(1.4, 2.0, 0.14), _fallback_color("aged_wood"), "aged_wood")
	_create_box("FrontStorehouseAnnexRoof", right_origin + Vector3(0.0, 2.85, 0.0), Vector3(11.8, 0.35, 9.6), _fallback_color("roof_tile"), "roof_tile")

func _create_courtyard_readable_silhouettes() -> void:
	_create_box("SarangchaeSilhouette", Vector3(-25.0, 1.35, -29.5), Vector3(10.0, 2.7, 6.0), _fallback_color("old_plaster"), "old_plaster")
	_create_box("SarangchaeSilhouetteRoof", Vector3(-25.0, 3.0, -29.5), Vector3(11.4, 0.35, 7.2), _fallback_color("roof_tile"), "roof_tile")
	_add_visual_box_world("SarangchaeSilhouetteDoor", Vector3(-25.0, 1.25, -26.4), Vector3(1.2, 1.8, 0.05), _fallback_color("paper"), "paper")

	_create_box("HaengrangchaeSilhouette", Vector3(24.0, 1.25, -31.0), Vector3(9.0, 2.5, 5.2), _fallback_color("old_plaster"), "old_plaster")
	_create_box("HaengrangchaeSilhouetteRoof", Vector3(24.0, 2.85, -31.0), Vector3(10.2, 0.32, 6.4), _fallback_color("roof_tile"), "roof_tile")
	_add_visual_box_world("HaengrangchaeSilhouetteDoor", Vector3(24.0, 1.2, -28.25), Vector3(1.0, 1.7, 0.05), _fallback_color("aged_wood"), "aged_wood")

	_create_box("SmallBarnSilhouette", Vector3(29.0, 1.1, -51.0), Vector3(7.0, 2.2, 5.5), _fallback_color("aged_wood"), "aged_wood")
	_create_box("SmallBarnSilhouetteRoof", Vector3(29.0, 2.55, -51.0), Vector3(8.2, 0.3, 6.5), _fallback_color("roof_tile"), "roof_tile")

func _create_servant_quarters() -> void:
	var origin := Vector3(27.5, 0.0, -75.0)
	_create_box("ServantQuartersFloor", origin + Vector3(0.0, 0.12, 0.0), Vector3(10.0, 0.24, 9.0), _fallback_color("packed_earth"), "packed_earth")
	_create_box("ServantQuartersBackWall", origin + Vector3(0.0, 1.45, 4.4), Vector3(10.2, 2.9, 0.45), _fallback_color("old_plaster"), "old_plaster")
	_create_box("ServantQuartersLeftWall", origin + Vector3(-5.0, 1.45, 0.0), Vector3(0.45, 2.9, 9.0), _fallback_color("old_plaster"), "old_plaster")
	_create_box("ServantQuartersRightWall", origin + Vector3(5.0, 1.45, 0.0), Vector3(0.45, 2.9, 9.0), _fallback_color("old_plaster"), "old_plaster")
	_create_box("ServantQuartersFrontLeft", origin + Vector3(-2.9, 1.45, -4.4), Vector3(4.2, 2.9, 0.45), _fallback_color("old_plaster"), "old_plaster")
	_create_box("ServantQuartersFrontRight", origin + Vector3(2.9, 1.45, -4.4), Vector3(4.2, 2.9, 0.45), _fallback_color("old_plaster"), "old_plaster")
	_create_box("ServantQuartersRoof", origin + Vector3(0.0, 3.15, 0.0), Vector3(11.2, 0.35, 10.2), _fallback_color("roof_tile"), "roof_tile")
	_create_interactable_box("ServantQuartersDoor", origin + Vector3(0.0, 1.12, -4.55), Vector3(1.35, 2.05, 0.16), _fallback_color("aged_wood"), "aged_wood", "행랑채 문 열기", "행랑채 문 닫기", Vector3(0.85, 0.0, 0.0), Vector3(0.0, 72.0, 0.0))
	_add_visual_box_world("ServantQuartersPaperPanel", origin + Vector3(-3.7, 1.6, -4.10), Vector3(1.0, 1.55, 0.05), _fallback_color("paper"), "paper")
	_add_visual_box_world("ServantQuartersHangingCloth", origin + Vector3(3.5, 1.6, -4.10), Vector3(0.9, 1.35, 0.05), Color(0.36, 0.32, 0.25), "paper")

func _create_collapsed_kitchen() -> void:
	var origin := Vector3(-27.0, 0.0, -116.5)
	_create_box("CollapsedKitchenFloor", origin + Vector3(0.0, 0.12, 0.0), Vector3(11.0, 0.24, 8.5), _fallback_color("mud"), "mud")
	_create_box("CollapsedKitchenBackWall", origin + Vector3(0.0, 1.35, 4.0), Vector3(11.0, 2.7, 0.45), _fallback_color("old_plaster"), "old_plaster")
	_create_box("CollapsedKitchenLeftWall", origin + Vector3(-5.25, 1.35, 0.0), Vector3(0.45, 2.7, 8.4), _fallback_color("old_plaster"), "old_plaster")
	_create_box("CollapsedKitchenRightBrokenWallA", origin + Vector3(5.25, 1.2, -2.0), Vector3(0.45, 2.4, 3.2), _fallback_color("old_plaster"), "old_plaster")
	_create_box("CollapsedKitchenRightBrokenWallB", origin + Vector3(5.25, 1.2, 3.0), Vector3(0.45, 2.4, 2.1), _fallback_color("old_plaster"), "old_plaster")
	_create_box("CollapsedKitchenRoof", origin + Vector3(-1.0, 3.0, 0.2), Vector3(10.5, 0.32, 7.8), _fallback_color("roof_tile"), "roof_tile")
	_create_box("CollapsedKitchenSaggingRoofPiece", origin + Vector3(4.0, 2.3, -2.7), Vector3(4.0, 0.26, 3.0), _fallback_color("roof_tile"), "roof_tile")
	_create_box("CollapsedKitchenHearth", origin + Vector3(-2.7, 0.55, 1.6), Vector3(2.4, 0.75, 1.4), _fallback_color("stone"), "stone")
	_create_interactable_box("CollapsedKitchenCabinet", origin + Vector3(2.8, 0.75, 2.2), Vector3(1.5, 1.05, 0.65), _fallback_color("black_wood"), "black_wood", "찬장 뒤지기", "찬장 닫기", Vector3(0.0, 0.12, -0.35), Vector3(0.0, 0.0, 0.0))

func _create_side_shrine_pavilion() -> void:
	var origin := Vector3(18.5, 0.0, -124.0)
	_create_box("SideShrinePavilionFloor", origin + Vector3(0.0, 0.18, 0.0), Vector3(7.0, 0.32, 7.0), _fallback_color("dark_stone"), "dark_stone")
	_create_box("SideShrinePavilionBackWall", origin + Vector3(0.0, 1.35, 3.4), Vector3(7.2, 2.7, 0.42), _fallback_color("shrine_red"), "shrine_red")
	_create_box("SideShrinePavilionLeftPost", origin + Vector3(-3.2, 1.45, -2.7), Vector3(0.38, 2.9, 0.38), _fallback_color("black_wood"), "black_wood")
	_create_box("SideShrinePavilionRightPost", origin + Vector3(3.2, 1.45, -2.7), Vector3(0.38, 2.9, 0.38), _fallback_color("black_wood"), "black_wood")
	_create_box("SideShrinePavilionRoof", origin + Vector3(0.0, 3.15, 0.0), Vector3(8.4, 0.34, 8.2), _fallback_color("roof_tile"), "roof_tile")
	_create_box("SideShrinePavilionAltar", origin + Vector3(0.0, 0.72, 2.0), Vector3(3.6, 0.9, 1.2), _fallback_color("black_wood"), "black_wood")
	_create_interactable_box("ShrineOfferingBox", origin + Vector3(0.0, 1.28, 1.35), Vector3(1.15, 0.5, 0.7), _fallback_color("aged_wood"), "aged_wood", "공양함 열기", "공양함 닫기", Vector3(0.0, 0.08, -0.28), Vector3(-15.0, 0.0, 0.0))
	_create_interactable_box("ShrineBellPull", origin + Vector3(2.55, 1.55, 0.1), Vector3(0.16, 1.35, 0.16), _fallback_color("shrine_red"), "shrine_red", "방울줄 당기기", "방울줄 놓기", Vector3(0.0, -0.2, 0.0), Vector3(0.0, 0.0, 8.0))

func _create_interactable_box(label: String, position: Vector3, size: Vector3, color: Color, material_key: String, prompt: String, active_prompt: String, active_offset: Vector3 = Vector3.ZERO, active_rotation: Vector3 = Vector3.ZERO) -> StaticBody3D:
	var body := _create_box(label, position, size, color, material_key)
	body.set_script(StatefulInteractableScript)
	body.prompt = prompt
	body.active_prompt = active_prompt
	body.active_offset = active_offset
	body.active_rotation_degrees = active_rotation
	return body

func _create_gate() -> void:
	var gate: Node3D = EstateGateScript.new()
	gate.name = GATE_NODE_NAME
	add_child(gate)
	gate.global_position = GATE_POSITION

	var left_hinge := Node3D.new()
	left_hinge.name = "LeftGateHinge"
	left_hinge.position = Vector3(-3.6, 0.0, 0.0)
	gate.add_child(left_hinge)

	var right_hinge := Node3D.new()
	right_hinge.name = "RightGateHinge"
	right_hinge.position = Vector3(3.6, 0.0, 0.0)
	gate.add_child(right_hinge)

	var left_panel := _create_child_body(left_hinge, "LeftSwingGatePanel", Vector3(1.75, 1.95, 0.0), Vector3(3.5, 3.9, 0.45), _fallback_color("aged_wood"), "aged_wood")
	var right_panel := _create_child_body(right_hinge, "RightSwingGatePanel", Vector3(-1.75, 1.95, 0.0), Vector3(3.5, 3.9, 0.45), _fallback_color("aged_wood"), "aged_wood")
	left_panel.set_script(GateLeafScript)
	right_panel.set_script(GateLeafScript)
	left_panel.gate = gate
	right_panel.gate = gate
	gate.setup(left_hinge, right_hinge)

	_create_child_body(gate, "GateLintel", Vector3(0.0, 4.45, 0.0), Vector3(8.5, 0.5, 0.9), _fallback_color("black_wood"), "black_wood")
	_create_child_body(gate, "GateLeftPost", Vector3(-4.25, 2.35, 0.0), Vector3(0.68, 4.7, 0.92), _fallback_color("black_wood"), "black_wood")
	_create_child_body(gate, "GateRightPost", Vector3(4.25, 2.35, 0.0), Vector3(0.68, 4.7, 0.92), _fallback_color("black_wood"), "black_wood")
	_add_visual_box(gate, "GateRoofLine", Vector3(0.0, 4.95, -0.08), Vector3(9.6, 0.35, 1.25), _fallback_color("roof_tile"), "roof_tile")
	_create_tall_wall_box("GateBypassBlockLeft", Vector3(-5.05, 1.7, -12.0), Vector3(1.1, 3.4, 1.25), "old_plaster")
	_create_tall_wall_box("GateBypassBlockRight", Vector3(5.05, 1.7, -12.0), Vector3(1.1, 3.4, 1.25), "old_plaster")
	_create_tall_wall_box("GateSideSeamLeft", Vector3(-5.55, 1.15, -10.5), Vector3(0.9, 2.3, 3.0), "old_plaster")
	_create_tall_wall_box("GateSideSeamRight", Vector3(5.55, 1.15, -10.5), Vector3(0.9, 2.3, 3.0), "old_plaster")
	_create_box("OuterGateTalismanA", Vector3(-1.25, 2.55, -11.68), Vector3(0.38, 0.82, 0.05), Color(0.86, 0.74, 0.42), "paper")
	_create_box("OuterGateTalismanB", Vector3(1.25, 2.55, -11.68), Vector3(0.38, 0.82, 0.05), Color(0.86, 0.74, 0.42), "paper")
	_create_box("OuterGateGeumjulRope", Vector3(0.0, 2.95, -11.66), Vector3(7.3, 0.08, 0.08), Color(0.74, 0.62, 0.32), "straw")

func _create_side_passage_risk_trigger(main: Node) -> void:
	var area := Area3D.new()
	area.name = "RiskySidePassageTrigger"
	add_child(area)
	area.global_position = Vector3(-11.7, 1.0, -19.5)

	var collision := CollisionShape3D.new()
	collision.name = "CollisionShape3D"
	var shape := BoxShape3D.new()
	shape.size = Vector3(2.8, 2.0, 18.0)
	collision.shape = shape
	area.add_child(collision)
	if main.has_method("_on_risky_side_passage_entered"):
		area.body_entered.connect(main._on_risky_side_passage_entered)

func _create_planned_landmarks() -> void:
	for landmark_data: Dictionary in EstateLayoutPlanScript.get_landmarks():
		var label := str(landmark_data["name"])
		if label == GATE_NODE_NAME:
			continue
		var position: Vector3 = landmark_data["position"]
		var size: Vector3 = landmark_data["size"]
		var material_key := str(landmark_data["material_key"])
		if label.begins_with("JangdokdaeCluster"):
			_create_jar_cluster(label, position, size)
		elif label == "CourtyardWell":
			_create_well(label, position)
		elif label == "DwitganOuthouse":
			_create_outhouse(label, position)
		elif label == "StorehouseShed":
			_create_storehouse(label, position, size)
		elif label == "DeadTreeCourtyard" or label.begins_with("BackyardTreeLine"):
			_create_dead_tree_cluster(label, position, size)
		elif label == "ToenmaruLongStep":
			_create_box(label, Vector3(position.x, 0.34, position.z), Vector3(size.x, 0.26, size.z), _fallback_color(material_key), material_key)
		elif label.begins_with("BrokenWallGap"):
			_create_rubble_marker(label, position, size)
		elif label == "ShrinePaperCharms":
			_create_charm_cluster(label, position, size)
		else:
			_create_box(label, position, size, _fallback_color(material_key), material_key)

func _create_well(label: String, position: Vector3) -> void:
	_create_cylinder(label, position, 1.1, 1.1, _fallback_color("well_stone"), "well_stone")
	_create_cylinder("%sInnerShadow" % label, position + Vector3(0.0, 0.12, 0.0), 0.72, 0.08, Color(0.025, 0.025, 0.022), "")
	_create_box("%sWoodCover" % label, position + Vector3(0.0, 0.72, 0.0), Vector3(1.8, 0.12, 0.35), _fallback_color("aged_wood"), "aged_wood")

func _create_jar_cluster(label: String, position: Vector3, size: Vector3) -> void:
	var base := Node3D.new()
	base.name = label
	add_child(base)
	base.global_position = position
	var offsets := [
		Vector3(-1.7, 0.0, -0.8),
		Vector3(-0.5, 0.0, -0.95),
		Vector3(0.8, 0.0, -0.55),
		Vector3(1.75, 0.0, 0.6),
		Vector3(-1.2, 0.0, 0.75),
		Vector3(0.35, 0.0, 0.9),
	]
	for i in range(offsets.size()):
		var scale := 0.75 + float(i % 3) * 0.12
		var jar_position: Vector3 = position + offsets[i]
		if abs(offsets[i].x) > size.x * 0.5:
			continue
		_create_cylinder("%sJar%d" % [label, i + 1], jar_position + Vector3(0.0, 0.25 * scale, 0.0), 0.34 * scale, 0.62 * scale, Color(0.28, 0.14, 0.075), "clay_pot")
		_create_cylinder("%sLid%d" % [label, i + 1], jar_position + Vector3(0.0, 0.62 * scale, 0.0), 0.29 * scale, 0.08, Color(0.18, 0.1, 0.065), "clay_pot")

func _create_outhouse(label: String, position: Vector3) -> void:
	var house := Node3D.new()
	house.name = label
	add_child(house)
	house.global_position = position
	_create_child_body(house, "%sFloor" % label, Vector3(0.0, -1.05, 0.0), Vector3(4.2, 0.18, 4.6), _fallback_color("mud"), "mud")
	_create_child_body(house, "%sBackWall" % label, Vector3(0.0, 0.1, 2.15), Vector3(4.2, 2.3, 0.22), _fallback_color("straw"), "straw")
	_create_child_body(house, "%sLeftWall" % label, Vector3(-2.05, 0.1, 0.0), Vector3(0.22, 2.3, 4.3), _fallback_color("straw"), "straw")
	_create_child_body(house, "%sRightWall" % label, Vector3(2.05, 0.1, 0.0), Vector3(0.22, 2.3, 4.3), _fallback_color("straw"), "straw")
	_create_child_body(house, "%sFrontLeft" % label, Vector3(-1.25, 0.1, -2.15), Vector3(1.55, 2.3, 0.22), _fallback_color("straw"), "straw")
	_create_child_body(house, "%sFrontRight" % label, Vector3(1.25, 0.1, -2.15), Vector3(1.55, 2.3, 0.22), _fallback_color("straw"), "straw")
	_create_child_body(house, "%sRoof" % label, Vector3(0.0, 1.38, 0.0), Vector3(4.7, 0.3, 5.0), _fallback_color("roof_tile"), "roof_tile")
	_add_visual_box(house, "%sDarkDoorway" % label, Vector3(0.0, 0.0, -2.34), Vector3(1.0, 1.85, 0.05), Color(0.02, 0.018, 0.014), "")

func _create_storehouse(label: String, position: Vector3, size: Vector3) -> void:
	var shed := Node3D.new()
	shed.name = label
	add_child(shed)
	shed.global_position = position
	_create_child_body(shed, "%sBackWall" % label, Vector3(0.0, 0.0, size.z * 0.5), Vector3(size.x, size.y, 0.28), _fallback_color("aged_wood"), "aged_wood")
	_create_child_body(shed, "%sLeftWall" % label, Vector3(-size.x * 0.5, 0.0, 0.0), Vector3(0.28, size.y, size.z), _fallback_color("aged_wood"), "aged_wood")
	_create_child_body(shed, "%sRightWall" % label, Vector3(size.x * 0.5, 0.0, 0.0), Vector3(0.28, size.y, size.z), _fallback_color("aged_wood"), "aged_wood")
	_create_child_body(shed, "%sRoof" % label, Vector3(0.0, size.y * 0.55, 0.0), Vector3(size.x + 0.8, 0.32, size.z + 0.8), _fallback_color("roof_tile"), "roof_tile")
	_create_child_body(shed, "%sShelf" % label, Vector3(0.0, -0.45, 0.9), Vector3(size.x - 1.2, 0.2, 1.0), _fallback_color("black_wood"), "black_wood")
	_create_interactable_box("StorehouseSlidingDoor", position + Vector3(0.0, -0.05, -size.z * 0.5 - 0.18), Vector3(2.0, 2.15, 0.18), _fallback_color("aged_wood"), "aged_wood", "광 문 열기", "광 문 닫기", Vector3(1.35, 0.0, 0.0), Vector3.ZERO)

func _create_dead_tree_cluster(label: String, position: Vector3, size: Vector3) -> void:
	var count := 1
	if label.begins_with("BackyardTreeLine"):
		count = 4
	for i in range(count):
		var offset := Vector3(float(i) * 0.75 - float(count - 1) * 0.38, 0.0, sin(float(i)) * size.z * 0.18)
		var trunk_height := size.y * (0.72 + float(i % 2) * 0.12)
		_create_cylinder("%sTrunk%d" % [label, i + 1], position + offset, 0.22, trunk_height, _fallback_color("dead_tree"), "dead_tree")
		_create_box("%sBranch%dA" % [label, i + 1], position + offset + Vector3(0.35, trunk_height * 0.28, 0.0), Vector3(0.9, 0.12, 0.16), _fallback_color("dead_tree"), "dead_tree")
		_create_box("%sBranch%dB" % [label, i + 1], position + offset + Vector3(-0.25, trunk_height * 0.38, 0.18), Vector3(0.75, 0.1, 0.14), _fallback_color("dead_tree"), "dead_tree")

func _create_rubble_marker(label: String, position: Vector3, size: Vector3) -> void:
	_create_box(label, Vector3(position.x, 0.3, position.z), Vector3(size.x, 0.45, size.z), _fallback_color("old_plaster"), "old_plaster")
	_create_box("%sLowGap" % label, Vector3(position.x, 0.62, position.z), Vector3(size.x * 0.55, 0.25, size.z * 0.35), _fallback_color("stone"), "stone")

func _create_charm_cluster(label: String, position: Vector3, size: Vector3) -> void:
	var root := Node3D.new()
	root.name = label
	add_child(root)
	root.global_position = position
	for i in range(7):
		var x := -size.x * 0.42 + float(i) * size.x / 7.0
		var y := sin(float(i) * 1.7) * 0.18
		_add_visual_box(root, "%sPaper%d" % [label, i + 1], Vector3(x, y, 0.12), Vector3(0.34, 0.95, 0.04), _fallback_color("paper"), "paper")
	_create_box("%sBeam" % label, position + Vector3(0.0, size.y * 0.45, 0.0), Vector3(size.x, 0.12, 0.12), _fallback_color("black_wood"), "black_wood")

func _create_bongo_van() -> void:
	for part_data: Dictionary in BongoVanPlanScript.ALL_PARTS:
		var original_name := str(part_data["name"])
		var label := _bongo_part_name(original_name)
		var position: Vector3 = part_data["position"] + BongoVanPlanScript.WORLD_OFFSET
		var size: Vector3 = part_data["size"]
		var color: Color = part_data["color"]
		var rotation := Vector3.ZERO
		if part_data.has("rotation_degrees"):
			rotation = part_data["rotation_degrees"]
		var material_key := _bongo_material_key(label)
		if bool(part_data["collision"]):
			var body := _create_box(label, position, size, color, material_key)
			body.rotation_degrees = rotation
		else:
			_add_visual_box_world(label, position, size, color, material_key, rotation)
	_create_bongo_hub_rear_door_blocker()
	_create_bongo_quota_monitor()
	_create_bongo_map_selector()
	_create_bongo_settlement_map_selector()
	_create_bongo_departure_button()

func _create_bongo_hub_rear_door_blocker() -> void:
	var blocker := _create_box(
		BongoVanPlanScript.HUB_REAR_DOOR_BLOCKER_NAME,
		BongoVanPlanScript.HUB_REAR_DOOR_BLOCKER_POSITION,
		BongoVanPlanScript.HUB_REAR_DOOR_BLOCKER_SIZE,
		BongoVanPlanScript.COLOR_BODY_SHADOW,
		"van_paint"
	)
	blocker.set_script(BongoHubRearDoorLockScript)

func _create_bongo_quota_monitor() -> void:
	var monitor := StaticBody3D.new()
	monitor.name = BongoVanPlanScript.QUOTA_MONITOR_NAME
	monitor.set_script(BongoQuotaMonitorScript)
	add_child(monitor)
	monitor.global_position = BongoVanPlanScript.QUOTA_MONITOR_POSITION

	_add_visual_box(monitor, "BongoQuotaMonitorBacking", Vector3.ZERO, BongoVanPlanScript.QUOTA_MONITOR_BACKING_SIZE, BongoVanPlanScript.COLOR_MONITOR_BACKING, "metal")
	_add_visual_box(monitor, "BongoQuotaMonitorScreen", Vector3(0.0, 0.0, -0.07), BongoVanPlanScript.QUOTA_MONITOR_SCREEN_SIZE, BongoVanPlanScript.COLOR_MONITOR_SCREEN)
	var collision := CollisionShape3D.new()
	collision.name = "CollisionShape3D"
	var shape := BoxShape3D.new()
	shape.size = BongoVanPlanScript.QUOTA_MONITOR_BACKING_SIZE
	collision.shape = shape
	monitor.add_child(collision)

	var label := Label3D.new()
	label.name = "BongoQuotaMonitorText"
	label.text = BongoVanPlanScript.QUOTA_MONITOR_TEXT
	label.position = Vector3(0.0, -0.06, -0.13)
	label.pixel_size = 0.009
	label.modulate = BongoVanPlanScript.COLOR_MONITOR_GLOW
	label.outline_modulate = Color(0.0, 0.0, 0.0)
	label.outline_size = 8
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	monitor.add_child(label)

func _create_bongo_settlement_station() -> void:
	var station := _create_box(
		BongoVanPlanScript.SETTLEMENT_STATION_NAME,
		BongoVanPlanScript.SETTLEMENT_STATION_POSITION,
		BongoVanPlanScript.SETTLEMENT_STATION_SIZE,
		BongoVanPlanScript.COLOR_MONITOR_BACKING,
		"metal"
	)
	station.set_script(BongoSettlementStationScript)
	_add_visual_box_world("BongoSettlementStationScreen", BongoVanPlanScript.SETTLEMENT_STATION_POSITION + Vector3(0.0, 0.18, -0.27), Vector3(0.55, 0.28, 0.04), BongoVanPlanScript.COLOR_MONITOR_SCREEN)

func _create_bongo_map_selector() -> void:
	var selector := _create_box(
		BongoVanPlanScript.MAP_SELECTOR_NAME,
		BongoVanPlanScript.MAP_SELECTOR_POSITION,
		BongoVanPlanScript.MAP_SELECTOR_SIZE,
		Color(0.05, 0.14, 0.18),
		"metal"
	)
	selector.set_script(BongoMapSelectorScript)
	_add_visual_box_world("BongoMapSelectorScreen", BongoVanPlanScript.MAP_SELECTOR_POSITION + Vector3(0.0, 0.04, -0.22), Vector3(0.64, 0.18, 0.04), Color(0.24, 0.8, 0.72), "glass")

func _create_bongo_settlement_map_selector() -> void:
	var selector := _create_box(
		BongoVanPlanScript.SETTLEMENT_MAP_SELECTOR_NAME,
		BongoVanPlanScript.SETTLEMENT_MAP_SELECTOR_POSITION,
		BongoVanPlanScript.SETTLEMENT_MAP_SELECTOR_SIZE,
		Color(0.12, 0.10, 0.04),
		"metal"
	)
	selector.set_script(BongoSettlementMapSelectorScript)
	_add_visual_box_world("BongoSettlementMapSelectorLamp", BongoVanPlanScript.SETTLEMENT_MAP_SELECTOR_POSITION + Vector3(0.0, 0.04, -0.22), Vector3(0.38, 0.14, 0.04), Color(0.92, 0.72, 0.25), "metal")

func _create_bongo_departure_button() -> void:
	var button := _create_box(
		BongoVanPlanScript.DEPARTURE_BUTTON_NAME,
		BongoVanPlanScript.DEPARTURE_BUTTON_POSITION,
		BongoVanPlanScript.DEPARTURE_BUTTON_SIZE,
		Color(0.42, 0.05, 0.04),
		"shrine_red"
	)
	button.set_script(BongoDepartureButtonScript)
	_add_visual_box_world("BongoDepartureButtonLamp", BongoVanPlanScript.DEPARTURE_BUTTON_POSITION + Vector3(0.0, 0.04, -0.22), Vector3(0.32, 0.14, 0.04), Color(0.95, 0.08, 0.04), "shrine_red")

func _create_settlement_office_map() -> void:
	var origin: Vector3 = BongoVanPlanScript.SETTLEMENT_OFFICE_ORIGIN
	var floor_size: Vector3 = BongoVanPlanScript.SETTLEMENT_OFFICE_FLOOR_SIZE
	var half_x := floor_size.x * 0.5
	var half_z := floor_size.z * 0.5
	_create_box("SettlementOfficeFloor", origin, floor_size, _fallback_color("dark_stone"), "dark_stone")
	_create_box("SettlementOfficeBackWall", origin + Vector3(0.0, 1.9, half_z - 0.25), Vector3(floor_size.x, 3.8, 0.5), _fallback_color("old_plaster"), "old_plaster")
	_create_box("SettlementOfficeLeftWall", origin + Vector3(-half_x + 0.25, 1.9, 0.0), Vector3(0.5, 3.8, floor_size.z), _fallback_color("old_plaster"), "old_plaster")
	_create_box("SettlementOfficeRightWall", origin + Vector3(half_x - 0.25, 1.9, 0.0), Vector3(0.5, 3.8, floor_size.z), _fallback_color("old_plaster"), "old_plaster")
	_create_box("SettlementOfficeFrontLeftWall", origin + Vector3(-15.0, 1.9, -half_z + 0.25), Vector3(18.0, 3.8, 0.5), _fallback_color("old_plaster"), "old_plaster")
	_create_box("SettlementOfficeFrontRightWall", origin + Vector3(15.0, 1.9, -half_z + 0.25), Vector3(18.0, 3.8, 0.5), _fallback_color("old_plaster"), "old_plaster")
	_create_box("SettlementOfficeCeiling", origin + Vector3(0.0, 3.85, 0.0), Vector3(floor_size.x + 0.4, 0.32, floor_size.z + 0.4), _fallback_color("black_wood"), "black_wood")
	_create_box("SettlementOfficeCounter", origin + Vector3(0.0, 0.75, -13.5), Vector3(16.0, 1.1, 1.1), _fallback_color("black_wood"), "black_wood")
	_create_box("SettlementOfficeMonitor", origin + Vector3(0.0, 1.68, -14.08), Vector3(4.8, 1.25, 0.12), Color(0.02, 0.17, 0.13), "glass")
	_create_box("SettlementOfficePaperStack", origin + Vector3(-6.1, 1.38, -13.55), Vector3(1.2, 0.2, 0.72), _fallback_color("paper"), "paper")
	_create_box("SettlementOfficeLedgerStack", origin + Vector3(6.0, 1.4, -13.55), Vector3(1.5, 0.26, 0.8), _fallback_color("paper"), "paper")
	_create_box("SettlementOfficeLeftStorageShelf", origin + Vector3(-18.5, 1.0, -5.5), Vector3(2.2, 1.8, 8.0), _fallback_color("black_wood"), "black_wood")
	_create_box("SettlementOfficeRightStorageShelf", origin + Vector3(18.5, 1.0, -5.5), Vector3(2.2, 1.8, 8.0), _fallback_color("black_wood"), "black_wood")
	_create_box("SettlementOfficeQueueRailLeft", origin + Vector3(-4.2, 0.85, 2.5), Vector3(0.18, 1.1, 18.0), _fallback_color("metal"), "metal")
	_create_box("SettlementOfficeQueueRailRight", origin + Vector3(4.2, 0.85, 2.5), Vector3(0.18, 1.1, 18.0), _fallback_color("metal"), "metal")
	_create_box("SettlementOfficeOverheadBeamA", origin + Vector3(0.0, 3.15, 8.5), Vector3(43.0, 0.28, 0.42), _fallback_color("black_wood"), "black_wood")
	_create_box("SettlementOfficeOverheadBeamB", origin + Vector3(0.0, 3.15, -4.0), Vector3(43.0, 0.28, 0.42), _fallback_color("black_wood"), "black_wood")
	_create_bongo_settlement_station()
	_create_settlement_office_bongo_van()

func _create_settlement_office_bongo_van() -> void:
	var origin: Vector3 = BongoVanPlanScript.SETTLEMENT_BONGO_ORIGIN
	_create_box("SettlementOfficeBongoParkingPad", BongoVanPlanScript.SETTLEMENT_BONGO_PARKING_PAD_POSITION, BongoVanPlanScript.SETTLEMENT_BONGO_PARKING_PAD_SIZE, _fallback_color("packed_earth"), "packed_earth")
	_create_box("SettlementOfficeBongoInteriorFloor", origin, Vector3(3.8, 0.18, 6.2), BongoVanPlanScript.COLOR_FLOOR, "van_paint")
	_create_box("SettlementOfficeBongoLeftCargoWall", origin + Vector3(-2.05, 1.44, 0.15), Vector3(0.22, 2.55, 5.7), BongoVanPlanScript.COLOR_BODY, "van_paint")
	_create_box("SettlementOfficeBongoRightCargoWall", origin + Vector3(2.05, 1.44, 0.15), Vector3(0.22, 2.55, 5.7), BongoVanPlanScript.COLOR_BODY, "van_paint")
	_create_box("SettlementOfficeBongoRoof", origin + Vector3(0.0, 2.78, 0.15), Vector3(4.25, 0.22, 5.7), BongoVanPlanScript.COLOR_BODY, "van_paint")
	_create_box("SettlementOfficeBongoCabin", origin + Vector3(0.0, 1.06, -4.05), Vector3(3.65, 1.74, 1.85), BongoVanPlanScript.COLOR_BODY_SHADOW, "van_paint")
	_create_box("SettlementOfficeBongoWindshield", origin + Vector3(0.0, 1.65, -3.08), Vector3(2.85, 0.7, 0.12), BongoVanPlanScript.COLOR_GLASS, "glass")
	_create_box("SettlementOfficeBongoFrontBumper", origin + Vector3(0.0, 0.42, -5.08), Vector3(3.65, 0.28, 0.22), BongoVanPlanScript.COLOR_TRIM, "metal")
	_create_box("SettlementOfficeBongoRearStep", origin + Vector3(0.0, -0.04, 3.65), Vector3(3.45, 0.24, 0.85), BongoVanPlanScript.COLOR_TRIM, "metal")
	var ramp := _create_box("SettlementOfficeBongoEntryRamp", origin + Vector3(0.0, -0.08, 4.35), Vector3(3.25, 0.14, 1.1), BongoVanPlanScript.COLOR_TRIM, "metal")
	ramp.rotation_degrees.x = 3.5
	_create_box("SettlementOfficeBongoOpenLeftRearDoor", origin + Vector3(-2.7, 1.1, 3.1), Vector3(0.22, 2.15, 1.5), BongoVanPlanScript.COLOR_BODY, "van_paint")
	_create_box("SettlementOfficeBongoOpenRightRearDoor", origin + Vector3(2.7, 1.1, 3.1), Vector3(0.22, 2.15, 1.5), BongoVanPlanScript.COLOR_BODY, "van_paint")
	_create_box("SettlementOfficeBongoWheelFrontLeft", origin + Vector3(-1.7, -0.12, -3.85), Vector3(0.45, 0.72, 0.72), BongoVanPlanScript.COLOR_TRIM, "metal")
	_create_box("SettlementOfficeBongoWheelFrontRight", origin + Vector3(1.7, -0.12, -3.85), Vector3(0.45, 0.72, 0.72), BongoVanPlanScript.COLOR_TRIM, "metal")
	_create_box("SettlementOfficeBongoWheelRearLeft", origin + Vector3(-1.7, -0.12, 2.15), Vector3(0.45, 0.72, 0.72), BongoVanPlanScript.COLOR_TRIM, "metal")
	_create_box("SettlementOfficeBongoWheelRearRight", origin + Vector3(1.7, -0.12, 2.15), Vector3(0.45, 0.72, 0.72), BongoVanPlanScript.COLOR_TRIM, "metal")
	var return_button := _create_box(
		BongoVanPlanScript.SETTLEMENT_BONGO_RETURN_BUTTON_NAME,
		BongoVanPlanScript.SETTLEMENT_BONGO_RETURN_BUTTON_POSITION,
		BongoVanPlanScript.SETTLEMENT_BONGO_RETURN_BUTTON_SIZE,
		Color(0.42, 0.05, 0.04),
		"shrine_red"
	)
	return_button.set_script(BongoDepartureButtonScript)
	_add_visual_box_world("SettlementOfficeBongoReturnButtonLamp", BongoVanPlanScript.SETTLEMENT_BONGO_RETURN_BUTTON_POSITION + Vector3(0.0, 0.04, 0.22), Vector3(0.34, 0.14, 0.04), Color(0.95, 0.08, 0.04), "shrine_red")

func _create_extraction_zone() -> void:
	var extraction := ExtractionScene.instantiate()
	add_child(extraction)
	extraction.name = BongoVanPlanScript.RETURN_ZONE_NAME
	extraction.global_position = BongoVanPlanScript.RETURN_ZONE_POSITION

	var collision := extraction.get_node_or_null("CollisionShape3D") as CollisionShape3D
	if collision != null and collision.shape is BoxShape3D:
		var shape := collision.shape.duplicate() as BoxShape3D
		shape.size = BongoVanPlanScript.RETURN_ZONE_SIZE
		collision.shape = shape
	var mesh_instance := extraction.get_node_or_null("MeshInstance3D") as MeshInstance3D
	if mesh_instance != null and mesh_instance.mesh is BoxMesh:
		var mesh := mesh_instance.mesh.duplicate() as BoxMesh
		mesh.size = Vector3(BongoVanPlanScript.RETURN_ZONE_SIZE.x, 0.08, BongoVanPlanScript.RETURN_ZONE_SIZE.z)
		mesh.material = _make_material("metal", Color(0.12, 0.18, 0.16, 0.35))
		mesh_instance.mesh = mesh

func _spawn_planned_artifacts(main: Node) -> void:
	for spawn_data: Dictionary in EstateLayoutPlanScript.get_artifact_spawns():
		var tags := _string_tags(spawn_data["tags"])
		var positions: Array = spawn_data["positions"]
		for position: Vector3 in positions:
			_spawn_artifact(
				main,
				str(spawn_data["display_name"]),
				int(spawn_data["value"]),
				float(spawn_data["weight"]),
				int(spawn_data["resentment"]),
				position,
				tags,
				int(spawn_data["hand_slots"])
			)

func _create_floor(label: String, position: Vector3, size: Vector3, color: Color, material_key: String = "") -> StaticBody3D:
	return _create_box(label, position, size, color, material_key)

func _create_tall_wall_box(label: String, position: Vector3, size: Vector3, material_key: String, min_height: float = 4.0) -> StaticBody3D:
	var raised_size := _heightened_size(size, min_height)
	var raised_position := _keep_bottom_position(position, size, raised_size)
	return _create_box(label, raised_position, raised_size, _fallback_color(material_key), material_key)

func _create_box(label: String, position: Vector3, size: Vector3, color: Color, material_key: String = "") -> StaticBody3D:
	var adjusted := _adjust_estate_structure_box(label, position, size)
	position = adjusted["position"]
	size = adjusted["size"]
	var body := StaticBody3D.new()
	body.name = label
	add_child(body)
	body.global_position = position
	_add_mesh_and_collision(body, size, color, material_key)
	return body

func _heightened_size(size: Vector3, min_height: float) -> Vector3:
	return Vector3(size.x, max(size.y, min_height), size.z)

func _keep_bottom_position(position: Vector3, original_size: Vector3, raised_size: Vector3) -> Vector3:
	var bottom_y := position.y - original_size.y * 0.5
	return Vector3(position.x, bottom_y + raised_size.y * 0.5, position.z)

func _create_child_body(parent: Node3D, label: String, local_position: Vector3, size: Vector3, color: Color, material_key: String = "") -> StaticBody3D:
	var adjusted := _adjust_estate_structure_box(label, local_position, size)
	local_position = adjusted["position"]
	size = adjusted["size"]
	var body := StaticBody3D.new()
	body.name = label
	parent.add_child(body)
	body.position = local_position
	_add_mesh_and_collision(body, size, color, material_key)
	return body

func _adjust_estate_structure_box(label: String, position: Vector3, size: Vector3) -> Dictionary:
	if _skip_estate_height_adjustment(label):
		return {"position": position, "size": size}
	if _should_snap_estate_box(label):
		var snapped := _snap_estate_footprint(position, size)
		position = snapped["position"]
		size = snapped["size"]
	if _is_estate_wall_like(label):
		var raised_size := _heightened_size(size, ESTATE_WALL_MIN_HEIGHT)
		return {"position": _keep_bottom_position(position, size, raised_size), "size": raised_size}
	if label.contains("Roof") and size.y <= 0.8:
		position.y = max(position.y, ESTATE_ROOF_MIN_CENTER_Y)
	return {"position": position, "size": size}

func _should_snap_estate_box(label: String) -> bool:
	return (
		label.contains("Wall")
		or label.contains("Gate")
		or label.contains("Roof")
		or label.contains("Floor")
		or label.contains("Ground")
		or label.contains("Road")
		or label.contains("Threshold")
		or label.contains("Stitch")
		or label.contains("Seal")
		or label.contains("Chamber")
		or label.contains("Porch")
	)

func _snap_estate_footprint(position: Vector3, size: Vector3) -> Dictionary:
	return {
		"position": Vector3(_snap_float(position.x), position.y, _snap_float(position.z)),
		"size": Vector3(_snap_size_component(size.x), size.y, _snap_size_component(size.z)),
	}

func _snap_size_component(value: float) -> float:
	return max(ESTATE_BOX_SNAP, _snap_float(value))

func _snap_float(value: float) -> float:
	return round(value / ESTATE_BOX_SNAP) * ESTATE_BOX_SNAP

func _skip_estate_height_adjustment(label: String) -> bool:
	return (
		label.begins_with("Bongo")
		or label.begins_with("Settlement")
		or label.begins_with("StoredCargo")
		or label.begins_with("Threat")
	)

func _is_estate_wall_like(label: String) -> bool:
	return (
		label.contains("Wall")
		or label.contains("Post")
		or label.contains("Pillar")
		or label.contains("GateBypassBlock")
		or label.contains("GateSideSeam")
		or label.contains("ApproachWall")
	)

func _create_cylinder(label: String, position: Vector3, radius: float, height: float, color: Color, material_key: String = "") -> StaticBody3D:
	var body := StaticBody3D.new()
	body.name = label
	add_child(body)
	body.global_position = position

	var mesh_instance := MeshInstance3D.new()
	var mesh := CylinderMesh.new()
	mesh.top_radius = radius
	mesh.bottom_radius = radius
	mesh.height = height
	mesh.radial_segments = 18
	mesh.material = _make_material(material_key, color)
	mesh_instance.mesh = mesh
	body.add_child(mesh_instance)

	var collision := CollisionShape3D.new()
	collision.name = "CollisionShape3D"
	var shape := CylinderShape3D.new()
	shape.radius = radius
	shape.height = height
	collision.shape = shape
	body.add_child(collision)
	return body

func _add_mesh_and_collision(body: StaticBody3D, size: Vector3, color: Color, material_key: String = "") -> void:
	var mesh_instance := MeshInstance3D.new()
	var box_mesh := BoxMesh.new()
	box_mesh.size = size
	box_mesh.material = _make_material(material_key, color)
	mesh_instance.mesh = box_mesh
	body.add_child(mesh_instance)

	var collision := CollisionShape3D.new()
	collision.name = "CollisionShape3D"
	var shape := BoxShape3D.new()
	shape.size = size
	collision.shape = shape
	body.add_child(collision)

func _add_visual_box(parent: Node3D, label: String, local_position: Vector3, size: Vector3, color: Color, material_key: String = "") -> void:
	var mesh_instance := MeshInstance3D.new()
	mesh_instance.name = label
	mesh_instance.position = local_position
	var box_mesh := BoxMesh.new()
	box_mesh.size = size
	box_mesh.material = _make_material(material_key, color)
	mesh_instance.mesh = box_mesh
	parent.add_child(mesh_instance)

func _add_visual_box_world(label: String, position: Vector3, size: Vector3, color: Color, material_key: String = "", rotation: Vector3 = Vector3.ZERO) -> void:
	var mesh_instance := MeshInstance3D.new()
	mesh_instance.name = label
	add_child(mesh_instance)
	mesh_instance.global_position = position
	mesh_instance.rotation_degrees = rotation
	var box_mesh := BoxMesh.new()
	box_mesh.size = size
	box_mesh.material = _make_material(material_key, color)
	mesh_instance.mesh = box_mesh

func _make_material(material_key: String, fallback_color: Color) -> StandardMaterial3D:
	var external_material := _try_make_external_material(material_key)
	if external_material != null:
		return _finalize_material(external_material)
	var surface_name := str(MATERIAL_SURFACES.get(material_key, ""))
	if surface_name != "":
		return _finalize_material(VisualPaletteScript.make_material(surface_name))
	var material := StandardMaterial3D.new()
	material.albedo_color = fallback_color
	material.roughness = 0.86
	return _finalize_material(material)

func _finalize_material(material: StandardMaterial3D) -> StandardMaterial3D:
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	return material

func _try_make_external_material(material_key: String) -> StandardMaterial3D:
	if PerformanceSettingsScript.is_low_spec_mode():
		return null
	var data: Dictionary = EXTERNAL_MATERIALS_BY_KEY.get(material_key, {})
	if data.is_empty():
		return null
	var asset_name := str(data.get("asset", ""))
	if asset_name == "":
		return null
	var uv_scale := float(data.get("uv_scale", 1.0))
	var cache_key := "%s|%s" % [material_key, uv_scale]
	if _material_cache.has(cache_key):
		var cached_material := _material_cache[cache_key] as StandardMaterial3D
		if cached_material != null:
			return cached_material.duplicate(true) as StandardMaterial3D

	var color_texture := _load_external_texture(asset_name, "Color")
	var normal_texture := _load_external_texture(asset_name, "NormalGL")
	if color_texture == null or normal_texture == null:
		return null
	var material := StandardMaterial3D.new()
	material.resource_local_to_scene = true
	material.albedo_texture = color_texture
	material.normal_enabled = true
	material.normal_texture = normal_texture
	material.roughness = 0.9
	var roughness_texture := _load_external_texture(asset_name, "Roughness")
	if roughness_texture != null:
		material.roughness_texture = roughness_texture
	var ao_texture := _load_external_texture(asset_name, "AmbientOcclusion")
	if ao_texture != null:
		material.ao_enabled = true
		material.ao_texture = ao_texture
	material.uv1_scale = Vector3(uv_scale, uv_scale, 1.0)
	_material_cache[cache_key] = material
	return material.duplicate(true) as StandardMaterial3D

func _load_external_texture(asset_name: String, texture_type: String) -> Texture2D:
	var resource_path := "res://assets/external/ambientcg/materials/%s/%s_1K-JPG_%s.jpg" % [asset_name, asset_name, texture_type]
	if _texture_cache.has(resource_path):
		return _texture_cache[resource_path] as Texture2D
	if not FileAccess.file_exists(resource_path):
		return null
	var image := Image.new()
	var error := image.load(ProjectSettings.globalize_path(resource_path))
	if error != OK:
		return null
	var texture := ImageTexture.create_from_image(image)
	_texture_cache[resource_path] = texture
	return texture

func _fallback_color(material_key: String) -> Color:
	match material_key:
		"clay_pot":
			return Color(0.28, 0.14, 0.075)
		"well_stone":
			return Color(0.19, 0.19, 0.17)
		"dead_tree":
			return Color(0.11, 0.07, 0.04)
		"paper":
			return Color(0.62, 0.595, 0.5)
		"mud":
			return Color(0.075, 0.105, 0.07)
		"wet_moss":
			return VisualPaletteScript.color("wet_moss", Color(0.055, 0.105, 0.07))
		"shadow":
			return Color(0.035, 0.05, 0.04)
		_:
			var surface_name := str(MATERIAL_SURFACES.get(material_key, ""))
			if surface_name != "":
				return VisualPaletteScript.color(surface_name, Color(0.3, 0.3, 0.28))
			return Color(0.3, 0.3, 0.28)

func _bongo_part_name(original_name: String) -> String:
	match original_name:
		"BongoLeftCargoWall":
			return "BongoLeftWall"
		"BongoRightCargoWall":
			return "BongoRightWall"
		"BongoHighCargoRoof":
			return "BongoRoof"
		"BongoCabOverCabin":
			return "BongoCabin"
		"BongoFlatWindshield":
			return "BongoWindshield"
		"BongoBlackFrontBumper":
			return "BongoFrontBumper"
		"BongoThinFrontGrille":
			return "BongoFrontGrille"
		"BongoFrontLeftWheel":
			return "BongoWheelFrontLeft"
		"BongoFrontRightWheel":
			return "BongoWheelFrontRight"
		"BongoRearLeftWheel":
			return "BongoWheelRearLeft"
		"BongoRearRightWheel":
			return "BongoWheelRearRight"
		_:
			return original_name

func _bongo_material_key(label: String) -> String:
	if label.contains("Windshield"):
		return "glass"
	if label.contains("Bumper") or label.contains("Grille") or label.contains("Wheel") or label.contains("Step") or label.contains("Ramp"):
		return "metal"
	return "van_paint"

func _string_tags(source: Array) -> Array[String]:
	var result: Array[String] = []
	for value in source:
		result.append(str(value))
	return result

func _spawn_artifact(main: Node, display_name: String, value: int, weight: float, resentment_gain: int, position: Vector3, tags: Array[String], hand_slots: int) -> void:
	var artifact := ArtifactScene.instantiate()
	add_child(artifact)
	artifact.display_name = display_name
	artifact.value = value
	artifact.weight = weight
	artifact.resentment_gain = resentment_gain
	artifact.tags = tags
	artifact.hand_slots = hand_slots
	if artifact.has_method("configure_visuals"):
		artifact.configure_visuals()
	artifact.global_position = position
	if main.has_method("register_artifact"):
		main.register_artifact(artifact)
