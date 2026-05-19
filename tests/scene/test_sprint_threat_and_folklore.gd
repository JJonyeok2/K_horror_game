extends SceneTree

const MainScene := preload("res://scenes/Main.tscn")

var _failed := false

func _initialize() -> void:
	var main: Node = MainScene.instantiate()
	root.add_child(main)
	for _i in range(360):
		await physics_frame

	var player := main.get("player") as Node3D
	if player == null:
		_fail("Main did not create player")
		return

	_assert_sprint_stamina(player)
	_assert_folklore_route_props(main)
	await _assert_threat_manifestation(main)
	if _failed:
		quit(1)
		return
	print("SPRINT_THREAT_SMOKE: sprint stamina, jangseung, hidden path cover, threat present")
	quit(0)

func _assert_sprint_stamina(player: Node3D) -> void:
	if not InputMap.has_action("sprint"):
		_fail("InputMap is missing sprint action")
		return
	var max_stamina := float(player.get("max_stamina_seconds"))
	if max_stamina != 15.0:
		_fail("Sprint max stamina should be 15 seconds, got %s" % max_stamina)
		return
	var before := float(player.get("stamina_seconds"))
	player.set("movement_enabled", true)
	Input.action_press("move_forward")
	Input.action_press("sprint")
	for _i in range(60):
		await physics_frame
	Input.action_release("sprint")
	Input.action_release("move_forward")
	var after := float(player.get("stamina_seconds"))
	if after >= before:
		_fail("Sprint did not drain stamina: before=%s after=%s" % [before, after])
		return
	if float(player.get("stamina_ratio")) >= 1.0:
		_fail("Stamina ratio did not reflect drain")
		return

func _assert_folklore_route_props(main: Node) -> void:
	var required := [
		"JangseungLeft01",
		"JangseungRight01",
		"HiddenPathScreeningBrushA",
		"HiddenPathScreeningBrushB",
	]
	for label in required:
		var node := main.find_child(label, true, false)
		if node == null:
			_fail("Missing folklore/hidden-path prop: %s" % label)
			return
		if node.find_child("CollisionShape3D", true, false) == null:
			_fail("%s has no collision shape" % label)
			return

func _assert_threat_manifestation(main: Node) -> void:
	var resentment: Variant = main.get("resentment")
	resentment.call("add_resentment", 5, "test escalation")
	for _i in range(4):
		await process_frame
	var threat := main.find_child("ThreatApparition", true, false) as Node3D
	if threat == null:
		_fail("Threat apparition did not spawn when resentment escalated")
		return
	if not threat.visible:
		_fail("Threat apparition exists but is not visible")
		return

func _fail(message: String) -> void:
	_failed = true
	Input.action_release("move_forward")
	if InputMap.has_action("sprint"):
		Input.action_release("sprint")
	push_error(message)
