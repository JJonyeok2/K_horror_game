extends StaticBody3D
class_name BongoHubRearDoorLock

func interaction_label() -> String:
	return "단말기에서 목적지를 선택하세요"

func interact(_actor: Node) -> void:
	print("봉고차 뒷문은 아직 열 수 없습니다. 먼저 목적지를 선택하세요.")
