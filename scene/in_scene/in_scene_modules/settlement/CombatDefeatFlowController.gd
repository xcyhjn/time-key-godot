class_name CombatDefeatFlowController
extends RefCounted


## CombatDefeatFlowController 只编排战斗失败后的结算动作。
## 它不判断失败条件、不切换场景，也不处理战斗胜利或最终胜利页。


func run_defeat_flow(config: Dictionary) -> void:
	_stop_bgm(config.get("sound_manager"))
	_call_if_valid(config.get("hide_debug_buttons"))
	_call_if_valid(config.get("hide_ui_for_external_scene"))

	var stats: Dictionary = _build_defeat_stats(
		int(config.get("current_era_value", 1)),
		config.get("global_timecoin")
	)
	_start_game_over_ui(config.get("game_over_ui"), stats)
	_delete_save(config.get("saver"), int(config.get("save_slot", 0)))


func _build_defeat_stats(current_era_value: int, global_timecoin: Variant) -> Dictionary:
	return {
		"所处时代": str(current_era_value),
		"因果状态": "彻底断裂",
		"时间资产": str(_get_timecoins(global_timecoin)),
		"同步率": "0%",
	}


func _get_timecoins(global_timecoin: Variant) -> int:
	if is_instance_valid(global_timecoin) and global_timecoin.has_method("get_timecoins"):
		return int(global_timecoin.get_timecoins())
	return 0


func _start_game_over_ui(game_over_ui: Variant, stats: Dictionary) -> void:
	if is_instance_valid(game_over_ui) and game_over_ui.has_method("start_sequence"):
		game_over_ui.start_sequence(stats)


func _delete_save(saver: Variant, save_slot: int) -> void:
	if is_instance_valid(saver) and saver.has_method("Delete_save"):
		saver.Delete_save(save_slot)


func _stop_bgm(sound_manager: Variant) -> void:
	if is_instance_valid(sound_manager) and sound_manager.has_method("stop_bgm"):
		sound_manager.stop_bgm()


func _call_if_valid(callback: Variant) -> void:
	if callback is Callable:
		var callable: Callable = callback
		if callable.is_valid():
			callable.call()
