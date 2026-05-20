extends SceneTree

const MainScene := preload("res://scenes/Main.tscn")
const ArtifactDefinition := preload("res://scripts/core/artifact_definition.gd")

var _failed := false

func _initialize() -> void:
	var main: Node = MainScene.instantiate()
	root.add_child(main)
	for _i in range(90):
		await physics_frame

	_assert_bongo_quota_monitor(main)
	await _assert_bongo_deposit_waits_for_manual_settlement(main)
	if _failed:
		quit(1)
		return
	print("BONGO_MONITOR_SMOKE: in-world quota monitor present inside van")
	quit(0)

func _assert_bongo_quota_monitor(main: Node) -> void:
	var monitor := main.find_child("BongoQuotaMonitor", true, false) as Node3D
	if monitor == null:
		_fail("Missing BongoQuotaMonitor")
		return
	var floor := main.find_child("BongoInteriorFloor", true, false) as Node3D
	if floor == null:
		_fail("Missing BongoInteriorFloor")
		return
	var floor_size := _box_shape_size(floor)
	var local_delta := monitor.global_position - floor.global_position
	if abs(local_delta.x) > floor_size.x * 0.5 or abs(local_delta.z) > floor_size.z * 0.5:
		_fail("BongoQuotaMonitor is outside van footprint: delta=%s floor_size=%s" % [local_delta, floor_size])
		return
	var quota_text := _quota_monitor_text(monitor)
	if quota_text == "":
		_fail("BongoQuotaMonitor has no Label3D or TextMesh text")
		return
	if quota_text.find("최종") == -1 or quota_text.find("미정산") == -1:
		_fail("BongoQuotaMonitor text does not show final and pending values: %s" % quota_text)
		return

func _assert_bongo_deposit_waits_for_manual_settlement(main: Node) -> void:
	var player := main.get("player") as Node3D
	var zone := main.find_child("VanInteriorReturnZone", true, false) as Area3D
	if player == null or zone == null:
		_fail("Missing player or van extraction zone")
		return
	player.call("try_collect_artifact", ArtifactDefinition.new("monitor test", 70, 1.0, 0, [], 1))
	player.global_position = zone.global_position
	player.set("velocity", Vector3.ZERO)
	main.call("extract_player_inventory")
	await process_frame
	var quota: Variant = main.get("quota")
	if int(quota.get("recovered_value")) != 0:
		_fail("Depositing in the van should not immediately finalize quota")
		return
	if int(main.get("pending_recovered_value")) != 70:
		_fail("Depositing in the van should create pending cargo value")
		return
	if main.find_child("StoredCargoItem01", true, false) == null:
		_fail("Deposited item should remain visible in the van before settlement")
		return
	var monitor := main.find_child("BongoQuotaMonitor", true, false)
	var quota_text := _quota_monitor_text(monitor)
	if quota_text.find("미정산") == -1 or quota_text.find("70") == -1:
		_fail("BongoQuotaMonitor did not show pending value after deposit: %s" % quota_text)
		return
	var settlement := main.find_child("BongoSettlementStation", true, false)
	if settlement == null or not settlement.has_method("interact"):
		_fail("Missing interactive BongoSettlementStation")
		return
	settlement.interact(player)
	await process_frame
	if int(quota.get("recovered_value")) != 70:
		_fail("Manual settlement did not finalize quota")
		return
	if int(main.get("pending_recovered_value")) != 0:
		_fail("Pending cargo value was not cleared after settlement")
		return
	quota_text = _quota_monitor_text(monitor)
	if quota_text.find("최종") == -1 or quota_text.find("70") == -1:
		_fail("BongoQuotaMonitor did not show finalized quota after settlement: %s" % quota_text)
		return

func _quota_monitor_text(root: Node) -> String:
	for child in root.get_children():
		if child is Label3D:
			return str((child as Label3D).text)
		if child is MeshInstance3D:
			var mesh_instance := child as MeshInstance3D
			if mesh_instance.mesh is TextMesh:
				return str((mesh_instance.mesh as TextMesh).text)
		var nested := _quota_monitor_text(child)
		if nested != "":
			return nested
	return ""

func _box_shape_size(node: Node) -> Vector3:
	if node == null:
		return Vector3.ZERO
	var collision := node.find_child("CollisionShape3D", true, false) as CollisionShape3D
	if collision == null:
		return Vector3.ZERO
	var shape := collision.shape as BoxShape3D
	if shape == null:
		return Vector3.ZERO
	return shape.size

func _fail(message: String) -> void:
	_failed = true
	push_error(message)
