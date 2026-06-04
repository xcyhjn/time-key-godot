class_name CombatCartoonUiController
extends RefCounted


## CombatCartoonUiController 只负责局内顶部 CartoonUI 的布局和进度文字。
## 它不推进 GlobalClock，不决定战斗阶段，也不触发任何回合流程。


func setup(config: Dictionary) -> Dictionary:
	var progress_result: Dictionary = refresh_progress(config)
	var combat_cartoon_ui: Variant = config.get("combat_cartoon_ui")
	if not is_instance_valid(combat_cartoon_ui):
		return progress_result

	var map_state: Variant = config.get("map_state")
	if map_state:
		combat_cartoon_ui.set_character_index(int(map_state.chosen_char_index))

	combat_cartoon_ui.apply_combat_layout()

	var timeline_ui: Variant = config.get("timeline_ui")
	if is_instance_valid(timeline_ui):
		timeline_ui.set_top_reserved_space(float(combat_cartoon_ui.get_reserved_height()))

	return progress_result


func refresh_progress(config: Dictionary) -> Dictionary:
	var combat_cartoon_ui: Variant = config.get("combat_cartoon_ui")
	var global_clock_bridge: Variant = config.get("global_clock_bridge")
	if not is_instance_valid(combat_cartoon_ui) or not is_instance_valid(global_clock_bridge):
		return {}

	var current_era_value: int = global_clock_bridge.pull_era()
	var phase_value: int = global_clock_bridge.get_phase()
	combat_cartoon_ui.set_progress_labels(current_era_value, phase_value)

	return {
		"current_era_value": current_era_value,
		"phase_value": phase_value,
	}
