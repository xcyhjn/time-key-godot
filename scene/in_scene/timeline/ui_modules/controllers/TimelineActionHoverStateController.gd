extends RefCounted

## TimelineActionHoverStateController 只负责时间轴行动方块 hover 的本地状态切换和旧信号通知。
## 它不创建行动方块，不展示 tooltip，不处理敌方意图预览，也不修改 TimelineManager 的网格数据。


func enter_hover(action: TimelineAction, timeline_manager: TimelineManager) -> TimelineAction:
	if not is_instance_valid(action):
		return null

	_emit_hover_changed(timeline_manager, action, true)
	return action


func exit_hover(current_action: TimelineAction, timeline_manager: TimelineManager) -> TimelineAction:
	if current_action == null:
		return null

	_emit_hover_changed(timeline_manager, current_action, false)
	return null


func clear_removed_action_hover(
	current_action: TimelineAction,
	removed_action: TimelineAction,
	timeline_manager: TimelineManager
) -> TimelineAction:
	if current_action != removed_action:
		return current_action

	_emit_hover_changed(timeline_manager, removed_action, false)
	return null


func _emit_hover_changed(timeline_manager: TimelineManager, action: TimelineAction, is_hovering: bool) -> void:
	if timeline_manager and timeline_manager.has_signal("action_hovered_changed"):
		timeline_manager.action_hovered_changed.emit(action, is_hovering)
