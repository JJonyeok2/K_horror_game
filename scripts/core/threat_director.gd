extends RefCounted
class_name ThreatDirector

const DAMAGE_PER_HIT_BY_STAGE := [0, 0, 10, 15, 20, 30]
const ATTACK_RANGE_BY_STAGE := [0.0, 0.0, 1.4, 1.7, 2.0, 2.4]
const ATTACK_INTERVAL_SECONDS_BY_STAGE := [0.0, 0.0, 2.0, 2.0, 2.0, 2.0]
const PURSUIT_SPEED_BY_STAGE := [0.0, 0.0, 2.2, 2.8, 3.4, 4.0]
const TRAP_TRIGGER_RESENTMENT_BY_STAGE := [0, 0, 1, 1, 2, 3]

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
