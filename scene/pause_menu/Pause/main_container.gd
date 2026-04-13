extends Control

# 引用背景
@onready var bg_rect = $BackgroundShader
# 引用新的内容容器（包含 OptionsMenu 和 SaveOptions）
@onready var content = $MenuContainer

# 记录内容的原始位置
var original_content_pos: Vector2

func _ready():
	# 确保父级容器不拦截鼠标，允许点击穿透到按钮
	if content:
		original_content_pos = content.position
		content.mouse_filter = Control.MOUSE_FILTER_IGNORE
	
	self.hide()
	self.modulate.a = 0
	
	# 初始化 Shader 状态（完全不可见的状态）
	# -1.0 保证斜切线在屏幕左上角之外
	set_shader_slant(-1.0)
	
	process_mode = Node.PROCESS_MODE_ALWAYS

func _input(event):
	if event.is_action_pressed("ui_cancel"):
		get_viewport().set_input_as_handled()
		if not self.visible:
			open_menu()
		else:
			close_menu()

func open_menu():
	self.show()
	get_tree().paused = true
	
	var tween = create_tween().set_parallel(true).set_trans(Tween.TRANS_QUART).set_ease(Tween.EASE_OUT)
	
	# 1. 整体淡入 (最快)
	tween.tween_property(self, "modulate:a", 1.0, 0.2)
	
	# 2. 背景斜切扫入 (先于内容)
	# 从 -0.5 (左上角外) 扫到 2.5 (完全覆盖右下角)
	# 2.5 是根据你的 degree=1.0 计算出的安全值，如果 degree 更大，这个值也要更大
	tween.tween_method(set_shader_slant, -0.5, 0.8, 0.5) # 0.8 留出一部分
	
	# 3. 内容容器滑入 + 旋转 (设置 delay 让它晚一点出现)
	content.position.x = original_content_pos.x - 150 # 初始偏移
	content.rotation_degrees = -3.0 # 初始旋转
	content.modulate.a = 0.0 # 初始透明
	
	# 延迟 0.15秒，等背景出来一半了再出来
	tween.tween_property(content, "position:x", original_content_pos.x, 0.5).set_delay(0.15)
	tween.tween_property(content, "rotation_degrees", 0.0, 0.5).set_delay(0.15)
	tween.tween_property(content, "modulate:a", 1.0, 0.3).set_delay(0.15)

func close_menu():
	var tween = create_tween().set_parallel(true).set_trans(Tween.TRANS_QUART).set_ease(Tween.EASE_IN)
	
	# 1. 内容先消失
	tween.tween_property(content, "position:x", original_content_pos.x - 50, 0.2)
	tween.tween_property(content, "modulate:a", 0.0, 0.2)
	
	# 2. 背景斜切扫出 (延迟一点点，等内容看不见了再撤背景)
	# 也就是把 open_menu 的过程反过来，从 2.5 变回 -0.5
	tween.tween_method(set_shader_slant, 0.8, -0.5, 0.4).set_delay(0.1)
	
	# 3. 整体淡出
	tween.tween_property(self, "modulate:a", 0.0, 0.3).set_delay(0.2)
	
	await tween.finished
	self.hide()
	get_tree().paused = false

# 辅助函数：将 Tween 的值传递给 Shader
func set_shader_slant(value: float):
	# 必须确保 ColorRect 上挂载了 ShaderMaterial
	if bg_rect.material:
		bg_rect.material.set_shader_parameter("slant_offset", value)
