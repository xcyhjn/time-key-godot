class_name GameWinFlowController
extends RefCounted


## GameWinFlowController 只处理最终胜利页触发。
## 它不处理战斗胜利进入结算期，也不修改局外返回 payload。


func run_win_flow(config: Dictionary) -> void:
	_stop_bgm(config.get("sound_manager"))
	_play_win_sfx(config.get("sound_manager"), str(config.get("sfx_name", "game_win")))
	_call_if_valid(config.get("hide_debug_buttons"))
	_trigger_win_screen(config.get("win_screen"))


func _stop_bgm(sound_manager: Variant) -> void:
	if is_instance_valid(sound_manager) and sound_manager.has_method("stop_bgm"):
		sound_manager.stop_bgm()


func _play_win_sfx(sound_manager: Variant, sfx_name: String) -> void:
	if is_instance_valid(sound_manager) and sound_manager.has_method("play_looping_sfx"):
		sound_manager.play_looping_sfx(sfx_name)


func _trigger_win_screen(win_screen: Variant) -> void:
	if is_instance_valid(win_screen) and win_screen.has_method("_on_victory_triggered"):
		win_screen._on_victory_triggered()


func _call_if_valid(callback: Variant) -> void:
	if callback is Callable:
		var callable: Callable = callback
		if callable.is_valid():
			callable.call()
