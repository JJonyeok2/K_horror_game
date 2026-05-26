extends SceneTree

const MainScene := preload("res://scenes/Main.tscn")

func _initialize() -> void:
	var main: Node = MainScene.instantiate()
	root.add_child(main)
	await process_frame
	var gate := main.find_child("OuterEstateGate", true, false)
	_print_node_check(gate, "OuterEstateGate")
	_print_node_check(main.find_child("LeftSwingGatePanel", true, false), "LeftSwingGatePanel")
	_print_node_check(main.find_child("RightSwingGatePanel", true, false), "RightSwingGatePanel")
	_print_node_check(main.find_child("GateLeftPost", true, false), "GateLeftPost")
	_print_node_check(main.find_child("GateRightPost", true, false), "GateRightPost")
	_print_node_check(main.find_child("GateThreshold", true, false), "GateThreshold")
	_print_node_check(main.find_child("LargeInnerCourtyard", true, false), "LargeInnerCourtyard")
	_print_node_check(main.find_child("BongoInteriorFloor", true, false), "BongoInteriorFloor")
	_print_node_check(main.find_child("BongoRoof", true, false), "BongoRoof")
	_print_node_check(main.find_child("BongoLowExitRamp", true, false), "BongoLowExitRamp")
	_print_node_check(main.find_child("RiskySidePassageTrigger", true, false), "RiskySidePassageTrigger")
	_print_node_check(main.find_child("DwitganOuthouse", true, false), "DwitganOuthouse")
	quit(0)

func _print_node_check(node: Node, label: String) -> void:
	if node == null:
		print("%s: MISSING" % label)
		return
	var has_collision := node.find_child("CollisionShape3D", true, false) != null
	print("%s: type=%s interact=%s collision=%s" % [
		label,
		node.get_class(),
		node.has_method("interact"),
		has_collision
	])
