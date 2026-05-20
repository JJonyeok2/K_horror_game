extends CanvasLayer
class_name HUD

signal restart_requested

var label: Label
var health_back: ColorRect
var health_fill: ColorRect
var stamina_back: ColorRect
var stamina_fill: ColorRect
var restart_button: Button
var bongo_monitor_panel: ColorRect
var bongo_monitor_title: Label
var bongo_monitor_body: Label

const GAUGE_WIDTH: float = 220.0
const GAUGE_HEIGHT: float = 14.0
const GAUGE_FILL_WIDTH: float = 216.0
const GAUGE_FILL_HEIGHT: float = 10.0
const GAUGE_RIGHT_MARGIN: float = 28.0
const HEALTH_GAUGE_TOP: float = 150.0
const STAMINA_GAUGE_TOP: float = 170.0

func _ready() -> void:
	label = Label.new()
	label.position = Vector2(20, 20)
	label.add_theme_font_size_override("font_size", 18)
	add_child(label)
	health_back = ColorRect.new()
	health_back.name = "HealthGaugeBack"
	_place_right_gauge(health_back, HEALTH_GAUGE_TOP)
	health_back.color = Color(0.045, 0.025, 0.025, 0.84)
	add_child(health_back)
	health_fill = ColorRect.new()
	health_fill.name = "HealthGaugeFill"
	health_fill.position = Vector2(2, 2)
	health_fill.size = Vector2(GAUGE_FILL_WIDTH, GAUGE_FILL_HEIGHT)
	health_fill.color = Color(0.72, 0.12, 0.1, 0.96)
	health_back.add_child(health_fill)
	stamina_back = ColorRect.new()
	stamina_back.name = "StaminaGaugeBack"
	_place_right_gauge(stamina_back, STAMINA_GAUGE_TOP)
	stamina_back.color = Color(0.04, 0.045, 0.04, 0.82)
	add_child(stamina_back)
	stamina_fill = ColorRect.new()
	stamina_fill.name = "StaminaGaugeFill"
	stamina_fill.position = Vector2(2, 2)
	stamina_fill.size = Vector2(GAUGE_FILL_WIDTH, GAUGE_FILL_HEIGHT)
	stamina_fill.color = Color(0.54, 0.78, 0.42, 0.95)
	stamina_back.add_child(stamina_fill)
	restart_button = Button.new()
	restart_button.name = "RestartRunButton"
	restart_button.text = "다시 시작 (R)"
	restart_button.visible = false
	restart_button.anchor_left = 0.5
	restart_button.anchor_right = 0.5
	restart_button.anchor_top = 0.5
	restart_button.anchor_bottom = 0.5
	restart_button.offset_left = -96.0
	restart_button.offset_right = 96.0
	restart_button.offset_top = 38.0
	restart_button.offset_bottom = 82.0
	restart_button.pressed.connect(_on_restart_pressed)
	add_child(restart_button)
	_create_bongo_monitor_panel()

func _place_right_gauge(gauge: ColorRect, top: float) -> void:
	gauge.anchor_left = 1.0
	gauge.anchor_right = 1.0
	gauge.anchor_top = 0.0
	gauge.anchor_bottom = 0.0
	gauge.offset_left = -(GAUGE_WIDTH + GAUGE_RIGHT_MARGIN)
	gauge.offset_right = -GAUGE_RIGHT_MARGIN
	gauge.offset_top = top
	gauge.offset_bottom = top + GAUGE_HEIGHT

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
	health_fill.size.x = GAUGE_FILL_WIDTH * clamp(health_ratio, 0.0, 1.0)
	health_fill.color = Color(0.94, 0.35, 0.18, 0.96) if health_ratio < 0.3 else Color(0.72, 0.12, 0.1, 0.96)
	stamina_fill.size.x = GAUGE_FILL_WIDTH * clamp(stamina_ratio, 0.0, 1.0)
	stamina_fill.color = Color(0.78, 0.55, 0.35, 0.95) if stamina_ratio < 0.25 else Color(0.54, 0.78, 0.42, 0.95)

func _create_bongo_monitor_panel() -> void:
	bongo_monitor_panel = ColorRect.new()
	bongo_monitor_panel.name = "BongoMonitorPanel"
	bongo_monitor_panel.visible = false
	bongo_monitor_panel.anchor_left = 0.5
	bongo_monitor_panel.anchor_right = 0.5
	bongo_monitor_panel.anchor_top = 0.5
	bongo_monitor_panel.anchor_bottom = 0.5
	bongo_monitor_panel.offset_left = -230.0
	bongo_monitor_panel.offset_right = 230.0
	bongo_monitor_panel.offset_top = -165.0
	bongo_monitor_panel.offset_bottom = 165.0
	bongo_monitor_panel.color = Color(0.015, 0.03, 0.026, 0.92)
	add_child(bongo_monitor_panel)

	bongo_monitor_title = Label.new()
	bongo_monitor_title.name = "BongoMonitorTitle"
	bongo_monitor_title.position = Vector2(22.0, 18.0)
	bongo_monitor_title.size = Vector2(416.0, 32.0)
	bongo_monitor_title.add_theme_font_size_override("font_size", 20)
	bongo_monitor_title.text = "봉고 단말기"
	bongo_monitor_panel.add_child(bongo_monitor_title)

	bongo_monitor_body = Label.new()
	bongo_monitor_body.name = "BongoMonitorBody"
	bongo_monitor_body.position = Vector2(22.0, 62.0)
	bongo_monitor_body.size = Vector2(416.0, 238.0)
	bongo_monitor_body.add_theme_font_size_override("font_size", 18)
	bongo_monitor_body.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	bongo_monitor_panel.add_child(bongo_monitor_body)

func set_bongo_monitor_visible(is_visible: bool) -> void:
	if bongo_monitor_panel != null:
		bongo_monitor_panel.visible = is_visible

func update_bongo_monitor(title: String, body: String, is_visible: bool) -> void:
	if bongo_monitor_panel == null:
		return
	bongo_monitor_title.text = title
	bongo_monitor_body.text = body
	bongo_monitor_panel.visible = is_visible

func set_player_down(is_down: bool) -> void:
	if restart_button != null:
		restart_button.visible = is_down

func _on_restart_pressed() -> void:
	restart_requested.emit()
