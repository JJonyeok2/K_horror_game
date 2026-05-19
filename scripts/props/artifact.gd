extends Interactable
class_name Artifact

signal picked_up(definition: ArtifactDefinition)

@export var display_name: String = "회수품"
@export var value: int = 100
@export var weight: float = 1.0
@export var resentment_gain: int = 1
@export var tags: Array[String] = []

func definition() -> ArtifactDefinition:
	return ArtifactDefinition.new(display_name, value, weight, resentment_gain, tags)

func interaction_label() -> String:
	return "%s 회수" % display_name

func interact(actor: Node) -> void:
	var item := definition()
	if actor.has_method("try_collect_artifact") and actor.try_collect_artifact(item):
		picked_up.emit(item)
		queue_free()
