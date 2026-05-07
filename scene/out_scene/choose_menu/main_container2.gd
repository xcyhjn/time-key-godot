extends Control

# --- 引用 UI 节点 ---
@onready var bg_rect = $BackgroundShader
@onready var content = $MenuContainer
@onready var name_label = $MenuContainer/SaveOptions/Name
@onready var desc_label = $MenuContainer/SaveOptions/Description
@onready var confirm_button = $MenuContainer/SaveOptions/ButtonGroup/SaveToTitle

# --- 角色资源配置 (Inspector 中配置) ---
@export_group("角色内容配置")
@export var names: Array[String] = ["角色1", "角色2", "角色3", "角色4", "角色5", "角色6"]
@export var descriptions: Array[String] = ["描述1", "描述2", "描述3", "描述4", "描述5", "描述6"]
@export var colors_start: Array[Color] = [Color.WHITE, Color.WHITE, Color.WHITE, Color.WHITE, Color.WHITE, Color.WHITE]
@export var colors_end: Array[Color] = [Color.WHITE, Color.WHITE, Color.WHITE, Color.WHITE, Color.WHITE, Color.WHITE]
@export var images: Array[Texture2D] = [null, null, null, null, null, null]

@export_group("全局配置")
@export_range(0.0, 1.0) var right_image_opacity: float = 0.8

# 动画控制
var target_slants = Vector2(0.72, 0.55) 
var initial_slants = Vector2(-1.2, -1.0)
var is_animating: bool = false
var is_selectable: bool = false # 记录当前角色是否为初始可选

func _ready():
	self.process_mode = Node.PROCESS_MODE_ALWAYS
	self.hide()
	self.modulate.a = 0
	
	set_left_slant(initial_slants.x)
	set_right_slant(initial_slants.y)
	
	if Global.has_signal("choose"):
		Global.choose.connect(change)


func _input(event: InputEvent) -> void:
	if not visible or is_animating:
		return
	if event.is_action_pressed("ui_cancel"):
		_on_quit_button_down()
		get_viewport().set_input_as_handled()

func change(type: int = 6):
	if is_animating: return
	if not self.visible:
		open_menu(type)
	else:
		close_menu()

func open_menu(type: int):
	is_animating = true
	
	# 计算索引 (type 6 -> index 0)
	var index = type - 6
	
	# --- 核心逻辑：判断是否为开放角色 (索引 1 和 3) ---
	is_selectable = (index == 1 or index == 3)
	
	# 控制确认按钮的显示与隐藏
	if confirm_button:
		confirm_button.visible = is_selectable

	# --- 1. 更新 UI 文本内容 ---
	if index >= 0 and index < names.size():
		if name_label: name_label.text = names[index]
	
	if index >= 0 and index < descriptions.size():
		if desc_label: desc_label.text = descriptions[index]

	# --- 2. 动态应用 Shader 配置 ---
	if index >= 0 and index < images.size():
		if bg_rect.material:
			bg_rect.material.set_shader_parameter("left_color_start", colors_start[index])
			bg_rect.material.set_shader_parameter("left_color_end", colors_end[index])
			bg_rect.material.set_shader_parameter("right_texture", images[index])
			bg_rect.material.set_shader_parameter("right_texture_alpha", right_image_opacity)
	else:
		push_warning("选角类型 ", type, " 配置缺失")

	# --- 3. 动画逻辑 ---
	self.show()
	get_tree().paused = true
	
	var tween = create_tween().set_parallel(true).set_trans(Tween.TRANS_QUART).set_ease(Tween.EASE_OUT)
	tween.set_pause_mode(Tween.TWEEN_PAUSE_PROCESS) 
	
	tween.tween_property(self, "modulate:a", 1.0, 0.2)
	tween.tween_method(set_left_slant, initial_slants.x, target_slants.x, 0.5)
	tween.tween_method(set_right_slant, initial_slants.y, target_slants.y, 0.45) 
	
	if content:
		content.modulate.a = 0
		content.position.x = 100
		tween.tween_property(content, "modulate:a", 1.0, 0.3).set_delay(0.2)
		tween.tween_property(content, "position:x", 0, 0.4).set_delay(0.2)
	
	await tween.finished
	is_animating = false

func close_menu():
	is_animating = true
	var tween = create_tween().set_parallel(true).set_trans(Tween.TRANS_QUART).set_ease(Tween.EASE_IN)
	tween.set_pause_mode(Tween.TWEEN_PAUSE_PROCESS) 
	
	if content:
		tween.tween_property(content, "modulate:a", 0.0, 0.2)
		tween.tween_property(content, "position:x", -50, 0.2)
	
	tween.tween_method(set_left_slant, target_slants.x, initial_slants.x, 0.4).set_delay(0.1)
	tween.tween_method(set_right_slant, target_slants.y, initial_slants.y, 0.35).set_delay(0.1)
	tween.tween_property(self, "modulate:a", 0.0, 0.2).set_delay(0.3)
	
	await tween.finished
	self.hide()
	get_tree().paused = false
	is_animating = false

func set_left_slant(value: float):
	if bg_rect and bg_rect.material:
		bg_rect.material.set_shader_parameter("left_slant_pos", value)

func set_right_slant(value: float):
	if bg_rect and bg_rect.material:
		bg_rect.material.set_shader_parameter("right_slant_pos", value)

# --- 按钮回调 ---
func _on_save_to_title_button_down():
	# 动画中或者角色不可选时，禁止点击
	if is_animating or not is_selectable: 
		return
	Global.choose_confirm.emit()
	close_menu()

func _on_quit_button_down():
	if is_animating: return
	Global.choose_cancel.emit()
	close_menu()
