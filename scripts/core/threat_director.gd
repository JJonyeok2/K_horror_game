extends RefCounted
class_name ThreatDirector

const DAMAGE_PER_HIT_BY_STAGE := [0, 0, 0, 8, 20, 30]
const ATTACK_RANGE_BY_STAGE := [0.0, 0.0, 0.0, 1.35, 2.0, 2.4]
const ATTACK_INTERVAL_SECONDS_BY_STAGE := [0.0, 0.0, 0.0, 2.8, 2.0, 1.35]
const PURSUIT_SPEED_BY_STAGE := [0.0, 0.0, 0.0, 2.1, 3.4, 4.6]
const TRAP_TRIGGER_RESENTMENT_BY_STAGE := [0, 0, 1, 1, 2, 3]
const GHOST_ROSTER := [
	{"slot": 3, "id": "dokkaebi", "display_name": "도깨비", "role": "gate_trickster", "stage": 3, "zone": "outside_gate_forest", "attack_pattern": "dokkaebi_forest_trickster"},
	{"slot": 4, "id": "sangbok_ghost", "display_name": "상복귀", "role": "wall_phasing_hunter", "stage": 4, "zone": "inner_building_only", "attack_pattern": "sangbok_steady_pursuit"},
	{"slot": 5, "id": "dalgyal_gwisin", "display_name": "달걀귀신", "role": "look_away_strangler", "stage": 5, "zone": "inner_building_only", "attack_pattern": "dalgyal_blind_lunge"},
	{"slot": 6, "id": "eoduksini", "display_name": "어둑시니", "role": "flashlight_growth_shadow", "stage": 5, "zone": "inner_building_only", "attack_pattern": "flashlight_growth_shadow"},
	{"slot": 7, "id": "changgwi", "display_name": "창귀", "role": "corridor_lure", "stage": 5, "zone": "inner_building_only", "attack_pattern": "corridor_lure_pursuit"},
	{"slot": 8, "id": "jangsanbeom", "display_name": "장산범", "role": "voice_lure_crawler", "stage": 5, "zone": "inner_building_only", "attack_pattern": "voice_lure_crawl"},
	{"id": "well_spirit", "display_name": "우물귀", "role": "courtyard_ambush"},
]

func _clamped_stage(stage: int) -> int:
	return clamp(stage, 0, 5)

func state_for_stage(stage: int) -> String:
	match _clamped_stage(stage):
		0:
			return "dormant"
		1:
			return "subtle_presence"
		2:
			return "visible"
		3:
			return "route_interference"
		4:
			return "pursuit"
		5:
			return "contested_extraction"
	return "dormant"

func audio_cues_for_stage(stage: int) -> Array[String]:
	match _clamped_stage(stage):
		0:
			return ["night_wind", "paper_door_rattle"]
		1:
			return ["distant_floor_creak", "low_cough"]
		2:
			return ["cloth_drag_far", "faint_funeral_wail"]
		3:
			return ["door_slide_false_route", "ritual_bowl_clink"]
		4:
			return ["cloth_drag_near", "behind_breath"]
		5:
			return ["close_funeral_wail", "locked_gate_hit", "false_vehicle_call"]
	return []

func can_damage(stage: int) -> bool:
	return damage_per_hit(stage) > 0

func can_phase_through_walls(stage: int) -> bool:
	return _clamped_stage(stage) >= 4

func ghost_roster() -> Array:
	return GHOST_ROSTER.duplicate(true)

func ghost_type_for_stage(stage: int) -> String:
	match _clamped_stage(stage):
		3:
			return "dokkaebi"
		4:
			return "sangbok_ghost"
		5:
			return "dalgyal_gwisin"
	return ""

func active_ghost_types_for_stage(stage: int) -> Array[String]:
	var result: Array[String] = []
	var current_stage := _clamped_stage(stage)
	for profile in GHOST_ROSTER:
		if not profile.has("stage") or int(profile["stage"]) > current_stage:
			continue
		result.append(str(profile["id"]))
	return result

func active_threat_stages_for_stage(stage: int) -> Array[int]:
	var result: Array[int] = []
	var current_stage := _clamped_stage(stage)
	for profile in GHOST_ROSTER:
		if not profile.has("stage") or not profile.has("slot"):
			continue
		if int(profile["stage"]) <= current_stage:
			result.append(int(profile["slot"]))
	return result

func ghost_type_for_threat_slot(slot: int) -> String:
	var profile := _profile_for_slot(slot)
	return str(profile.get("id", ""))

func resentment_stage_for_threat_slot(slot: int) -> int:
	var profile := _profile_for_slot(slot)
	if profile.has("stage"):
		return int(profile["stage"])
	return _clamped_stage(slot)

func attack_pattern_for_ghost_type(ghost_type: String) -> String:
	var profile := _profile_for_ghost_type(ghost_type)
	return str(profile.get("attack_pattern", ""))

func attack_pattern_for_threat_slot(slot: int) -> String:
	var profile := _profile_for_slot(slot)
	return str(profile.get("attack_pattern", attack_pattern_for_stage(slot)))

func threat_zone_for_threat_slot(slot: int) -> String:
	var profile := _profile_for_slot(slot)
	return str(profile.get("zone", threat_zone_for_stage(slot)))

func threat_zone_for_stage(stage: int) -> String:
	match _clamped_stage(stage):
		3:
			return "outside_gate_forest"
		4, 5:
			return "inner_building_only"
	return ""

func attack_pattern_for_stage(stage: int) -> String:
	match _clamped_stage(stage):
		3:
			return "dokkaebi_forest_trickster"
		4:
			return "sangbok_steady_pursuit"
		5:
			return "dalgyal_blind_lunge"
	return ""

func _profile_for_slot(slot: int) -> Dictionary:
	for profile in GHOST_ROSTER:
		if profile.has("slot") and int(profile["slot"]) == slot:
			return profile
	return {}

func _profile_for_ghost_type(ghost_type: String) -> Dictionary:
	for profile in GHOST_ROSTER:
		if str(profile.get("id", "")) == ghost_type:
			return profile
	return {}

func damage_per_hit(stage: int) -> int:
	return DAMAGE_PER_HIT_BY_STAGE[_clamped_stage(stage)]

func attack_range(stage: int) -> float:
	return ATTACK_RANGE_BY_STAGE[_clamped_stage(stage)]

func attack_interval_seconds(stage: int) -> float:
	return ATTACK_INTERVAL_SECONDS_BY_STAGE[_clamped_stage(stage)]

func pursuit_speed(stage: int) -> float:
	return PURSUIT_SPEED_BY_STAGE[_clamped_stage(stage)]

func trap_trigger_resentment(stage: int) -> int:
	return TRAP_TRIGGER_RESENTMENT_BY_STAGE[_clamped_stage(stage)]
