extends CanvasLayer

@export_group("全局配置")
@export var custom_font: Font
@export var main_title_text: String = "观测终止"

@onready var vfx_bg = $PostProcess
@onready var vfx_clock = $VFX_Clock
@onready var ui_manager = $UI_Layout

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
	
	# 1. 背景变灰与时钟弹出【同步开始】
	vfx_bg.play_burn_out(s)
	
	# 2. 等待时钟死亡序列（内含变灰逻辑）
	# 此函数结束时，正好是时钟碎裂、尘埃(Ashes)升起的瞬间
	await vfx_clock.play_death_sequence(2.0 * s)
	
	# 3. 【同步点】尘埃出现的同时，展开总标题
	await ui_manager.play_main_title(main_title_text, custom_font, 80, Color.RED, 4, 2.0 * s)
	
	# 4. 最后流出数据结算
	ui_manager.pour_stats(stats, custom_font, 38, Color.WHITE, 4, s)

func _unhandled_input(event: InputEvent):
	if event is InputEventKey and event.pressed and event.keycode == KEY_G:
		if visible: hide()
		start_sequence({"因果": "断裂", "同步率": "0%", "观测": "终止"})
	
	if visible and event is InputEventMouseButton and event.pressed:
		get_tree().paused = false
		get_tree().reload_current_scene()
