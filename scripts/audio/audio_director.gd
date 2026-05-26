extends Node
class_name AudioDirector

signal cue_played(cue_name: String)

var played_cues: Array[String] = []

func play_cue(cue_name: String) -> void:
	played_cues.append(cue_name)
	cue_played.emit(cue_name)
	print("AUDIO_CUE:%s" % cue_name)

func play_stage_cues(cues: Array[String]) -> void:
	for cue in cues:
		play_cue(cue)
