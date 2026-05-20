extends Node3D
class_name KoreanMonsterAI

@export var monster_type: String = "sangbok_ghost"
@export var base_speed: float = 2.0
@export var chase_speed: float = 3.4
@export var behind_speed_multiplier: float = 2.2
@export var stutter_step_interval: float = 0.18
@export var flashlight_growth_rate: float = 0.45
@export var max_light_scale: float = 3.2
@export var autonomous_movement: bool = false

var player: Node3D
var flashlight: SpotLight3D
var navigation_agent: NavigationAgent3D
var _step_elapsed: float = 0.0
var _roam_seed: float = 0.0

func _ready() -> void:
	_find_navigation_agent()

func configure(p_monster_type: String, p_player: Node3D, p_flashlight: SpotLight3D = null) -> void:
	monster_type = p_monster_type
	player = p_player
	flashlight = p_flashlight
	_find_navigation_agent()
	_apply_profile_defaults()
	set_meta("ai_logic", "navigation_stutter_chase")

func movement_speed_for_context(player_looking_at: bool, flashlight_hit: bool) -> float:
	match monster_type:
		"dalgyal_gwisin":
			return base_speed if player_looking_at else chase_speed * behind_speed_multiplier
		"eoduksini":
			var scale_pressure: float = clamp((scale.y - 0.55) / max(max_light_scale - 0.55, 0.1), 0.0, 1.0)
			return lerp(base_speed * 0.55, chase_speed, scale_pressure + (0.35 if flashlight_hit else 0.0))
		"jangsanbeom":
			return chase_speed * 0.72
		"changgwi":
			return chase_speed * 1.08
		_:
			return chase_speed

func apply_flashlight_pressure(delta: float) -> void:
	if monster_type != "eoduksini":
		return
	var growth: float = flashlight_growth_rate * max(delta, 0.0)
	var next_y: float = min(scale.y + growth, max_light_scale)
	var next_xz: float = min(scale.x + growth * 0.25, 1.35)
	scale = Vector3(next_xz, next_y, next_xz)
	set_meta("flashlight_growth_scale", scale.y)

func lure_lines() -> Array[String]:
	if monster_type != "jangsanbeom":
		return []
	return ["여기 비싼 아이템 있어!", "살려줘!", "안쪽 방으로 와"]

func _physics_process(delta: float) -> void:
	if not autonomous_movement or not visible or player == null:
		return
	var flashlight_hit := _is_hit_by_flashlight()
	if flashlight_hit:
		apply_flashlight_pressure(delta)
	_step_elapsed += delta
	if _step_elapsed < stutter_step_interval:
		return
	var step_delta := _step_elapsed
	_step_elapsed = 0.0
	var target := _target_position()
	if navigation_agent != null:
		navigation_agent.target_position = target
		if not navigation_agent.is_navigation_finished():
			target = navigation_agent.get_next_path_position()
	var player_looking_at := _player_is_looking_at_me()
	var speed := movement_speed_for_context(player_looking_at, flashlight_hit)
	var direction := target - global_position
	direction.y = 0.0
	if direction.length() <= 0.05:
		return
	global_position += direction.normalized() * speed * step_delta
	_face_flat(player.global_position)

func _find_navigation_agent() -> void:
	navigation_agent = get_node_or_null("NavigationAgent3D") as NavigationAgent3D

func _apply_profile_defaults() -> void:
	match monster_type:
		"dalgyal_gwisin":
			base_speed = 0.45
			chase_speed = 4.9
			behind_speed_multiplier = 1.85
			stutter_step_interval = 0.13
		"eoduksini":
			base_speed = 0.25
			chase_speed = 2.8
			flashlight_growth_rate = 0.82
			max_light_scale = 3.35
			stutter_step_interval = 0.22
		"changgwi":
			base_speed = 1.6
			chase_speed = 3.8
			stutter_step_interval = 0.16
		"jangsanbeom":
			base_speed = 1.1
			chase_speed = 3.1
			stutter_step_interval = 0.24
		_:
			base_speed = 1.4
			chase_speed = 3.2
			stutter_step_interval = 0.18

func _target_position() -> Vector3:
	if monster_type == "jangsanbeom":
		_roam_seed += 0.37
		return player.global_position + Vector3(sin(_roam_seed) * 2.2, 0.0, -1.8)
	if monster_type == "changgwi":
		return player.global_position + Vector3(0.0, 0.0, -0.65)
	return player.global_position

func _player_is_looking_at_me() -> bool:
	if player == null:
		return false
	var camera := player.get_node_or_null("Camera3D") as Camera3D
	if camera == null:
		return false
	var to_monster := global_position - camera.global_position
	to_monster.y = 0.0
	if to_monster.length() <= 0.01:
		return true
	var forward := -camera.global_transform.basis.z
	forward.y = 0.0
	if forward.length() <= 0.01:
		return false
	return forward.normalized().dot(to_monster.normalized()) > 0.18

func _is_hit_by_flashlight() -> bool:
	if flashlight == null or not flashlight.visible:
		return false
	var to_monster := global_position - flashlight.global_position
	var distance := to_monster.length()
	if distance <= 0.01 or distance > flashlight.spot_range:
		return false
	var forward := -flashlight.global_transform.basis.z.normalized()
	var cone_cos := cos(deg_to_rad(flashlight.spot_angle * 0.5))
	return forward.dot(to_monster.normalized()) >= cone_cos

func _face_flat(target: Vector3) -> void:
	var look_target := Vector3(target.x, global_position.y, target.z)
	if global_position.distance_to(look_target) > 0.05:
		look_at(look_target, Vector3.UP)
