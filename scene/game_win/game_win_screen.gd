extends CanvasLayer

@export var custom_font: Font
@export var main_title_text: String = "   你赢了?..."
@export var in_scene: Control
@export var hex_map: Node2D
@export var main_menu_scene_path: String = "res://scene/main_menu/main_menu.tscn"

@onready var victory_instance = $VictoryOrchestrator
@onready var ui_manager = $UI_Layout
@onready var vfx_bg = $PostProcess
@onready var ashes = $VFX_Ashes
@onready var btn = $UI_Layout/menu_btn
@onready var dim = $UI_Layout/DimMenu

var progress: Array[float] = []

func _ready() -> void:
	set_process(false)
	process_mode = Node.PROCESS_MODE_ALWAYS
	layer = 100
	if vfx_bg and vfx_bg.material:
		vfx_bg.material.set("shader_parameter/grayscale_amount", 0.0)
		vfx_bg.material.set("shader_parameter/burn_intensity", 0.0)

func _on_victory_triggered() -> void:
	if in_scene: in_scene.hide_ui_for_external_scene()
	var target_pos = hex_map.global_position if is_instance_valid(hex_map) else Vector2.ZERO
	victory_instance.start_performance(target_pos)
	vfx_bg.show()
	await get_tree().create_timer(1.5).timeout
	ui_manager.show()
	ashes.show()
	start_sequence({"因果": "断裂", "同步率": "0%", "观测": "终止"})

func start_sequence(stats: Dictionary):
	get_tree().paused = true
	var s = Global.get_anim_speed()
	vfx_bg.play_burn_out(s,1.0)
	await get_tree().create_timer(0.5).timeout
	await ui_manager.play_main_title(main_title_text, custom_font, 80, Color.WHITE, 4, 2.0 * s)
	await ui_manager.pour_stats(stats, custom_font, 38, Color.WHITE, 4, s)
	await get_tree().create_timer(0.5).timeout
	var tween = create_tween()
	if btn:
		tween.tween_property(btn, "modulate:a", 1.0, 0.5)\
			.set_trans(Tween.TRANS_SINE)\
			.set_ease(Tween.EASE_IN_OUT)

func _on_btn_main_menu_button_down() -> void:
	await dim.use(0,0)
	await get_tree().create_timer(0.5).timeout
	_start_async_load()
	
func _start_async_load():
	ResourceLoader.load_threaded_request(main_menu_scene_path)
	set_process(true)
	
func _process(_delta: float) -> void:
	var status = ResourceLoader.load_threaded_get_status(main_menu_scene_path, progress)
	if status == ResourceLoader.THREAD_LOAD_LOADED:
		set_process(false)
		var packed_scene = ResourceLoader.load_threaded_get(main_menu_scene_path)
		var next_scene_instance = packed_scene.instantiate()
		var old_scene = get_tree().current_scene
		get_tree().root.add_child(next_scene_instance)
		get_tree().current_scene = next_scene_instance
		get_tree().paused = false
		if old_scene:
			old_scene.queue_free()
		self.queue_free()
	elif status == ResourceLoader.THREAD_LOAD_FAILED:
		print("加载失败！")
		set_process(false)
