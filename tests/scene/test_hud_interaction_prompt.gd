extends SceneTree

const HUDScript := preload("res://scripts/ui/hud.gd")

var _failed := false

func _initialize() -> void:
	root.size = Vector2i(1280, 720)
	var hud := HUDScript.new()
	root.add_child(hud)
	await process_frame

	_assert_pickup_prompt_moves_to_bottom_center(hud)
	_assert_door_prompt_uses_target_name(hud)
	_assert_prompt_hides_without_interaction(hud)

	if _failed:
		quit(1)
		return
	print("HUD_INTERACTION_PROMPT: bottom centered E prompt with faint target name")
	quit(0)

func _assert_pickup_prompt_moves_to_bottom_center(hud: CanvasLayer) -> void:
	hud.update_status(0, 800, 1.0, 12.0, 0, "놋그릇 회수", "빈손", 1.0, 1.0, false)
	var status_label := hud.get("label") as Label
	if status_label == null:
		_fail("HUD has no status label")
		return
	if status_label.text.find("놋그릇") != -1 or status_label.text.find("회수") != -1:
		_fail("Interaction text should not stay in the upper-left status label: %s" % status_label.text)
		return
	var prompt_root := hud.find_child("InteractionPromptRoot", true, false) as Control
	if prompt_root == null or not prompt_root.visible:
		_fail("Interaction prompt root should be visible")
		return
	if prompt_root.anchor_left != 0.5 or prompt_root.anchor_right != 0.5 or prompt_root.anchor_top != 1.0:
		_fail("Interaction prompt should be anchored to bottom center")
		return
	var key_label := hud.find_child("InteractionKeyLabel", true, false) as Label
	var target_label := hud.find_child("InteractionTargetLabel", true, false) as Label
	if key_label == null or target_label == null:
		_fail("Interaction prompt labels are missing")
		return
	if key_label.text.find("[E]") == -1 or key_label.text.find("줍기") == -1:
		_fail("Pickup prompt should show [E] and 줍기: %s" % key_label.text)
		return
	if target_label.text.find("놋그릇") == -1:
		_fail("Pickup prompt should show the item name as target text: %s" % target_label.text)
		return
	if target_label.modulate.a > 0.68:
		_fail("Target name should be faint, alpha=%s" % target_label.modulate.a)

func _assert_door_prompt_uses_target_name(hud: CanvasLayer) -> void:
	hud.update_status(0, 800, 0.0, 12.0, 0, "대문 열기", "빈손", 1.0, 1.0, false)
	var key_label := hud.find_child("InteractionKeyLabel", true, false) as Label
	var target_label := hud.find_child("InteractionTargetLabel", true, false) as Label
	if key_label == null or target_label == null:
		_fail("Interaction prompt labels are missing after door update")
		return
	if key_label.text.find("열기") == -1:
		_fail("Door prompt should show 열기 action: %s" % key_label.text)
		return
	if target_label.text != "대문":
		_fail("Door prompt should show only target name, got: %s" % target_label.text)

func _assert_prompt_hides_without_interaction(hud: CanvasLayer) -> void:
	hud.update_status(0, 800, 0.0, 12.0, 0, "", "빈손", 1.0, 1.0, false)
	var prompt_root := hud.find_child("InteractionPromptRoot", true, false) as Control
	if prompt_root != null and prompt_root.visible:
		_fail("Interaction prompt should hide when no target is aimed at")

func _fail(message: String) -> void:
	_failed = true
	push_error(message)
