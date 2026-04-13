extends Control

@export var Scene_path: String = "res://scene/out_scene/Out_Scene.tscn"

@onready var Set = $UI/MainSet
@onready var Guide = $UI/MainGuide
@onready var Quit = $UI/MainQuit
@onready var Clock = $BG_Layer/Clock/Point
@onready var Mask = $ColorBG

var progress: Array[float] = []
# 用于暂存点击不同按钮时产生的参数
var pending_data: String = ""

func _ready() -> void:
	set_process(false)
	$UI_Layer/ProgressBar.hide()

func _process(_delta: float) -> void:
	var status = ResourceLoader.load_threaded_get_status(Scene_path, progress)
	
	if status == ResourceLoader.THREAD_LOAD_IN_PROGRESS:
		$UI_Layer/ProgressBar.value = progress[0] * 100.0
		
	elif status == ResourceLoader.THREAD_LOAD_LOADED:
		set_process(false)
		$UI_Layer/ProgressBar.value = 100.0
		
		# --- 关键逻辑：手动实例化并传参 ---
		var packed_scene = ResourceLoader.load_threaded_get(Scene_path)
		var next_scene_instance = packed_scene.instantiate()
		
		# 检查目标场景是否有接收变量，并赋值
		if "received_text" in next_scene_instance:
			next_scene_instance.received_text = pending_data
		
		# 将新场景添加到根节点并切换
		get_tree().root.add_child(next_scene_instance)
		get_tree().current_scene = next_scene_instance
		
		# 销毁当前菜单场景
		queue_free()
		
	elif status == ResourceLoader.THREAD_LOAD_FAILED:
		print("加载失败！")
		set_process(false)

# 通用启动加载函数
func _start_async_load():
	ResourceLoader.load_threaded_request(Scene_path)
	$UI_Layer/ProgressBar.show()
	$UI_Layer/ProgressBar.value = 0
	set_process(true)

# --- 按钮逻辑 ---

# 按钮 1：传递空字符串
func _on_new_game_btn_button_down() -> void:
	pending_data = "" 
	Global.clock.emit(2)
	await Mask.start_iris_in(1.0)
	_start_async_load()

# 按钮 2：传递玩家输入的字符
func _on_new_game_btn_2_button_down() -> void:
	$BG_Layer/LineEdit.show()
	while not Input.is_action_just_pressed("ui_accept"):
	# 关键：必须 await 每一帧，否则死循环会卡死游戏
		await get_tree().process_frame
	pending_data = $BG_Layer/LineEdit.text
	_start_async_load()

func _on_continue_btn_button_down() -> void:
	pass # Replace with function body.

func _on_settings_btn_button_down() -> void:
	Set.play_entrance()

func _on_database_btn_button_down() -> void:
	Guide.play_entrance()

func _on_quit_btn_button_down() -> void:
	Quit.play_entrance()
