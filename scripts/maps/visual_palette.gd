extends RefCounted
class_name VisualPalette

# Shared low-saturation night palette for map builders. Values favor damp
# courtyard materials and old estate surfaces over clay-like saturated colors.

const EARTH_ROAD := Color(0.185, 0.165, 0.125)
const PACKED_COURTYARD_DIRT := Color(0.125, 0.145, 0.105)
const STONE_THRESHOLD := Color(0.155, 0.152, 0.135)
const DARK_WOOD := Color(0.095, 0.055, 0.035)
const AGED_PLASTER := Color(0.285, 0.275, 0.235)
const TILE_ROOF := Color(0.075, 0.068, 0.06)
const WET_MOSS := Color(0.055, 0.105, 0.07)
const RITUAL_RED := Color(0.235, 0.045, 0.04)
const VAN_PAINT := Color(0.64, 0.61, 0.51)
const GLASS := Color(0.105, 0.165, 0.185, 0.72)
const METAL := Color(0.185, 0.185, 0.17)
const PAPER_DOOR := Color(0.43, 0.41, 0.35)
const STRAW_TOILET_WOOD := Color(0.215, 0.155, 0.09)

const NIGHT_AMBIENT := Color(0.035, 0.045, 0.055)
const DAMP_SHADOW_TINT := Color(0.045, 0.065, 0.055)
const WARNING_ACCENT := RITUAL_RED

const SURFACE_EARTH_ROAD := "earth_road"
const SURFACE_PACKED_COURTYARD_DIRT := "packed_courtyard_dirt"
const SURFACE_STONE_THRESHOLD := "stone_threshold"
const SURFACE_DARK_WOOD := "dark_wood"
const SURFACE_AGED_PLASTER := "aged_plaster"
const SURFACE_TILE_ROOF := "tile_roof"
const SURFACE_WET_MOSS := "wet_moss"
const SURFACE_RITUAL_RED := "ritual_red"
const SURFACE_VAN_PAINT := "van_paint"
const SURFACE_GLASS := "glass"
const SURFACE_METAL := "metal"
const SURFACE_PAPER_DOOR := "paper_door"
const SURFACE_STRAW_TOILET_WOOD := "straw_toilet_wood"

const COLOR_BY_SURFACE := {
	"earth_road": EARTH_ROAD,
	"packed_courtyard_dirt": PACKED_COURTYARD_DIRT,
	"stone_threshold": STONE_THRESHOLD,
	"dark_wood": DARK_WOOD,
	"aged_plaster": AGED_PLASTER,
	"tile_roof": TILE_ROOF,
	"wet_moss": WET_MOSS,
	"ritual_red": RITUAL_RED,
	"van_paint": VAN_PAINT,
	"glass": GLASS,
	"metal": METAL,
	"paper_door": PAPER_DOOR,
	"straw_toilet_wood": STRAW_TOILET_WOOD,
}

const MATERIAL_DATA_BY_SURFACE := {
	"earth_road": {"color": EARTH_ROAD, "roughness": 0.92, "metallic": 0.0},
	"packed_courtyard_dirt": {"color": PACKED_COURTYARD_DIRT, "roughness": 0.96, "metallic": 0.0},
	"stone_threshold": {"color": STONE_THRESHOLD, "roughness": 0.82, "metallic": 0.0},
	"dark_wood": {"color": DARK_WOOD, "roughness": 0.76, "metallic": 0.0},
	"aged_plaster": {"color": AGED_PLASTER, "roughness": 0.9, "metallic": 0.0},
	"tile_roof": {"color": TILE_ROOF, "roughness": 0.84, "metallic": 0.0},
	"wet_moss": {"color": WET_MOSS, "roughness": 0.72, "metallic": 0.0},
	"ritual_red": {"color": RITUAL_RED, "roughness": 0.78, "metallic": 0.0},
	"van_paint": {"color": VAN_PAINT, "roughness": 0.48, "metallic": 0.08},
	"glass": {"color": GLASS, "roughness": 0.2, "metallic": 0.0, "alpha": 0.72},
	"metal": {"color": METAL, "roughness": 0.55, "metallic": 0.65},
	"paper_door": {"color": PAPER_DOOR, "roughness": 0.88, "metallic": 0.0},
	"straw_toilet_wood": {"color": STRAW_TOILET_WOOD, "roughness": 0.86, "metallic": 0.0},
}

const PROP_DENSITY_SPARSE := 0.35
const PROP_DENSITY_NORMAL := 0.65
const PROP_DENSITY_CLUSTERED := 0.9

static func color(surface_name: String, fallback: Color = Color.WHITE) -> Color:
	return COLOR_BY_SURFACE.get(surface_name, fallback)

static func material_data(surface_name: String) -> Dictionary:
	return MATERIAL_DATA_BY_SURFACE.get(surface_name, {})

static func make_material(surface_name: String) -> StandardMaterial3D:
	var data := material_data(surface_name)
	var material := StandardMaterial3D.new()
	var albedo: Color = data.get("color", Color.WHITE)
	material.roughness = float(data.get("roughness", 0.8))
	material.metallic = float(data.get("metallic", 0.0))
	if data.has("alpha"):
		material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
		albedo.a = float(data["alpha"])
	material.albedo_color = albedo
	return material
