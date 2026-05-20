extends StaticBody3D
class_name BongoQuotaMonitor

func interaction_label() -> String:
	return "봉고 단말기 조작"

func interact(actor: Node) -> void:
	if actor == null:
		return
	var main := actor.get_parent()
	if main != null and main.has_method("operate_bongo_terminal"):
		main.operate_bongo_terminal()
	elif main != null and main.has_method("toggle_bongo_monitor_panel"):
		main.toggle_bongo_monitor_panel()
