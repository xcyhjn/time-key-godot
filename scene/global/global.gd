extends Node2D

var era : int
var phase : int
var speed_index: int = 1

signal choose
signal choose_confirm
signal choose_cancel

signal clock(state: int)

signal dim_in
signal dim_out

signal phase_change

func _ready() -> void:
	reset()

func reset():
	era = 1
	phase = 1
	pass

func time_detect():
	if phase > 8:
		era_change()

func era_change():
	era += 1
	phase = 1

func get_anim_speed() -> float:
	match speed_index:
		0: return 1.5  # 慢
		1: return 1.0  # 中
		2: return 0.5  # 高 (数值越小动画越快)
	return 1.0
