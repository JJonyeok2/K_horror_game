extends RefCounted
class_name EstateLayoutPlan

const PLAN_NAME := "LargeHanokEstate"
const PLAN_VERSION := 1
const SPAWN_POINT := Vector3(0.0, 1.0, 26.0)
const REQUIRED_ROUTE_GATE := "OuterEstateGate"

const MATERIAL_KEYS := {
	"packed_earth": "packed_earth",
	"mud": "mud",
	"stone": "stone",
	"dark_stone": "dark_stone",
	"old_plaster": "old_plaster",
	"aged_wood": "aged_wood",
	"black_wood": "black_wood",
	"roof_tile": "roof_tile",
	"straw": "straw",
	"clay_pot": "clay_pot",
	"well_stone": "well_stone",
	"shrine_red": "shrine_red",
	"dead_tree": "dead_tree",
	"shadow": "shadow"
}

const STABLE_NAMES := [
	"OuterEstateGate",
	"RiskySidePassage",
	"LargeInnerCourtyard",
	"BackyardLoop",
	"ShrineDeepZone"
]

const PATH_CONTEXT := {
	"required_route": [
		"VanStartGround",
		"LongApproachRoad",
		"OuterEstateGate",
		"GateThreshold",
		"LargeInnerCourtyard",
		"BuildingFrontPad",
		"MainHouseInterior",
		"BackyardLoop",
		"ShrineDeepZone"
	],
	"optional_risky_route": [
		"VanStartGround",
		"OuterWallSideSlip",
		"RiskySidePassage",
		"SideYardChoke",
		"LargeInnerCourtyard"
	],
	"loop_routes": {
		"storehouse_loop": [
			"LargeInnerCourtyard",
			"LeftWallGap",
			"StorehouseLoop",
			"BackKitchenYard",
			"MainHouseInterior"
		],
		"backyard_loop": [
			"MainHouseInterior",
			"RightWallGap",
			"BackyardLoop",
			"ShrineDeepZone",
			"StorehouseLoop"
		]
	},
	"route_notes": {
		"OuterEstateGate": "Proper required route through the main estate gate.",
		"RiskySidePassage": "Narrow side path beside the gate; faster but cramped, noisy, and exposed.",
		"LargeInnerCourtyard": "Primary orientation space before paths split.",
		"BackyardLoop": "Rear loop with outhouse, jars, trees, and broken wall sightlines.",
		"ShrineDeepZone": "Deepest ritual endpoint with highest resentment pressure."
	}
}

const FLOOR_ZONES := [
	{
		"name": "VanStartGround",
		"position": Vector3(0.0, 0.0, 26.0),
		"size": Vector3(24.0, 0.35, 16.0),
		"material_key": "packed_earth",
		"tags": ["spawn", "exterior"]
	},
	{
		"name": "LongApproachRoad",
		"position": Vector3(0.0, 0.02, 8.0),
		"size": Vector3(8.5, 0.25, 34.0),
		"material_key": "packed_earth",
		"tags": ["required_route", "approach"]
	},
	{
		"name": "OuterWallSideSlip",
		"position": Vector3(-8.7, 0.02, -1.0),
		"size": Vector3(2.4, 0.22, 25.0),
		"material_key": "mud",
		"tags": ["optional_route", "risk_setup"]
	},
	{
		"name": "GateThreshold",
		"position": Vector3(0.0, 0.05, -12.0),
		"size": Vector3(16.0, 0.30, 8.0),
		"material_key": "stone",
		"tags": ["required_route", "gate"]
	},
	{
		"name": "RiskySidePassage",
		"position": Vector3(-11.7, 0.03, -16.0),
		"size": Vector3(2.6, 0.24, 30.0),
		"material_key": "shadow",
		"tags": ["optional_route", "narrow", "risk"]
	},
	{
		"name": "LargeInnerCourtyard",
		"position": Vector3(0.0, 0.0, -40.0),
		"size": Vector3(58.0, 0.35, 48.0),
		"material_key": "packed_earth",
		"tags": ["required_route", "courtyard", "orientation"]
	},
	{
		"name": "LeftSideYard",
		"position": Vector3(-31.0, 0.0, -47.0),
		"size": Vector3(12.0, 0.30, 52.0),
		"material_key": "mud",
		"tags": ["side_yard", "flank"]
	},
	{
		"name": "SideYardChoke",
		"position": Vector3(-23.0, 0.02, -29.0),
		"size": Vector3(6.0, 0.26, 12.0),
		"material_key": "dark_stone",
		"tags": ["choke", "risk_exit"]
	},
	{
		"name": "BuildingFrontPad",
		"position": Vector3(0.0, 0.08, -69.0),
		"size": Vector3(34.0, 0.30, 10.0),
		"material_key": "stone",
		"tags": ["required_route", "house_front"]
	},
	{
		"name": "ToenmaruPorch",
		"position": Vector3(0.0, 0.42, -75.0),
		"size": Vector3(38.0, 0.55, 5.0),
		"material_key": "aged_wood",
		"tags": ["툇마루", "porch", "transition"]
	},
	{
		"name": "MainHouseInterior",
		"position": Vector3(0.0, 0.05, -86.0),
		"size": Vector3(32.0, 0.30, 25.0),
		"material_key": "old_plaster",
		"tags": ["required_route", "interior"]
	},
	{
		"name": "StorehouseLoop",
		"position": Vector3(-25.0, 0.04, -92.0),
		"size": Vector3(16.0, 0.30, 44.0),
		"material_key": "mud",
		"tags": ["loop", "storehouse", "less_straight"]
	},
	{
		"name": "BackKitchenYard",
		"position": Vector3(-8.0, 0.03, -108.0),
		"size": Vector3(24.0, 0.26, 18.0),
		"material_key": "packed_earth",
		"tags": ["rear", "connector"]
	},
	{
		"name": "BackyardLoop",
		"position": Vector3(25.0, 0.04, -96.0),
		"size": Vector3(18.0, 0.30, 54.0),
		"material_key": "mud",
		"tags": ["loop", "backyard", "less_straight"]
	},
	{
		"name": "OuthouseYard",
		"position": Vector3(36.0, 0.03, -107.0),
		"size": Vector3(10.0, 0.26, 16.0),
		"material_key": "mud",
		"tags": ["뒷간", "dead_end_feint"]
	},
	{
		"name": "ShrineApproach",
		"position": Vector3(0.0, 0.04, -119.0),
		"size": Vector3(18.0, 0.30, 24.0),
		"material_key": "dark_stone",
		"tags": ["deep", "ritual"]
	},
	{
		"name": "ShrineDeepZone",
		"position": Vector3(0.0, 0.05, -136.0),
		"size": Vector3(22.0, 0.32, 18.0),
		"material_key": "shrine_red",
		"tags": ["deepest", "shrine", "high_resentment"]
	}
]

const WALL_SEGMENTS := [
	{"name": "ApproachWallLeft", "position": Vector3(-5.1, 1.3, 8.0), "size": Vector3(0.7, 2.6, 34.0), "material_key": "old_plaster", "tags": ["approach"]},
	{"name": "ApproachWallRight", "position": Vector3(5.1, 1.3, 8.0), "size": Vector3(0.7, 2.6, 34.0), "material_key": "old_plaster", "tags": ["approach"]},
	{"name": "OuterEstateGateLeftWall", "position": Vector3(-10.6, 1.8, -12.0), "size": Vector3(9.2, 3.6, 0.9), "material_key": "old_plaster", "tags": ["OuterEstateGate", "required_route"]},
	{"name": "OuterEstateGateRightWall", "position": Vector3(10.6, 1.8, -12.0), "size": Vector3(9.2, 3.6, 0.9), "material_key": "old_plaster", "tags": ["OuterEstateGate", "required_route"]},
	{"name": "RiskyPassageOuterWall", "position": Vector3(-13.5, 1.45, -16.0), "size": Vector3(0.6, 2.9, 31.0), "material_key": "old_plaster", "tags": ["RiskySidePassage", "risk"]},
	{"name": "RiskyPassageInnerWallA", "position": Vector3(-9.8, 1.45, -8.0), "size": Vector3(0.55, 2.9, 12.0), "material_key": "old_plaster", "tags": ["RiskySidePassage", "staggered"]},
	{"name": "RiskyPassageInnerWallB", "position": Vector3(-9.8, 1.45, -25.0), "size": Vector3(0.55, 2.9, 12.0), "material_key": "old_plaster", "tags": ["RiskySidePassage", "staggered"]},
	{"name": "RiskyPassageLowBeam", "position": Vector3(-11.7, 2.05, -20.0), "size": Vector3(3.7, 0.35, 0.5), "material_key": "black_wood", "tags": ["RiskySidePassage", "obstacle"]},
	{"name": "CourtyardOuterWallLeft", "position": Vector3(-36.5, 1.55, -47.0), "size": Vector3(0.8, 3.1, 70.0), "material_key": "old_plaster", "tags": ["courtyard", "outer"]},
	{"name": "CourtyardOuterWallRight", "position": Vector3(36.5, 1.55, -50.0), "size": Vector3(0.8, 3.1, 76.0), "material_key": "old_plaster", "tags": ["courtyard", "outer"]},
	{"name": "CourtyardBackWallLeft", "position": Vector3(-21.0, 1.55, -63.5), "size": Vector3(18.0, 3.1, 0.8), "material_key": "old_plaster", "tags": ["courtyard", "building_front"]},
	{"name": "CourtyardBackWallRight", "position": Vector3(21.0, 1.55, -63.5), "size": Vector3(18.0, 3.1, 0.8), "material_key": "old_plaster", "tags": ["courtyard", "building_front"]},
	{"name": "LeftWallGapNorth", "position": Vector3(-16.4, 1.6, -78.0), "size": Vector3(0.7, 3.2, 10.0), "material_key": "old_plaster", "tags": ["wall_gap", "storehouse_loop"]},
	{"name": "LeftWallGapSouth", "position": Vector3(-16.4, 1.6, -98.0), "size": Vector3(0.7, 3.2, 12.0), "material_key": "old_plaster", "tags": ["wall_gap", "storehouse_loop"]},
	{"name": "RightWallGapNorth", "position": Vector3(16.4, 1.6, -78.0), "size": Vector3(0.7, 3.2, 9.0), "material_key": "old_plaster", "tags": ["wall_gap", "backyard_loop"]},
	{"name": "RightWallGapSouth", "position": Vector3(16.4, 1.6, -99.0), "size": Vector3(0.7, 3.2, 11.0), "material_key": "old_plaster", "tags": ["wall_gap", "backyard_loop"]},
	{"name": "MainHouseBackWall", "position": Vector3(0.0, 1.75, -99.0), "size": Vector3(33.0, 3.5, 0.8), "material_key": "old_plaster", "tags": ["main_house"]},
	{"name": "StorehouseOuterWall", "position": Vector3(-33.5, 1.35, -92.0), "size": Vector3(0.7, 2.7, 45.0), "material_key": "old_plaster", "tags": ["storehouse_loop"]},
	{"name": "StorehouseBrokenWallA", "position": Vector3(-18.0, 1.35, -83.0), "size": Vector3(0.7, 2.7, 9.0), "material_key": "old_plaster", "tags": ["wall_break", "storehouse_loop"]},
	{"name": "StorehouseBrokenWallB", "position": Vector3(-18.0, 1.35, -105.0), "size": Vector3(0.7, 2.7, 10.0), "material_key": "old_plaster", "tags": ["wall_break", "storehouse_loop"]},
	{"name": "BackyardOuterWall", "position": Vector3(35.0, 1.35, -96.0), "size": Vector3(0.7, 2.7, 55.0), "material_key": "old_plaster", "tags": ["backyard_loop"]},
	{"name": "BackyardBrokenWallA", "position": Vector3(17.0, 1.35, -84.0), "size": Vector3(0.7, 2.7, 10.0), "material_key": "old_plaster", "tags": ["wall_break", "backyard_loop"]},
	{"name": "BackyardBrokenWallB", "position": Vector3(17.0, 1.35, -109.0), "size": Vector3(0.7, 2.7, 10.0), "material_key": "old_plaster", "tags": ["wall_break", "backyard_loop"]},
	{"name": "ShrineLeftWall", "position": Vector3(-11.5, 1.45, -135.0), "size": Vector3(0.7, 2.9, 26.0), "material_key": "shrine_red", "tags": ["shrine"]},
	{"name": "ShrineRightWall", "position": Vector3(11.5, 1.45, -135.0), "size": Vector3(0.7, 2.9, 26.0), "material_key": "shrine_red", "tags": ["shrine"]},
	{"name": "ShrineBackWall", "position": Vector3(0.0, 1.45, -145.5), "size": Vector3(23.0, 2.9, 0.7), "material_key": "shrine_red", "tags": ["shrine", "deepest"]}
]

const LANDMARKS := [
	{"name": "OuterEstateGate", "kind": "gate", "position": Vector3(0.0, 0.0, -12.0), "size": Vector3(9.0, 4.2, 1.1), "material_key": "aged_wood", "tags": ["required_route", "interaction"]},
	{"name": "GateWarningPost", "kind": "post", "position": Vector3(-6.7, 1.6, -8.5), "size": Vector3(0.45, 3.2, 0.45), "material_key": "shrine_red", "tags": ["warning"]},
	{"name": "SidePassageNoiseJar", "kind": "jar", "position": Vector3(-12.3, 0.5, -22.5), "size": Vector3(0.75, 1.0, 0.75), "material_key": "clay_pot", "tags": ["RiskySidePassage", "noise"]},
	{"name": "CourtyardWell", "kind": "우물", "position": Vector3(-15.0, 0.65, -41.0), "size": Vector3(2.2, 1.3, 2.2), "material_key": "well_stone", "tags": ["LargeInnerCourtyard", "landmark"]},
	{"name": "JangdokdaeClusterA", "kind": "장독대", "position": Vector3(18.0, 0.5, -45.0), "size": Vector3(5.0, 1.0, 3.0), "material_key": "clay_pot", "tags": ["LargeInnerCourtyard", "cover"]},
	{"name": "JangdokdaeClusterB", "kind": "장독대", "position": Vector3(-24.5, 0.5, -88.0), "size": Vector3(4.5, 1.0, 3.5), "material_key": "clay_pot", "tags": ["StorehouseLoop", "noise"]},
	{"name": "ToenmaruLongStep", "kind": "툇마루", "position": Vector3(0.0, 0.75, -75.0), "size": Vector3(36.0, 0.9, 3.0), "material_key": "aged_wood", "tags": ["porch", "transition"]},
	{"name": "DeadTreeCourtyard", "kind": "tree", "position": Vector3(12.5, 1.6, -33.0), "size": Vector3(1.2, 3.2, 1.2), "material_key": "dead_tree", "tags": ["LargeInnerCourtyard", "sightline"]},
	{"name": "BackyardTreeLineA", "kind": "trees", "position": Vector3(29.0, 1.8, -84.0), "size": Vector3(4.0, 3.6, 8.0), "material_key": "dead_tree", "tags": ["BackyardLoop", "cover"]},
	{"name": "BackyardTreeLineB", "kind": "trees", "position": Vector3(29.5, 1.8, -111.0), "size": Vector3(4.0, 3.6, 8.0), "material_key": "dead_tree", "tags": ["BackyardLoop", "cover"]},
	{"name": "DwitganOuthouse", "kind": "뒷간", "position": Vector3(37.5, 1.2, -108.0), "size": Vector3(4.0, 2.4, 4.5), "material_key": "straw", "tags": ["OuthouseYard", "dead_end_feint"]},
	{"name": "StorehouseShed", "kind": "storehouse", "position": Vector3(-27.0, 1.2, -101.0), "size": Vector3(8.0, 2.4, 7.0), "material_key": "aged_wood", "tags": ["StorehouseLoop", "landmark"]},
	{"name": "BrokenWallGapStorehouse", "kind": "wall_break", "position": Vector3(-18.0, 1.0, -94.0), "size": Vector3(1.8, 2.0, 5.0), "material_key": "old_plaster", "tags": ["wall_break", "less_straight"]},
	{"name": "BrokenWallGapBackyard", "kind": "wall_break", "position": Vector3(17.0, 1.0, -96.0), "size": Vector3(1.8, 2.0, 5.5), "material_key": "old_plaster", "tags": ["wall_break", "less_straight"]},
	{"name": "ShrineAltar", "kind": "제단", "position": Vector3(0.0, 0.8, -141.0), "size": Vector3(5.0, 1.2, 1.6), "material_key": "black_wood", "tags": ["ShrineDeepZone", "ritual"]},
	{"name": "ShrinePaperCharms", "kind": "charm_cluster", "position": Vector3(0.0, 1.7, -143.0), "size": Vector3(6.5, 2.2, 0.2), "material_key": "shrine_red", "tags": ["ShrineDeepZone", "ritual"]}
]

const ARTIFACT_SPAWNS := [
	{
		"id": "brass_spoon_courtyard",
		"display_name": "놋수저",
		"value": 90,
		"weight": 0.8,
		"resentment": 1,
		"tags": ["ancestor_item", "courtyard", "small"],
		"hand_slots": 1,
		"positions": [Vector3(-13.5, 0.55, -44.0), Vector3(16.0, 0.55, -47.0)]
	},
	{
		"id": "family_register",
		"display_name": "족보",
		"value": 320,
		"weight": 1.1,
		"resentment": 3,
		"tags": ["document_item", "main_house", "ancestor_item"],
		"hand_slots": 1,
		"positions": [Vector3(-6.0, 0.65, -87.0), Vector3(5.5, 0.65, -91.0)]
	},
	{
		"id": "norigae_porch",
		"display_name": "낡은 노리개",
		"value": 180,
		"weight": 0.4,
		"resentment": 2,
		"tags": ["personal_item", "porch", "small"],
		"hand_slots": 1,
		"positions": [Vector3(12.0, 0.95, -75.0), Vector3(-11.5, 0.95, -75.0)]
	},
	{
		"id": "jar_lid_backyard",
		"display_name": "금 간 장독 뚜껑",
		"value": 130,
		"weight": 1.7,
		"resentment": 2,
		"tags": ["장독대", "noise", "backyard"],
		"hand_slots": 1,
		"positions": [Vector3(18.5, 0.8, -45.5), Vector3(-24.0, 0.8, -88.5)]
	},
	{
		"id": "well_rope_charm",
		"display_name": "우물 금줄",
		"value": 240,
		"weight": 1.0,
		"resentment": 3,
		"tags": ["우물", "ritual", "courtyard"],
		"hand_slots": 1,
		"positions": [Vector3(-15.0, 1.2, -41.0)]
	},
	{
		"id": "outhouse_lantern",
		"display_name": "뒷간 등잔",
		"value": 210,
		"weight": 1.4,
		"resentment": 3,
		"tags": ["뒷간", "backyard", "risk"],
		"hand_slots": 1,
		"positions": [Vector3(37.0, 0.65, -108.5)]
	},
	{
		"id": "storehouse_lockbox",
		"display_name": "뒤주 열쇠함",
		"value": 460,
		"weight": 2.6,
		"resentment": 4,
		"tags": ["storehouse", "heavy", "loop_reward"],
		"hand_slots": 2,
		"positions": [Vector3(-27.5, 0.75, -101.0), Vector3(-30.0, 0.75, -92.0)]
	},
	{
		"id": "ancestor_tablet",
		"display_name": "위패",
		"value": 760,
		"weight": 2.0,
		"resentment": 6,
		"tags": ["shrine_item", "ancestor_item", "high_value"],
		"hand_slots": 2,
		"positions": [Vector3(0.0, 1.25, -141.0)]
	},
	{
		"id": "altar_bell",
		"display_name": "제단 방울",
		"value": 620,
		"weight": 0.9,
		"resentment": 5,
		"tags": ["제단", "shrine_item", "noise"],
		"hand_slots": 1,
		"positions": [Vector3(-2.5, 1.25, -140.8), Vector3(2.5, 1.25, -140.8)]
	}
]

static func get_floor_zones() -> Array:
	return FLOOR_ZONES.duplicate(true)

static func get_wall_segments() -> Array:
	return WALL_SEGMENTS.duplicate(true)

static func get_landmarks() -> Array:
	return LANDMARKS.duplicate(true)

static func get_artifact_spawns() -> Array:
	return ARTIFACT_SPAWNS.duplicate(true)

static func get_path_context() -> Dictionary:
	return PATH_CONTEXT.duplicate(true)

static func get_stable_names() -> Array:
	return STABLE_NAMES.duplicate()
