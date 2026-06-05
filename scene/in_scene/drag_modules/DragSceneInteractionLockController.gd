class_name DragSceneInteractionLockController
extends RefCounted


## DragSceneInteractionLockController 只负责拖拽期间局内场景交互的锁定和恢复。
## 它不处理卡牌状态、不更新预览，也不判断或执行放置。


func lock_for_drag(hex_map: Node, timeline_ui: Control) -> void:
	if is_instance_valid(hex_map) and hex_map.has_method("set_tiles_interactive"):
		hex_map.set_tiles_interactive(false)
		if hex_map.has_method("set_visuals_locked"):
			hex_map.set_visuals_locked(true)

	if is_instance_valid(timeline_ui):
		timeline_ui.mouse_filter = Control.MOUSE_FILTER_PASS


func restore_after_drag(hex_map: Node, timeline_ui: Control) -> void:
	if is_instance_valid(hex_map) and hex_map.has_method("set_tiles_interactive"):
		hex_map.set_tiles_interactive(true)
		if hex_map.has_method("set_visuals_locked"):
			hex_map.set_visuals_locked(false)

	if is_instance_valid(timeline_ui):
		timeline_ui.mouse_filter = Control.MOUSE_FILTER_PASS
