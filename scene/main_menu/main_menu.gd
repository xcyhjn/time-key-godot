extends Control

@export var Scene_path: String = "res://scene/out_scene/Out_Scene.tscn"

@onready var Set = $UI/MainSet
@onready var Guide = $UI/MainGuide
@onready var Quit = $UI/MainQuit
@onready var Clock = $BG_Layer/Clock/Point
@onready var Mask = $ColorBG
@onready var point = $BG_Layer/Clock/Point
@onready var dim = $UI/DimMenu

var progress: Array[float] = []
# 用于暂存点击不同按钮时产生的参数
var pending_data: String = ""

func _ready() -> void:
	await dim.use(1,1)
	Global.clock.emit(1)
	set_process(false)
	$UI_Layer/ProgressBar.hide()

func _process(_delta: float) -> void:
	var status = ResourceLoader.load_threaded_get_status(Scene_path, progress)
	
	if status == ResourceLoader.THREAD_LOAD_IN_PROGRESS:
		$UI_Layer/ProgressBar.value = progress[0] * 100.0
		
	elif status == ResourceLoader.THREAD_LOAD_LOADED:
		set_process(false)
		$UI_Layer/ProgressBar.value = 100.0
		
		var packed_scene = ResourceLoader.load_threaded_get(Scene_path)
		var next_scene_instance = packed_scene.instantiate()
		
		if _object_has_property(next_scene_instance, &"received_text"):
			next_scene_instance.received_text = pending_data
		
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


## 安全判断对象是否声明了某个属性。
func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	for property_info in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true
	return false


## 新开局前统一重置本轮 run 的全局状态。
## 这里会清理：
## - 局外地图保存状态 MapState
## - 时代与阶段 GlobalClock / Global
## - 时间币 GlobalTimecoin
## - 玩家牌组 GlobalDB.player_deck
##
## 这样点击“新游戏”后，OutScene 进入时一定会走“生成新地图”的逻辑，
## 而不是继续读取上一局还没玩完的地图与进度。
func _reset_run_state_for_new_game() -> void:
	if MapState and MapState.has_method("reset"):
		MapState.reset()

	if GlobalClock and GlobalClock.has_method("reset"):
		GlobalClock.reset()

	if Global and Global.has_method("reset"):
		Global.reset()

	if GlobalTimecoin and GlobalTimecoin.has_method("set_timecoins"):
		GlobalTimecoin.set_timecoins(0)

	if GlobalDB:
		if GlobalDB.has_method("reset_player_deck"):
			GlobalDB.reset_player_deck()
		elif _object_has_property(GlobalDB, &"player_deck"):
			GlobalDB.player_deck = ["1", "1", "1", "1", "1", "1", "1", "2", "2", "2", "3", "3", "3", "3"]
			#硬编码初始牌组，未来可以改成从外部数据表读取或者在编辑器里配置，兜底内容

	if MapState and MapState.has_method("set_saved_deck") and GlobalDB and _object_has_property(GlobalDB, &"player_deck"):
		MapState.set_saved_deck(GlobalDB.player_deck)

# --- 按钮逻辑 ---

# 按钮 1：传递空字符串
func _on_new_game_btn_button_down() -> void:
	_reset_run_state_for_new_game()
	pending_data = "" 
	Global.clock.emit(2)
	await Mask.start_iris_in(1.0)
	_start_async_load()

# 按钮 2：传递玩家输入的字符
func _on_new_game_btn_2_button_down() -> void:
	_reset_run_state_for_new_game()
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
