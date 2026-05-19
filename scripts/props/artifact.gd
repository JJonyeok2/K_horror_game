extends "res://scripts/interactions/interactable.gd"
class_name Artifact

const ArtifactDefinition = preload("res://scripts/core/artifact_definition.gd")

signal picked_up(definition: ArtifactDefinition)

@export var display_name: String = "회수품"
@export var value: int = 100
@export var weight: float = 1.0
@export var resentment_gain: int = 1
@export var tags: Array[String] = []
@export_range(1, 2, 1) var hand_slots: int = 1

func definition() -> ArtifactDefinition:
	return ArtifactDefinition.new(display_name, value, weight, resentment_gain, tags, hand_slots)

func interaction_label() -> String:
	return "%s 회수" % display_name

func interact(actor: Node) -> void:
	var item := definition()
	if actor.has_method("try_collect_artifact") and actor.try_collect_artifact(item):
		picked_up.emit(item)
		queue_free()
