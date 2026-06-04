class_name CombatVictorySettlementController
extends RefCounted


## CombatVictorySettlementController 只编排“战斗胜利后进入结算期”的场景动作。
## 它不判断胜利条件、不切换局外场景，也不处理失败结算。


func enter_settlement(config: Dictionary) -> void:
	_stop_bgm(config.get("sound_manager"))
	_call_if_valid(config.get("hide_debug_buttons"))
	_call_if_valid(config.get("disable_player_inputs"))
	_call_if_valid(config.get("hide_combat_phase_ui"))
	_call_if_valid(config.get("prepare_deck_button_for_settlement"), [true])
	_clear_timeline(config.get("timeline_manager"))
	_enter_reward_mode(config.get("hex_map"), config.get("reward_host"))
	_hide_total_enemy_health(config.get("total_enemy_health_bar"))
	_call_if_valid(config.get("set_single_health_bars_visible"), [false])

	await _play_victory_banner(
		config.get("combat_victory_banner"),
		str(config.get("banner_text", "战斗胜利"))
	)

	_call_if_valid(config.get("show_settlement_buttons"))


func _stop_bgm(sound_manager: Variant) -> void:
	if is_instance_valid(sound_manager) and sound_manager.has_method("stop_bgm"):
		sound_manager.stop_bgm()


func _clear_timeline(timeline_manager: Variant) -> void:
	if is_instance_valid(timeline_manager) and timeline_manager.has_method("clear_grid"):
		timeline_manager.clear_grid()


func _enter_reward_mode(hex_map: Variant, reward_host: Variant) -> void:
	if not is_instance_valid(hex_map):
		return

	if hex_map.has_method("set_tiles_interactive"):
		hex_map.set_tiles_interactive(false)
	if hex_map.has_method("set_visuals_locked"):
		hex_map.set_visuals_locked(true)
	if hex_map.has_method("update_all_stack_conditional_effects"):
		hex_map.update_all_stack_conditional_effects()
	if hex_map.has_method("enter_settlement_reward_mode"):
		hex_map.enter_settlement_reward_mode(reward_host)


func _hide_total_enemy_health(total_enemy_health_bar: Variant) -> void:
	if is_instance_valid(total_enemy_health_bar):
		total_enemy_health_bar.hide()


func _play_victory_banner(combat_victory_banner: Variant, banner_text: String) -> void:
	if is_instance_valid(combat_victory_banner) and combat_victory_banner.has_method("play_banner"):
		await combat_victory_banner.play_banner(banner_text)


func _call_if_valid(callback: Variant, args: Array = []) -> void:
	if callback is Callable:
		var callable: Callable = callback
		if callable.is_valid():
			callable.callv(args)
