extends CanvasLayer
class_name HUD

var label: Label
var health_back: ColorRect
var health_fill: ColorRect
var stamina_back: ColorRect
var stamina_fill: ColorRect

func _ready() -> void:
	label = Label.new()
	label.position = Vector2(20, 20)
	label.add_theme_font_size_override("font_size", 18)
	add_child(label)
	health_back = ColorRect.new()
	health_back.name = "HealthGaugeBack"
	health_back.position = Vector2(20, 150)
	health_back.size = Vector2(220, 14)
	health_back.color = Color(0.045, 0.025, 0.025, 0.84)
	add_child(health_back)
	health_fill = ColorRect.new()
	health_fill.name = "HealthGaugeFill"
	health_fill.position = health_back.position + Vector2(2, 2)
	health_fill.size = Vector2(216, 10)
	health_fill.color = Color(0.72, 0.12, 0.1, 0.96)
	add_child(health_fill)
	stamina_back = ColorRect.new()
	stamina_back.name = "StaminaGaugeBack"
	stamina_back.position = Vector2(20, 170)
	stamina_back.size = Vector2(220, 14)
	stamina_back.color = Color(0.04, 0.045, 0.04, 0.82)
	add_child(stamina_back)
	stamina_fill = ColorRect.new()
	stamina_fill.name = "StaminaGaugeFill"
	stamina_fill.position = stamina_back.position + Vector2(2, 2)
	stamina_fill.size = Vector2(216, 10)
	stamina_fill.color = Color(0.54, 0.78, 0.42, 0.95)
	add_child(stamina_fill)

func update_status(
	quota_value: int,
	quota_required: int,
	weight: float,
	max_weight: float,
	resentment_stage: int,
	interaction_label: String,
	hand_status: String = "손: 비어 있음",
	health_ratio: float = 1.0,
	stamina_ratio: float = 1.0,
	is_sprinting: bool = false
) -> void:
	var sprint_label := "달리기" if is_sprinting else "스태미너"
	label.text = "무게 %.1f/%.1f\n원한 단계 %d\n체력 %.0f%%\n%s %.0f%%\n%s\n%s" % [
		weight,
		max_weight,
		resentment_stage,
		clamp(health_ratio, 0.0, 1.0) * 100.0,
		sprint_label,
		clamp(stamina_ratio, 0.0, 1.0) * 100.0,
		hand_status,
		interaction_label
	]
	health_fill.size.x = 216.0 * clamp(health_ratio, 0.0, 1.0)
	health_fill.color = Color(0.94, 0.35, 0.18, 0.96) if health_ratio < 0.3 else Color(0.72, 0.12, 0.1, 0.96)
	stamina_fill.size.x = 216.0 * clamp(stamina_ratio, 0.0, 1.0)
	stamina_fill.color = Color(0.78, 0.55, 0.35, 0.95) if stamina_ratio < 0.25 else Color(0.54, 0.78, 0.42, 0.95)
