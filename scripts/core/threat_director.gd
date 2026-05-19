extends RefCounted
class_name ThreatDirector

func state_for_stage(stage: int) -> String:
	match clamp(stage, 0, 5):
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
	match clamp(stage, 0, 5):
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
