extends Node3D

const PlayerScene := preload("res://scenes/player/Player.tscn")
const AudioDirectorScript := preload("res://scripts/audio/audio_director.gd")
const JonggaEstateBuilderScript := preload("res://scripts/maps/jongga_estate_builder.gd")
const QuotaTrackerScript := preload("res://scripts/core/quota_tracker.gd")
const ResentmentTrackerScript := preload("res://scripts/core/resentment_tracker.gd")
const ThreatDirectorScript := preload("res://scripts/core/threat_director.gd")
const HUDScript := preload("res://scripts/ui/hud.gd")

var player: Node3D
var extraction_zone: Node
var quota: Variant = QuotaTrackerScript.new(800)
var resentment: Variant = ResentmentTrackerScript.new()
var threat_director: Variant = ThreatDirectorScript.new()
var audio_director: Variant
var hud: Variant

func _ready() -> void:
	audio_director = AudioDirectorScript.new()
	add_child(audio_director)
	var map: Node3D = JonggaEstateBuilderScript.new()
	add_child(map)
	map.build(self)
	player = PlayerScene.instantiate() as Node3D
	add_child(player)
	player.global_position = Vector3(0, 1.25, -8)
	hud = HUDScript.new()
	add_child(hud)
	resentment.resentment_changed.connect(_on_resentment_changed)
	print("K Horror Retrieval Prototype booted")

func _process(_delta: float) -> void:
	var interaction_label: String = ""
	var interactor: Node = player.get_node_or_null("Camera3D/Interactor")
	if interactor != null:
		interaction_label = str(interactor.get("current_label"))
	var inventory: Variant = player.get("inventory")
	hud.update_status(
		quota.recovered_value,
		quota.required_value,
		inventory.used_slots(),
		inventory.max_slots,
		resentment.stage(),
		interaction_label
	)

func register_artifact(artifact: Node) -> void:
	artifact.connect("picked_up", Callable(self, "_on_artifact_picked_up"))

func register_extraction_zone(zone: Node) -> void:
	extraction_zone = zone
	extraction_zone.connect("extracted", Callable(self, "_on_extracted"))

func _on_artifact_picked_up(definition: Variant) -> void:
	resentment.add_resentment(definition.resentment_gain, "%s 회수" % definition.display_name)

func _on_resentment_changed(_value: int, stage: int, _reason: String) -> void:
	audio_director.play_stage_cues(threat_director.audio_cues_for_stage(stage))

func extract_player_inventory() -> void:
	if extraction_zone != null:
		extraction_zone.call("extract_inventory", player.get("inventory"))

func _on_extracted(value: int) -> void:
	quota.add_recovered_value(value)
	print("정산:%d / 할당량:%d" % [quota.recovered_value, quota.required_value])
