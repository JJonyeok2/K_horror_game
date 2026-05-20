extends RefCounted
class_name PerformanceSettings

const LOW_SPEC_SETTING := "k_horror/low_spec_mode"

static func is_low_spec_mode() -> bool:
	return bool(ProjectSettings.get_setting(LOW_SPEC_SETTING, true))
