extends Control

const TutorialSaveScript = preload("res://scene/tutorial/tutorial_save.gd")

@export var Scene_path: String = "res://scene/out_scene/Out_Scene.tscn"
@export_file("*.tscn") var normal_scene_path: String = "res://scene/out_scene/Out_Scene.tscn"
@export_file("*.tscn") var tutorial_scene_path: String = "res://scene/tutorial/tutorial_out_scene.tscn"

@export_group("教程入口弹窗")
@export var tutorial_font: Font
@export var custom_theme: Theme
@export var tutorial_prompt_title: String = "新手教程"
@export var tutorial_prompt_text: String = "是否进入新手教程？\n教程会从局外选角开始，带你完成第一场战斗与收获流程。"
@export var tutorial_prompt_yes_text: String = "是"
@export var tutorial_prompt_no_text: String = "否"

@export_group("教程调试")
## 开启后，每次点击进入新游戏前都会删除 user://tutorial_settings.cfg，方便反复测试首次教程弹窗。
@export var debug_clear_tutorial_record_on_new_game: bool = true

@onready var Set = $UI/MainSet
@onready var Guide = $UI/MainGuide
@onready var Quit = $UI/MainQuit
@onready var Clock = $BG_Layer/Clock/Point
@onready var Mask = $ColorBG
@onready var point = $BG_Layer/Clock/Point
@onready var dim = $UI/DimMenu
@onready var seed_line_edit: LineEdit = $LineEdit

var progress: Array[float] = []
# 用于暂存点击不同按钮时产生的参数
var pending_data: String = ""
var _tutorial_prompt_dialog: ConfirmationDialog = null
var _tutorial_prompt_decision_made: bool = false
var _tutorial_prompt_close_only: bool = false

func _ready() -> void:
	await dim.use(1,1)
	Global.clock.emit(1)
	set_process(false)
	$UI_Layer/ProgressBar.hide()
	if is_instance_valid(seed_line_edit):
		seed_line_edit.hide()


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		if _handle_escape():
			get_viewport().set_input_as_handled()


func _handle_escape() -> bool:
	if _close_tutorial_prompt_from_escape():
		return true
	if _close_seed_input_from_escape():
		return true
	if _close_active_overlay_from_escape():
		return true
	if _can_open_settings_from_escape():
		_on_settings_btn_button_down()
		return true
	return false


func _can_open_settings_from_escape() -> bool:
	if _tutorial_prompt_is_open():
		return false
	if _is_seed_input_open():
		return false
	if not is_instance_valid(Set):
		return false
	if Set.visible:
		return false
	if is_instance_valid(Guide) and Guide.visible:
		return false
	if is_instance_valid(Quit) and Quit.visible:
		return false
	return true


func _close_tutorial_prompt_from_escape() -> bool:
	if not _tutorial_prompt_is_open():
		return false
	_on_tutorial_prompt_close_requested()
	return true


func _close_seed_input_from_escape() -> bool:
	if not _is_seed_input_open():
		return false
	_hide_seed_input()
	return true


func _close_active_overlay_from_escape() -> bool:
	if is_instance_valid(Set) and Set.visible and Set.has_method("_on_back_button_down"):
		Set._on_back_button_down()
		return true
	if is_instance_valid(Guide) and Guide.visible and Guide.has_method("_on_back_button_down"):
		Guide._on_back_button_down()
		return true
	if is_instance_valid(Quit) and Quit.visible and Quit.has_method("_on_back_button_down"):
		Quit._on_back_button_down()
		return true
	return false

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
	_clear_tutorial_record_for_debug()
	if TutorialSaveScript.should_show_prompt():
		_show_tutorial_prompt()
		return

	await _start_normal_new_game()


func _start_normal_new_game() -> void:
	_reset_run_state_for_new_game()
	Scene_path = normal_scene_path
	pending_data = "" 
	Global.clock.emit(2)
	await Mask.start_iris_in(1.0)
	_start_async_load()


func _start_tutorial_new_game() -> void:
	_reset_run_state_for_new_game()
	Scene_path = tutorial_scene_path
	pending_data = "tutorial"
	Global.clock.emit(2)
	await Mask.start_iris_in(1.0)
	_start_async_load()


func _clear_tutorial_record_for_debug() -> void:
	if debug_clear_tutorial_record_on_new_game:
		TutorialSaveScript.delete_settings_file_if_exists()


## 首次点击新游戏时弹出教程选择。
## 核心逻辑：无论选择“是/否”，都会立刻写入 user://，保证弹窗只触发一次。
func _show_tutorial_prompt() -> void:
	if not is_instance_valid(_tutorial_prompt_dialog):
		_tutorial_prompt_dialog = ConfirmationDialog.new()
		_tutorial_prompt_dialog.name = "TutorialPromptDialog"
		_tutorial_prompt_dialog.exclusive = true
		_tutorial_prompt_dialog.unresizable = true
		add_child(_tutorial_prompt_dialog)
		if tutorial_font:
			_tutorial_prompt_dialog.add_theme_font_override("font", tutorial_font)
			_tutorial_prompt_dialog.add_theme_font_override("title_font", tutorial_font)
			_tutorial_prompt_dialog.get_ok_button().add_theme_font_override("font", tutorial_font)
			_tutorial_prompt_dialog.get_cancel_button().add_theme_font_override("font", tutorial_font)
		if custom_theme:
			_tutorial_prompt_dialog.theme = custom_theme
		_tutorial_prompt_dialog.get_ok_button().pressed.connect(_on_tutorial_prompt_yes_pressed)
		_tutorial_prompt_dialog.get_cancel_button().pressed.connect(_on_tutorial_prompt_no_pressed)
		_tutorial_prompt_dialog.close_requested.connect(_on_tutorial_prompt_close_requested)

	_tutorial_prompt_decision_made = false
	_tutorial_prompt_close_only = false
	TutorialSaveScript.mark_prompt_seen(false)
	_tutorial_prompt_dialog.title = tutorial_prompt_title
	_tutorial_prompt_dialog.dialog_text = tutorial_prompt_text
	_tutorial_prompt_dialog.ok_button_text = tutorial_prompt_yes_text
	_tutorial_prompt_dialog.cancel_button_text = tutorial_prompt_no_text
	_tutorial_prompt_dialog.popup_centered(Vector2i(520, 240))


func _on_tutorial_prompt_yes_pressed() -> void:
	if _tutorial_prompt_decision_made:
		return
	_tutorial_prompt_decision_made = true
	TutorialSaveScript.mark_prompt_seen(true)
	await _start_tutorial_new_game()


func _on_tutorial_prompt_no_pressed() -> void:
	if _tutorial_prompt_decision_made:
		return
	_tutorial_prompt_decision_made = true
	TutorialSaveScript.mark_prompt_seen(false)
	await _start_normal_new_game()


func _on_tutorial_prompt_close_requested() -> void:
	if _tutorial_prompt_decision_made or _tutorial_prompt_close_only:
		return

	_tutorial_prompt_close_only = true
	TutorialSaveScript.mark_prompt_seen(false)
	if is_instance_valid(_tutorial_prompt_dialog):
		_tutorial_prompt_dialog.hide()
	_tutorial_prompt_close_only = false


func _tutorial_prompt_is_open() -> bool:
	return is_instance_valid(_tutorial_prompt_dialog) and _tutorial_prompt_dialog.visible


func _is_seed_input_open() -> bool:
	return is_instance_valid(seed_line_edit) and seed_line_edit.visible


func _hide_seed_input(clear_text: bool = false) -> void:
	if not is_instance_valid(seed_line_edit):
		return
	seed_line_edit.hide()
	seed_line_edit.release_focus()
	if clear_text:
		seed_line_edit.text = ""

# 按钮 2：传递玩家输入的字符
func _on_new_game_btn_2_button_down() -> void:
	_clear_tutorial_record_for_debug()
	_reset_run_state_for_new_game()
	Scene_path = normal_scene_path
	if not is_instance_valid(seed_line_edit):
		return
	seed_line_edit.show()
	seed_line_edit.grab_focus()
	while not Input.is_action_just_pressed("ui_accept"):
	# 关键：必须 await 每一帧，否则死循环会卡死游戏
		await get_tree().process_frame
	pending_data = seed_line_edit.text
	_hide_seed_input()
	_start_async_load()

func _on_continue_btn_button_down() -> void:
	pass # Replace with function body.

func _on_settings_btn_button_down() -> void:
	Set.play_entrance()

func _on_database_btn_button_down() -> void:
	Guide.play_entrance()

func _on_quit_btn_button_down() -> void:
	Quit.play_entrance()
