extends CheckBox

func _ready():
	# 1. 连接信号
	# 检查是否已经连接，如果没有连接，才进行连接
	if not toggled.is_connected(_on_toggled):
		toggled.connect(_on_toggled)
	
	# 2. 初始化按钮状态
	# 获取当前窗口模式，检查是否已经是全屏，如果是，则把勾选框设为勾选状态
	var current_mode = DisplayServer.window_get_mode()
	if current_mode == DisplayServer.WINDOW_MODE_FULLSCREEN or current_mode == DisplayServer.WINDOW_MODE_EXCLUSIVE_FULLSCREEN:
		button_pressed = true
	else:
		button_pressed = false

func _on_toggled(toggled_on: bool):
	if toggled_on:
		# 切换到全屏模式
		# WINDOW_MODE_FULLSCREEN 通常是无边框全屏窗口（兼容性好，切换快）
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
		
		# 如果你需要独占全屏（性能可能更好，但切换时会有黑屏闪烁），可以使用：
		# DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_EXCLUSIVE_FULLSCREEN)
	else:
		# 切换回窗口模式
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
		
		# 可选：如果你希望窗口变回特定大小（例如 1280x720）
		# DisplayServer.window_set_size(Vector2i(1280, 720))
		# 可选：将窗口居中
		# DisplayServer.window_set_position(DisplayServer.screen_get_position() + DisplayServer.screen_get_size() / 2 - DisplayServer.window_get_size() / 2)
