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
signal progress_changed(era_value: int, phase_value: int)


## 统一提供给其它场景读取当前时代值的接口。
## 之所以单独封装成方法，是为了让奖励页、局内场景和局外场景都不要直接依赖字段名。
func get_current_era() -> int:
	return max(era, 1)


## 统一设置当前时代值。
## 这里会做最基本的保护，防止时代被写成 0 或负数。
func set_current_era(new_era: int) -> void:
	era = max(new_era, 1)
	_emit_progress_changed()


## 提供当前阶段值读取接口，方便局外 UI 或后续调试面板展示。
func get_current_phase() -> int:
	return max(phase, 1)


## 统一设置当前阶段值。
func set_current_phase(new_phase: int) -> void:
	phase = max(new_phase, 1)
	_normalize_phase_rollover()
	_emit_progress_changed()

func reset():
	era = 1
	phase = 1
	_emit_progress_changed()

func time_detect():
	if phase > 8:
		_normalize_phase_rollover()
		_emit_progress_changed()

func era_change():
	era += 1
	phase = 1
	_emit_progress_changed()


func advance_phase(step_amount: int = 1) -> void:
	phase += max(step_amount, 1)
	_normalize_phase_rollover()
	_emit_progress_changed()


func _normalize_phase_rollover() -> void:
	if phase > 8:
		era += 1
		phase = 1


func _emit_progress_changed() -> void:
	_sync_progress_snapshot()
	progress_changed.emit(get_current_era(), get_current_phase())

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	_restore_progress_snapshot()
	timeline_finished.connect(__advance)


## 把当前时代/阶段写入 MapState，作为跨场景保底快照。
func _sync_progress_snapshot() -> void:
	if MapState and MapState.has_method("set_saved_era_progress"):
		MapState.set_saved_era_progress(era, phase)


## 如果 GlobalClock 被重新初始化，则尝试从 MapState 恢复进度。
## 没有快照时才回退到默认 reset()。
func _restore_progress_snapshot() -> void:
	if MapState and MapState.has_method("get_saved_era") and MapState.has_method("get_saved_phase"):
		era = MapState.get_saved_era()
		phase = MapState.get_saved_phase()
	else:
		reset()

	_sync_progress_snapshot()

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
