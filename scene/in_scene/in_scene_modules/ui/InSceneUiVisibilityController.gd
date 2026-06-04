class_name InSceneUiVisibilityController
extends RefCounted


## InSceneUiVisibilityController 集中执行局外场景和结算阶段的 UI 显隐。
## 它只读配置字典里的节点和状态，不切换场景、不消费奖励、不改变战斗流程。


func hide_for_external_scene(config: Dictionary) -> void:
	_hide_all(config, ["player_hand", "deck_pile", "discard_pile"])
	_hide_all(config, ["deck_button", "discard_button", "end_turn_button", "end_combat_button"])
	_set_button_visible(config.get("height_view_toggle_button"), false, true)
	_hide_all(config, ["timeline_ui", "cursor_tooltip", "cursor_tooltip_panel"])
	_call_if_valid(config.get("hide_tooltip"))
	_hide_all(config, ["shop_button", "acquire_reward_button", "remove_reward_button", "craft_reward_button", "total_enemy_health_bar"])
	set_single_health_bars_visible(config.get("hex_map"), false)

	var hex_map: Variant = config.get("hex_map")
	if is_instance_valid(hex_map):
		hex_map.show()
		hex_map.z_index = 0

	var timecoin_container: Variant = config.get("timecoin_container")
	if is_instance_valid(timecoin_container):
		timecoin_container.show()
		timecoin_container.z_index = 100


func set_single_health_bars_visible(hex_map: Variant, is_visible: bool) -> void:
	if not is_instance_valid(hex_map):
		return
	var bar_manager: Variant = hex_map.get_node_or_null("BarManager")
	if bar_manager and bar_manager.has_method("set_all_health_bars_visible"):
		bar_manager.set_all_health_bars_visible(is_visible)


func hide_debug_buttons(debug_buttons: Array) -> void:
	for button in debug_buttons:
		if not is_instance_valid(button):
			continue
		button.hide()
		if button is BaseButton:
			(button as BaseButton).disabled = true


func refresh_height_view_toggle_after_external_scene(config: Dictionary) -> void:
	_set_button_visible(
		config.get("height_view_toggle_button"),
		bool(config.get("is_settlement", false)),
		not bool(config.get("is_settlement", false))
	)


func restore_after_external_scene(config: Dictionary) -> void:
	refresh_height_view_toggle_after_external_scene(config)

	if bool(config.get("resolution_hides_debug_buttons", false)):
		_restore_settlement_exit_if_needed(config)
		return

	_set_settlement_debug_buttons_visible(config, bool(config.get("show_settlement_debug_buttons", false)))
	_restore_settlement_exit_if_needed(config)


func restore_all(config: Dictionary) -> void:
	_show_all(config, ["player_hand", "deck_pile", "discard_pile"])
	_show_all(config, ["deck_button", "discard_button", "end_turn_button", "end_combat_button"])
	_set_button_visible(config.get("height_view_toggle_button"), true, false)
	_show_all(config, ["timeline_ui"])

	if bool(config.get("is_settlement", false)):
		show_settlement_buttons(config)
		_hide_all(config, ["total_enemy_health_bar"])
		set_single_health_bars_visible(config.get("hex_map"), false)
	else:
		hide_settlement_buttons(config)
		_show_all(config, ["total_enemy_health_bar"])
		set_single_health_bars_visible(config.get("hex_map"), true)


func hide_settlement_buttons(config: Dictionary) -> void:
	_hide_all(config, ["shop_button", "acquire_reward_button", "remove_reward_button", "craft_reward_button"])
	_set_button_visible(config.get("end_combat_button"), false, false)


func show_settlement_buttons(config: Dictionary) -> void:
	if bool(config.get("resolution_hides_debug_buttons", false)):
		_set_button_visible(config.get("end_combat_button"), true, false)
		return

	_set_settlement_debug_buttons_visible(config, bool(config.get("show_settlement_debug_buttons", false)))
	_set_button_visible(config.get("end_combat_button"), true, false)


func hide_combat_phase_for_settlement(config: Dictionary) -> void:
	_hide_all(config, ["player_hand", "deck_pile", "discard_pile", "discard_button", "end_turn_button"])
	var timeline_ui: Variant = config.get("timeline_ui")
	if is_instance_valid(timeline_ui):
		if timeline_ui.has_method("clear_grid_preview"):
			timeline_ui.clear_grid_preview()
		timeline_ui.hide()
	_hide_all(config, ["cursor_tooltip", "cursor_tooltip_panel"])


func _restore_settlement_exit_if_needed(config: Dictionary) -> void:
	if not bool(config.get("is_settlement", false)):
		return

	_set_button_visible(config.get("end_combat_button"), true, false)
	_call_if_valid(config.get("prepare_deck_button_for_settlement"))
	_hide_all(config, ["total_enemy_health_bar"])
	set_single_health_bars_visible(config.get("hex_map"), false)


func _set_settlement_debug_buttons_visible(config: Dictionary, is_visible: bool) -> void:
	var keys: Array[String] = [
		"shop_button",
		"acquire_reward_button",
		"remove_reward_button",
		"craft_reward_button",
	]
	for key in keys:
		var button: Variant = config.get(key)
		if not is_instance_valid(button):
			continue
		if is_visible:
			button.show()
		else:
			button.hide()


func _show_all(config: Dictionary, keys: Array[String]) -> void:
	for key in keys:
		var node: Variant = config.get(key)
		if is_instance_valid(node):
			node.show()


func _hide_all(config: Dictionary, keys: Array[String]) -> void:
	for key in keys:
		var node: Variant = config.get(key)
		if is_instance_valid(node):
			node.hide()


func _set_button_visible(target: Variant, is_visible: bool, disabled: bool) -> void:
	if not is_instance_valid(target):
		return
	if is_visible:
		target.show()
	else:
		target.hide()
	if target is BaseButton:
		(target as BaseButton).disabled = disabled


func _call_if_valid(callback: Variant) -> void:
	if callback is Callable:
		var callable: Callable = callback
		if callable.is_valid():
			callable.call()
