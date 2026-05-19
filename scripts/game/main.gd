extends Node3D

const PlayerScene := preload("res://scenes/player/Player.tscn")
const AudioDirectorScript := preload("res://scripts/audio/audio_director.gd")
const JonggaEstateBuilder := preload("res://scripts/maps/jongga_estate_builder.gd")
const QuotaTracker := preload("res://scripts/core/quota_tracker.gd")
const ResentmentTracker := preload("res://scripts/core/resentment_tracker.gd")
const ThreatDirector := preload("res://scripts/core/threat_director.gd")
const HUDScript := preload("res://scripts/ui/hud.gd")

var player: PlayerController
var extraction_zone: ExtractionZone
var quota := QuotaTracker.new(800)
var resentment := ResentmentTracker.new()
var threat_director := ThreatDirector.new()
var audio_director: AudioDirector
var hud: HUD

func _ready() -> void:
	audio_director = AudioDirectorScript.new()
	add_child(audio_director)
	var map := JonggaEstateBuilder.new()
	add_child(map)
	map.build(self)
	player = PlayerScene.instantiate()
	add_child(player)
	player.global_position = Vector3(0, 1, 4)
	hud = HUDScript.new()
	add_child(hud)
	resentment.resentment_changed.connect(_on_resentment_changed)
	print("K Horror Retrieval Prototype booted")

func _process(_delta: float) -> void:
	var interaction_label := ""
	var interactor := player.get_node_or_null("Camera3D/Interactor")
	if interactor != null:
		interaction_label = interactor.current_label
	hud.update_status(
		quota.recovered_value,
		quota.required_value,
		player.inventory.total_weight(),
		player.inventory.max_weight,
		resentment.stage(),
		interaction_label
	)

func register_artifact(artifact: Artifact) -> void:
	artifact.picked_up.connect(_on_artifact_picked_up)

func register_extraction_zone(zone: ExtractionZone) -> void:
	extraction_zone = zone
	extraction_zone.extracted.connect(_on_extracted)

func _on_artifact_picked_up(definition: ArtifactDefinition) -> void:
	resentment.add_resentment(definition.resentment_gain, "%s 회수" % definition.display_name)

func _on_resentment_changed(_value: int, stage: int, _reason: String) -> void:
	audio_director.play_stage_cues(threat_director.audio_cues_for_stage(stage))

func extract_player_inventory() -> void:
	if extraction_zone != null:
		extraction_zone.extract_inventory(player.inventory)

func _on_extracted(value: int) -> void:
	quota.add_recovered_value(value)
	print("정산:%d / 할당량:%d" % [quota.recovered_value, quota.required_value])
