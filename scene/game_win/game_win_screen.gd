extends CanvasLayer
# 节点引用
@onready var quit_button: Button = $CenterContainer/VBoxContainer/HBoxContainer/BtnQuit
@onready var main_menu_button: Button = $CenterContainer/VBoxContainer/HBoxContainer/BtnMainMenu

# 场景路径配置
@export var main_menu_scene_path: String = "res://scene/main_menu/main_menu.tscn"

func _ready() -> void:
	GameLogger.info("游戏结束画面已加载", "GameOverScreen")
	
	# 连接按钮信号
	if quit_button:
		quit_button.pressed.connect(_on_quit_button_pressed)
	else:
		GameLogger.warning("未找到退出游戏按钮", "GameOverScreen")
	
	if main_menu_button:
		main_menu_button.pressed.connect(_on_main_menu_button_pressed)
	else:
		GameLogger.warning("未找到返回主菜单按钮", "GameOverScreen")
	
	# 设置焦点（可选）
	# quit_button.grab_focus()

## 退出游戏按钮回调
func _on_quit_button_pressed() -> void:
	quit_game()

## 返回主菜单按钮回调
func _on_main_menu_button_pressed() -> void:
	switch_to_main_menu()

## 退出游戏主逻辑
func quit_game() -> void:
	# 可选：保存游戏进度
	# _save_game_progress()
	
	# 执行退出
	get_tree().quit()
## 切换到主菜单
func switch_to_main_menu() -> void:

	var err = get_tree().change_scene_to_file(main_menu_scene_path)
	if err != OK:
		GameLogger.error("切换到主菜单失败: " + str(err), "GameOverScreen")
		pass
