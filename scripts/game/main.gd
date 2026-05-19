extends Node3D

const PlayerScene := preload("res://scenes/player/Player.tscn")
const AudioDirectorScript := preload("res://scripts/audio/audio_director.gd")
const JonggaEstateBuilder := preload("res://scripts/maps/jongga_estate_builder.gd")
const QuotaTracker := preload("res://scripts/core/quota_tracker.gd")
const ResentmentTracker := preload("res://scripts/core/resentment_tracker.gd")
const ThreatDirector := preload("res://scripts/core/threat_director.gd")

var player: Node
var quota := QuotaTracker.new(800)
var resentment := ResentmentTracker.new()
var threat_director := ThreatDirector.new()
var audio_director: Node

func _ready() -> void:
	audio_director = AudioDirectorScript.new()
	add_child(audio_director)
	var map := JonggaEstateBuilder.new()
	add_child(map)
	map.build(self)
	player = PlayerScene.instantiate()
	add_child(player)
	player.global_position = Vector3(0, 1, 4)
	resentment.resentment_changed.connect(_on_resentment_changed)
	print("K Horror Retrieval Prototype booted")

func register_artifact(artifact: Variant) -> void:
	artifact.picked_up.connect(_on_artifact_picked_up)

func _on_artifact_picked_up(definition: Variant) -> void:
	resentment.add_resentment(definition.resentment_gain, "%s 회수" % definition.display_name)

func _on_resentment_changed(_value: int, stage: int, _reason: String) -> void:
	audio_director.play_stage_cues(threat_director.audio_cues_for_stage(stage))

func extract_player_inventory() -> void:
	var value := player.inventory.total_value()
	player.inventory.clear()
	quota.add_recovered_value(value)
	print("정산:%d / 할당량:%d" % [quota.recovered_value, quota.required_value])
