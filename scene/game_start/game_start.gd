# 功能: 游戏启动画面控制器，负责播放开场 Logo 动画并进入主菜单。
# 核心逻辑: _ready() 自动播放指定 AnimationPlayer 动画；动画结束或兜底等待结束后调用 _go_to_main_menu() 切换场景。
extends Control

@export_file("*.tscn") var main_menu_scene_path: String = "res://scene/main_menu/main_menu.tscn"
@export var intro_animation_name: StringName = &"logo_label"
@export_range(0.0, 10.0, 0.1, "or_greater") var fallback_wait_time: float = 1.0
@export var allow_skip: bool = false

@onready var animation_player: AnimationPlayer = $AnimationPlayer

var _is_switching: bool = false


func _ready() -> void:
	await _play_intro()
	_go_to_main_menu()

#任意输入直接跳过
func _unhandled_input(event: InputEvent) -> void:
	if allow_skip and _is_skip_event(event):
		_go_to_main_menu()

#防止找不到动画卡死
func _play_intro() -> void:
	if is_instance_valid(animation_player) and animation_player.has_animation(intro_animation_name):
		animation_player.play(intro_animation_name)
		await animation_player.animation_finished
	else:
		await get_tree().create_timer(fallback_wait_time).timeout


func _go_to_main_menu() -> void:
	if _is_switching:
		return
	_is_switching = true

	var err := get_tree().change_scene_to_file(main_menu_scene_path)
	if err != OK:
		_is_switching = false
		push_error("GameStart: 无法切换到主菜单: %s, err=%s" % [main_menu_scene_path, err])

#神秘多端输入适配
func _is_skip_event(event: InputEvent) -> bool:
	if event is InputEventKey:
		return event.pressed and not event.echo
	if event is InputEventMouseButton:
		return event.pressed
	if event is InputEventJoypadButton:
		return event.pressed
	return false
