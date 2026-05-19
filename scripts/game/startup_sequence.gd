extends RefCounted
class_name StartupSequence

const BongoVanPlanScript := preload("res://scripts/maps/bongo_van_plan.gd")

const PLAYER_START := BongoVanPlanScript.PLAYER_START_POSITION
const STARTUP_GATE_NODE := "OuterEstateGate"

func apply_player_start(player: Node3D) -> void:
	player.global_position = PLAYER_START

func find_startup_gate(root: Node) -> Node:
	return root.find_child(STARTUP_GATE_NODE, true, false)
