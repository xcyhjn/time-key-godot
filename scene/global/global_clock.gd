extends Node2D
class_name global_clock

@export_group("暂停菜单")
@export var PAUSE_MENU_SCENE : PackedScene
signal Step_next(Step, beha)

var landform_property_pool : Dictionary = \
		{
			"delete" : [ preload("res://scene/in_scene/enermy/village.gd")]\
		}

var landform_h_pool : Dictionary = \
		{
			1 : [preload("res://scene/in_scene/enermy/village.gd")]
		}

var tile_h_pool : Dictionary = \
		{1 : [],
		2 : [],
		3 : [],
		4 : [],
		5 : [],
		6 : []}

var current_pause_menu: Node = null

var pause_menu_instance: Node = null
var is_pause_open: bool = false

var speed_index: int = 1

signal choose
signal choose_confirm
signal choose_cancel

signal clock(state: int)

signal dim_in
signal dim_out

func get_anim_speed() -> float:
	match speed_index:
		0: return 1.5  # 慢
		1: return 1.0  # 中
		2: return 0.5  # 高 (数值越小动画越快)
	return 1.0


var era : int
var phase : int

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

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	reset()
	timeline_finished.connect(__advance)

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		toggle_pause()
		get_viewport().set_input_as_handled()

func toggle_pause() -> void:
	if not get_tree().paused:
		current_pause_menu = PAUSE_MENU_SCENE.instantiate()
		add_child(current_pause_menu) 
		
		get_tree().paused = true
	else:
		resume_game()

func resume_game() -> void:
	get_tree().paused = false
	if current_pause_menu != null:
		current_pause_menu.queue_free()
		current_pause_menu = null

func switch_to_scene(target_scene_path: String) -> void:
	resume_game() 
	var err = get_tree().change_scene_to_file(target_scene_path)
	
	if err != OK:
		push_error("场景切换失败，请检查路径是否正确: " + target_scene_path)



signal timeline_finished 

var max_steps: int
var current_step: int = 0
var is_completed: bool = false


func __advance(step_amount: int = 1) -> void:
	pass
