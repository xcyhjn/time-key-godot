class_name FirstTurnIntroRunner
extends RefCounted


## FirstTurnIntroRunner 只处理“首回合何时启动”的等待与入场 UI 动画。
## 它不推进回合规则，不生成敌人意图；真正的开局动作由调用方传入回调。


func should_wait_for_map_intro(hex_map: Node) -> bool:
	if not is_instance_valid(hex_map):
		return false
	if not hex_map.has_method("is_map_intro_reveal_active"):
		return false
	return bool(hex_map.call("is_map_intro_reveal_active"))


func connect_map_intro_finished(hex_map: Node, callback: Callable) -> void:
	if not is_instance_valid(hex_map):
		return
	if not hex_map.has_signal("map_intro_reveal_finished"):
		return

	var intro_finished: Signal = Signal(hex_map, "map_intro_reveal_finished")
	if not intro_finished.is_connected(callback):
		intro_finished.connect(callback)


func can_start_first_turn(state: Dictionary) -> bool:
	if bool(state.get("first_turn_started", false)):
		return false
	if bool(state.get("first_turn_starting", false)):
		return false
	if not is_instance_valid(state.get("timeline_manager")):
		return false

	var timeline_ui: Variant = state.get("timeline_ui")
	var force_timeline_visible: bool = bool(state.get("force_timeline_visible", false))
	if is_instance_valid(timeline_ui) and not timeline_ui.visible and not force_timeline_visible:
		return false

	return true


func wait_for_card_system_ready(owner: Node, is_ready: Callable) -> void:
	while is_instance_valid(owner) and owner.is_inside_tree() and not bool(is_ready.call()):
		await owner.get_tree().process_frame


func play_intro_if_visible(owner: Node, config: Dictionary) -> void:
	var intro_started: bool = false
	intro_started = _play_intro(config.get("combat_cartoon_ui")) or intro_started
	intro_started = _play_intro(config.get("total_enemy_health_bar")) or intro_started

	var timeline_ui: Variant = config.get("timeline_ui")
	if is_instance_valid(timeline_ui) and timeline_ui.visible:
		intro_started = _play_intro(timeline_ui) or intro_started

	if intro_started:
		await wait_for_battle_intro_ui(owner, config)


func wait_for_battle_intro_ui(owner: Node, config: Dictionary) -> void:
	while is_any_battle_intro_ui_running(config):
		if not is_instance_valid(owner) or not owner.is_inside_tree():
			return
		await owner.get_tree().process_frame


func is_any_battle_intro_ui_running(config: Dictionary) -> bool:
	if _is_intro_running(config.get("combat_cartoon_ui")):
		return true
	if _is_intro_running(config.get("total_enemy_health_bar")):
		return true

	var timeline_ui: Variant = config.get("timeline_ui")
	if is_instance_valid(timeline_ui) and timeline_ui.visible and _is_intro_running(timeline_ui):
		return true

	return false


func _play_intro(target: Variant) -> bool:
	if is_instance_valid(target) and target.has_method("play_intro"):
		target.play_intro()
		return true
	return false


func _is_intro_running(target: Variant) -> bool:
	if is_instance_valid(target) and target.has_method("is_intro_in_progress"):
		return bool(target.call("is_intro_in_progress"))
	return false
