extends RefCounted
class_name BongoVanPlan

const COORDINATE_SPACE = "world"
const WORLD_OFFSET := Vector3(0.0, 0.0, 260.0)
const VAN_ORIGIN := Vector3(0.0, 0.35, 273.2)

const PLAYER_HEIGHT := 1.8
const PLAYER_CAMERA_LOCAL_Y := 1.55
const CARGO_FLOOR_TOP_Y := 0.44
const PLAYER_START_POSITION := Vector3(0.0, 1.34, 272.25)
const PLAYER_START_YAW_DEGREES := 0.0
const PLAYER_START_CAMERA_Y := 2.89
const ROOF_UNDERSIDE_Y := 3.21
const CAMERA_CLEARANCE_UNDER_ROOF := 0.32

const RETURN_ZONE_NAME = "VanInteriorReturnZone"
const RETURN_ZONE_POSITION := Vector3(0.0, 0.55, 273.35)
const RETURN_ZONE_SIZE := Vector3(2.8, 2.1, 2.4)

const COLOR_BODY := Color(0.72, 0.68, 0.56)
const COLOR_BODY_SHADOW := Color(0.56, 0.53, 0.44)
const COLOR_FLOOR := Color(0.18, 0.17, 0.14)
const COLOR_TRIM := Color(0.08, 0.08, 0.075)
const COLOR_GLASS := Color(0.16, 0.27, 0.32)
const COLOR_LIGHT := Color(0.94, 0.86, 0.58)
const COLOR_TAIL_LIGHT := Color(0.72, 0.08, 0.05)
const COLOR_MONITOR_BACKING := Color(0.035, 0.04, 0.035)
const COLOR_MONITOR_SCREEN := Color(0.02, 0.17, 0.13)
const COLOR_MONITOR_GLOW := Color(0.42, 0.95, 0.72)

const QUOTA_MONITOR_NAME = "BongoQuotaMonitor"
const QUOTA_MONITOR_TEXT = "최종 ₩0 / 목표\n미정산 ₩0"
const QUOTA_MONITOR_POSITION := Vector3(0.0, 1.62, 276.0)
const QUOTA_MONITOR_BACKING_SIZE := Vector3(1.42, 0.88, 0.12)
const QUOTA_MONITOR_SCREEN_SIZE := Vector3(1.12, 0.58, 0.08)
const SETTLEMENT_STATION_NAME = "BongoSettlementStation"
const SETTLEMENT_STATION_POSITION := Vector3(1.08, 0.95, 275.45)
const SETTLEMENT_STATION_SIZE := Vector3(0.72, 0.82, 0.48)
const STORED_CARGO_START_POSITION := Vector3(-0.95, 0.72, 272.65)

const STRUCTURAL_PARTS = [
	{
		"name": "BongoInteriorFloor",
		"position": Vector3(0.0, 0.35, 13.2),
		"size": Vector3(3.8, 0.18, 6.2),
		"color": COLOR_FLOOR,
		"collision": true,
	},
	{
		"name": "BongoLeftCargoWall",
		"position": Vector3(-2.05, 1.875, 13.45),
		"size": Vector3(0.22, 2.87, 5.65),
		"color": COLOR_BODY,
		"collision": true,
	},
	{
		"name": "BongoRightCargoWall",
		"position": Vector3(2.05, 1.875, 13.45),
		"size": Vector3(0.22, 2.87, 5.65),
		"color": COLOR_BODY,
		"collision": true,
	},
	{
		"name": "BongoHighCargoRoof",
		"position": Vector3(0.0, 3.32, 13.45),
		"size": Vector3(4.25, 0.22, 5.65),
		"color": COLOR_BODY,
		"collision": true,
		"underside_y": ROOF_UNDERSIDE_Y,
	},
	{
		"name": "BongoCabinBulkhead",
		"position": Vector3(0.0, 1.82, 16.35),
		"size": Vector3(4.1, 2.76, 0.22),
		"color": COLOR_BODY_SHADOW,
		"collision": true,
	},
	{
		"name": "BongoOpenLeftRearDoor",
		"position": Vector3(-2.7, 1.55, 10.05),
		"size": Vector3(0.22, 2.2, 1.55),
		"color": COLOR_BODY,
		"collision": true,
	},
	{
		"name": "BongoOpenRightRearDoor",
		"position": Vector3(2.7, 1.55, 10.05),
		"size": Vector3(0.22, 2.2, 1.55),
		"color": COLOR_BODY,
		"collision": true,
	},
	{
		"name": "BongoRearStep",
		"position": Vector3(0.0, 0.3, 9.55),
		"size": Vector3(3.45, 0.24, 0.85),
		"color": COLOR_TRIM,
		"collision": true,
	},
	{
		"name": "BongoLowExitRamp",
		"position": Vector3(0.0, 0.215, 8.75),
		"size": Vector3(3.2, 0.14, 1.1),
		"rotation_degrees": Vector3(-3.5, 0.0, 0.0),
		"color": COLOR_TRIM,
		"collision": true,
	},
	{
		"name": "BongoCabOverCabin",
		"position": Vector3(0.0, 1.36, 17.55),
		"size": Vector3(3.8, 1.85, 2.1),
		"color": COLOR_BODY,
		"collision": true,
	},
]

const VISUAL_PARTS = [
	{
		"name": "BongoFlatWindshield",
		"position": Vector3(0.0, 1.73, 18.66),
		"size": Vector3(2.55, 0.78, 0.08),
		"color": COLOR_GLASS,
		"collision": false,
	},
	{
		"name": "BongoBlackFrontBumper",
		"position": Vector3(0.0, 0.47, 18.73),
		"size": Vector3(4.0, 0.28, 0.25),
		"color": COLOR_TRIM,
		"collision": false,
	},
	{
		"name": "BongoThinFrontGrille",
		"position": Vector3(0.0, 0.92, 18.78),
		"size": Vector3(1.4, 0.25, 0.08),
		"color": COLOR_TRIM,
		"collision": false,
	},
	{
		"name": "BongoLeftHeadlight",
		"position": Vector3(-1.1, 0.91, 18.82),
		"size": Vector3(0.48, 0.22, 0.07),
		"color": COLOR_LIGHT,
		"collision": false,
	},
	{
		"name": "BongoRightHeadlight",
		"position": Vector3(1.1, 0.91, 18.82),
		"size": Vector3(0.48, 0.22, 0.07),
		"color": COLOR_LIGHT,
		"collision": false,
	},
	{
		"name": "BongoLeftSideMirror",
		"position": Vector3(-2.15, 1.65, 17.95),
		"size": Vector3(0.08, 0.34, 0.28),
		"color": COLOR_TRIM,
		"collision": false,
	},
	{
		"name": "BongoRightSideMirror",
		"position": Vector3(2.15, 1.65, 17.95),
		"size": Vector3(0.08, 0.34, 0.28),
		"color": COLOR_TRIM,
		"collision": false,
	},
	{
		"name": "BongoLeftRearTailLight",
		"position": Vector3(-1.72, 0.9, 9.95),
		"size": Vector3(0.18, 0.55, 0.08),
		"color": COLOR_TAIL_LIGHT,
		"collision": false,
	},
	{
		"name": "BongoRightRearTailLight",
		"position": Vector3(1.72, 0.9, 9.95),
		"size": Vector3(0.18, 0.55, 0.08),
		"color": COLOR_TAIL_LIGHT,
		"collision": false,
	},
	{
		"name": "BongoFrontLeftWheel",
		"position": Vector3(-1.83, 0.38, 17.15),
		"size": Vector3(0.28, 0.74, 0.74),
		"color": COLOR_TRIM,
		"collision": false,
	},
	{
		"name": "BongoFrontRightWheel",
		"position": Vector3(1.83, 0.38, 17.15),
		"size": Vector3(0.28, 0.74, 0.74),
		"color": COLOR_TRIM,
		"collision": false,
	},
	{
		"name": "BongoRearLeftWheel",
		"position": Vector3(-1.83, 0.38, 11.25),
		"size": Vector3(0.28, 0.74, 0.74),
		"color": COLOR_TRIM,
		"collision": false,
	},
	{
		"name": "BongoRearRightWheel",
		"position": Vector3(1.83, 0.38, 11.25),
		"size": Vector3(0.28, 0.74, 0.74),
		"color": COLOR_TRIM,
		"collision": false,
	},
]

const ALL_PARTS = STRUCTURAL_PARTS + VISUAL_PARTS
