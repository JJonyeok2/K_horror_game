extends CanvasLayer
class_name HUD

var label: Label

func _ready() -> void:
	label = Label.new()
	label.position = Vector2(20, 20)
	label.add_theme_font_size_override("font_size", 18)
	add_child(label)

func update_status(quota_value: int, quota_required: int, weight: float, max_weight: float, resentment_stage: int, interaction_label: String) -> void:
	label.text = "회수금액 %d/%d\n무게 %.1f/%.1f\n원한 단계 %d\n%s" % [
		quota_value,
		quota_required,
		weight,
		max_weight,
		resentment_stage,
		interaction_label
	]
