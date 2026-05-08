extends CanvasLayer

@export var custom_font: Font
@export var main_title_text: String = "观测终止"
@export var main_menu_scene_path: String = "res://scene/main_menu/main_menu.tscn"

@onready var vfx_bg = $PostProcess
@onready var vfx_clock = $VFX_Clock
@onready var ui_manager = $UI_Layout
@onready var btn = $UI_Layout/menu_btn
@onready var dim = $UI_Layout/DimMenu

var progress: Array[float] = []
var _threaded_load_in_progress: bool = false

func _ready():
	process_mode = Node.PROCESS_MODE_ALWAYS
	layer = 100
	hide() 
	# 初始重置背景 Shader
	if vfx_bg and vfx_bg.material:
		vfx_bg.material.set("shader_parameter/grayscale_amount", 0.0)
		vfx_bg.material.set("shader_parameter/burn_intensity", 0.0)

func start_sequence(stats: Dictionary):
	show()
	get_tree().paused = true
	var s = Global.get_anim_speed()
	
	vfx_bg.play_burn_out(s,0.5)
	await vfx_clock.play_death_sequence(2.0 * s)
	await ui_manager.play_main_title(main_title_text, custom_font, 80, Color.RED, 4, 2.0 * s)
	await ui_manager.pour_stats(stats, custom_font, 38, Color.WHITE, 4, s)
	var tween = create_tween()
	if btn:
		tween.tween_property(btn, "modulate:a", 1.0, 0.5)\
			.set_trans(Tween.TRANS_SINE)\
			.set_ease(Tween.EASE_IN_OUT)

func _on_menu_btn_button_down() -> void:
	await dim.use(0,0)
	await get_tree().create_timer(0.5).timeout
	_start_async_load()

func _start_async_load():
	var err := ResourceLoader.load_threaded_request(main_menu_scene_path)
	if err != OK:
		if SceneLog:
			SceneLog.error_event("GameOver", "threaded request failed", {"path": main_menu_scene_path, "err": err})
		return
	_threaded_load_in_progress = true
	if SceneLog:
		SceneLog.scene_event("GameOver", "threaded request started", {"path": main_menu_scene_path})
	set_process(true)

func _process(_delta: float) -> void:
	if not _threaded_load_in_progress:
		return
	var status = ResourceLoader.load_threaded_get_status(main_menu_scene_path, progress)
	if status == ResourceLoader.THREAD_LOAD_LOADED:
		_threaded_load_in_progress = false
		set_process(false)
		var packed_scene = ResourceLoader.load_threaded_get(main_menu_scene_path)
		var next_scene_instance = packed_scene.instantiate()
		var old_scene = get_tree().current_scene
		get_tree().root.add_child(next_scene_instance)
		get_tree().current_scene = next_scene_instance
		if SceneLog:
			SceneLog.scene_event("GameOver", "switch to main menu success", {"path": main_menu_scene_path})
		get_tree().paused = false
		if old_scene:
			old_scene.queue_free()
		self.queue_free()
	elif status == ResourceLoader.THREAD_LOAD_FAILED:
		print("加载失败！")
		if SceneLog:
			SceneLog.error_event("GameOver", "threaded load failed", {"path": main_menu_scene_path})
		set_process(false)
		_threaded_load_in_progress = false
